param()

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$setupProjectPath = Join-Path $PSScriptRoot "AutoHwp2PdfSetup\AutoHwp2PdfSetup.csproj"
$setupProjectDirectory = Split-Path -Parent $setupProjectPath
$appProjectPath = Join-Path $projectRoot "AutoHwp2Anything.csproj"
$appPublishDirectory = Join-Path $projectRoot "bin\Release\net9.0-windows\win-x64\publish"
$payloadDirectory = Join-Path $PSScriptRoot "payload"
$publishDirectory = Join-Path $PSScriptRoot "publish"
$setupExePath = Join-Path $PSScriptRoot "AutoHwp2AnythingSetup.exe"
$packageZipPath = Join-Path $PSScriptRoot "AutoHwp2AnythingSetup-package.zip"

$payloadFiles = @(
    "AutoHwp2Anything.deps.json",
    "AutoHwp2Anything.dll",
    "AutoHwp2Anything.exe",
    "AutoHwp2Anything.pdb",
    "AutoHwp2Anything.runtimeconfig.json",
    "FilePathCheckerModuleExample.dll",
    "Launch-AutoHwp2Anything.cmd",
    "Launch-AutoHwp2Anything.ps1"
)

dotnet publish $appProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained false

if (Test-Path $payloadDirectory) {
    Remove-Item -LiteralPath $payloadDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $payloadDirectory -Force | Out-Null

foreach ($fileName in $payloadFiles) {
    $sourcePath = Join-Path $appPublishDirectory $fileName
    if (-not (Test-Path $sourcePath)) {
        throw "Payload source file not found: $sourcePath"
    }

    Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $payloadDirectory $fileName) -Force
}

if (Test-Path $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

dotnet publish $setupProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDirectory

Copy-Item -LiteralPath (Join-Path $publishDirectory "AutoHwp2PdfSetup.exe") -Destination $setupExePath -Force

if (Test-Path $packageZipPath) {
    Remove-Item -LiteralPath $packageZipPath -Force
}

$itemsToPackage = @(
    $setupExePath,
    $payloadDirectory
)

Compress-Archive -Path $itemsToPackage -DestinationPath $packageZipPath -Force

Remove-Item -LiteralPath $publishDirectory -Recurse -Force

$transientDirectories = @(
    (Join-Path $setupProjectDirectory "bin"),
    (Join-Path $setupProjectDirectory "obj")
)

foreach ($directory in $transientDirectories) {
    if (Test-Path $directory) {
        Remove-Item -LiteralPath $directory -Recurse -Force
    }
}

Write-Host "Installer published:"
Write-Host "  $setupExePath"
Write-Host "Payload directory:"
Write-Host "  $payloadDirectory"
Write-Host "Package zip:"
Write-Host "  $packageZipPath"
