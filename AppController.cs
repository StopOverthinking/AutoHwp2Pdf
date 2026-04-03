using AutoHwp2Pdf.Services;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AutoHwp2Pdf;

public sealed class AppController : ApplicationContext, IDisposable
{
    private static readonly string[] BundledSecurityModuleDirectories =
    [
        AppContext.BaseDirectory,
        Path.Combine(AppContext.BaseDirectory, "SecurityModules"),
    ];

    private static readonly string[] BundledSecurityModuleFileNames =
    [
        "FilePathCheckerModuleExample.dll",
        "FilePathCheckerModule.dll",
    ];

    private readonly bool _startHidden;
    private readonly SettingsService _settingsService = new();
    private readonly AutoStartService _autoStartService = new();
    private readonly SecurityModuleRegistryService _securityModuleRegistryService = new();
    private readonly LogService _logService;
    private readonly HancomConversionWorker _worker;
    private readonly WatchCoordinator _watchCoordinator;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _openSettingsMenuItem;
    private readonly ToolStripMenuItem _openLogsFolderMenuItem;
    private readonly ToolStripMenuItem _pauseResumeMenuItem;
    private readonly ToolStripMenuItem _scanNowMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;
    private bool _disposed;
    private bool _isExiting;

    public AppController(bool startHidden)
    {
        _startHidden = startHidden;
        _logService = new LogService(Path.Combine(_settingsService.SettingsDirectoryPath, "activity.log"));
        CurrentSettings = _settingsService.Load();
        CurrentSettings = PopulateSecurityModulePathFromRegistry(CurrentSettings);
        CurrentSettings = PopulateSecurityModulePathFromBundledFiles(CurrentSettings);
        _settingsService.Save(CurrentSettings);
        _worker = new HancomConversionWorker(_logService);
        _watchCoordinator = new WatchCoordinator(_logService, _worker);

        _statusMenuItem = new ToolStripMenuItem(UiText.Initializing(CurrentSettings.UiLanguage))
        {
            Enabled = false,
        };

        _openSettingsMenuItem = new ToolStripMenuItem();
        _openSettingsMenuItem.Click += (_, _) => ShowMainWindow();

        _openLogsFolderMenuItem = new ToolStripMenuItem();
        _openLogsFolderMenuItem.Click += (_, _) => OpenSettingsStorage();

        _pauseResumeMenuItem = new ToolStripMenuItem();
        _pauseResumeMenuItem.Click += (_, _) => TogglePause();

        _scanNowMenuItem = new ToolStripMenuItem();
        _scanNowMenuItem.Click += (_, _) => ScanNow();

        _exitMenuItem = new ToolStripMenuItem();
        _exitMenuItem.Click += (_, _) => ExitApplication();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_openSettingsMenuItem);
        menu.Items.Add(_openLogsFolderMenuItem);
        menu.Items.Add(_pauseResumeMenuItem);
        menu.Items.Add(_scanNowMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Text = UiText.AppTitle(CurrentSettings.UiLanguage),
            Icon = AppIcon.Create(),
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        _worker.Start();
        LogSecurityModuleStatus();
        ApplyStartupSetting(CurrentSettings.RunAtStartup);
        _watchCoordinator.ApplySettings(CurrentSettings);

        if (CurrentSettings.StartPaused)
        {
            _watchCoordinator.Pause();
        }
        else
        {
            _watchCoordinator.Resume();
            _watchCoordinator.ScanExisting();
        }

        if (_startHidden)
        {
            _notifyIcon.ShowBalloonTip(2000, UiText.AppTitle(CurrentSettings.UiLanguage), UiText.StartedMinimized(CurrentSettings.UiLanguage), ToolTipIcon.Info);
        }
        else
        {
            MainForm = new MainForm(this);
            MainForm.Show();
        }

        UpdateTrayState();
        _logService.Info("Application initialized.");
    }

    public AppSettings CurrentSettings { get; private set; }

    public bool IsExiting => _isExiting;

    public UiState GetUiState()
    {
        UpdateTrayState();
        return new UiState(
            CurrentSettings,
            _watchCoordinator.IsPaused,
            _worker.QueueLength,
            _watchCoordinator.GetStatusText(_worker.QueueLength, CurrentSettings.UiLanguage),
            GetSecurityModuleStatusText(CurrentSettings.UiLanguage),
            _logService.GetSnapshot());
    }

