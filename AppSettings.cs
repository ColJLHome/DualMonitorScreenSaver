using System.Text.Json;

namespace DualMonitorScreensaver;

internal enum ScaleMode { Fit, Width, Height }

internal sealed class AppSettings
{
    public const int MinimumIntervalSeconds = 2;
    public const int MaximumIntervalSeconds = 3600;

    public string Folder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    public List<string> Folders { get; set; } = new();
    public ScaleMode ScaleMode { get; set; } = ScaleMode.Fit;
    public int ChangeIntervalSeconds { get; set; } = 15;
    public bool IncludeSubfolders { get; set; } = true;
    public Dictionary<string, bool> FolderIncludeSubfolders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int MinimumImageResolutionPercent { get; set; } = 0;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DualMonitorScreensaver", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new();
                settings.Normalize();
                return settings;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A missing, unreadable, or damaged settings file must not prevent startup.
        }
        var defaults = new AppSettings();
        defaults.Normalize();
        return defaults;
    }

    private void Normalize()
    {
        // Settings can be edited outside the UI; validate before using them in a timer.
        Folder = string.IsNullOrWhiteSpace(Folder)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            : Folder.Trim();
        // Preserve the selected folder when upgrading settings that only stored Folder.
        Folders = (Folders ?? new List<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Append(Folder)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var folderOptions = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in FolderIncludeSubfolders ?? new Dictionary<string, bool>())
        {
            if (!string.IsNullOrWhiteSpace(option.Key)) folderOptions[option.Key.Trim()] = option.Value;
        }
        // Older settings used one value for every folder; keep that behavior on migration.
        FolderIncludeSubfolders = Folders.ToDictionary(path => path,
            path => folderOptions.TryGetValue(path, out var enabled) ? enabled : IncludeSubfolders,
            StringComparer.OrdinalIgnoreCase);
        IncludeSubfolders = FolderIncludeSubfolders[Folder];
        ChangeIntervalSeconds = Math.Clamp(ChangeIntervalSeconds, MinimumIntervalSeconds, MaximumIntervalSeconds);
        MinimumImageResolutionPercent = Math.Clamp(MinimumImageResolutionPercent, 0, 100);
        if (!Enum.IsDefined(ScaleMode)) ScaleMode = ScaleMode.Fit;
    }

    public void Save()
    {
        Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var temporaryPath = SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            // Replace only after serialization and writing succeed, preserving the previous settings on failure.
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, SettingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
