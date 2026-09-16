namespace DualMonitorScreensaver;

internal sealed class SaverForm : Form
{
    private readonly AppSettings settings;
    private readonly string[] images;
    private readonly System.Windows.Forms.Timer timer = new();
    private Image? current;
    private Point? initialMouse;
    private readonly bool preview;
    private static readonly Random Random = new();

    public SaverForm(Rectangle bounds, AppSettings settings, string[] images, bool preview = false)
    {
        this.settings = settings; this.images = images; this.preview = preview;
        Bounds = bounds; BackColor = Color.Black; DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false; TopMost = !preview;
        Cursor.Hide();
        timer.Interval = settings.ChangeIntervalSeconds * 1000;
        timer.Tick += (_, _) => NextImage();
        Shown += (_, _) => { NextImage(); timer.Start(); Activate(); };
        FormClosed += (_, _) => { timer.Dispose(); current?.Dispose(); Cursor.Show(); };
        KeyDown += (_, _) => ExitSaver(); MouseDown += (_, _) => ExitSaver();
        MouseMove += (_, e) =>
        {
            if (preview) return;
            initialMouse ??= e.Location;
            if (Math.Abs(e.X - initialMouse.Value.X) > 5 || Math.Abs(e.Y - initialMouse.Value.Y) > 5) ExitSaver();
        };
    }

    private void ExitSaver()
    {
        if (preview) return;
        foreach (Form form in Application.OpenForms.Cast<Form>().ToArray()) form.Close();
    }

    private void NextImage()
    {
        if (images.Length == 0) return;
        for (var tries = 0; tries < images.Length; tries++)
        {
            try
            {
                var bytes = File.ReadAllBytes(images[Random.Next(images.Length)]);
                using var stream = new MemoryStream(bytes);
                using var loaded = Image.FromStream(stream);
                var replacement = new Bitmap(loaded);
                var old = current; current = replacement; old?.Dispose(); Invalidate(); return;
            }
            catch { }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (current is null) return;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        var sx = ClientSize.Width / (double)current.Width;
        var sy = ClientSize.Height / (double)current.Height;
        var factor = settings.ScaleMode switch { ScaleMode.Width => sx, ScaleMode.Height => sy, _ => Math.Min(sx, sy) };
        var width = (int)Math.Round(current.Width * factor); var height = (int)Math.Round(current.Height * factor);
        var destination = new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);
        e.Graphics.DrawImage(current, destination);
    }
}
