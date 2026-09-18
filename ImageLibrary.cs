namespace DualMonitorScreensaver;

internal static class ImageLibrary
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff" };

    public static IEnumerable<string> FindImages(string folder, bool recursive)
    {
        if (!Directory.Exists(folder)) yield break;
        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            // Do not follow junctions or symbolic links back into an already visited tree.
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        var pendingFolders = new Stack<string>();
        pendingFolders.Push(folder);

        while (pendingFolders.TryPop(out var currentFolder))
        {
            string[] files;
            try
            {
                // Materialize inside the try: lazy enumeration can fail during MoveNext, not just creation.
                files = Directory.GetFiles(currentFolder, "*", options);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
                if (Extensions.Contains(Path.GetExtension(file))) yield return file;

            if (!recursive) continue;
            try
            {
                foreach (var subfolder in Directory.GetDirectories(currentFolder, "*", options))
                    pendingFolders.Push(subfolder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A removed or unavailable directory must not discard images from other folders.
            }
        }
    }
}
