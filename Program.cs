using System.Runtime.InteropServices;

namespace DualMonitorScreensaver;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var command = args.Length == 0 ? "/c" : args[0].ToLowerInvariant().Trim();
        if (command.StartsWith("/c") || command.StartsWith("-c"))
        {
            using var settingsForm = new SettingsForm();
            Application.Run(settingsForm);
            return;
        }

        if (command.StartsWith("/p") || command.StartsWith("-p"))
        {
            var handleText = command[2..].TrimStart(':').Trim();
            if (handleText.Length == 0) handleText = args.ElementAtOrDefault(1);
            if (long.TryParse(handleText, out var value))
                RunPreview(new IntPtr(value));
            return;
        }

        if (command.StartsWith("/s") || command.StartsWith("-s"))
            RunFullScreen();
    }

    private static void RunFullScreen()
    {
        var settings = AppSettings.Load();
        var images = ImageLibrary.FindImages(settings.Folder, settings.IncludeSubfolders).ToArray();
        if (images.Length == 0)
        {
            MessageBox.Show("No supported images were found. Open Settings and select an image folder.",
                "Dual Monitor Screensaver", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var forms = Screen.AllScreens.Select(screen => new SaverForm(screen.Bounds, settings, images)).ToArray();
        try
        {
            foreach (var form in forms.Skip(1)) form.Show();
            Application.Run(forms[0]);
        }
        finally
        {
            foreach (var form in forms) form.Dispose();
        }
    }

    private static void RunPreview(IntPtr parent)
    {
        if (parent == IntPtr.Zero) return;
        if (!GetClientRect(parent, out var rect) || rect.Right <= 0 || rect.Bottom <= 0) return;
        var settings = AppSettings.Load();
        var images = ImageLibrary.FindImages(settings.Folder, settings.IncludeSubfolders).ToArray();
        using var form = new SaverForm(new Rectangle(0, 0, rect.Right, rect.Bottom), settings, images, true);
        // SetParent alone does not change a top-level window into a child window.
        form.TopLevel = false;
        SetParent(form.Handle, parent);
        form.Bounds = new Rectangle(0, 0, rect.Right, rect.Bottom);
        form.Show();
        Application.Run(form);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr child, IntPtr parent);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr handle, out NativeRect rect);
}
