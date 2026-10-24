namespace Lss;

/// <summary>
/// Legge le librerie, i manifest e i giochi di Steam senza modificare nulla.
/// </summary>
internal static class SteamLibrary
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// Librerie di Steam da steamapps/libraryfolders.vdf, la cartella di Steam
    /// per prima. Solo le cartelle che esistono, senza doppioni.
    /// </summary>
    public static IReadOnlyList<string> LibraryFolders(string steamRoot)
    {
        var result = new List<string> { steamRoot };
        string file = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");

        if (!File.Exists(file))
            return result;

        try
        {
            Vdf.Node root = Vdf.Parse(File.ReadAllText(file));

            if (root["libraryfolders"] is not Vdf.Node libraries)
                return result;

            foreach (object entry in libraries.Values)
            {
                if (entry is not Vdf.Node library || library.GetValueOrDefault("path") is not string path || path == "")
                    continue;

                if (Directory.Exists(path) && !Contains(result, path))
                    result.Add(path);
            }
        }
        catch (Exception ex)
        {
            Log.Write("Librerie di Steam non lette", ex);
        }

        return result;
    }

    /// <summary>
    /// Nome e cartella di installazione dal manifest del gioco, cercato in
    /// tutte le librerie. null se il gioco non è installato.
    /// </summary>
    public static (string Name, string InstallDir, string Library)? ReadManifest(
        IReadOnlyList<string> libraries, string appId)
    {
        foreach (string library in libraries)
        {
            string file = Path.Combine(library, "steamapps", "appmanifest_" + appId + ".acf");

            if (!File.Exists(file))
                continue;

            try
            {
                Vdf.Node root = Vdf.Parse(File.ReadAllText(file));
                string? name = Vdf.Find(root, "AppState", "name");

                if (!string.IsNullOrEmpty(name))
                    return (name, Vdf.Find(root, "AppState", "installdir") ?? "", library);
            }
            catch (Exception ex)
            {
                Log.Write("Manifest di Steam non letto: " + file, ex);
            }
        }

        return null;
    }

    private static bool Contains(List<string> paths, string path) =>
        paths.Any(existing => string.Equals(NormalizePath(existing), NormalizePath(path), PathComparison));

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
