using System.Drawing;
using System.Windows.Forms;

namespace AutoHwp2Pdf;

public sealed class MainForm : Form
{
    private readonly AppController _controller;
    private readonly GroupBox _settingsGroup = new() { Dock = DockStyle.Top, AutoSize = true };
    private readonly GroupBox _logsGroup = new() { Dock = DockStyle.Fill };
    private readonly Button _languageButton = new() { Width = 36, Height = 32, Margin = new Padding(0), AutoSize = false };
    private readonly ContextMenuStrip _languageMenu = new();
    private readonly ToolStripMenuItem _englishLanguageMenuItem = new();
    private readonly ToolStripMenuItem _koreanLanguageMenuItem = new();
    private readonly ToolTip _toolTip = new();
    private readonly TextBox _watchFolderTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _outputRootTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _outputSubfolderTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _securityModuleDllTextBox = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _outputFormatComboBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _outputModeComboBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _includeSubdirectoriesCheckBox = new() { AutoSize = true };
    private readonly CheckBox _runAtStartupCheckBox = new() { AutoSize = true };
    private readonly CheckBox _startPausedCheckBox = new() { AutoSize = true };
    private readonly NumericUpDown _stableDelayNumeric = new() { Minimum = 500, Maximum = 10000, Increment = 100, Dock = DockStyle.Fill };
    private readonly NumericUpDown _retryCountNumeric = new() { Minimum = 1, Maximum = 10, Dock = DockStyle.Fill };
    private readonly Label _statusLabel = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _securityModuleStatusLabel = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _pauseResumeButton = new() { AutoSize = true };
    private readonly ListBox _logListBox = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 1000 };
    private readonly List<Action<UiLanguage>> _localizedTextBindings = new();

    private string _lastLogSignature = string.Empty;
    private string _lastStatusText = string.Empty;
    private bool _loading;

    public MainForm(AppController controller)
    {
        _controller = controller;

        BindText(this, UiText.AppTitle);
        BindText(_settingsGroup, UiText.SettingsGroup);
        BindText(_logsGroup, UiText.LogsGroup);
        BindText(_includeSubdirectoriesCheckBox, UiText.IncludeSubdirectories);
        BindText(_runAtStartupCheckBox, UiText.RunAtStartup);
        BindText(_startPausedCheckBox, UiText.StartPaused);

        _languageButton.Image = CreateLanguageButtonImage();
        _languageButton.ImageAlign = ContentAlignment.MiddleCenter;
        _languageButton.Click += (_, _) => _languageMenu.Show(_languageButton, new Point(0, _languageButton.Height));

        _englishLanguageMenuItem.Click += (_, _) => ChangeUiLanguage(UiLanguage.English);
        _koreanLanguageMenuItem.Click += (_, _) => ChangeUiLanguage(UiLanguage.Korean);
        _languageMenu.Items.Add(_englishLanguageMenuItem);
        _languageMenu.Items.Add(_koreanLanguageMenuItem);

        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(920, 640);
        Size = new Size(1100, 720);
        Icon = AppIcon.Create();

        BuildUi();
        LoadFromSettings(_controller.CurrentSettings);
        RefreshState();

        _refreshTimer.Tick += (_, _) => RefreshState();
        _refreshTimer.Start();

        FormClosing += OnFormClosing;
        Resize += OnResize;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 5,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topBar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8),
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topBar.Controls.Add(new Panel { Dock = DockStyle.Fill, AutoSize = true }, 0, 0);
        topBar.Controls.Add(_languageButton, 1, 0);

        var settingsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 4,
            AutoSize = true,
        };
        settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        settingsTable.Controls.Add(CreateLabel(UiText.WatchFolderLabel), 0, 0);
        settingsTable.Controls.Add(_watchFolderTextBox, 1, 0);
        settingsTable.Controls.Add(CreateButton(UiText.Browse, (_, _) => BrowseFolder(_watchFolderTextBox)), 2, 0);
        settingsTable.Controls.Add(CreateButton(UiText.Open, (_, _) => _controller.OpenWatchFolder()), 3, 0);

        settingsTable.Controls.Add(CreateLabel(UiText.OutputFormatLabel), 0, 1);
        settingsTable.Controls.Add(_outputFormatComboBox, 1, 1);
        settingsTable.Controls.Add(new Label(), 2, 1);
        settingsTable.Controls.Add(new Label(), 3, 1);

        settingsTable.Controls.Add(CreateLabel(UiText.OutputModeLabel), 0, 2);
        settingsTable.Controls.Add(_outputModeComboBox, 1, 2);
        settingsTable.Controls.Add(new Label(), 2, 2);
        settingsTable.Controls.Add(new Label(), 3, 2);

        settingsTable.Controls.Add(CreateLabel(UiText.OutputRootLabel), 0, 3);
        settingsTable.Controls.Add(_outputRootTextBox, 1, 3);
        settingsTable.Controls.Add(CreateButton(UiText.Browse, (_, _) => BrowseFolder(_outputRootTextBox)), 2, 3);
        settingsTable.Controls.Add(CreateButton(UiText.Open, (_, _) => _controller.OpenOutputLocation()), 3, 3);

        settingsTable.Controls.Add(CreateLabel(UiText.OutputSubfolderLabel), 0, 4);
        settingsTable.Controls.Add(_outputSubfolderTextBox, 1, 4);
        settingsTable.Controls.Add(new Label(), 2, 4);
        settingsTable.Controls.Add(new Label(), 3, 4);

        settingsTable.Controls.Add(CreateLabel(UiText.SecurityModuleDllLabel), 0, 5);
        settingsTable.Controls.Add(_securityModuleDllTextBox, 1, 5);
        settingsTable.Controls.Add(CreateButton(UiText.Browse, (_, _) => BrowseFile(_securityModuleDllTextBox)), 2, 5);
        settingsTable.Controls.Add(CreateButton(UiText.Register, (_, _) => RegisterSecurityModuleFromForm()), 3, 5);

        settingsTable.Controls.Add(CreateLabel(UiText.StableDelayLabel), 0, 6);
        settingsTable.Controls.Add(_stableDelayNumeric, 1, 6);
        settingsTable.Controls.Add(CreateLabel(UiText.RetryCountLabel), 2, 6);
        settingsTable.Controls.Add(_retryCountNumeric, 3, 6);

        settingsTable.Controls.Add(CreateLabel(UiText.SecurityStatusLabel), 0, 7);
        settingsTable.Controls.Add(_securityModuleStatusLabel, 1, 7);
        settingsTable.SetColumnSpan(_securityModuleStatusLabel, 3);

        var optionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
        };
        optionsPanel.Controls.Add(_includeSubdirectoriesCheckBox);
        optionsPanel.Controls.Add(_runAtStartupCheckBox);
        optionsPanel.Controls.Add(_startPausedCheckBox);

        settingsTable.Controls.Add(CreateLabel(UiText.OptionsLabel), 0, 8);
        settingsTable.Controls.Add(optionsPanel, 1, 8);
        settingsTable.SetColumnSpan(optionsPanel, 3);

        _settingsGroup.Controls.Add(settingsTable);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 8),
        };
        actionPanel.Controls.Add(CreateButton(UiText.SaveSettings, (_, _) => SaveSettings(), 120));
        actionPanel.Controls.Add(CreateButton(UiText.ScanNow, (_, _) => _controller.ScanNow(), 120));
        actionPanel.Controls.Add(_pauseResumeButton);
        actionPanel.Controls.Add(CreateButton(UiText.OpenWatchFolder, (_, _) => _controller.OpenWatchFolder(), 120));
        actionPanel.Controls.Add(CreateButton(UiText.OpenOutputFolder, (_, _) => _controller.OpenOutputLocation(), 120));
        actionPanel.Controls.Add(CreateButton(UiText.OpenLogsFolder, (_, _) => _controller.OpenSettingsStorage(), 120));

        _pauseResumeButton.Click += (_, _) => _controller.TogglePause();

        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8),
        };
        statusPanel.Controls.Add(_statusLabel, 0, 0);

        _logsGroup.Controls.Add(_logListBox);
        _outputFormatComboBox.SelectedIndexChanged += (_, _) => HandleOutputFormatChanged();
        _outputModeComboBox.SelectedIndexChanged += (_, _) => UpdateOutputModeFields();

        root.Controls.Add(topBar, 0, 0);
        root.Controls.Add(_settingsGroup, 0, 1);
        root.Controls.Add(actionPanel, 0, 2);
        root.Controls.Add(statusPanel, 0, 3);
        root.Controls.Add(_logsGroup, 0, 4);

        Controls.Add(root);
    }

    private void LoadFromSettings(AppSettings settings)
    {
        _loading = true;

        ApplyLocalizedTexts(settings.UiLanguage);
        _watchFolderTextBox.Text = settings.WatchFolder;
        _outputRootTextBox.Text = settings.OutputRoot;
        _outputSubfolderTextBox.Text = settings.OutputSubfolderName;
        _securityModuleDllTextBox.Text = settings.SecurityModuleDllPath;
        _includeSubdirectoriesCheckBox.Checked = settings.IncludeSubdirectories;
        _runAtStartupCheckBox.Checked = settings.RunAtStartup;
        _startPausedCheckBox.Checked = settings.StartPaused;
        _stableDelayNumeric.Value = settings.StableCheckDelayMs;
        _retryCountNumeric.Value = settings.MaxRetryCount;
        SelectOutputFormatOption(settings.OutputFormat);
        SelectOutputModeOption(settings.OutputMode);

        _loading = false;
        UpdateOutputModeFields();
    }

    private void RefreshState()
    {
        var state = _controller.GetUiState();

        if (_lastStatusText != state.StatusText)
        {
            _statusLabel.Text = state.StatusText;
            _lastStatusText = state.StatusText;
        }

        _securityModuleStatusLabel.Text = state.SecurityModuleStatusText;
        _pauseResumeButton.Text = state.IsPaused ? UiText.ResumeWatching(state.Settings.UiLanguage) : UiText.PauseWatching(state.Settings.UiLanguage);

        var latestLog = state.Logs.Count > 0 ? state.Logs[^1].ToString() : string.Empty;
        var logSignature = $"{state.Logs.Count}:{latestLog}";

        if (_lastLogSignature != logSignature)
        {
            _logListBox.BeginUpdate();
            _logListBox.Items.Clear();

            foreach (var entry in state.Logs)
            {
                _logListBox.Items.Add(entry.ToString());
            }

            if (_logListBox.Items.Count > 0)
            {
                _logListBox.TopIndex = _logListBox.Items.Count - 1;
            }

            _logListBox.EndUpdate();
            _lastLogSignature = logSignature;
        }
    }

    private void SaveSettings()
    {
        try
        {
            _controller.ApplySettings(ReadSettingsFromForm());
            LoadFromSettings(_controller.CurrentSettings);
            RefreshState();
        }
        catch (Exception ex)
        {
            var language = CurrentUiLanguage;
            MessageBox.Show(this, $"{UiText.SaveSettingsError(language)}\r\n{ex.Message}", UiText.AppTitle(language), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private AppSettings ReadSettingsFromForm()
    {
        var selectedFormat = _outputFormatComboBox.SelectedItem as OutputFormatOption
            ?? new OutputFormatOption(OutputFormat.Pdf, string.Empty);
        var selectedMode = _outputModeComboBox.SelectedItem as OutputModeOption
            ?? new OutputModeOption(OutputMode.SameDirectory, string.Empty);

        return new AppSettings
        {
            UiLanguage = CurrentUiLanguage,
            WatchFolder = _watchFolderTextBox.Text,
            OutputRoot = _outputRootTextBox.Text,
            OutputSubfolderName = _outputSubfolderTextBox.Text,
            SecurityModuleDllPath = _securityModuleDllTextBox.Text,
            OutputFormat = selectedFormat.Format,
            OutputMode = selectedMode.Mode,
            IncludeSubdirectories = _includeSubdirectoriesCheckBox.Checked,
            RunAtStartup = _runAtStartupCheckBox.Checked,
            StartPaused = _startPausedCheckBox.Checked,
            StableCheckDelayMs = (int)_stableDelayNumeric.Value,
            MaxRetryCount = (int)_retryCountNumeric.Value,
        };
    }

    private void ChangeUiLanguage(UiLanguage language)
    {
        if (_loading || CurrentUiLanguage == language)
        {
            return;
        }

        _controller.SetUiLanguage(language);
        ApplyLocalizedTexts(language);
        RefreshState();
    }

    private void HandleOutputFormatChanged()
    {
        if (_loading)
        {
            return;
        }

        var selectedFormat = (_outputFormatComboBox.SelectedItem as OutputFormatOption)?.Format ?? OutputFormat.Pdf;
        if (IsDefaultSubfolderName(_outputSubfolderTextBox.Text))
        {
            _outputSubfolderTextBox.Text = selectedFormat.GetDefaultSubfolderName();
        }

        UpdateOutputModeFields();
    }

    private void UpdateOutputModeFields()
    {
        if (_loading)
        {
            return;
        }

        var selectedMode = (_outputModeComboBox.SelectedItem as OutputModeOption)?.Mode ?? OutputMode.SameDirectory;

        _outputRootTextBox.Enabled = selectedMode == OutputMode.CustomRoot;
        _outputSubfolderTextBox.Enabled = selectedMode == OutputMode.ChildSubfolder;
    }

    private void BrowseFolder(TextBox targetTextBox)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = UiText.SelectFolder(CurrentUiLanguage),
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(targetTextBox.Text) ? targetTextBox.Text : string.Empty,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            targetTextBox.Text = dialog.SelectedPath;
        }
    }

    private void BrowseFile(TextBox targetTextBox)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = UiText.DllFileFilter(CurrentUiLanguage),
            CheckFileExists = true,
            FileName = File.Exists(targetTextBox.Text) ? targetTextBox.Text : string.Empty,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            targetTextBox.Text = dialog.FileName;
        }
    }

    private void RegisterSecurityModuleFromForm()
    {
        try
        {
            _controller.RegisterSecurityModule(_securityModuleDllTextBox.Text);
            RefreshState();

            var language = CurrentUiLanguage;
            MessageBox.Show(this, UiText.SecurityModuleRegistered(language), UiText.AppTitle(language), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            var language = CurrentUiLanguage;
            MessageBox.Show(this, $"{UiText.SecurityModuleRegistrationFailed(language)}\r\n{ex.Message}", UiText.AppTitle(language), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_controller.IsExiting)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        _controller.NotifyHiddenToTray();
    }

    private void OnResize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }

    private void ApplyLocalizedTexts(UiLanguage displayLanguage)
    {
        foreach (var binding in _localizedTextBindings)
        {
            binding(displayLanguage);
        }

        RefreshLanguageMenu(displayLanguage);
        RefreshOutputFormatOptions(displayLanguage);
        RefreshOutputModeOptions(displayLanguage);
        _toolTip.SetToolTip(_languageButton, UiText.ChangeLanguage(displayLanguage));
        _languageButton.AccessibleName = UiText.ChangeLanguage(displayLanguage);
    }

    private void RefreshLanguageMenu(UiLanguage displayLanguage)
    {
        _englishLanguageMenuItem.Text = UiText.LanguageName(UiLanguage.English, displayLanguage);
        _koreanLanguageMenuItem.Text = UiText.LanguageName(UiLanguage.Korean, displayLanguage);
        _englishLanguageMenuItem.Checked = CurrentUiLanguage == UiLanguage.English;
        _koreanLanguageMenuItem.Checked = CurrentUiLanguage == UiLanguage.Korean;
    }

    private void RefreshOutputFormatOptions(UiLanguage language)
    {
        var selectedFormat = (_outputFormatComboBox.SelectedItem as OutputFormatOption)?.Format ?? OutputFormat.Pdf;

        _outputFormatComboBox.BeginUpdate();
        _outputFormatComboBox.Items.Clear();
        _outputFormatComboBox.Items.Add(new OutputFormatOption(OutputFormat.Pdf, UiText.OutputFormatName(OutputFormat.Pdf, language)));
        _outputFormatComboBox.Items.Add(new OutputFormatOption(OutputFormat.Docx, UiText.OutputFormatName(OutputFormat.Docx, language)));
        _outputFormatComboBox.Items.Add(new OutputFormatOption(OutputFormat.Png, UiText.OutputFormatName(OutputFormat.Png, language)));
        SelectOutputFormatOption(selectedFormat);
        _outputFormatComboBox.EndUpdate();
    }

    private void RefreshOutputModeOptions(UiLanguage language)
    {
        var selectedMode = (_outputModeComboBox.SelectedItem as OutputModeOption)?.Mode ?? OutputMode.SameDirectory;

        _outputModeComboBox.BeginUpdate();
        _outputModeComboBox.Items.Clear();
        _outputModeComboBox.Items.Add(new OutputModeOption(OutputMode.SameDirectory, UiText.OutputModeName(OutputMode.SameDirectory, language)));
        _outputModeComboBox.Items.Add(new OutputModeOption(OutputMode.ChildSubfolder, UiText.OutputModeName(OutputMode.ChildSubfolder, language)));
        _outputModeComboBox.Items.Add(new OutputModeOption(OutputMode.CustomRoot, UiText.OutputModeName(OutputMode.CustomRoot, language)));
        SelectOutputModeOption(selectedMode);
        _outputModeComboBox.EndUpdate();
    }

    private UiLanguage CurrentUiLanguage => _controller.CurrentSettings.UiLanguage;

    private void SelectOutputFormatOption(OutputFormat format)
    {
        for (var index = 0; index < _outputFormatComboBox.Items.Count; index++)
        {
            if (_outputFormatComboBox.Items[index] is OutputFormatOption option && option.Format == format)
            {
                _outputFormatComboBox.SelectedIndex = index;
                return;
            }
        }

        if (_outputFormatComboBox.Items.Count > 0)
        {
            _outputFormatComboBox.SelectedIndex = 0;
        }
    }

    private void SelectOutputModeOption(OutputMode mode)
    {
        for (var index = 0; index < _outputModeComboBox.Items.Count; index++)
        {
            if (_outputModeComboBox.Items[index] is OutputModeOption option && option.Mode == mode)
            {
                _outputModeComboBox.SelectedIndex = index;
                return;
            }
        }

        if (_outputModeComboBox.Items.Count > 0)
        {
            _outputModeComboBox.SelectedIndex = 0;
        }
    }

    private Label CreateLabel(Func<UiLanguage, string> textFactory)
    {
        return BindText(
            new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Margin = new Padding(3, 8, 3, 8),
            },
            textFactory);
    }

    private Button CreateButton(Func<UiLanguage, string> textFactory, EventHandler onClick, int width = 90)
    {
        var button = BindText(
            new Button
            {
                Width = width,
                AutoSize = false,
                Height = 30,
            },
            textFactory);
        button.Click += onClick;
        return button;
    }

    private T BindText<T>(T control, Func<UiLanguage, string> textFactory)
        where T : Control
    {
        _localizedTextBindings.Add(language => control.Text = textFactory(language));
        return control;
    }

    private static Image CreateLanguageButtonImage()
    {
        var bitmap = new Bitmap(18, 18);

        using var graphics = Graphics.FromImage(bitmap);
        using var pen = new Pen(Color.FromArgb(45, 45, 45), 1.4f);

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        graphics.DrawEllipse(pen, 1, 1, 15, 15);
        graphics.DrawLine(pen, 8.5f, 1, 8.5f, 16);
        graphics.DrawArc(pen, 4, 1, 9, 15, 90, 180);
        graphics.DrawArc(pen, 4, 1, 9, 15, 270, 180);
        graphics.DrawArc(pen, 1, 4, 15, 9, 0, 180);
        graphics.DrawArc(pen, 1, 6, 15, 5, 0, 180);

        return bitmap;
    }

    private static bool IsDefaultSubfolderName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return string.Equals(value, OutputFormat.Pdf.GetDefaultSubfolderName(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, OutputFormat.Docx.GetDefaultSubfolderName(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, OutputFormat.Png.GetDefaultSubfolderName(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class OutputFormatOption
    {
        public OutputFormatOption(OutputFormat format, string label)
        {
            Format = format;
            Label = label;
        }

        public OutputFormat Format { get; }

        public string Label { get; }

        public override string ToString()
        {
            return Label;
        }
    }

    private sealed class OutputModeOption
    {
        public OutputModeOption(OutputMode mode, string label)
        {
            Mode = mode;
            Label = label;
        }

        public OutputMode Mode { get; }

        public string Label { get; }

        public override string ToString()
        {
            return Label;
        }
    }
}
