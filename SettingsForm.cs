namespace DualMonitorScreensaver;

internal sealed class SettingsForm : Form
{
    private readonly ComboBox folder = new() { DropDownStyle = ComboBoxStyle.DropDown, Dock = DockStyle.Fill };
    private readonly Button removeFolder = new() { Text = "Remove", AutoSize = true };
    private readonly ComboBox scale = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown interval = new()
    {
        Minimum = AppSettings.MinimumIntervalSeconds,
        Maximum = AppSettings.MaximumIntervalSeconds,
        Dock = DockStyle.Left,
        Width = 100
    };
    private readonly CheckBox recursive = new() { Text = "Include subfolders for this folder", AutoSize = true };
    private readonly Dictionary<string, bool> folderIncludeSubfolders = new(StringComparer.OrdinalIgnoreCase);
    private bool updatingFolderOption;
    private readonly NumericUpDown minimumResolution = new()
    {
        Minimum = 0,
        Maximum = 100,
        Dock = DockStyle.Left,
        Width = 100
    };

    public SettingsForm()
    {
        Text = "Dual Monitor Screensaver Settings";
        ClientSize = new Size(720, 355);
        MinimumSize = new Size(720, 394);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        scale.Items.AddRange(Enum.GetNames<ScaleMode>());
        var settings = AppSettings.Load();
        foreach (var option in settings.FolderIncludeSubfolders)
            folderIncludeSubfolders.Add(option.Key, option.Value);
        folder.Items.AddRange(settings.Folders.Cast<object>().ToArray());
        folder.SelectedItem = FindFolder(settings.Folder);
        folder.TextChanged += (_, _) =>
        {
            UpdateRemoveFolderButton();
            UpdateFolderOption();
        };
        recursive.CheckedChanged += (_, _) =>
        {
            if (!updatingFolderOption && !string.IsNullOrWhiteSpace(folder.Text))
                folderIncludeSubfolders[folder.Text.Trim()] = recursive.Checked;
        };
        removeFolder.Click += (_, _) => RemoveFolder();
        UpdateRemoveFolderButton();
        scale.SelectedItem = settings.ScaleMode.ToString();
        interval.Value = settings.ChangeIntervalSeconds;
        UpdateFolderOption();
        minimumResolution.Value = settings.MinimumImageResolutionPercent;

        var browse = new Button { Text = "Browse…", AutoSize = true };
        browse.Click += (_, _) => Browse();
        var ok = new Button { Text = "OK", AutoSize = true };
        ok.Click += (_, _) => SaveSettings();
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel, CausesValidation = false };
        // Settings runs with Application.Run, so DialogResult alone does not close it.
        cancel.Click += (_, _) => Close();
        var preview = new Button { Text = "Test", AutoSize = true };
        preview.Click += (_, _) => Test();

        var folderRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        folderRow.Controls.Add(folder);
        folderRow.Controls.Add(browse);
        folderRow.Controls.Add(removeFolder);
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
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
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
        layout.Controls.Add(new Label { Text = "Minimum image resolution (%):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        layout.Controls.Add(minimumResolution, 1, 3);
        var resolutionHelp = new Label
        {
            Text = "Percentage of each monitor's pixel count. 0 disables filtering.",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(resolutionHelp, 0, 4);
        layout.SetColumnSpan(resolutionHelp, 2);
        layout.Controls.Add(recursive, 1, 5);
        var folderHelp = new Label
        {
            Text = "Choose a saved folder, or browse/type a new one. OK saves the list and selection.",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(folderHelp, 0, 6);
        layout.SetColumnSpan(folderHelp, 2);
        layout.Controls.Add(buttons, 0, 7);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = folder.Text, UseDescriptionForTitle = true, Description = "Select the image folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var existing = FindFolder(dialog.SelectedPath);
            if (existing is null) folder.Items.Add(dialog.SelectedPath);
            folder.SelectedItem = existing ?? dialog.SelectedPath;
        }
    }

    private string? FindFolder(string path) => folder.Items.Cast<string>()
        .FirstOrDefault(item => string.Equals(item, path.Trim(), StringComparison.OrdinalIgnoreCase));

    private void UpdateRemoveFolderButton() => removeFolder.Enabled = FindFolder(folder.Text) is not null;

    private void UpdateFolderOption()
    {
        updatingFolderOption = true;
        try
        {
            var savedFolder = FindFolder(folder.Text);
            if (savedFolder is not null) folderIncludeSubfolders.TryAdd(savedFolder, true);
            recursive.Enabled = !string.IsNullOrWhiteSpace(folder.Text);
            recursive.Checked = !folderIncludeSubfolders.TryGetValue(folder.Text.Trim(), out var enabled) || enabled;
        }
        finally
        {
            updatingFolderOption = false;
        }
    }

    private void RemoveFolder()
    {
        var selected = FindFolder(folder.Text);
        if (selected is null) return;
        var index = folder.Items.IndexOf(selected);
        folder.Items.RemoveAt(index);
        if (folder.Items.Count > 0)
            folder.SelectedIndex = Math.Min(index, folder.Items.Count - 1);
        else
            folder.Text = string.Empty;
        folderIncludeSubfolders.Remove(selected);
        UpdateRemoveFolderButton();
    }

    private AppSettings ReadSettings()
    {
        if (!string.IsNullOrWhiteSpace(folder.Text))
            folderIncludeSubfolders[folder.Text.Trim()] = recursive.Checked;
        return new()
        {
            Folder = folder.Text.Trim(),
            Folders = folder.Items.Cast<string>().ToList(),
            ScaleMode = Enum.TryParse<ScaleMode>(scale.SelectedItem as string, out var selectedMode) ? selectedMode : ScaleMode.Fit,
            ChangeIntervalSeconds = (int)interval.Value,
            IncludeSubfolders = recursive.Checked,
            FolderIncludeSubfolders = new Dictionary<string, bool>(folderIncludeSubfolders, StringComparer.OrdinalIgnoreCase),
            MinimumImageResolutionPercent = (int)minimumResolution.Value
        };
    }

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
