namespace AutoHwp2Pdf;

public static class UiText
{
    public static string AppTitle(UiLanguage language) =>
        language == UiLanguage.Korean ? "AutoHwp2Anything" : "AutoHwp2Anything";

    public static string SettingsGroup(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC124\uC815" : "Settings";

    public static string LogsGroup(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uB85C\uADF8" : "Logs";

    public static string WatchFolderLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uAC10\uC2DC \uD3F4\uB354" : "Watch folder";

    public static string OutputFormatLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uCD9C\uB825 \uD615\uC2DD" : "Output format";

    public static string OutputModeLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uCD9C\uB825 \uC704\uCE58 \uBAA8\uB4DC" : "Output location mode";

    public static string OutputRootLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uCD9C\uB825 \uB8E8\uD2B8" : "Output root";

    public static string OutputSubfolderLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uD558\uC704 \uD3F4\uB354 \uC774\uB984" : "Child folder name";

    public static string SecurityModuleDllLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uBCF4\uC548 \uBAA8\uB4C8 DLL" : "Security module DLL";

    public static string StableDelayLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC548\uC815\uD654 \uB300\uAE30(ms)" : "Stable delay (ms)";

    public static string RetryCountLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC7AC\uC2DC\uB3C4 \uD69F\uC218" : "Retries";

    public static string SecurityStatusLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uBCF4\uC548 \uC0C1\uD0DC" : "Security status";

    public static string OptionsLabel(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC635\uC158" : "Options";

    public static string IncludeSubdirectories(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uD558\uC704 \uD3F4\uB354 \uD3EC\uD568" : "Include subfolders";

    public static string RunAtStartup(UiLanguage language) =>
        language == UiLanguage.Korean ? "Windows \uC2DC\uC791 \uC2DC \uC2E4\uD589" : "Run at Windows startup";

    public static string StartPaused(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC2DC\uC791 \uC2DC \uAC10\uC2DC \uC77C\uC2DC \uC911\uC9C0" : "Start with watching paused";

    public static string Browse(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uCC3E\uC544\uBCF4\uAE30" : "Browse";

    public static string Open(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC5F4\uAE30" : "Open";

    public static string Register(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uB4F1\uB85D" : "Register";

    public static string SaveSettings(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC124\uC815 \uC800\uC7A5" : "Save Settings";

    public static string ScanNow(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC9C0\uAE08 \uAC80\uC0AC" : "Scan Now";

    public static string PauseWatching(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uAC10\uC2DC \uC77C\uC2DC \uC911\uC9C0" : "Pause Watching";

    public static string ResumeWatching(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uAC10\uC2DC \uC7AC\uAC1C" : "Resume Watching";

    public static string OpenWatchFolder(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uAC10\uC2DC \uD3F4\uB354 \uC5F4\uAE30" : "Open Watch Folder";

    public static string OpenOutputFolder(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uCD9C\uB825 \uD3F4\uB354 \uC5F4\uAE30" : "Open Output Folder";

    public static string OpenLogsFolder(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uB85C\uADF8 \uD3F4\uB354 \uC5F4\uAE30" : "Open Logs Folder";

    public static string TrayOpenSettings(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC124\uC815 \uC5F4\uAE30" : "Open Settings";

    public static string Exit(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC885\uB8CC" : "Exit";

    public static string Initializing(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uCD08\uAE30\uD654 \uC911" : "Initializing";

    public static string OutputFormatName(OutputFormat format, UiLanguage language)
    {
        return (format, language) switch
        {
            (OutputFormat.Pdf, UiLanguage.Korean) => "PDF \uBB38\uC11C",
            (OutputFormat.Docx, UiLanguage.Korean) => "DOCX \uBB38\uC11C",
            (OutputFormat.Png, UiLanguage.Korean) => "PNG \uC774\uBBF8\uC9C0",
            (OutputFormat.Pdf, _) => "PDF document",
            (OutputFormat.Docx, _) => "DOCX document",
            _ => "PNG image",
        };
    }

    public static string OutputModeName(OutputMode mode, UiLanguage language)
    {
        return (mode, language) switch
        {
            (OutputMode.SameDirectory, UiLanguage.Korean) => "\uC6D0\uBCF8\uACFC \uAC19\uC740 \uD3F4\uB354",
            (OutputMode.ChildSubfolder, UiLanguage.Korean) => "\uC6D0\uBCF8 \uC544\uB798 \uD558\uC704 \uD3F4\uB354",
            (OutputMode.CustomRoot, UiLanguage.Korean) => "\uC0AC\uC6A9\uC790 \uC9C0\uC815 \uB8E8\uD2B8 \uD3F4\uB354",
            (OutputMode.SameDirectory, _) => "Same folder as source",
            (OutputMode.ChildSubfolder, _) => "Child folder under source",
            _ => "Custom root folder",
        };
    }

    public static string LanguageName(UiLanguage option, UiLanguage displayLanguage)
    {
        return (option, displayLanguage) switch
        {
            (UiLanguage.Korean, UiLanguage.Korean) => "\uD55C\uAD6D\uC5B4",
            (UiLanguage.Korean, _) => "Korean",
            _ => "English",
        };
    }

    public static string ChangeLanguage(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC5B8\uC5B4 \uBCC0\uACBD" : "Change language";

    public static string SaveSettingsError(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC124\uC815 \uC800\uC7A5 \uC911 \uC624\uB958\uAC00 \uBC1C\uC0DD\uD588\uC2B5\uB2C8\uB2E4." : "An error occurred while saving settings.";

    public static string SelectFolder(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uD3F4\uB354\uB97C \uC120\uD0DD\uD558\uC138\uC694." : "Select a folder.";

    public static string DllFileFilter(UiLanguage language) =>
        language == UiLanguage.Korean ? "DLL \uD30C\uC77C (*.dll)|*.dll|\uBAA8\uB4E0 \uD30C\uC77C (*.*)|*.*" : "DLL files (*.dll)|*.dll|All files (*.*)|*.*";

    public static string SecurityModuleRegistered(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uBCF4\uC548 \uBAA8\uB4C8\uC744 \uB4F1\uB85D\uD588\uC2B5\uB2C8\uB2E4." : "Security module registered.";

    public static string SecurityModuleRegistrationFailed(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uBCF4\uC548 \uBAA8\uB4C8 \uB4F1\uB85D\uC5D0 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4." : "Security module registration failed.";

    public static string SecurityModuleStatus(UiLanguage language, string? registeredPath)
    {
        if (string.IsNullOrWhiteSpace(registeredPath))
        {
            return language == UiLanguage.Korean ? "\uBCF4\uC548 \uBAA8\uB4C8: \uBBF8\uB4F1\uB85D" : "Security module: not registered";
        }

        return language == UiLanguage.Korean ? $"\uBCF4\uC548 \uBAA8\uB4C8: {registeredPath}" : $"Security module: {registeredPath}";
    }

    public static string WatchStatus(UiLanguage language, bool paused, int queueLength, string watchFolder)
    {
        return language == UiLanguage.Korean
            ? $"{(paused ? "\uC77C\uC2DC \uC911\uC9C0" : "\uAC10\uC2DC \uC911")} | \uB300\uAE30\uC5F4 {queueLength} | {watchFolder}"
            : $"{(paused ? "Paused" : "Watching")} | Queue {queueLength} | {watchFolder}";
    }

    public static string WatchFolderNotConfigured(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uAC10\uC2DC \uD3F4\uB354\uAC00 \uC124\uC815\uB418\uC9C0 \uC54A\uC558\uC2B5\uB2C8\uB2E4." : "Watch folder is not configured.";

    public static string StartedMinimized(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC2DC\uC2A4\uD15C \uD2B8\uB808\uC774\uC5D0\uC11C \uCD5C\uC18C\uD654\uB41C \uC0C1\uD0DC\uB85C \uC2DC\uC791\uD588\uC2B5\uB2C8\uB2E4." : "Started minimized in the system tray.";

    public static string HiddenToTray(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uCC3D\uC740 \uD2B8\uB808\uC774\uB85C \uC228\uACA8\uC9C0\uACE0 \uD504\uB85C\uADF8\uB7A8\uC740 \uACC4\uC18D \uC2E4\uD589\uB429\uB2C8\uB2E4." : "The window is hidden to tray and keeps running.";

    public static string WatchFolderRequired(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uAC10\uC2DC \uD3F4\uB354\uB294 \uD544\uC218\uC785\uB2C8\uB2E4." : "Watch folder is required.";

    public static string OutputRootRequired(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC0AC\uC6A9\uC790 \uC9C0\uC815 \uB8E8\uD2B8 \uBAA8\uB4DC\uC5D0\uC11C\uB294 \uCD9C\uB825 \uB8E8\uD2B8\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4." : "Output root is required when custom root mode is selected.";

    public static string ConfiguredSecurityModuleNotFound(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC124\uC815\uB41C \uBCF4\uC548 \uBAA8\uB4C8 DLL\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4." : "Configured security module DLL was not found.";

    public static string SecurityModulePathRequired(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uBCF4\uC548 \uBAA8\uB4C8 DLL \uACBD\uB85C\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4." : "Security module DLL path is required.";

    public static string SecurityModuleDllNotFound(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uBCF4\uC548 \uBAA8\uB4C8 DLL\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4." : "Security module DLL was not found.";

    public static string NoFolderPathConfigured(UiLanguage language) =>
        language == UiLanguage.Korean ? "\uC124\uC815\uB41C \uD3F4\uB354 \uACBD\uB85C\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4." : "No folder path is configured.";
}
