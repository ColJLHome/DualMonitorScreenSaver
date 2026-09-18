namespace DualMonitorScreensaver;

internal sealed class SettingsForm : Form
{
    private readonly TextBox folder = new() { Dock = DockStyle.Fill };
    private readonly ComboBox scale = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown interval = new()
    {
        Minimum = AppSettings.MinimumIntervalSeconds,
        Maximum = AppSettings.MaximumIntervalSeconds,
        Dock = DockStyle.Left,
        Width = 100
    };
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
        interval.Value = settings.ChangeIntervalSeconds;
        recursive.Checked = settings.IncludeSubfolders;

        var browse = new Button { Text = "Browse…", AutoSize = true };
        browse.Click += (_, _) => Browse();
        var ok = new Button { Text = "OK", AutoSize = true };
        ok.Click += (_, _) => SaveSettings();
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel, CausesValidation = false };
        // Settings runs with Application.Run, so DialogResult alone does not close it.
        cancel.Click += (_, _) => Close();
        var preview = new Button { Text = "Test", AutoSize = true };
        preview.Click += (_, _) => Test();

        var folderRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderRow.Controls.Add(folder);
        folderRow.Controls.Add(browse);
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        buttons.Controls.Add(preview);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Image folder:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(folderRow, 1, 0);
        layout.Controls.Add(new Label { Text = "Scaling:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(scale, 1, 1);
        layout.Controls.Add(new Label { Text = "Change every (seconds):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        layout.Controls.Add(interval, 1, 2);
        layout.Controls.Add(recursive, 1, 3);
        layout.Controls.Add(buttons, 0, 4);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = folder.Text, UseDescriptionForTitle = true, Description = "Select the image folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
    }

    private AppSettings ReadSettings() => new()
    {
        Folder = folder.Text.Trim(),
        ScaleMode = Enum.TryParse<ScaleMode>(scale.SelectedItem as string, out var selectedMode) ? selectedMode : ScaleMode.Fit,
        ChangeIntervalSeconds = (int)interval.Value,
        IncludeSubfolders = recursive.Checked
    };

    private void SaveSettings()
    {
        var settings = ReadSettings();
        if (!Directory.Exists(settings.Folder))
        {
            MessageBox.Show(this, "Please select an existing folder.");
            return;
        }
        try
        {
            settings.Save();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Could not save settings: {exception.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }

    private void Test()
    {
        if (Application.OpenForms.OfType<SaverForm>().Any()) return;
        var settings = ReadSettings();
        var images = ImageLibrary.FindImages(settings.Folder, settings.IncludeSubfolders).ToArray();
        if (images.Length == 0)
        {
            MessageBox.Show(this, "No supported images were found in that folder.");
            return;
        }
        foreach (var screen in Screen.AllScreens)
        {
            var form = new SaverForm(screen.Bounds, settings, images);
            form.Show(this);
        }
    }
}
