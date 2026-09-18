# Dual Monitor Screensaver

A Windows screensaver that displays a separately chosen random image on every connected monitor.

## Features

- Uses all connected monitors
- Random images from a chosen folder, optionally including subfolders
- `Fit`, `Width`, and `Height` scaling modes
- Configurable image-change interval
- Supports JPEG, PNG, BMP, GIF, and TIFF files

## Build and install

```powershell
dotnet publish -c Release
Copy-Item .\bin\Release\net8.0-windows\win-x64\publish\DualMonitorScreensaver.exe .\DualMonitorScreensaver.scr
```

Right-click `DualMonitorScreensaver.scr` and choose **Install**, or copy it to `C:\Windows\System32` and select it in Windows Screen Saver Settings.
The target computer needs the .NET 8 Desktop Runtime (x64).

During development, run without arguments (or with `/c`) for settings and use `/s` to start the screensaver.

The **Test** button uses the current, unsaved settings. Press a key, click, or move the mouse to exit the test and return to Settings.
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
