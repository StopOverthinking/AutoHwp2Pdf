using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace AutoHwp2PdfSetup;

internal sealed record InstallRequest(
    string SetupBaseDirectory,
    string InstallDirectory,
    InstallerLanguage Language,
    bool CreateDesktopShortcut);

internal sealed record InstallResult(
    string InstallDirectory,
    string LauncherScriptPath);

internal static class InstallerOperations
{
    private const string AppProcessName = "AutoHwp2Anything";
    private const string StartMenuFolderName = "AutoHwp2Anything";
    private const string MaintenanceExeName = "AutoHwp2Anything Maintenance.exe";
    private const string LauncherScriptFileName = "Launch-AutoHwp2AnythingHidden.vbs";
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoHwp2Anything";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string[] PayloadFiles =
    {
        "AutoHwp2Anything.deps.json",
        "AutoHwp2Anything.dll",
        "AutoHwp2Anything.exe",
        "AutoHwp2Anything.pdb",
        "AutoHwp2Anything.runtimeconfig.json",
        "FilePathCheckerModuleExample.dll",
        "Launch-AutoHwp2Anything.cmd",
        "Launch-AutoHwp2Anything.ps1"
    };

    public static string DefaultInstallDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "AutoHwp2Anything");

    public static bool IsAppRunning()
    {
        return Process.GetProcessesByName(AppProcessName).Length > 0;
    }

    public static async Task<InstallResult> InstallAsync(InstallRequest request, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var payloadDirectory = Path.Combine(request.SetupBaseDirectory, "payload");
        if (!Directory.Exists(payloadDirectory))
        {
            throw new DirectoryNotFoundException(payloadDirectory);
        }

        progress.Report(Localization.Get(request.Language, "PreparingFiles"));
        foreach (var fileName in PayloadFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payloadPath = Path.Combine(payloadDirectory, fileName);
            if (!File.Exists(payloadPath))
            {
                throw new FileNotFoundException($"Missing payload file: {fileName}", payloadPath);
            }
        }

        Directory.CreateDirectory(request.InstallDirectory);

        progress.Report(Localization.Get(request.Language, "CopyingFiles"));
        foreach (var fileName in PayloadFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourcePath = Path.Combine(payloadDirectory, fileName);
            var destinationPath = Path.Combine(request.InstallDirectory, fileName);
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }

        var maintenanceExePath = Path.Combine(request.InstallDirectory, MaintenanceExeName);
        var currentProcessPath = Environment.ProcessPath ?? throw new InvalidOperationException("Unable to resolve the setup executable path.");
        File.Copy(currentProcessPath, maintenanceExePath, overwrite: true);

        var launcherScriptPath = Path.Combine(request.InstallDirectory, LauncherScriptFileName);
        File.WriteAllText(launcherScriptPath, BuildLauncherScript(request.InstallDirectory), new UTF8Encoding(false));

        progress.Report(Localization.Get(request.Language, "WritingSettings"));
        await WriteSettingsAsync(request, cancellationToken);

        progress.Report(Localization.Get(request.Language, "CreatingShortcuts"));
        CreateShortcuts(request, launcherScriptPath, maintenanceExePath);

        progress.Report(Localization.Get(request.Language, "RegisteringUninstall"));
        RegisterUninstallEntry(request, maintenanceExePath, payloadDirectory);

        progress.Report(Localization.Get(request.Language, "Finalizing"));
        return new InstallResult(request.InstallDirectory, launcherScriptPath);
    }

    public static InstallerLanguage DetectInstalledLanguage(string installDirectory)
    {
        try
        {
            var settingsPath = Path.Combine(installDirectory, "settings.json");
            if (!File.Exists(settingsPath))
            {
                return Localization.DetectPreferredLanguage();
            }

            var root = JsonNode.Parse(File.ReadAllText(settingsPath)) as JsonObject;
            var uiLanguage = root?["UiLanguage"]?.GetValue<string>();
            return string.Equals(uiLanguage, "English", StringComparison.OrdinalIgnoreCase)
                ? InstallerLanguage.English
                : InstallerLanguage.Korean;
        }
        catch
        {
            return Localization.DetectPreferredLanguage();
        }
    }

    public static void Uninstall(string installDirectory)
    {
        RemoveShortcuts();
        RemoveUninstallEntry();
        ScheduleInstallDirectoryDeletion(installDirectory);
    }

    private static async Task WriteSettingsAsync(InstallRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settingsPath = Path.Combine(request.InstallDirectory, "settings.json");
        JsonObject root;

        if (File.Exists(settingsPath))
        {
            try
            {
                root = JsonNode.Parse(await File.ReadAllTextAsync(settingsPath, cancellationToken)) as JsonObject ?? new JsonObject();
            }
            catch
            {
                root = new JsonObject();
            }
        }
        else
        {
            root = new JsonObject();
        }

        var documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        root["UiLanguage"] = request.Language == InstallerLanguage.Korean ? "Korean" : "English";
        root["WatchFolder"] ??= documentsDirectory;
        root["OutputRoot"] ??= Path.Combine(documentsDirectory, "hwp2anything");
        root["OutputSubfolderName"] ??= "pdf";
        root["OutputFormat"] ??= "Pdf";
        root["OutputMode"] ??= "CustomRoot";
        root["IncludeSubdirectories"] ??= true;
        root["RunAtStartup"] ??= true;
        root["StartPaused"] ??= false;
        root["SecurityModuleDllPath"] = Path.Combine(request.InstallDirectory, "FilePathCheckerModuleExample.dll");
        root["StableCheckDelayMs"] ??= 1500;
        root["MaxRetryCount"] ??= 3;

        var json = root.ToJsonString(JsonOptions);
        await File.WriteAllTextAsync(settingsPath, json, new UTF8Encoding(false), cancellationToken);
    }

    private static string BuildLauncherScript(string installDirectory)
    {
        var launcherPath = Path.Combine(installDirectory, "Launch-AutoHwp2Anything.cmd");
        return
            "Set shell = CreateObject(\"WScript.Shell\")" + Environment.NewLine +
            $"shell.CurrentDirectory = \"{EscapeForVbScript(installDirectory)}\"" + Environment.NewLine +
            $"shell.Run \"\"\"{EscapeForVbScript(launcherPath)}\"\"\", 0, False" + Environment.NewLine;
    }

    private static string EscapeForVbScript(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    private static void CreateShortcuts(InstallRequest request, string launcherScriptPath, string maintenanceExePath)
    {
        var startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), StartMenuFolderName);
        Directory.CreateDirectory(startMenuFolder);

        var desktopShortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "AutoHwp2Anything.lnk");
        var appShortcutPath = Path.Combine(startMenuFolder, "AutoHwp2Anything.lnk");
        var uninstallShortcutPath = Path.Combine(startMenuFolder, $"{Localization.Get(request.Language, "UninstallShortcutName")}.lnk");
        var uninstallShortcutCandidates = new[]
        {
            Path.Combine(startMenuFolder, "AutoHwp2Anything \uC81C\uAC70.lnk"),
            Path.Combine(startMenuFolder, "Uninstall AutoHwp2Anything.lnk")
        };

        foreach (var candidatePath in uninstallShortcutCandidates)
        {
            if (File.Exists(candidatePath))
            {
                File.Delete(candidatePath);
            }
        }

        CreateShortcut(
            appShortcutPath,
            Path.Combine(Environment.SystemDirectory, "wscript.exe"),
            $"\"{launcherScriptPath}\"",
            request.InstallDirectory,
            Path.Combine(request.InstallDirectory, "AutoHwp2Anything.exe"));

        CreateShortcut(
            uninstallShortcutPath,
            maintenanceExePath,
            $"--uninstall --install-dir \"{request.InstallDirectory}\"",
            request.InstallDirectory,
            maintenanceExePath);

        if (request.CreateDesktopShortcut)
        {
            CreateShortcut(
                desktopShortcutPath,
                Path.Combine(Environment.SystemDirectory, "wscript.exe"),
                $"\"{launcherScriptPath}\"",
                request.InstallDirectory,
                Path.Combine(request.InstallDirectory, "AutoHwp2Anything.exe"));
        }
        else if (File.Exists(desktopShortcutPath))
        {
            File.Delete(desktopShortcutPath);
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string arguments, string workingDirectory, string iconPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell is unavailable.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.Arguments = arguments;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.IconLocation = iconPath;
        shortcut.Save();
    }

    private static void RegisterUninstallEntry(InstallRequest request, string maintenanceExePath, string payloadDirectory)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key?.SetValue("DisplayName", "AutoHwp2Anything");
        key?.SetValue("DisplayVersion", "1.0.0");
        key?.SetValue("Publisher", "AutoHwp2Anything");
        key?.SetValue("InstallLocation", request.InstallDirectory);
        key?.SetValue("DisplayIcon", Path.Combine(request.InstallDirectory, "AutoHwp2Anything.exe"));
        key?.SetValue("UninstallString", $"\"{maintenanceExePath}\" --uninstall --install-dir \"{request.InstallDirectory}\"");
        key?.SetValue("QuietUninstallString", $"\"{maintenanceExePath}\" --uninstall --install-dir \"{request.InstallDirectory}\"");
        key?.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key?.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key?.SetValue("EstimatedSize", CalculateEstimatedSizeKb(payloadDirectory), RegistryValueKind.DWord);
    }

    private static int CalculateEstimatedSizeKb(string payloadDirectory)
    {
        long totalBytes = 0;
        foreach (var fileName in PayloadFiles)
        {
            totalBytes += new FileInfo(Path.Combine(payloadDirectory, fileName)).Length;
        }

        return (int)Math.Max(1, totalBytes / 1024);
    }

    private static void RemoveShortcuts()
    {
        var desktopShortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "AutoHwp2Anything.lnk");
        if (File.Exists(desktopShortcutPath))
        {
            File.Delete(desktopShortcutPath);
        }

        var startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), StartMenuFolderName);
        if (Directory.Exists(startMenuFolder))
        {
            Directory.Delete(startMenuFolder, recursive: true);
        }
    }

    private static void RemoveUninstallEntry()
    {
        Registry.CurrentUser.DeleteSubKeyTree(RegistryKeyPath, throwOnMissingSubKey: false);
    }

    private static void ScheduleInstallDirectoryDeletion(string installDirectory)
    {
        var cleanupScriptPath = Path.Combine(Path.GetTempPath(), $"AutoHwp2Anything-Uninstall-{Guid.NewGuid():N}.cmd");
        var script = $"""
@echo off
:retry
rmdir /s /q "{installDirectory}"
if exist "{installDirectory}" (
    ping 127.0.0.1 -n 2 > nul
    goto retry
)
del /f /q "%~f0"
""";
        File.WriteAllText(cleanupScriptPath, script, Encoding.ASCII);

        var startInfo = new ProcessStartInfo("cmd.exe", $"/c \"{cleanupScriptPath}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(startInfo);
    }
}
