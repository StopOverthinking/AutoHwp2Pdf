namespace AutoHwp2Pdf;

public sealed class AppSettings
{
    public UiLanguage UiLanguage { get; set; } = UiLanguage.English;

    public string WatchFolder { get; set; } = string.Empty;

    public string OutputRoot { get; set; } = string.Empty;

    public string OutputSubfolderName { get; set; } = OutputFormat.Pdf.GetDefaultSubfolderName();

    public OutputFormat OutputFormat { get; set; } = OutputFormat.Pdf;

    public OutputMode OutputMode { get; set; } = OutputMode.SameDirectory;

    public bool IncludeSubdirectories { get; set; } = true;

    public bool RunAtStartup { get; set; }

    public bool StartPaused { get; set; }

    public string SecurityModuleDllPath { get; set; } = string.Empty;

    public int StableCheckDelayMs { get; set; } = 1500;

    public int MaxRetryCount { get; set; } = 3;

    public AppSettings Normalize()
    {
        var subfolderName = string.IsNullOrWhiteSpace(OutputSubfolderName)
            ? OutputFormat.GetDefaultSubfolderName()
            : OutputSubfolderName.Trim();

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            subfolderName = subfolderName.Replace(invalidChar, '_');
        }

        if (string.IsNullOrWhiteSpace(subfolderName))
        {
            subfolderName = OutputFormat.GetDefaultSubfolderName();
        }

        return new AppSettings
        {
            UiLanguage = UiLanguage,
            WatchFolder = NormalizePath(WatchFolder),
            OutputRoot = NormalizePath(OutputRoot),
            OutputSubfolderName = subfolderName,
            OutputFormat = OutputFormat,
            OutputMode = OutputMode,
            IncludeSubdirectories = IncludeSubdirectories,
            RunAtStartup = RunAtStartup,
            StartPaused = StartPaused,
            SecurityModuleDllPath = NormalizePath(SecurityModuleDllPath),
            StableCheckDelayMs = Math.Clamp(StableCheckDelayMs, 500, 10000),
            MaxRetryCount = Math.Clamp(MaxRetryCount, 1, 10),
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(WatchFolder))
        {
            throw new InvalidOperationException(UiText.WatchFolderRequired(UiLanguage));
        }

        if (OutputMode == OutputMode.CustomRoot && string.IsNullOrWhiteSpace(OutputRoot))
        {
            throw new InvalidOperationException(UiText.OutputRootRequired(UiLanguage));
        }

        if (!string.IsNullOrWhiteSpace(SecurityModuleDllPath) && !File.Exists(SecurityModuleDllPath))
        {
            throw new InvalidOperationException(UiText.ConfiguredSecurityModuleNotFound(UiLanguage));
        }
    }

    private static string NormalizePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(value.Trim()));
    }
}
