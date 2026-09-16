using System.Text.Json;

namespace DualMonitorScreensaver;

internal enum ScaleMode { Fit, Width, Height }

internal sealed class AppSettings
{
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
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
