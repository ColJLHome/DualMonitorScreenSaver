# Dual Monitor Screensaver

A Windows screensaver that displays a separately chosen random image on every connected monitor.

## Features

- Uses all connected monitors
- Random images from a chosen folder, with a separate include-subfolders option for each saved folder
- Saved image-folder dropdown: add folders with Browse or by typing a path, switch between them, and remove entries
- `Fit`, `Width`, and `Height` scaling modes
- Configurable image-change interval
- Minimum image resolution as a percentage of each monitor's pixel count (0% disables filtering)
- Supports JPEG, PNG, BMP, GIF, and TIFF files

## Prerequisites for the built screensaver

The current `DualMonitorScreensaver.scr` targets **Windows x64 and .NET 8**. It is a framework-dependent build: the .NET runtime is **not bundled** in the file.

- **Windows on an x64 (Intel/AMD 64-bit) PC**, using a Windows version supported by .NET 8. This build is not for 32-bit Windows, macOS, or Linux. See Microsoft's [Windows installation and OS requirements](https://learn.microsoft.com/en-us/dotnet/core/install/windows).
- **.NET 8 Desktop Runtime for Windows x64**. Download the latest available **8.0.x** patch from the [.NET 8 download page](https://dotnet.microsoft.com/en-us/download/dotnet/8.0), selecting **.NET Desktop Runtime > Windows > x64**. The Desktop Runtime includes the base .NET runtime; the base runtime or ASP.NET Core Runtime alone is not sufficient for this Windows Forms application.
- **An accessible image folder** containing JPEG, PNG, BMP, GIF, or TIFF files. Your Windows account needs permission to read the images and write settings under `%LOCALAPPDATA%\DualMonitorScreensaver`.
- **At least one connected monitor**. Two monitors are not required; the screensaver uses all connected monitors.

You do **not** need Visual Studio or the .NET SDK to run the built `.scr` file. Installing only .NET 9 or .NET 10 does not satisfy this build's default .NET 8 requirement; different major runtimes can be installed side by side. See Microsoft's [runtime version selection documentation](https://learn.microsoft.com/en-us/dotnet/core/versions/selection).

To check the x64 installation, run this in PowerShell (for the standard installation location):

```powershell
& "$env:ProgramFiles\dotnet\dotnet.exe" --list-runtimes
```

The output should include **both** `Microsoft.WindowsDesktop.App 8.0.x` and `Microsoft.NETCore.App 8.0.x`, where `x` is the installed patch number. If Windows asks you to install .NET when opening the screensaver, check that you installed the **8.0 Desktop Runtime, x64**, not just another runtime version or architecture.

## Install the built version

1. Install the prerequisites above.
2. Keep `DualMonitorScreensaver.scr` in a permanent folder, then right-click it and choose **Install**. Alternatively, copy it to `C:\Windows\System32` (administrator permission required) and select it in Windows Screen Saver Settings.
3. Open **Settings**, select your image folder, and use **Test** to check the display. Select **OK** to save.

When updating an existing installation, close the screensaver and its Settings window, then replace the installed `.scr` with the new build. Rebuilding the project does not automatically update a separate installed copy.

## Build from source

Building requires a .NET SDK capable of targeting `net8.0-windows`, such as the .NET 8 SDK or a compatible newer SDK, on Windows.

```powershell
dotnet publish -c Release
Copy-Item .\bin\Release\net8.0-windows\win-x64\publish\DualMonitorScreensaver.exe .\DualMonitorScreensaver.scr
```

During development, run without arguments (or with `/c`) for settings and use `/s` to start the screensaver.

The **Test** button uses the current, unsaved settings. Press a key, click, or move the mouse to exit the test and return to Settings.
The **Image folder** dropdown remembers your folder list and the selected folder when you choose **OK**. **Browse** adds a folder and selects it; typing a new path adds it when you save. **Remove** removes an entry from the list without deleting the folder or its images. Only the selected folder is searched. Existing settings with a single folder are carried over automatically.
**Include subfolders for this folder** is remembered separately for each folder and updates when you switch folders. **Test** uses that folder's current option, and **OK** saves all folder options. Existing folders inherit your previous include-subfolders setting; newly added folders default to including subfolders.
The **Minimum image resolution (%)** setting skips images whose width times height is below the selected percentage of the monitor's width times height. For example, on a 1920 x 1080 monitor, 50% requires at least 1,036,800 pixels; a 1440 x 720 image qualifies, while a 1280 x 720 image does not. The threshold is checked separately for each monitor, before scaling, and the embedded preview uses its host monitor's resolution. The default is 0% (disabled). If no image qualifies on a monitor, that monitor stays black.
Windows uses `/p <window handle>` for the embedded preview; the preview does not hide the cursor or dismiss on input.

## Code organization

- `Program.cs`: command-line modes, Windows preview integration, and full-screen window lifetime.
- `AppSettings.cs`: JSON persistence and validation (image intervals are limited to 2–3600 seconds).
- `ImageLibrary.cs`: image discovery, skipping inaccessible directories and symbolic links/junctions.
- `SaverForm.cs`: image selection, scaling, exit input, and timer/image resource cleanup.
- `SettingsForm.cs`: settings editing, saving, and testing without closing the settings window.

Settings are stored in `%LOCALAPPDATA%\DualMonitorScreensaver\settings.json`. Invalid or unreadable JSON falls back to defaults.
Unreadable or corrupt images are skipped; if no replacement can be loaded, the current image remains displayed.

## Development checks

Run `dotnet build -c Release` after source changes. Then check Settings → Test on all monitors,
exit by keyboard and mouse, and verify the embedded preview in Windows Screen Saver Settings.
Try a folder containing both valid and corrupt images, and a folder with no supported images.

The root `.scr` files are prebuilt artifacts, not automatically updated by a build. Run the publish/copy commands above to use modified code.
