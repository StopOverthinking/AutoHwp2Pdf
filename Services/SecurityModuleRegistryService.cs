using Microsoft.Win32;

namespace AutoHwp2Pdf.Services;

public sealed class SecurityModuleRegistryService
{
    private const string AutomationModulesKeyPath = @"Software\HNC\HwpAutomation\Modules";
    public const string DefaultModuleName = "FilePathCheckerModule";

    public string? GetRegisteredModulePath(string moduleName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutomationModulesKeyPath, writable: false);
        return key?.GetValue(moduleName) as string;
    }

    public bool IsRegistered(string moduleName)
    {
        return !string.IsNullOrWhiteSpace(GetRegisteredModulePath(moduleName));
    }

    public void RegisterModule(string moduleName, string dllPath)
    {
        var fullPath = Path.GetFullPath(dllPath);

        using var key = Registry.CurrentUser.CreateSubKey(AutomationModulesKeyPath);
        key?.SetValue(moduleName, fullPath);
    }

    public void UnregisterModule(string moduleName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutomationModulesKeyPath, writable: true);
        key?.DeleteValue(moduleName, throwOnMissingValue: false);
    }
}
