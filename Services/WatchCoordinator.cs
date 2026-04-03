using System.Collections.Concurrent;
using Timer = System.Threading.Timer;

namespace AutoHwp2Pdf.Services;

public sealed class WatchCoordinator : IDisposable
{
    private readonly ConcurrentDictionary<string, Timer> _debounceTimers = new(StringComparer.OrdinalIgnoreCase);
    private readonly LogService _log;
    private readonly HancomConversionWorker _worker;
    private readonly object _sync = new();
    private FileSystemWatcher? _watcher;
    private AppSettings _settings = new AppSettings().Normalize();
    private bool _disposed;
    private bool _paused;

    public WatchCoordinator(LogService log, HancomConversionWorker worker)
    {
        _log = log;
        _worker = worker;
    }

    public bool IsPaused => _paused;

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings.Normalize();

        lock (_sync)
        {
            DisposeWatcher();

            if (string.IsNullOrWhiteSpace(_settings.WatchFolder))
            {
                _log.Warning("Watch folder is empty, watcher was not started.");
                return;
            }

            Directory.CreateDirectory(_settings.WatchFolder);

            if (_settings.OutputMode == OutputMode.CustomRoot && !string.IsNullOrWhiteSpace(_settings.OutputRoot))
            {
                Directory.CreateDirectory(_settings.OutputRoot);
            }

            _watcher = new FileSystemWatcher(_settings.WatchFolder)
            {
                Filter = "*.*",
                IncludeSubdirectories = _settings.IncludeSubdirectories,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
            };

            _watcher.Created += OnFileEvent;
            _watcher.Changed += OnFileEvent;
            _watcher.Renamed += OnRenamed;
            _watcher.Error += OnWatcherError;
            _watcher.EnableRaisingEvents = !_paused;
        }

        _log.Info($"Watch folder applied: {_settings.WatchFolder}");
        ScanExisting();
    }

    public void Pause()
    {
        _paused = true;

        lock (_sync)
        {
            if (_watcher is not null)
            {
                _watcher.EnableRaisingEvents = false;
            }
        }

        _log.Info("Watching paused.");
    }

    public void Resume()
    {
        _paused = false;

        lock (_sync)
        {
            if (_watcher is not null)
            {
                _watcher.EnableRaisingEvents = true;
            }
        }

        _log.Info("Watching resumed.");
    }

    public void ScanExisting()
    {
        if (_disposed || _paused || string.IsNullOrWhiteSpace(_settings.WatchFolder) || !Directory.Exists(_settings.WatchFolder))
        {
            return;
        }

        var watchFolder = _settings.WatchFolder;
        var option = _settings.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        _ = Task.Run(() =>
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(watchFolder, "*.*", option))
                {
                    Schedule(path);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Initial scan failed: {ex.Message}");
            }
        });
    }

    public string GetStatusText(int queueLength, UiLanguage language)
    {
        if (string.IsNullOrWhiteSpace(_settings.WatchFolder))
        {
            return UiText.WatchFolderNotConfigured(language);
        }

        return UiText.WatchStatus(language, _paused, queueLength, _settings.WatchFolder);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeWatcher();

        foreach (var timer in _debounceTimers.Values)
        {
            timer.Dispose();
        }

        _debounceTimers.Clear();
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        Schedule(e.FullPath);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        Schedule(e.FullPath);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _log.Error($"Watcher error: {exception.Message}");
    }

    private void Schedule(string path)
    {
        if (_disposed || _paused || !ShouldWatch(path))
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        var delay = _settings.StableCheckDelayMs;

        _debounceTimers.AddOrUpdate(
            fullPath,
            key => new Timer(ProcessCandidate, key, delay, Timeout.Infinite),
            (_, existingTimer) =>
            {
                existingTimer.Change(delay, Timeout.Infinite);
                return existingTimer;
            });
    }

    private void ProcessCandidate(object? state)
    {
        if (state is not string path)
        {
            return;
        }

        if (_debounceTimers.TryRemove(path, out var timer))
        {
            timer.Dispose();
        }

        if (_disposed || _paused || !File.Exists(path) || !ShouldWatch(path))
        {
            return;
        }

        if (!IsFileStable(path))
        {
            Schedule(path);
            return;
        }

        var outputDirectory = BuildOutputDirectory(path);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return;
        }

        _worker.Enqueue(new ConversionRequest(
            path,
            outputDirectory,
            Path.GetFileNameWithoutExtension(path),
            _settings.OutputFormat,
            0,
            _settings.MaxRetryCount));
    }

    private string BuildOutputDirectory(string sourcePath)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath) ?? _settings.WatchFolder;

        return _settings.OutputMode switch
        {
            OutputMode.SameDirectory => sourceDirectory,
            OutputMode.ChildSubfolder => Path.Combine(sourceDirectory, _settings.OutputSubfolderName),
            OutputMode.CustomRoot => BuildCustomRootPath(sourceDirectory),
            _ => sourceDirectory,
        };
    }

    private string BuildCustomRootPath(string sourceDirectory)
    {
        if (string.IsNullOrWhiteSpace(_settings.OutputRoot))
        {
            return sourceDirectory;
        }

        var relativeDirectory = ".";

        try
        {
            relativeDirectory = Path.GetRelativePath(_settings.WatchFolder, sourceDirectory);
        }
        catch
        {
        }

        if (relativeDirectory.StartsWith("..", StringComparison.OrdinalIgnoreCase))
        {
            relativeDirectory = ".";
        }

        return relativeDirectory == "."
            ? _settings.OutputRoot
            : Path.Combine(_settings.OutputRoot, relativeDirectory);
    }

    private static bool ShouldWatch(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".hwp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".hwpx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFileStable(string path)
    {
        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var first = new FileInfo(path);
                var firstLength = first.Length;
                var firstWrite = first.LastWriteTimeUtc;

                using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                }

                Thread.Sleep(700);

                var second = new FileInfo(path);
                second.Refresh();

                if (firstLength == second.Length && firstWrite == second.LastWriteTimeUtc)
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private void DisposeWatcher()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileEvent;
        _watcher.Changed -= OnFileEvent;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= OnWatcherError;
        _watcher.Dispose();
        _watcher = null;
    }
}
