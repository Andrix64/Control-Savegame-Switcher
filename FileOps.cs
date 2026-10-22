namespace ControlSaveManager;

/// <summary>
/// Copia e sincronizzazione di cartelle.
/// Entrambi i metodi lanciano un'eccezione al primo errore, anche nelle
/// sottocartelle, così un backup incompleto non viene mai considerato riuscito.
/// </summary>
internal static class FileOps
{
    // File di Steam Cloud: non va mai copiato né cancellato.
    private const string SteamCacheFile = "remotecache.vdf";

    public static void CopyFolder(string source, string destination)
    {
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException("Cartella non trovata: " + source);

        Directory.CreateDirectory(destination);

        foreach (string file in Directory.GetFiles(source))
        {
            string name = Path.GetFileName(file);

            if (name == SteamCacheFile)
                continue;

            File.Copy(file, Path.Combine(destination, name), true);
        }

        foreach (string dir in Directory.GetDirectories(source))
        {
            string name = Path.GetFileName(dir);

            if (IsExcluded(name))
                continue;

            CopyFolder(dir, Path.Combine(destination, name));
        }
    }

    /// <summary>
    /// Rende destination identica a source, cancellando ciò che manca in source.
    /// </summary>
    public static void MirrorFolder(string source, string destination)
    {
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException("Cartella non trovata: " + source);

        Directory.CreateDirectory(destination);

        // Elimina i file che non esistono più nella sorgente.
        foreach (string file in Directory.GetFiles(destination))
        {
            string name = Path.GetFileName(file);

            if (name == SteamCacheFile)
                continue;

            if (!File.Exists(Path.Combine(source, name)))
                File.Delete(file);
        }

        // Elimina le cartelle non presenti nella sorgente.
        foreach (string dir in Directory.GetDirectories(destination))
        {
            string name = Path.GetFileName(dir);

            if (IsExcluded(name))
                continue;

            if (!Directory.Exists(Path.Combine(source, name)))
                Directory.Delete(dir, true);
        }

        // Copia i file aggiornati.
        foreach (string file in Directory.GetFiles(source))
        {
            string name = Path.GetFileName(file);

            if (name == SteamCacheFile)
                continue;

            File.Copy(file, Path.Combine(destination, name), true);
        }

        // Sincronizza le sottocartelle.
        foreach (string dir in Directory.GetDirectories(source))
        {
            string name = Path.GetFileName(dir);

            if (IsExcluded(name))
                continue;

            MirrorFolder(dir, Path.Combine(destination, name));
        }
    }

    /// <summary>
    /// true se la cartella non contiene file di salvataggio (a qualsiasi livello).
    /// </summary>
    public static bool HasNoSaveFiles(string path)
    {
        if (!Directory.Exists(path))
            return true;

        return !Directory
            .EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Any(file => Path.GetFileName(file) != SteamCacheFile);
    }

    /// <summary>
    /// true se le due cartelle contengono gli stessi file con lo stesso contenuto,
    /// con le stesse esclusioni di CopyFolder e MirrorFolder.
    /// </summary>
    public static bool FoldersAreEqual(string first, string second)
    {
        if (!Directory.Exists(first) || !Directory.Exists(second))
            return Directory.Exists(first) == Directory.Exists(second);

        List<string> files = ListNames(Directory.GetFiles(first), name => name != SteamCacheFile);

        if (!files.SequenceEqual(
                ListNames(Directory.GetFiles(second), name => name != SteamCacheFile),
                StringComparer.OrdinalIgnoreCase))
            return false;

        foreach (string name in files)
        {
            if (!FilesAreEqual(Path.Combine(first, name), Path.Combine(second, name)))
                return false;
        }

        List<string> dirs = ListNames(Directory.GetDirectories(first), name => !IsExcluded(name));

        if (!dirs.SequenceEqual(
                ListNames(Directory.GetDirectories(second), name => !IsExcluded(name)),
                StringComparer.OrdinalIgnoreCase))
            return false;

        return dirs.All(name =>
            FoldersAreEqual(Path.Combine(first, name), Path.Combine(second, name)));
    }

    private static List<string> ListNames(string[] paths, Func<string, bool> include)
    {
        return paths
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(include)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool FilesAreEqual(string first, string second)
    {
        if (new FileInfo(first).Length != new FileInfo(second).Length)
            return false;

        using FileStream a = File.OpenRead(first);
        using FileStream b = File.OpenRead(second);

        byte[] bufferA = new byte[81920];
        byte[] bufferB = new byte[81920];

        while (true)
        {
            int readA = a.ReadAtLeast(bufferA, bufferA.Length, throwOnEndOfStream: false);
            int readB = b.ReadAtLeast(bufferB, bufferB.Length, throwOnEndOfStream: false);

            if (readA != readB ||
                !bufferA.AsSpan(0, readA).SequenceEqual(bufferB.AsSpan(0, readB)))
                return false;

            if (readA == 0)
                return true;
        }
    }

    private static bool IsExcluded(string name)
    {
        return name == "Backup" || name == "Profili" || name.StartsWith("Profilo_");
    }
}
