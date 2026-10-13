namespace DualMonitorScreensaver;

internal sealed class SaverForm : Form
{
    private readonly AppSettings settings;
    private readonly string[] images;
    private readonly System.Windows.Forms.Timer timer = new();
    private Image? current;
    private Point initialMouse;
    private bool cursorHidden;
    private readonly bool preview;
    private readonly Size screenResolution;

    public SaverForm(Rectangle bounds, AppSettings settings, string[] images, bool preview = false, Size? screenResolution = null)
    {
        this.settings = settings;
        this.images = images;
        this.preview = preview;
        this.screenResolution = screenResolution ?? bounds.Size;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        BackColor = Color.Black;
        DoubleBuffered = true;
        ShowInTaskbar = false;
        TopMost = !preview;
        timer.Interval = Math.Clamp(settings.ChangeIntervalSeconds,
            AppSettings.MinimumIntervalSeconds, AppSettings.MaximumIntervalSeconds) * 1000;
        timer.Tick += (_, _) => NextImage();
        KeyDown += (_, _) => ExitSaver();
        MouseDown += (_, _) => ExitSaver();
        MouseMove += (_, _) =>
        {
            if (preview) return;
            var position = Cursor.Position;
            if (Math.Abs(position.X - initialMouse.X) > 5 || Math.Abs(position.Y - initialMouse.Y) > 5)
                ExitSaver();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!preview)
        {
            initialMouse = Cursor.Position;
            Cursor.Hide();
            cursorHidden = true;
            Activate();
        }
        NextImage();
        timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            current?.Dispose();
            current = null;
            // Balance each full-screen window's Hide, including direct disposal without FormClosed.
            if (cursorHidden)
            {
                Cursor.Show();
                cursorHidden = false;
            }
        }
        base.Dispose(disposing);
    }

    private void ExitSaver()
    {
        if (preview) return;
        // Test mode shares the UI thread with Settings: close only full-screen saver windows.
        foreach (var form in Application.OpenForms.OfType<SaverForm>().Where(form => !form.preview).ToArray())
            form.Close();
    }

    private void NextImage()
    {
        if (images.Length == 0) return;
        var startIndex = Random.Shared.Next(images.Length);
        for (var offset = 0; offset < images.Length; offset++)
        {
            try
            {
                // Try every candidate once rather than repeatedly selecting the same broken file.
                var bytes = File.ReadAllBytes(images[(int)(((long)startIndex + offset) % images.Length)]);
                using var stream = new MemoryStream(bytes);
                using var loaded = Image.FromStream(stream);
                if (!ImageLibrary.MeetsMinimumResolution(loaded.Size, screenResolution,
                    settings.MinimumImageResolutionPercent)) continue;
                // Detach the displayed bitmap from the stream so files are never left locked.
                var replacement = new Bitmap(loaded);
                var old = current;
                current = replacement;
                old?.Dispose();
                Invalidate();
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or ArgumentException or System.Runtime.InteropServices.ExternalException or OutOfMemoryException)
            {
                // GDI+ also reports invalid/unsupported image data as OutOfMemoryException.
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (current is null || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        var sx = ClientSize.Width / (double)current.Width;
        var sy = ClientSize.Height / (double)current.Height;
        var factor = settings.ScaleMode switch { ScaleMode.Width => sx, ScaleMode.Height => sy, _ => Math.Min(sx, sy) };
        var width = Math.Max(1, (int)Math.Round(current.Width * factor));
        var height = Math.Max(1, (int)Math.Round(current.Height * factor));
        var destination = new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);
        e.Graphics.DrawImage(current, destination);
    }
}
