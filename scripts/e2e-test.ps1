param(
    [string]$ExecutablePath = "C:\Users\PEN\Documents\AutoHwp2Pdf\bin\Release\net9.0-windows\win-x64\publish\AutoHwp2Anything.exe",
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"

function Write-Section {
    param([string]$Message)
    Write-Output "== $Message =="
}

function New-BlankHwpx {
    param([string]$Path)

    $hwp = $null

    try {
        $hwp = New-Object -ComObject HWPFrame.HwpObject

        try { $hwp.XHwpWindows.Active_XHwpWindow.Visible = $false } catch {}
        try { $hwp.SetMessageBoxMode(0x00214411) } catch {}
        try { $hwp.RegisterModule("FilePathCheckDLL", "FilePathCheckerModule") } catch {}

        $saved = $hwp.SaveAs($Path, "HWPX", "")
        if (-not $saved) {
            throw "Hancom SaveAs(HWPX) returned false."
        }
    }
    finally {
        if ($null -ne $hwp) {
            try { $hwp.Clear(1) } catch {}
            try { $hwp.Quit() } catch {}
            [System.Runtime.InteropServices.Marshal]::ReleaseComObject($hwp) | Out-Null
        }
    }
}

if (-not (Test-Path $ExecutablePath)) {
    throw "Executable not found: $ExecutablePath"
}

$existing = Get-Process -Name "AutoHwp2Anything" -ErrorAction SilentlyContinue
if ($existing) {
    throw "AutoHwp2Anything is already running. Stop it before running the E2E test."
}

$appDirectory = Split-Path -Parent $ExecutablePath
$settingsPath = Join-Path $appDirectory "settings.json"
$logPath = Join-Path $appDirectory "activity.log"
$backupPath = Join-Path $appDirectory "settings.backup.e2e.json"
$testRoot = "C:\Users\PEN\Documents\AutoHwp2Pdf\_e2e"
$watchDir = Join-Path $testRoot "watch"

Write-Section "Preparing test folders"
New-Item -ItemType Directory -Force -Path $appDirectory | Out-Null
Remove-Item $testRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $watchDir | Out-Null

if (Test-Path $settingsPath) {
    Copy-Item $settingsPath $backupPath -Force
}

$settingsObject = [ordered]@{
    WatchFolder = $watchDir
    OutputRoot = ""
    OutputSubfolderName = "pdf"
    OutputFormat = "Pdf"
    OutputMode = "SameDirectory"
    IncludeSubdirectories = $true
    RunAtStartup = $false
    StartPaused = $false
    StableCheckDelayMs = 1000
    MaxRetryCount = 3
}

$settingsObject | ConvertTo-Json | Set-Content -Path $settingsPath

$process = $null

try {
    Write-Section "Starting AutoHwp2Anything"
    $process = Start-Process -FilePath $ExecutablePath -ArgumentList "--minimized" -PassThru
    Start-Sleep -Seconds 3

    if ($process.HasExited) {
        throw "AutoHwp2Anything exited immediately with code $($process.ExitCode)."
    }

    Write-Section "Creating sample HWPX"
    $sourcePath = Join-Path $watchDir "sample.hwpx"
    $outputPath = Join-Path $watchDir "sample.pdf"
    New-BlankHwpx -Path $sourcePath

    Write-Section "Waiting for PDF output"
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if (Test-Path $outputPath) {
            break
        }

        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)

    $result = [ordered]@{
        SourceExists = (Test-Path $sourcePath)
        PdfExists = (Test-Path $outputPath)
        SourcePath = $sourcePath
        PdfPath = $outputPath
    }

    Write-Section "Result"
    $result | ConvertTo-Json

    if (Test-Path $logPath) {
        Write-Section "Recent logs"
        Get-Content $logPath -Tail 20
    }

    if (-not (Test-Path $outputPath)) {
        throw "PDF was not created within $TimeoutSeconds seconds."
    }
}
finally {
    Write-Section "Cleaning up"

    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }

    if (Test-Path $backupPath) {
        Move-Item -Force $backupPath $settingsPath
    }
    else {
        Remove-Item $settingsPath -Force -ErrorAction SilentlyContinue
    }
}
