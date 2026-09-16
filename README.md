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
