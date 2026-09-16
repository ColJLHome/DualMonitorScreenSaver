namespace DualMonitorScreensaver;

internal sealed class SettingsForm : Form
{
    private readonly TextBox folder = new() { Dock = DockStyle.Fill };
    private readonly ComboBox scale = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown interval = new() { Minimum = 2, Maximum = 3600, Dock = DockStyle.Left, Width = 100 };
    private readonly CheckBox recursive = new() { Text = "Include subfolders", AutoSize = true };

    public SettingsForm()
    {
        Text = "Dual Monitor Screensaver Settings";
        ClientSize = new Size(600, 235);
        MinimumSize = new Size(600, 274);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        scale.Items.AddRange(Enum.GetNames<ScaleMode>());
        var settings = AppSettings.Load();
        folder.Text = settings.Folder;
        scale.SelectedItem = settings.ScaleMode.ToString();
        interval.Value = Math.Clamp(settings.ChangeIntervalSeconds, 2, 3600);
        recursive.Checked = settings.IncludeSubfolders;

        var browse = new Button { Text = "Browse…", AutoSize = true };
        browse.Click += (_, _) => Browse();
        var ok = new Button { Text = "OK", AutoSize = true };
        ok.Click += (_, _) => SaveSettings();
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var preview = new Button { Text = "Test", AutoSize = true };
        preview.Click += (_, _) => Test();

        var folderRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderRow.Controls.Add(folder); folderRow.Controls.Add(browse);
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };
        buttons.Controls.Add(cancel); buttons.Controls.Add(ok); buttons.Controls.Add(preview);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Image folder:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(folderRow, 1, 0);
        layout.Controls.Add(new Label { Text = "Scaling:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1); layout.Controls.Add(scale, 1, 1);
        layout.Controls.Add(new Label { Text = "Change every (seconds):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2); layout.Controls.Add(interval, 1, 2);
        layout.Controls.Add(recursive, 1, 3); layout.Controls.Add(buttons, 0, 4); layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = ok; CancelButton = cancel;
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = folder.Text, UseDescriptionForTitle = true, Description = "Select the image folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
    }

    private AppSettings Current() => new()
    {
        Folder = folder.Text.Trim(), ScaleMode = Enum.Parse<ScaleMode>((string)scale.SelectedItem!),
        ChangeIntervalSeconds = (int)interval.Value, IncludeSubfolders = recursive.Checked
    };

    private void SaveSettings()
    {
        if (!Directory.Exists(folder.Text)) { MessageBox.Show(this, "Please select an existing folder."); return; }
        Current().Save(); DialogResult = DialogResult.OK; Close();
    }

    private void Test()
    {
        var s = Current();
        var images = ImageLibrary.FindImages(s.Folder, s.IncludeSubfolders).ToArray();
        if (images.Length == 0) { MessageBox.Show(this, "No supported images were found in that folder."); return; }
        var forms = Screen.AllScreens.Select(screen => new SaverForm(screen.Bounds, s, images)).ToArray();
        foreach (var f in forms) f.Show();
    }
}
