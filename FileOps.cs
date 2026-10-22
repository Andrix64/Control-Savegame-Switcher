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

    private static bool IsExcluded(string name)
    {
        return name == "Backup" || name == "Profili" || name.StartsWith("Profilo_");
    }
}
