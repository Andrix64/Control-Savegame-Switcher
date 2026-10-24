namespace Lss;

/// <summary>
/// Cartella dei dati dell'app (profili, backup, savestate, registro):
/// %LOCALAPPDATA%\ControlSaveManager, che OneDrive non sincronizza. Le versioni
/// di sviluppo precedenti usavano Documenti\ControlSaves, spesso sincronizzata
/// con OneDrive (file bloccati, scaricati a richiesta, copie "in conflitto"):
/// all'avvio quei dati vengono spostati qui.
/// </summary>
internal static class AppData
{
    public enum MoveResult
    {
        /// <summary>Nessuna vecchia cartella: niente da fare.</summary>
        NothingToMove,

        /// <summary>Dati spostati nella nuova cartella.</summary>
        Moved,

        /// <summary>Ci sono dati in tutte e due le cartelle: nessuna viene toccata.</summary>
        BothExist,

        /// <summary>Spostamento non riuscito: la vecchia cartella è intatta e resta in uso.</summary>
        Failed
    }

    public static string DefaultRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ControlSaveManager");

    public static string OldRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ControlSaves");

    /// <summary>
    /// Cartella in uso: DefaultRoot, oppure OldRoot se lo spostamento non è riuscito.
    /// </summary>
    public static string Root { get; private set; } = DefaultRoot;

    /// <summary>
    /// Sposta i dati da Documenti\ControlSaves, se ci sono. Da chiamare all'avvio,
    /// prima di scrivere nel registro.
    /// </summary>
    /// <returns>Esito e dettaglio (messaggio d'errore o avviso), anche vuoto.</returns>
    public static (MoveResult Result, string Detail) MoveFromDocuments()
    {
        (MoveResult result, string detail) = Move(OldRoot, DefaultRoot);

        if (result == MoveResult.Failed)
            Root = OldRoot;

        return (result, detail);
    }

    /// <summary>
    /// Sposta oldRoot in newRoot senza mai perdere dati: sullo stesso disco con
    /// un solo spostamento; su dischi diversi copiando in una cartella
    /// temporanea, verificando ogni file e solo dopo eliminando l'originale.
    /// </summary>
    internal static (MoveResult Result, string Detail) Move(string oldRoot, string newRoot)
    {
        if (!Directory.Exists(oldRoot))
            return (MoveResult.NothingToMove, "");

        if (Directory.Exists(newRoot) && Directory.EnumerateFileSystemEntries(newRoot).Any())
            return (MoveResult.BothExist, "");

        string temp = newRoot + ".spostamento";

        try
        {
            if (Directory.Exists(newRoot))
                Directory.Delete(newRoot);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(newRoot))!);

            if (SameVolume(oldRoot, newRoot))
            {
                Directory.Move(oldRoot, newRoot);
                return (MoveResult.Moved, "");
            }

            if (Directory.Exists(temp))
                Directory.Delete(temp, true);

            FileOps.CopyAllVerified(oldRoot, temp);
            Directory.Move(temp, newRoot);
        }
        catch (Exception ex)
        {
            try
            {
                if (Directory.Exists(temp))
                    Directory.Delete(temp, true);
            }
            catch
            {
                // Resta una copia temporanea; l'originale è comunque intatto.
            }

            return (MoveResult.Failed, ex.Message);
        }

        // I dati sono già al sicuro nella nuova cartella.
        try
        {
            Directory.Delete(oldRoot, true);
        }
        catch (Exception ex)
        {
            return (MoveResult.Moved, ex.Message);
        }

        return (MoveResult.Moved, "");
    }

    private static bool SameVolume(string first, string second)
    {
        return string.Equals(
            Path.GetPathRoot(Path.GetFullPath(first)),
            Path.GetPathRoot(Path.GetFullPath(second)),
            StringComparison.OrdinalIgnoreCase);
    }
}
