# AutoHwp2Anything

AutoHwp2Anything is a Windows tray app that watches a folder for `.hwp` and `.hwpx` files and automatically creates converted copies in `pdf`, `docx`, or `png` format.

## Implemented

- WinForms settings window
- Tray resident behavior when the window is closed
- Single-instance guard
- `FileSystemWatcher` with debounce and file stability checks
- Single-threaded conversion queue
- Hancom COM (`HWPFrame.HwpObject`) based conversion
- GUI settings for watch folder, output format, output folder, and output mode
- GUI settings for security module DLL path and registration
- Windows startup registration option
- Rolling activity log file next to the executable
- First-run creation of `settings.json` next to the executable
- Launcher script that installs a local .NET 9 Desktop Runtime if the target PC does not already have one

## Output Formats

- `PDF`: creates a single PDF document
- `DOCX`: creates a single Word document
- `PNG`: creates page images; Hancom appends page sequence numbers to the generated files

## Output Location Modes

- `Same folder as source`
- `Child folder under source`
- `Custom root folder`

Converted outputs keep prior versions instead of overwriting. When the source document changes, the app creates the next numbered output for that source in the target folder.

## Requirements

- Windows
- Hancom Office installed
- Hancom COM registered
  - Example: run `hwp.exe -regserver` from the Hancom install folder
- .NET 9 SDK for development

## Build

```powershell
dotnet build .\AutoHwp2Anything.csproj
```

## Publish

```powershell
dotnet publish .\AutoHwp2Anything.csproj -c Release -r win-x64 --self-contained false
```

If you place the Hancom security module DLL in the project root with a name like `FilePathCheckerModuleExample.dll`, or under `SecurityModules\`, it is copied into the build and publish output automatically.

The publish output also includes:

- `Launch-AutoHwp2Anything.cmd`
- `Launch-AutoHwp2Anything.ps1`

Use the launcher on target PCs. If .NET Desktop Runtime 9 is missing, the launcher downloads the official `dotnet-install.ps1` script from `https://dot.net/v1/dotnet-install.ps1` and installs a local runtime into `.dotnet` under the app folder before starting the app.

## Run

```powershell
dotnet run --project .\AutoHwp2Anything.csproj
```

To start minimized into the tray:

```powershell
dotnet run --project .\AutoHwp2Anything.csproj -- --minimized
```

For portable deployment, run this instead from the published folder:

```powershell
.\Launch-AutoHwp2Anything.cmd
```

To register a Hancom security module for the current user:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\register-security-module.ps1 -DllPath ".\bin\Release\net9.0-windows\win-x64\publish\SecurityModules\FilePathCheckerModuleExample.dll"
```

## Notes

- The converter opens documents with `Open(..., "", ...)` so the file format is auto-detected instead of forced from the extension.
- Some environments may still require a configured `FilePathCheckDLL` module. The app logs a warning when `FilePathCheckerModule` is not registered under `HKCU\Software\HNC\HwpAutomation\Modules`.
- On first run, if a bundled `FilePathCheckerModule*.dll` exists next to the app or under `SecurityModules\`, the app uses that path as the default security module DLL.
- You can either register the security module from the app UI or use `scripts/register-security-module.ps1`.
- `settings.json` is created next to `AutoHwp2Anything.exe`. If an older `%AppData%\AutoHwp2Anything\settings.json` or `%AppData%\AutoHwp2Pdf\settings.json` exists and no local settings file exists yet, it is copied into place automatically on first run.
