namespace DualMonitorScreensaver;

internal static class ImageLibrary
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff" };

    public static IEnumerable<string> FindImages(string folder, bool recursive)
    {
        if (!Directory.Exists(folder)) yield break;
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(folder, "*", option); }
        catch { yield break; }
        foreach (var file in files)
            if (Extensions.Contains(Path.GetExtension(file))) yield return file;
    }
}
