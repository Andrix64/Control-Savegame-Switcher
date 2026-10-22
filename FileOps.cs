namespace ControlSaveManager;

/// <summary>
/// Copia e sincronizzazione di cartelle.
/// I metodi di copia lanciano un'eccezione al primo errore, anche nelle
/// sottocartelle, così un backup incompleto non viene mai considerato riuscito.
/// L'unica eccezione è il file di Steam Cloud al primo livello della cartella.
/// </summary>
internal static class FileOps
{
    // File di Steam Cloud, al primo livello della cartella del gioco: non va mai
    // copiato né cancellato. Un file con lo stesso nome più in basso è un file
    // come gli altri.
    private const string SteamCacheFile = "remotecache.vdf";

    // Cartelle accanto alla destinazione durante ReplaceFolder.
    public const string TempSuffix = ".nuovo";
    public const string OldSuffix = ".vecchio";

    public static void CopyFolder(string source, string destination)
    {
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException("Cartella non trovata: " + source);

        CopyFolder(source, destination, topLevel: true);
    }

    private static void CopyFolder(string source, string destination, bool topLevel)
    {
        Directory.CreateDirectory(destination);

        foreach (string file in Directory.GetFiles(source))
        {
            string name = Path.GetFileName(file);

            if (IsSteamCache(name, topLevel))
                continue;

            File.Copy(file, Path.Combine(destination, name), true);
        }

        foreach (string dir in Directory.GetDirectories(source))
            CopyFolder(dir, Path.Combine(destination, Path.GetFileName(dir)), topLevel: false);
    }

    /// <summary>
    /// Rende destination identica a source, cancellando ciò che manca in source.
    /// </summary>
    public static void MirrorFolder(string source, string destination)
    {
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException("Cartella non trovata: " + source);

        MirrorFolder(source, destination, topLevel: true);
    }

    private static void MirrorFolder(string source, string destination, bool topLevel)
    {
        Directory.CreateDirectory(destination);

        // Elimina i file che non esistono più nella sorgente.
        foreach (string file in Directory.GetFiles(destination))
        {
            string name = Path.GetFileName(file);

            if (IsSteamCache(name, topLevel))
                continue;

            if (!File.Exists(Path.Combine(source, name)))
                File.Delete(file);
        }

        // Elimina le cartelle non presenti nella sorgente.
        foreach (string dir in Directory.GetDirectories(destination))
        {
            if (!Directory.Exists(Path.Combine(source, Path.GetFileName(dir))))
                Directory.Delete(dir, true);
        }

        // Copia i file aggiornati.
        foreach (string file in Directory.GetFiles(source))
        {
            string name = Path.GetFileName(file);

            if (IsSteamCache(name, topLevel))
                continue;

            File.Copy(file, Path.Combine(destination, name), true);
        }

        // Sincronizza le sottocartelle.
        foreach (string dir in Directory.GetDirectories(source))
            MirrorFolder(dir, Path.Combine(destination, Path.GetFileName(dir)), topLevel: false);
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
    /// com'era. Il file di Steam Cloud di destination viene conservato.
    /// </summary>
    public static void ReplaceFolder(string source, string destination)
    {
        string temp = destination + TempSuffix;
        string old = destination + OldSuffix;

        RecoverInterruptedReplace(destination);

        DeleteIfExists(temp);
        DeleteIfExists(old);

        try
        {
            CopyFolder(source, temp);

            if (Directory.Exists(destination))
                CopySteamCache(destination, temp);

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

    /// <summary>
    /// Rimette a posto destination dopo una ReplaceFolder interrotta (per
    /// esempio da uno spegnimento). Se manca la cartella ma c'è la copia
    /// .vecchio, la riporta al suo posto: è lo stato di prima dell'operazione,
    /// di cui esiste anche il backup. Se la cartella c'è, elimina gli avanzi
    /// .nuovo e .vecchio.
    /// </summary>
    /// <returns>true se ha trovato e sistemato qualcosa.</returns>
    public static bool RecoverInterruptedReplace(string destination)
    {
        string temp = destination + TempSuffix;
        string old = destination + OldSuffix;
        bool recovered = false;

        // Un'interruzione tra i due spostamenti lascia solo la cartella vecchia.
        if (!Directory.Exists(destination) && Directory.Exists(old))
        {
            Directory.Move(old, destination);
            recovered = true;
        }

        // Senza la cartella, un .nuovo da solo è l'unica copia: si lascia stare.
        if (Directory.Exists(destination))
        {
            foreach (string leftover in new[] { temp, old })
            {
                if (Directory.Exists(leftover))
                {
                    TryDelete(leftover);
                    recovered = true;
                }
            }
        }

        return recovered;
    }

    private static void Verify(string source, string destination)
    {
        if (!FoldersAreEqual(source, destination))
            throw new IOException("La copia non coincide con l'originale: " + destination);
    }

    /// <summary>
    /// Copia nella destinazione il file di Steam Cloud di source, se c'è.
    /// </summary>
    private static void CopySteamCache(string source, string destination)
    {
        string cache = Path.Combine(source, SteamCacheFile);

        if (File.Exists(cache))
            File.Copy(cache, Path.Combine(destination, SteamCacheFile), true);
    }

    /// <summary>
    /// Copia tutto, senza esclusioni, e controlla byte per byte ogni file copiato.
    /// </summary>
    public static void CopyAllVerified(string source, string destination)
    {
        CopyAll(source, destination);

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string copy = Path.Combine(destination, Path.GetRelativePath(source, file));

            if (!File.Exists(copy) || !FilesAreEqual(file, copy))
                throw new IOException("La copia non coincide con l'originale: " + copy);
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

        string cache = Path.Combine(path, SteamCacheFile);

        return !Directory
            .EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Any(file => !string.Equals(file, cache, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// true se le due cartelle contengono gli stessi file con lo stesso contenuto,
    /// a parte il file di Steam Cloud al primo livello, come CopyFolder e MirrorFolder.
    /// </summary>
    public static bool FoldersAreEqual(string first, string second)
    {
        if (!Directory.Exists(first) || !Directory.Exists(second))
            return Directory.Exists(first) == Directory.Exists(second);

        return FoldersAreEqual(first, second, topLevel: true);
    }

    private static bool FoldersAreEqual(string first, string second, bool topLevel)
    {
        List<string> files = ListNames(Directory.GetFiles(first), name => !IsSteamCache(name, topLevel));

        if (!files.SequenceEqual(
                ListNames(Directory.GetFiles(second), name => !IsSteamCache(name, topLevel)),
                StringComparer.OrdinalIgnoreCase))
            return false;

        foreach (string name in files)
        {
            if (!FilesAreEqual(Path.Combine(first, name), Path.Combine(second, name)))
                return false;
        }

        List<string> dirs = ListNames(Directory.GetDirectories(first), _ => true);

        if (!dirs.SequenceEqual(
                ListNames(Directory.GetDirectories(second), _ => true),
                StringComparer.OrdinalIgnoreCase))
            return false;

        return dirs.All(name =>
            FoldersAreEqual(Path.Combine(first, name), Path.Combine(second, name), topLevel: false));
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

    private static bool IsSteamCache(string name, bool topLevel)
    {
        return topLevel && string.Equals(name, SteamCacheFile, StringComparison.OrdinalIgnoreCase);
    }
}
