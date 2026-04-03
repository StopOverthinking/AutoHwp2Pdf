using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoHwp2Pdf.Services;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private string[] LegacySettingsFilePaths =>
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoHwp2Anything", "settings.json"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoHwp2Pdf", "settings.json"),
    ];

    public string SettingsDirectoryPath => AppContext.BaseDirectory;

    public string SettingsFilePath => Path.Combine(SettingsDirectoryPath, "settings.json");

    public AppSettings Load()
    {
        try
        {
            EnsureLocalSettingsFileExists();
            return LoadFromFile(SettingsFilePath) ?? new AppSettings().Normalize();
        }
        catch
        {
            return new AppSettings().Normalize();
        }
    }

    public void Save(AppSettings settings)
    {
        var normalized = settings.Normalize();
        Directory.CreateDirectory(SettingsDirectoryPath);
        var json = JsonSerializer.Serialize(normalized, _jsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    private void EnsureLocalSettingsFileExists()
    {
        Directory.CreateDirectory(SettingsDirectoryPath);

        if (File.Exists(SettingsFilePath))
        {
            return;
        }

        var initialSettings = LegacySettingsFilePaths
            .Select(LoadFromFile)
            .FirstOrDefault(settings => settings is not null)
            ?? new AppSettings().Normalize();

        Save(initialSettings);
    }

    private AppSettings? LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        return settings.Normalize();
    }
}
