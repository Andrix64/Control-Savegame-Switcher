namespace ControlSaveManager;

/// <summary>
/// Copia e sincronizzazione di cartelle.
/// I metodi di copia lanciano un'eccezione al primo errore, anche nelle
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
    /// Copia source in destination e controlla che la copia coincida.
    /// </summary>
    public static void CopyFolderVerified(string source, string destination)
    {
        CopyFolder(source, destination);
        Verify(source, destination);
    }

    /// <summary>
    /// Rende destination identica a source in due passaggi: copia source in una
    /// cartella temporanea accanto a destination, la verifica e poi scambia le
    /// cartelle. Se qualcosa va storto prima dello scambio, destination resta
    /// com'era. Il file di Steam Cloud e le cartelle escluse di destination
    /// vengono conservati.
    /// </summary>
    public static void ReplaceFolder(string source, string destination)
    {
        string temp = destination + ".nuovo";
        string old = destination + ".vecchio";

        // Un'interruzione tra i due spostamenti lascia solo la cartella vecchia.
        if (!Directory.Exists(destination) && Directory.Exists(old))
            Directory.Move(old, destination);

        DeleteIfExists(temp);
        DeleteIfExists(old);

        try
        {
            CopyFolder(source, temp);

            if (Directory.Exists(destination))
                CopyExcluded(destination, temp);

            Verify(source, temp);

            if (!Directory.Exists(destination))
            {
                Directory.Move(temp, destination);
                return;
            }

            try
            {
                Directory.Move(destination, old);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Cartella in uso (per esempio da Steam): copia diretta dalla
                // copia già verificata.
                MirrorFolder(temp, destination);
                Verify(source, destination);
                return;
            }

            try
            {
                Directory.Move(temp, destination);
            }
            catch
            {
                Directory.Move(old, destination);
                throw;
            }

            TryDelete(old);
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private static void Verify(string source, string destination)
    {
        if (!FoldersAreEqual(source, destination))
            throw new IOException("La copia non coincide con l'originale: " + destination);
    }

    /// <summary>
    /// Copia nella destinazione il file di Steam Cloud e le cartelle escluse
    /// che si trovano al primo livello di source.
    /// </summary>
    private static void CopyExcluded(string source, string destination)
    {
        string cache = Path.Combine(source, SteamCacheFile);

        if (File.Exists(cache))
            File.Copy(cache, Path.Combine(destination, SteamCacheFile), true);

        foreach (string dir in Directory.GetDirectories(source))
        {
            string name = Path.GetFileName(dir);

            if (IsExcluded(name))
                CopyAll(dir, Path.Combine(destination, name));
        }
    }

    /// <summary>
    /// Copia tutto, senza esclusioni.
    /// </summary>
    private static void CopyAll(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);

        foreach (string dir in Directory.GetDirectories(source))
            CopyAll(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    private static void DeleteIfExists(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    private static void TryDelete(string path)
    {
        try
        {
            DeleteIfExists(path);
        }
        catch
        {
            // Resta una cartella temporanea: viene eliminata alla prossima operazione.
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
