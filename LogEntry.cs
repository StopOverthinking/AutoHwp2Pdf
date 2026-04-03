namespace AutoHwp2Pdf;

public enum LogLevel
{
    Info,
    Warning,
    Error,
}

public sealed record LogEntry(DateTime Timestamp, LogLevel Level, string Message)
{
    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss}] {GetPrefix(Level)} {Message}";
    }

    private static string GetPrefix(LogLevel level)
    {
        return level switch
        {
            LogLevel.Info => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            _ => "INFO ",
        };
    }
}
