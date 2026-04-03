using Microsoft.Win32;
using System.Windows.Forms;

namespace AutoHwp2Pdf.Services;

public sealed class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryName = "AutoHwp2Anything";

    public void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            key.SetValue(AppRegistryName, BuildStartupCommand());
        }
        else
        {
            key.DeleteValue(AppRegistryName, throwOnMissingValue: false);
        }
    }

    private static string BuildStartupCommand()
    {
        var launcherPath = Path.Combine(AppContext.BaseDirectory, "Launch-AutoHwp2Anything.ps1");
        if (File.Exists(launcherPath))
        {
            return $"powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{launcherPath}\" --minimized";
        }

        return $"\"{Application.ExecutablePath}\" --minimized";
    }
}
