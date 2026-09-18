using System.Text.Json;

namespace DualMonitorScreensaver;

internal enum ScaleMode { Fit, Width, Height }

internal sealed class AppSettings
{
    public const int MinimumIntervalSeconds = 2;
    public const int MaximumIntervalSeconds = 3600;

    public string Folder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    public ScaleMode ScaleMode { get; set; } = ScaleMode.Fit;
    public int ChangeIntervalSeconds { get; set; } = 15;
    public bool IncludeSubfolders { get; set; } = true;

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
        return new();
    }

    private void Normalize()
    {
        // Settings can be edited outside the UI; validate before using them in a timer.
        Folder = string.IsNullOrWhiteSpace(Folder)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            : Folder.Trim();
        ChangeIntervalSeconds = Math.Clamp(ChangeIntervalSeconds, MinimumIntervalSeconds, MaximumIntervalSeconds);
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
