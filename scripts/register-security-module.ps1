param(
    [Parameter(Mandatory = $true)]
    [string]$DllPath,

    [string]$ModuleName = "FilePathCheckerModule"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $DllPath)) {
    throw "DLL not found: $DllPath"
}

$fullPath = [System.IO.Path]::GetFullPath($DllPath)
$registryPath = "HKCU:\Software\HNC\HwpAutomation\Modules"

New-Item -Path $registryPath -Force | Out-Null
Set-ItemProperty -Path $registryPath -Name $ModuleName -Value $fullPath

Write-Output "Registered module name: $ModuleName"
Write-Output "Registered module path: $fullPath"