    public void ApplySettings(AppSettings settings)
    {
        CurrentSettings = settings.Normalize();
        CurrentSettings.Validate();
        ApplySecurityModuleSetting(CurrentSettings);
        _settingsService.Save(CurrentSettings);
        ApplyStartupSetting(CurrentSettings.RunAtStartup);
        _watchCoordinator.ApplySettings(CurrentSettings);

        if (CurrentSettings.StartPaused)
        {
            _watchCoordinator.Pause();
        }
        else
        {
            _watchCoordinator.Resume();
            _watchCoordinator.ScanExisting();
        }

        _logService.Info("Settings saved.");
        UpdateTrayState();
    }

    public void SetUiLanguage(UiLanguage language)
    {
        if (CurrentSettings.UiLanguage == language)
        {
            return;
        }

        var updated = CurrentSettings.Normalize();
        updated.UiLanguage = language;
        CurrentSettings = updated.Normalize();
        _settingsService.Save(CurrentSettings);
        UpdateTrayState();
    }

    public string GetRegisteredSecurityModulePath()
    {
        return _securityModuleRegistryService.GetRegisteredModulePath(SecurityModuleRegistryService.DefaultModuleName) ?? string.Empty;
    }

    public void RegisterSecurityModule(string dllPath)
    {
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            throw new InvalidOperationException(UiText.SecurityModulePathRequired(CurrentSettings.UiLanguage));
        }

        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException(UiText.SecurityModuleDllNotFound(CurrentSettings.UiLanguage), dllPath);
        }

        _securityModuleRegistryService.RegisterModule(SecurityModuleRegistryService.DefaultModuleName, dllPath);
        _logService.Info($"Security module registered: {Path.GetFullPath(dllPath)}");
    }

    public void TogglePause()
    {
        if (_watchCoordinator.IsPaused)
        {
            _watchCoordinator.Resume();
            _watchCoordinator.ScanExisting();
        }
        else
        {
            _watchCoordinator.Pause();
        }

        UpdateTrayState();
    }

    public void ScanNow()
    {
        _watchCoordinator.ScanExisting();
        _logService.Info("Manual scan requested.");
    }

    public void ShowMainWindow()
    {
        if (MainForm is null || MainForm.IsDisposed)
        {
            MainForm = new MainForm(this);
        }

        MainForm.Show();
        MainForm.WindowState = FormWindowState.Normal;
        MainForm.BringToFront();
        MainForm.Activate();
    }

    public void NotifyHiddenToTray()
    {
        _notifyIcon.ShowBalloonTip(2000, UiText.AppTitle(CurrentSettings.UiLanguage), UiText.HiddenToTray(CurrentSettings.UiLanguage), ToolTipIcon.Info);
    }

    public void OpenWatchFolder()
    {
        OpenPath(CurrentSettings.WatchFolder);
    }

    public void OpenOutputLocation()
    {
        var path = CurrentSettings.OutputMode switch
        {
            OutputMode.CustomRoot when !string.IsNullOrWhiteSpace(CurrentSettings.OutputRoot) => CurrentSettings.OutputRoot,
            OutputMode.ChildSubfolder when !string.IsNullOrWhiteSpace(CurrentSettings.WatchFolder) => Path.Combine(CurrentSettings.WatchFolder, CurrentSettings.OutputSubfolderName),
            _ => CurrentSettings.WatchFolder,
        };

        OpenPath(path);
    }

    public void OpenSettingsStorage()
    {
        OpenPath(_settingsService.SettingsDirectoryPath);
    }

    public void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _notifyIcon.Visible = false;

        if (MainForm is not null && !MainForm.IsDisposed)
        {
            MainForm.Close();
        }

        ExitThread();
    }

    public new void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Dispose();
        _watchCoordinator.Dispose();
        _worker.Dispose();
        base.Dispose();
    }

    protected override void ExitThreadCore()
    {
        Dispose();
        base.ExitThreadCore();
    }

    private void ApplyStartupSetting(bool enabled)
    {
        try
        {
            _autoStartService.Apply(enabled);
        }
        catch (Exception ex)
        {
            _logService.Warning($"Failed to apply startup setting: {ex.Message}");
        }
    }

    private void UpdateTrayState()
    {
        var language = CurrentSettings.UiLanguage;
        var status = _watchCoordinator.GetStatusText(_worker.QueueLength, language);
        _openSettingsMenuItem.Text = UiText.TrayOpenSettings(language);
        _openLogsFolderMenuItem.Text = UiText.OpenLogsFolder(language);
        _statusMenuItem.Text = status;
        _pauseResumeMenuItem.Text = _watchCoordinator.IsPaused ? UiText.ResumeWatching(language) : UiText.PauseWatching(language);
        _scanNowMenuItem.Text = UiText.ScanNow(language);
        _exitMenuItem.Text = UiText.Exit(language);

        var tooltip = status.Length > 63 ? status[..63] : status;
        _notifyIcon.Text = tooltip;
    }

    private void LogSecurityModuleStatus()
    {
        const string moduleName = SecurityModuleRegistryService.DefaultModuleName;

        var modulePath = _securityModuleRegistryService.GetRegisteredModulePath(moduleName);
        if (string.IsNullOrWhiteSpace(modulePath))
        {
            _logService.Warning(
                "Hancom security module is not registered in HKCU\\Software\\HNC\\HwpAutomation\\Modules. " +
                "Access prompts may appear until FilePathCheckerModule is registered.");
            return;
        }

        _logService.Info($"Registered Hancom security module found: {modulePath}");
    }

    private void ApplySecurityModuleSetting(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SecurityModuleDllPath))
        {
            return;
        }

        var currentPath = _securityModuleRegistryService.GetRegisteredModulePath(SecurityModuleRegistryService.DefaultModuleName);
        var normalizedTarget = Path.GetFullPath(settings.SecurityModuleDllPath);

        if (string.Equals(currentPath, normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        RegisterSecurityModule(normalizedTarget);
    }

    private string GetSecurityModuleStatusText(UiLanguage language)
    {
        var registeredPath = GetRegisteredSecurityModulePath();
        return UiText.SecurityModuleStatus(language, registeredPath);
    }

    private AppSettings PopulateSecurityModulePathFromRegistry(AppSettings settings)
    {
        if (HasAvailableSecurityModulePath(settings.SecurityModuleDllPath))
        {
            return settings;
        }

        var registeredPath = GetRegisteredSecurityModulePath();
        if (string.IsNullOrWhiteSpace(registeredPath))
        {
            return settings;
        }

        var updated = settings.Normalize();
        updated.SecurityModuleDllPath = registeredPath;
        return updated.Normalize();
    }

    private AppSettings PopulateSecurityModulePathFromBundledFiles(AppSettings settings)
    {
        var bundledPath = FindBundledSecurityModulePath();
        if (string.IsNullOrWhiteSpace(bundledPath))
        {
            return settings;
        }

        if (ShouldKeepConfiguredSecurityModulePath(settings.SecurityModuleDllPath, bundledPath))
        {
            return settings;
        }

        var updated = settings.Normalize();
        updated.SecurityModuleDllPath = bundledPath;
        return updated.Normalize();
    }

    private static string? FindBundledSecurityModulePath()
    {
        foreach (var directory in BundledSecurityModuleDirectories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var fileName in BundledSecurityModuleFileNames)
            {
                var candidatePath = Path.Combine(directory, fileName);
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            var firstMatch = Directory
                .EnumerateFiles(directory, "FilePathCheckerModule*.dll", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstMatch))
            {
                return firstMatch;
            }
        }

        return null;
    }

    private static bool HasAvailableSecurityModulePath(string? dllPath)
    {
        return !string.IsNullOrWhiteSpace(dllPath) && File.Exists(dllPath);
    }

    private static bool ShouldKeepConfiguredSecurityModulePath(string? configuredPath, string bundledPath)
    {
        if (!HasAvailableSecurityModulePath(configuredPath))
        {
            return false;
        }

        var configuredFileName = Path.GetFileName(configuredPath);
        var bundledFileName = Path.GetFileName(bundledPath);

        if (string.Equals(configuredFileName, bundledFileName, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(
                Path.GetFullPath(configuredPath!),
                Path.GetFullPath(bundledPath),
                StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private void OpenPath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logService.Warning(UiText.NoFolderPathConfigured(CurrentSettings.UiLanguage));
                return;
            }

            Directory.CreateDirectory(path);

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _logService.Error($"Failed to open folder: {ex.Message}");
        }
    }
}
