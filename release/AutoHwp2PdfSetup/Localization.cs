namespace AutoHwp2PdfSetup;

internal enum InstallerLanguage
{
    Korean,
    English
}

internal readonly record struct Translation(string Korean, string English);

internal static class Localization
{
    private static readonly Dictionary<string, Translation> Strings = new(StringComparer.Ordinal)
    {
        ["WizardTitle"] = new("AutoHwp2Anything \uC124\uCE58 \uB9C8\uBC95\uC0AC", "AutoHwp2Anything Setup Wizard"),
        ["WelcomeTitle"] = new("\uC124\uCE58\uB97C \uC2DC\uC791\uD569\uB2C8\uB2E4", "Let's install AutoHwp2Anything"),
        ["WelcomeBody"] = new(
            "\uC124\uCE58 \uC5B8\uC5B4\uB97C \uC120\uD0DD\uD558\uACE0 \uACC4\uC18D \uC9C4\uD589\uD558\uC138\uC694. \uC5EC\uAE30\uC11C \uC120\uD0DD\uD55C \uC5B8\uC5B4\uB294 \uC571\uC758 \uAE30\uBCF8 UI \uC5B8\uC5B4\uB85C\uB3C4 \uC801\uC6A9\uB429\uB2C8\uB2E4.",
            "Choose the language for setup and continue. The selected language will become the app's default language after installation."),
        ["LanguageGroup"] = new("\uC124\uCE58 \uC5B8\uC5B4", "Setup language"),
        ["LanguageHint"] = new("\uC774 \uC120\uD0DD\uC740 \uC124\uCE58 \uB9C8\uBC95\uC0AC\uC640 \uC124\uCE58 \uD6C4 \uC571\uC758 \uAE30\uBCF8 \uC5B8\uC5B4\uC5D0 \uBAA8\uB450 \uBC18\uC601\uB429\uB2C8\uB2E4.", "This choice applies to both the installer and the app's default language."),
        ["InstallTitle"] = new("\uC124\uCE58 \uC704\uCE58\uB97C \uD655\uC778\uD558\uC138\uC694", "Choose the installation location"),
        ["InstallBody"] = new(
            "\uC0AC\uC6A9\uC790\uBCC4 \uD504\uB85C\uADF8\uB7A8 \uD3F4\uB354\uC5D0 \uC124\uCE58\uD558\uBA74 \uC124\uC815 \uD30C\uC77C\uC744 \uC548\uC804\uD558\uAC8C \uC720\uC9C0\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.",
            "Installing to a per-user program folder lets the app save its settings safely."),
        ["InstallPath"] = new("\uC124\uCE58 \uD3F4\uB354", "Install folder"),
        ["Browse"] = new("\uCC3E\uC544\uBCF4\uAE30...", "Browse..."),
        ["DesktopShortcut"] = new("\uBC14\uD0D5 \uD654\uBA74 \uBC14\uB85C\uAC00\uAE30 \uB9CC\uB4E4\uAE30", "Create a desktop shortcut"),
        ["StartMenuShortcut"] = new("\uC2DC\uC791 \uBA54\uB274 \uBC14\uB85C\uAC00\uAE30\uB294 \uC790\uB3D9\uC73C\uB85C \uC0DD\uC131\uB429\uB2C8\uB2E4.", "A Start menu shortcut will be created automatically."),
        ["ProgressTitle"] = new("\uC124\uCE58 \uC911\uC785\uB2C8\uB2E4", "Installing"),
        ["ProgressBody"] = new("\uD30C\uC77C\uC744 \uBCF5\uC0AC\uD558\uACE0 \uC124\uC815\uC744 \uC900\uBE44\uD558\uACE0 \uC788\uC2B5\uB2C8\uB2E4.", "Copying files and preparing settings."),
        ["FinishTitle"] = new("\uC124\uCE58\uAC00 \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4", "Installation is complete"),
        ["FinishBody"] = new("AutoHwp2Anything\uB97C \uBC14\uB85C \uC2E4\uD589\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.", "AutoHwp2Anything is ready to launch."),
        ["RunNow"] = new("\uC124\uCE58\uAC00 \uB05D\uB098\uBA74 AutoHwp2Anything \uC2E4\uD589", "Launch AutoHwp2Anything when setup closes"),
        ["Back"] = new("\uC774\uC804", "Back"),
        ["Next"] = new("\uB2E4\uC74C", "Next"),
        ["Install"] = new("\uC124\uCE58", "Install"),
        ["Installing"] = new("\uC124\uCE58 \uC911...", "Installing..."),
        ["Finish"] = new("\uB9C8\uCE68", "Finish"),
        ["Cancel"] = new("\uCDE8\uC18C", "Cancel"),
        ["Close"] = new("\uB2EB\uAE30", "Close"),
        ["SelectFolder"] = new("\uC124\uCE58 \uD3F4\uB354 \uC120\uD0DD", "Select installation folder"),
        ["PayloadMissing"] = new("\uC124\uCE58\uC5D0 \uD544\uC694\uD55C payload \uD3F4\uB354\uB97C \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", "The payload folder required for installation could not be found."),
        ["InstallFailed"] = new("\uC124\uCE58 \uC911 \uC624\uB958\uAC00 \uBC1C\uC0DD\uD588\uC2B5\uB2C8\uB2E4.", "Setup encountered an error."),
        ["InstallCompletedStatus"] = new("\uC124\uCE58\uAC00 \uC131\uACF5\uC801\uC73C\uB85C \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4.", "Installation completed successfully."),
        ["PreparingFiles"] = new("\uBC30\uD3EC \uD30C\uC77C\uC744 \uD655\uC778\uD558\uB294 \uC911...", "Checking payload files..."),
        ["CopyingFiles"] = new("\uC571 \uD30C\uC77C\uC744 \uBCF5\uC0AC\uD558\uB294 \uC911...", "Copying application files..."),
        ["WritingSettings"] = new("\uC124\uC815 \uD30C\uC77C\uC744 \uC791\uC131\uD558\uB294 \uC911...", "Writing the settings file..."),
        ["CreatingShortcuts"] = new("\uBC14\uB85C\uAC00\uAE30\uB97C \uB9CC\uB4DC\uB294 \uC911...", "Creating shortcuts..."),
        ["RegisteringUninstall"] = new("\uC81C\uAC70 \uC815\uBCF4\uB97C \uB4F1\uB85D\uD558\uB294 \uC911...", "Registering uninstall information..."),
        ["Finalizing"] = new("\uC124\uCE58\uB97C \uB9C8\uBB34\uB9AC\uD558\uB294 \uC911...", "Finalizing setup..."),
        ["InvalidInstallPath"] = new("\uC62C\uBC14\uB978 \uC124\uCE58 \uD3F4\uB354\uB97C \uC120\uD0DD\uD558\uC138\uC694.", "Please choose a valid installation folder."),
        ["AppRunning"] = new("AutoHwp2Anything\uAC00 \uC2E4\uD589 \uC911\uC785\uB2C8\uB2E4. \uACC4\uC18D\uD558\uB824\uBA74 \uBA3C\uC800 \uD504\uB85C\uADF8\uB7A8\uC744 \uC885\uB8CC\uD574 \uC8FC\uC138\uC694.", "AutoHwp2Anything is currently running. Please close it before continuing."),
        ["UninstallConfirm"] = new("AutoHwp2Anything\uB97C \uC81C\uAC70\uD560\uAE4C\uC694?", "Do you want to uninstall AutoHwp2Anything?"),
        ["UninstallTitle"] = new("AutoHwp2Anything \uC81C\uAC70", "Uninstall AutoHwp2Anything"),
        ["UninstallComplete"] = new("\uC81C\uAC70\uAC00 \uC2DC\uC791\uB418\uC5C8\uC2B5\uB2C8\uB2E4. \uC7A0\uC2DC \uD6C4 \uD504\uB85C\uADF8\uB7A8 \uD3F4\uB354\uAC00 \uC815\uB9AC\uB429\uB2C8\uB2E4.", "Uninstallation has started. The program folder will be cleaned up shortly."),
        ["UninstallNotFound"] = new("\uC124\uCE58 \uD3F4\uB354\uB97C \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", "The installation folder could not be found."),
        ["UninstallFailed"] = new("\uC81C\uAC70 \uC911 \uC624\uB958\uAC00 \uBC1C\uC0DD\uD588\uC2B5\uB2C8\uB2E4.", "Uninstallation failed."),
        ["AlreadyInstalledPrompt"] = new("\uAC19\uC740 \uC704\uCE58\uC5D0 \uC124\uCE58\uB418\uC5B4 \uC788\uB294 \uD30C\uC77C\uC740 \uB36E\uC5B4\uC4F0\uC5EC\uC9D1\uB2C8\uB2E4.", "Existing files in the same location will be overwritten."),
        ["UninstallShortcutName"] = new("AutoHwp2Anything \uC81C\uAC70", "Uninstall AutoHwp2Anything")
    };

    public static InstallerLanguage DetectPreferredLanguage()
    {
        return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ko", StringComparison.OrdinalIgnoreCase)
            ? InstallerLanguage.Korean
            : InstallerLanguage.English;
    }

    public static string Get(InstallerLanguage language, string key)
    {
        if (!Strings.TryGetValue(key, out var translation))
        {
            return key;
        }

        return language == InstallerLanguage.Korean ? translation.Korean : translation.English;
    }
}
