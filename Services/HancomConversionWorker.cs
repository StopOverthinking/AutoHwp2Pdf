using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AutoHwp2Pdf.Services;

public sealed class HancomConversionWorker : IDisposable
{
    private readonly BlockingCollection<ConversionRequest> _queue = new();
    private readonly ConcurrentDictionary<string, byte> _pendingSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly LogService _log;
    private Thread? _workerThread;
    private bool _disposed;

    public HancomConversionWorker(LogService log)
    {
        _log = log;
    }

    public int QueueLength => _queue.Count;

    public void Start()
    {
        if (_workerThread is not null)
        {
            return;
        }

        _workerThread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "HancomConversionWorker",
        };
        _workerThread.SetApartmentState(ApartmentState.STA);
        _workerThread.Start();
        _log.Info("Conversion worker started.");
    }

    public bool Enqueue(ConversionRequest request)
    {
        if (_disposed || _queue.IsAddingCompleted)
        {
            return false;
        }

        var pendingKey = BuildPendingKey(request);
        if (!_pendingSources.TryAdd(pendingKey, 0))
        {
            return false;
        }

        try
        {
            _queue.Add(request);
            _log.Info($"Queued: {request.SourcePath}");
            return true;
        }
        catch
        {
            _pendingSources.TryRemove(pendingKey, out _);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _queue.CompleteAdding();

        if (_workerThread is { IsAlive: true })
        {
            _workerThread.Join(TimeSpan.FromSeconds(5));
        }

        _queue.Dispose();
    }

    private void WorkerLoop()
    {
        foreach (var request in _queue.GetConsumingEnumerable())
        {
            var pendingKey = BuildPendingKey(request);

            try
            {
                if (!File.Exists(request.SourcePath))
                {
                    _log.Warning($"Source file was not found, skipping: {request.SourcePath}");
                    continue;
                }

                var latestOutputPath = FindLatestOutputPath(request);
                if (IsUpToDate(request.SourcePath, latestOutputPath))
                {
                    _log.Info($"{request.OutputFormat} output is already up to date, skipping: {latestOutputPath}");
                    continue;
                }

                var outputPath = BuildNextOutputPath(request);
                ConvertDocument(request.SourcePath, outputPath);
                _log.Info($"Converted: {outputPath}");
            }
            catch (Exception ex)
            {
                _log.Error($"Conversion failed ({request.Attempt + 1}/{request.MaxAttempts}): {request.SourcePath} - {ex.Message}");

                if (request.Attempt + 1 < request.MaxAttempts && !_disposed)
                {
                    _pendingSources.TryRemove(pendingKey, out _);

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                        Enqueue(request with { Attempt = request.Attempt + 1 });
                    });

                    continue;
                }
            }
            finally
            {
                _pendingSources.TryRemove(pendingKey, out _);
            }
        }
    }

    private static bool IsUpToDate(string sourcePath, string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath) || !File.Exists(outputPath))
        {
            return false;
        }

        return File.GetLastWriteTimeUtc(outputPath) >= File.GetLastWriteTimeUtc(sourcePath);
    }

    private static string? FindLatestOutputPath(ConversionRequest request)
    {
        if (!Directory.Exists(request.OutputDirectory))
        {
            return null;
        }

        string? latestPath = null;
        var latestSequence = -1;

        foreach (var candidatePath in EnumerateCandidateOutputPaths(request))
        {
            var sequence = TryParseVersionSequence(candidatePath, request.OutputBaseName) ?? 0;
            if (sequence <= latestSequence)
            {
                continue;
            }

            latestSequence = sequence;
            latestPath = candidatePath;
        }

        return latestPath;
    }

    private static string BuildNextOutputPath(ConversionRequest request)
    {
        Directory.CreateDirectory(request.OutputDirectory);
        var extension = request.OutputFormat.GetExtension();

        var nextSequence = 1;
        foreach (var candidatePath in EnumerateCandidateOutputPaths(request))
        {
            var sequence = TryParseVersionSequence(candidatePath, request.OutputBaseName);
            if (sequence is int parsedSequence && parsedSequence >= nextSequence)
            {
                nextSequence = parsedSequence + 1;
            }
        }

        var dateStamp = DateTime.Now.ToString("yy.MM.dd", CultureInfo.InvariantCulture);
        var fileName = $"{request.OutputBaseName} (converted {dateStamp}._{nextSequence}).{extension}";
        return Path.Combine(request.OutputDirectory, fileName);
    }

    private static IEnumerable<string> EnumerateCandidateOutputPaths(ConversionRequest request)
    {
        if (!Directory.Exists(request.OutputDirectory))
        {
            yield break;
        }

        var extension = request.OutputFormat.GetExtension();
        var legacyPath = Path.Combine(request.OutputDirectory, $"{request.OutputBaseName}.{extension}");
        if (File.Exists(legacyPath))
        {
            yield return legacyPath;
        }

        foreach (var candidatePath in Directory.EnumerateFiles(request.OutputDirectory, $"*.{extension}", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(candidatePath, legacyPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryParseVersionSequence(candidatePath, request.OutputBaseName).HasValue)
            {
                yield return candidatePath;
            }
        }
    }

    private static int? TryParseVersionSequence(string candidatePath, string outputBaseName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(candidatePath);
        if (string.Equals(fileNameWithoutExtension, outputBaseName, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var legacyImageMatch = Regex.Match(
            fileNameWithoutExtension,
            $"^{Regex.Escape(outputBaseName)}(?<page>\\d{{3,4}})$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (legacyImageMatch.Success)
        {
            return 0;
        }

        var versionedMatch = Regex.Match(
            fileNameWithoutExtension,
            $"^{Regex.Escape(outputBaseName)} \\(converted (?<date>\\d{{2}}\\.\\d{{2}}\\.\\d{{2}})\\._(?<sequence>\\d+)\\)(?<page>\\d{{3,4}})?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!versionedMatch.Success)
        {
            return null;
        }

        var datePart = versionedMatch.Groups["date"].Value;
        if (!DateTime.TryParseExact(datePart, "yy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return null;
        }

        return int.TryParse(versionedMatch.Groups["sequence"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : null;
    }

    private void ConvertDocument(string sourcePath, string outputPath)
    {
        var hwpType = Type.GetTypeFromProgID("HWPFrame.HwpObject")
            ?? throw new InvalidOperationException("Hancom COM object was not found. Run hwp.exe -regserver and try again.");

        object? hwp = null;

        try
        {
            hwp = Activator.CreateInstance(hwpType)
                ?? throw new InvalidOperationException("Hancom COM object could not be created.");

            dynamic app = hwp;

            TryIgnore(() => app.XHwpWindows.Active_XHwpWindow.Visible = false);
            TryIgnore(() => app.RegisterModule("FilePathCheckDLL", "FilePathCheckerModule"));
            TryIgnore(() => app.SetMessageBoxMode(0x00214411));

            var opened = (bool)app.Open(sourcePath, string.Empty, "lock:false;forceopen:true;versionwarning:false;");
            if (!opened)
            {
                throw new InvalidOperationException("The source document could not be opened.");
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            if (!TrySaveAs(app, outputPath))
            {
                throw new InvalidOperationException($"Saving as {Path.GetExtension(outputPath)} failed.");
            }
        }
        finally
        {
            if (hwp is not null)
            {
                try
                {
                    ((dynamic)hwp).Clear(1);
                }
                catch
                {
                }

                try
                {
                    ((dynamic)hwp).Quit();
                }
                catch
                {
                }

                try
                {
                    Marshal.ReleaseComObject(hwp);
                }
                catch
                {
                }
            }
        }
    }

    private static void TryIgnore(Action action)
    {
        try
        {
            action();
        }
        catch
        {
        }
    }

    private static string BuildPendingKey(ConversionRequest request)
    {
        return $"{request.SourcePath}|{request.OutputFormat}";
    }

    private static bool TrySaveAs(dynamic app, string outputPath)
    {
        var format = InferOutputFormat(outputPath);

        foreach (var saveFormatCode in format.GetSaveFormatCodes())
        {
            try
            {
                if ((bool)app.SaveAs(outputPath, saveFormatCode, string.Empty))
                {
                    return true;
                }
            }
            catch
            {
            }
        }

        return false;
    }

    private static OutputFormat InferOutputFormat(string outputPath)
    {
        var extension = Path.GetExtension(outputPath);
        return extension.ToLowerInvariant() switch
        {
            ".docx" => OutputFormat.Docx,
            ".png" => OutputFormat.Png,
            _ => OutputFormat.Pdf,
        };
    }
}
