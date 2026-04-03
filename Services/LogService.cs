namespace AutoHwp2Pdf.Services;

public sealed class LogService
{
    private const int MaxEntries = 200;
    private readonly object _gate = new();
    private readonly List<LogEntry> _entries = new();
    private readonly string _logFilePath;

    public LogService(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    public void Info(string message)
    {
        Add(LogLevel.Info, message);
    }

    public void Warning(string message)
    {
        Add(LogLevel.Warning, message);
    }

    public void Error(string message)
    {
        Add(LogLevel.Error, message);
    }

    public IReadOnlyList<LogEntry> GetSnapshot()
    {
        lock (_gate)
        {
            return _entries.ToArray();
        }
    }

    private void Add(LogLevel level, string message)
    {
        lock (_gate)
        {
            var entry = new LogEntry(DateTime.Now, level, message);
            _entries.Add(entry);

            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
            }

            TryWriteToFile(entry);
        }
    }

    private void TryWriteToFile(LogEntry entry)
    {
        try
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            RotateIfNeeded();
            File.AppendAllText(_logFilePath, $"{entry}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logFilePath))
        {
            return;
        }

        var info = new FileInfo(_logFilePath);
        if (info.Length < 1024 * 1024)
        {
            return;
        }

        var backupPath = $"{_logFilePath}.1";
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        File.Move(_logFilePath, backupPath);
    }
}
