namespace Lss;

/// <summary>
/// Cartella dei dati dell'app (profili, backup, savestate, registro):
/// %LOCALAPPDATA%\LSS, che OneDrive non sincronizza. Control Save Manager la
/// chiamava ControlSaveManager, e le versioni di sviluppo ancora prima
/// usavano Documenti\ControlSaves, spesso sincronizzata con OneDrive (file
/// bloccati, scaricati a richiesta, copie "in conflitto"): all'avvio quei dati
/// vengono portati qui.
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
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LSS");

    /// <summary>
    /// Cartella dei dati di Control Save Manager, prima versione con dati in %LOCALAPPDATA%.
    /// </summary>
    public static string ControlSaveManagerRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ControlSaveManager");

    public static string OldRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ControlSaves");

    /// <summary>
    /// Cartella in uso: DefaultRoot, oppure ControlSaveManagerRoot o OldRoot se lo
    /// spostamento non è riuscito.
    /// </summary>
    public static string Root { get; private set; } = DefaultRoot;

    /// <summary>
    /// Cartella da cui sono stati spostati i dati nell'ultimo MigrateToLss, per
    /// avvisare l'utente. Vuota se non c'era niente da spostare.
    /// </summary>
    public static string LastMoveSource { get; private set; } = "";

    /// <summary>
    /// Porta i dati di Control Save Manager in LSS senza perdere nulla. Da
    /// chiamare all'avvio, prima di scrivere nel registro.
    /// </summary>
    /// <returns>Esito e dettaglio (messaggio d'errore o avviso), anche vuoto.</returns>
    public static (MoveResult Result, string Detail) MigrateToLss()
    {
        (MoveResult result, string detail, string root, string source) =
            MigrateToLss(OldRoot, ControlSaveManagerRoot, DefaultRoot);

        Root = root;
        LastMoveSource = source;

        return (result, detail);
    }

    /// <summary>
    /// Porta i dati da documentsRoot a csmRoot e poi a lssRoot, e rimette a posto
    /// i dati di ogni account. Restituisce anche la cartella in uso (Root) e
    /// quella da cui sono stati spostati i dati (Source).
    /// </summary>
    internal static (MoveResult Result, string Detail, string Root, string Source) MigrateToLss(
        string documentsRoot, string csmRoot, string lssRoot)
    {
        (MoveResult first, string firstDetail) = Move(documentsRoot, csmRoot);

        // Se uno spostamento fallisce si resta nella vecchia cartella, ma con le
        // cartelle di ogni account già al posto nuovo (IDAccount\870780):
        // altrimenti l'app non vi troverebbe le partite.
        if (first == MoveResult.Failed)
        {
            RestructureAccounts(documentsRoot);
            return (MoveResult.Failed, firstDetail, documentsRoot, documentsRoot);
        }

        (MoveResult second, string secondDetail) = Move(csmRoot, lssRoot);

        if (second == MoveResult.Failed)
        {
            RestructureAccounts(csmRoot);
            return (MoveResult.Failed, secondDetail, csmRoot, csmRoot);
        }

        // Anche senza niente da spostare: completa uno spostamento interrotto.
        RestructureAccounts(lssRoot);

        // Con dati in tutte e due le cartelle quelli di Control Save Manager
        // restano fuori uso, anche se prima sono arrivati lì da Documenti.
        MoveResult result = second == MoveResult.BothExist ? MoveResult.BothExist
            : first == MoveResult.Moved || second == MoveResult.Moved ? MoveResult.Moved
            : second;

        string detail = string.Join(" ", new[] { firstDetail, secondDetail }.Where(d => d != ""));
        string source = second == MoveResult.NothingToMove ? documentsRoot : csmRoot;

        return (result, detail, lssRoot, source);
    }

    /// <summary>
    /// Cartelle dei dati di un account che in Control Save Manager stavano
    /// direttamente sotto IDAccount.
    /// </summary>
    private static readonly string[] AccountFolders = ["Profili", "Info", "Backup", "Savestate"];

    /// <summary>
    /// Sposta Profili, Info, Backup, Savestate e stato.ini di ogni account in
    /// IDAccount\870780. Si può ripetere: salta quello che è già a posto.
    /// </summary>
    internal static void RestructureAccounts(string root)
    {
        if (!Directory.Exists(root))
            return;

        foreach (string account in Directory.GetDirectories(root)
                     .Where(dir => SaveManager.IsNumeric(Path.GetFileName(dir))))
        {
            string game = Path.Combine(account, GameInfo.ControlAppId);

            foreach (string name in AccountFolders)
                MoveOwnedItem(Path.Combine(account, name), Path.Combine(game, name), isFolder: true);

            MoveOwnedItem(Path.Combine(account, "stato.ini"), Path.Combine(game, "stato.ini"), isFolder: false);
        }

        MigrateAccountFile(root);
    }

    /// <summary>
    /// Sposta una cartella o un file solo se a destinazione non c'è nulla.
    /// Un errore viene registrato e non ferma gli altri spostamenti.
    /// </summary>
    private static void MoveOwnedItem(string source, string destination, bool isFolder)
    {
        try
        {
            if (isFolder ? !Directory.Exists(source) : !File.Exists(source))
                return;

            if (Directory.Exists(destination) || File.Exists(destination))
            {
                Log.Write("Non spostato, esiste già: " + destination);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            if (isFolder)
                Directory.Move(source, destination);
            else
                File.Move(source, destination);

            Log.Write("Spostato " + Path.GetFileName(source) + " in " + Path.GetDirectoryName(destination));
        }
        catch (Exception ex)
        {
            Log.Write("Spostamento di " + source, ex);
        }
    }

    /// <summary>
    /// Trasforma account.txt (ultimo account usato) in lss.ini, se lss.ini non c'è.
    /// account.txt viene eliminato solo quando lss.ini è stato scritto.
    /// </summary>
    private static void MigrateAccountFile(string root)
    {
        string accountFile = Path.Combine(root, "account.txt");
        string settingsFile = Path.Combine(root, "lss.ini");

        try
        {
            if (!File.Exists(accountFile) || File.Exists(settingsFile))
                return;

            var settings = new AppSettings(settingsFile)
            {
                LastAccount = File.ReadAllText(accountFile).Trim(),
                LastGame = GameInfo.ControlAppId
            };

            settings.Save();

            if (File.Exists(settingsFile))
                File.Delete(accountFile);
        }
        catch (Exception ex)
        {
            Log.Write("Spostamento di " + accountFile, ex);
        }
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
