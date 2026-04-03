param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AppArgs
)

$ErrorActionPreference = "Stop"

$appDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$appExePath = Join-Path $appDir "AutoHwp2Anything.exe"
$localDotnetRoot = Join-Path $appDir ".dotnet"
$dotnetInstallScriptPath = Join-Path $localDotnetRoot "dotnet-install.ps1"
$requiredChannel = "9.0"
$requiredArchitecture = "x64"

function Test-LocalDesktopRuntime {
    $desktopRuntimeRoot = Join-Path $localDotnetRoot "shared\\Microsoft.WindowsDesktop.App"
    $coreRuntimeRoot = Join-Path $localDotnetRoot "shared\\Microsoft.NETCore.App"
    $hostFxrRoot = Join-Path $localDotnetRoot "host\\fxr"

    if (-not (Test-Path $desktopRuntimeRoot) -or -not (Test-Path $coreRuntimeRoot) -or -not (Test-Path $hostFxrRoot)) {
        return $false
    }

    $hasDesktopRuntime = @(Get-ChildItem -Path $desktopRuntimeRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "9.*" }).Count -gt 0
    $hasCoreRuntime = @(Get-ChildItem -Path $coreRuntimeRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "9.*" }).Count -gt 0
    $hasHostFxr = @(Get-ChildItem -Path $hostFxrRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "9.*" }).Count -gt 0

    return $hasDesktopRuntime -and $hasCoreRuntime -and $hasHostFxr
}

function Test-SystemDesktopRuntime {
    $candidateRoots = @(
        ([System.IO.Path]::Combine(${env:ProgramFiles}, "dotnet", "shared", "Microsoft.WindowsDesktop.App")),
        ([System.IO.Path]::Combine(${env:ProgramFiles(x86)}, "dotnet", "shared", "Microsoft.WindowsDesktop.App"))
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidateRoot in $candidateRoots) {
        if (-not (Test-Path $candidateRoot)) {
            continue
        }

        if (@(Get-ChildItem -Path $candidateRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "9.*" }).Count -gt 0) {
            return $true
        }
    }

    return $false
}

function Ensure-LocalDesktopRuntime {
    New-Item -ItemType Directory -Path $localDotnetRoot -Force | Out-Null

    if (-not (Test-Path $dotnetInstallScriptPath)) {
        Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $dotnetInstallScriptPath
    }

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $dotnetInstallScriptPath `
        -Channel $requiredChannel `
        -Runtime dotnet `
        -Architecture $requiredArchitecture `
        -InstallDir $localDotnetRoot

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $dotnetInstallScriptPath `
        -Channel $requiredChannel `
        -Runtime windowsdesktop `
        -Architecture $requiredArchitecture `
        -InstallDir $localDotnetRoot
}

if (-not (Test-Path $appExePath)) {
    throw "AutoHwp2Anything.exe was not found next to the launcher."
}

if (-not (Test-LocalDesktopRuntime) -and -not (Test-SystemDesktopRuntime)) {
    Write-Host ".NET Desktop Runtime 9 was not found. Installing a local runtime for this app..."
    Ensure-LocalDesktopRuntime
}

if (Test-LocalDesktopRuntime) {
    $env:DOTNET_ROOT = $localDotnetRoot
    if ($env:PATH -notlike "$localDotnetRoot*") {
        $env:PATH = "$localDotnetRoot;$env:PATH"
    }
}

Push-Location $appDir
try {
    & $appExePath @AppArgs
}
finally {
    Pop-Location
}
