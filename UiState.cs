namespace AutoHwp2Pdf;

public sealed record UiState(
    AppSettings Settings,
    bool IsPaused,
    int QueueLength,
    string StatusText,
    string SecurityModuleStatusText,
    IReadOnlyList<LogEntry> Logs);
