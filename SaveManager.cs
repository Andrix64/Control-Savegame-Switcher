using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;

namespace ControlSaveManager;

/// <summary>
/// Copia dei salvataggi in una cartella con data: un backup o un savestate.
/// </summary>
/// <param name="Path">Cartella della copia.</param>
/// <param name="Date">Data di creazione, letta dal nome della cartella.</param>
/// <param name="Name">Nome del savestate (anche vuoto) o motivo del backup.</param>
public sealed record Snapshot(string Path, DateTime Date, string Name);

/// <summary>
/// Logica dei profili di salvataggio, senza input/output di console.
/// </summary>
public sealed class SaveManager
{
    private const string AppId = "870780";
    private const string ProfilePrefix = "Profilo_";
    private const string DateFormat = "dd/MM/yyyy HH:mm:ss";
    private const string BackupDateFormat = "yyyy-MM-dd_HH-mm-ss-fff";
    private const string SavestateDateFormat = "yyyy-MM-dd_HH-mm-ss";

    // Prefissi dei nomi dei backup, letti da BackupReasonText.
    private const string ActivateBackupPrefix = "Prima_di_";
    private const string SaveBackupPrefix = "Prima_salvataggio_";
    private const string RestoreBackupName = "Prima_ripristino";

    public string SavePath { get; }
    public string ProfilesPath { get; }
    public string BackupPath { get; }
    public string SavestatePath { get; }
    public AppState State { get; }

    /// <param name="savePath">Cartella dei salvataggi del gioco (…\870780).</param>
    /// <param name="basePath">Cartella dei dati dell'app (…\ControlSaves\IDAccount).</param>
    internal SaveManager(string savePath, string basePath)
    {
        SavePath = savePath;
        ProfilesPath = Path.Combine(basePath, "Profili");
        BackupPath = Path.Combine(basePath, "Backup");
        SavestatePath = Path.Combine(basePath, "Savestate");
        State = new AppState(Path.Combine(basePath, "stato.ini"));

        Directory.CreateDirectory(ProfilesPath);
        Directory.CreateDirectory(BackupPath);

        State.Load();
    }

    // INIZIALIZZAZIONE

    public static Result<SaveManager> Initialize()
    {
        string steam = FindSteam();

        if (steam == "")
            return Result<SaveManager>.Fail("Steam non trovato.");

        string userData = Path.Combine(steam, "userdata");

        if (!Directory.Exists(userData))
            return Result<SaveManager>.Fail("Cartella userdata non trovata.");

        // Cerca un account Steam con i salvataggi di Control.
        string? account = Directory.GetDirectories(userData)
            .FirstOrDefault(dir =>
                Directory.Exists(Path.Combine(dir, AppId, "remote")));

        if (account == null)
        {
            return Result<SaveManager>.Fail(
                "Salvataggi di Control non trovati.\nAvvia il gioco almeno una volta.");
        }

        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string basePath = Path.Combine(documents, "ControlSaves", Path.GetFileName(account));

        try
        {
            return Result<SaveManager>.Ok(
                new SaveManager(Path.Combine(account, AppId), basePath));
        }
        catch (Exception ex)
        {
            return Result<SaveManager>.Fail("Errore di inizializzazione: " + ex.Message);
        }
    }

    private static string FindSteam()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            string? path = key?.GetValue("SteamPath") as string;

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                return path;
        }
        catch
        {
            // Se il registro non è disponibile, prova i percorsi standard.
        }

        string[] paths =
        {
            @"C:\Program Files (x86)\Steam",
            @"C:\Program Files\Steam"
        };

        return paths.FirstOrDefault(Directory.Exists) ?? "";
    }

    public void ReloadState()
    {
        State.Load();
    }

    // PROFILI

    /// <summary>
    /// Nomi delle cartelle dei profili validi (es. "Profilo_Mario").
    /// </summary>
    public IReadOnlyList<string> GetProfiles()
    {
        return Directory.GetDirectories(ProfilesPath)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name =>
                name.StartsWith(ProfilePrefix) &&
                IsValidName(name[ProfilePrefix.Length..]))
            .OrderBy(name => name)
            .ToList();
    }

    public static string ProfileFolderName(string name)
    {
        return ProfilePrefix + name;
    }

    /// <summary>
    /// Nome da mostrare all'utente: "Profilo_Mario" diventa "Mario".
    /// </summary>
    public static string ProfileDisplayName(string profileName)
    {
        return profileName.StartsWith(ProfilePrefix)
            ? profileName[ProfilePrefix.Length..]
            : profileName;
    }

    public bool ProfileExists(string profileName)
    {
        return Directory.Exists(Path.Combine(ProfilesPath, profileName));
    }

    /// <summary>
    /// Data del file più recente nel profilo, o null se il profilo è vuoto.
    /// </summary>
    public DateTime? GetProfileLastModified(string profileName)
    {
        DateTime? latest = null;

        try
        {
            foreach (string file in Directory.EnumerateFiles(
                         Path.Combine(ProfilesPath, profileName), "*", SearchOption.AllDirectories))
            {
                DateTime time = File.GetLastWriteTime(file);

                if (latest == null || time > latest)
                    latest = time;
            }
        }
        catch
        {
            // Profilo non leggibile: nessuna data.
        }

        return latest;
    }

    public bool HasActiveProfile =>
        State.ActiveProfile != "" && ProfileExists(State.ActiveProfile);

    /// <summary>
    /// true se nella cartella del gioco non c'è nessun file di salvataggio.
    /// </summary>
    public bool GameSavesAreEmpty()
    {
        return FileOps.HasNoSaveFiles(SavePath);
    }

    /// <summary>
    /// true se i salvataggi del gioco sono diversi dal profilo attivo, cioè se
    /// cambiando profilo i progressi finirebbero solo nel backup.
    /// </summary>
    public bool HasUnsavedProgress()
    {
        if (!HasActiveProfile || GameSavesAreEmpty())
            return false;

        try
        {
            return !FileOps.FoldersAreEqual(
                SavePath,
                Path.Combine(ProfilesPath, State.ActiveProfile));
        }
        catch
        {
            // Nel dubbio meglio chiedere all'utente.
            return true;
        }
    }

    /// <summary>
    /// Crea un profilo. Non lo attiva: per farlo chiamare ActivateProfile.
    /// </summary>
    public Result CreateProfile(string name, bool copyCurrentSaves)
    {
        string operation = "Crea profilo " + ProfileFolderName(name) +
                           (copyCurrentSaves ? " (copia dei salvataggi)" : " (vuoto)");

        return Finish(operation, CreateProfileCore(name, copyCurrentSaves));
    }

    private Result CreateProfileCore(string name, bool copyCurrentSaves)
    {
        if (!IsValidName(name))
            return Result.Fail("Nome non valido.");

        string profileName = ProfileFolderName(name);
        string profile = Path.Combine(ProfilesPath, profileName);

        if (Directory.Exists(profile))
            return Result.Fail("Questo profilo esiste già.");

        if (!copyCurrentSaves)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(profile, "remote"));
                return Result.Ok("Profilo vuoto creato.");
            }
            catch (Exception ex)
            {
                TryDeleteFolder(profile);
                return Result.Fail("Errore: " + ex.Message);
            }
        }

        string backup = NewBackupPath("Prima_creazione");

        try
        {
            FileOps.CopyFolderVerified(SavePath, backup);
        }
        catch (Exception ex)
        {
            return Result.Fail("Backup non riuscito: " + ex.Message);
        }

        try
        {
            FileOps.CopyFolderVerified(SavePath, profile);
        }
        catch (Exception ex)
        {
            // Un profilo copiato a metà comparirebbe nella lista e bloccherebbe il nome.
            TryDeleteFolder(profile);
            return Result.Fail("Copia non riuscita: " + ex.Message, backup);
        }

        return Result.Ok("Profilo creato.", backup);
    }

    private static void TryDeleteFolder(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
            // Se non si riesce a eliminarla, resta la cartella parziale.
        }
    }

    /// <summary>
    /// Sostituisce i salvataggi del gioco con quelli del profilo,
    /// dopo averne fatto un backup.
    /// </summary>
    public Result ActivateProfile(string profileName)
    {
        return Finish("Attiva " + profileName, ActivateProfileCore(profileName));
    }

    private Result ActivateProfileCore(string profileName)
    {
        if (IsGameRunning())
            return Result.Fail("Chiudi Control prima di continuare.");

        string profile = Path.Combine(ProfilesPath, profileName);

        if (!Directory.Exists(profile))
            return Result.Fail("Profilo non trovato.");

        Result replaced = ReplaceGameSaves(profile, ActivateBackupPrefix + profileName);

        if (!replaced.Success)
            return replaced;

        string backup = replaced.BackupPath!;

        State.ActiveProfile = profileName;
        State.ActiveSince = DateTime.Now.ToString(DateFormat);
        State.LastSaved = "";

        try
        {
            State.Save();
        }
        catch (Exception ex)
        {
            return Result.Fail(
                "Profilo attivato, ma lo stato non è stato salvato: " + ex.Message,
                backup);
        }

        return Result.Ok("Profilo attivato correttamente.", backup);
    }

    // SALVATAGGIO DEI PROGRESSI

    /// <summary>
    /// Copia i salvataggi del gioco nel profilo attivo, dopo averne fatto un backup.
    /// </summary>
    /// <param name="allowEmptySaves">
    /// Se false, l'operazione viene rifiutata quando la cartella del gioco non
    /// contiene salvataggi, per non svuotare il profilo per errore.
    /// </param>
    public Result SaveProgress(bool allowEmptySaves = false)
    {
        return Finish("Salva progressi in " + State.ActiveProfile,
            SaveProgressCore(allowEmptySaves));
    }

    private Result SaveProgressCore(bool allowEmptySaves)
    {
        if (!HasActiveProfile)
            return Result.Fail("Non c'è un profilo attivo valido.");

        if (IsGameRunning())
            return Result.Fail("Chiudi Control prima di continuare.");

        if (!allowEmptySaves && GameSavesAreEmpty())
        {
            return Result.Fail(
                "La cartella dei salvataggi del gioco è vuota.\n" +
                "Salvando, il profilo verrebbe svuotato.");
        }

        string profile = Path.Combine(ProfilesPath, State.ActiveProfile);
        // Il nome dice di quale profilo è il backup.
        string backup = NewBackupPath(SaveBackupPrefix + State.ActiveProfile);

        try
        {
            FileOps.CopyFolderVerified(profile, backup);
        }
        catch (Exception ex)
        {
            return Result.Fail("Backup fallito. Operazione annullata.\n" + ex.Message);
        }

        try
        {
            FileOps.ReplaceFolder(SavePath, profile);
        }
        catch (Exception ex)
        {
            try
            {
                FileOps.ReplaceFolder(backup, profile);

                return Result.Fail(
                    "Salvataggio fallito: " + ex.Message +
                    "\nIl profilo è stato ripristinato.",
                    backup);
            }
            catch (Exception restoreEx)
            {
                return Result.Fail(
                    "Salvataggio fallito: " + ex.Message +
                    "\nRipristino del profilo fallito: " + restoreEx.Message +
                    "\nIl profilo precedente è nel backup.",
                    backup);
            }
        }

        State.LastSaved = DateTime.Now.ToString(DateFormat);

        try
        {
            State.Save();
        }
        catch (Exception ex)
        {
            return Result.Fail(
                "Progressi salvati, ma lo stato non è stato salvato: " + ex.Message,
                backup);
        }

        return Result.Ok("Progressi salvati nel profilo.", backup);
    }

    /// <summary>
    /// Fa il backup dei salvataggi del gioco e li sostituisce con source.
    /// Se la sostituzione fallisce, ripristina il backup.
    /// </summary>
    private Result ReplaceGameSaves(string source, string backupName)
    {
        string backup = NewBackupPath(backupName);

        try
        {
            FileOps.CopyFolderVerified(SavePath, backup);
        }
        catch (Exception ex)
        {
            return Result.Fail("Backup fallito. Operazione annullata.\n" + ex.Message);
        }

        try
        {
            FileOps.ReplaceFolder(source, SavePath);
        }
        catch (Exception ex)
        {
            try
            {
                FileOps.ReplaceFolder(backup, SavePath);

                return Result.Fail(
                    "Errore durante la sostituzione dei salvataggi: " + ex.Message +
                    "\nI salvataggi precedenti sono stati ripristinati.",
                    backup);
            }
            catch (Exception restoreEx)
            {
                return Result.Fatal(
                    "Errore durante la sostituzione dei salvataggi: " + ex.Message +
                    "\nRipristino fallito: " + restoreEx.Message +
                    "\nNON avviare il gioco. Il backup è in:\n" + backup,
                    backup);
            }
        }

        return Result.Ok("", backup);
    }

    /// <summary>
    /// Chiude un'operazione: elimina i backup in eccesso e scrive il registro.
    /// </summary>
    private Result Finish(string operation, Result result)
    {
        PruneBackups();
        return Log.Write(operation, result);
    }

    // SAVESTATE

    /// <summary>
    /// Cartella dei savestate del profilo attivo, o "" se non c'è un profilo attivo.
    /// </summary>
    public string ActiveSavestatePath => HasActiveProfile
        ? Path.Combine(SavestatePath, State.ActiveProfile)
        : "";

    /// <summary>
    /// Savestate del profilo attivo, dal più recente.
    /// </summary>
    public IReadOnlyList<Snapshot> GetSavestates()
    {
        string folder = ActiveSavestatePath;

        if (folder == "" || !Directory.Exists(folder))
            return [];

        var list = new List<Snapshot>();

        foreach (string dir in Directory.GetDirectories(folder))
        {
            string name = Path.GetFileName(dir);

            if (name.Length < SavestateDateFormat.Length ||
                !DateTime.TryParseExact(name[..SavestateDateFormat.Length], SavestateDateFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                continue;

            string label = name.Length > SavestateDateFormat.Length + 1
                ? name[(SavestateDateFormat.Length + 1)..]
                : "";

            list.Add(new Snapshot(dir, date, label));
        }

        return list.OrderByDescending(s => s.Date).ToList();
    }

    /// <summary>
    /// Nome facoltativo di un savestate: lettere, cifre, spazi, _ e -.
    /// </summary>
    public static bool IsValidSavestateName(string name)
    {
        if (name == "")
            return true;

        if (name.Length > 50 || name != name.Trim())
            return false;

        return name.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-');
    }

    /// <summary>
    /// Copia i salvataggi attuali del gioco in un nuovo savestate del profilo
    /// attivo, senza toccare né il gioco né il profilo.
    /// </summary>
    public Result CreateSavestate(string name)
    {
        return Finish($"Crea savestate \"{name}\" di {State.ActiveProfile}",
            CreateSavestateCore(name.Trim()));
    }

    private Result CreateSavestateCore(string name)
    {
        if (!HasActiveProfile)
            return Result.Fail("Non c'è un profilo attivo valido.");

        if (!IsValidSavestateName(name))
            return Result.Fail("Nome non valido.");

        if (IsGameRunning())
            return Result.Fail("Chiudi Control prima di continuare.");

        if (GameSavesAreEmpty())
            return Result.Fail("La cartella dei salvataggi del gioco è vuota.");

        string folder = DateTime.Now.ToString(SavestateDateFormat) + (name == "" ? "" : "_" + name);
        string path = Path.Combine(ActiveSavestatePath, folder);

        if (Directory.Exists(path))
            return Result.Fail("Esiste già un savestate con questo nome e questa data.");

        try
        {
            FileOps.CopyFolderVerified(SavePath, path);
        }
        catch (Exception ex)
        {
            TryDeleteFolder(path);
            return Result.Fail("Savestate non creato: " + ex.Message);
        }

        return Result.Ok(name == "" ? "Savestate creato." : $"Savestate \"{name}\" creato.");
    }

    /// <summary>
    /// Rimette un savestate nella cartella del gioco, dopo un backup.
    /// Il profilo attivo non cambia.
    /// </summary>
    public Result RestoreSavestate(Snapshot savestate)
    {
        return Finish("Ripristina savestate " + savestate.Path,
            RestoreToGame(savestate, SavestatePath, "Savestate ripristinato."));
    }

    public Result DeleteSavestate(Snapshot savestate)
    {
        return Log.Write("Elimina savestate " + savestate.Path, DeleteSavestateCore(savestate));
    }

    private Result DeleteSavestateCore(Snapshot savestate)
    {
        if (!IsInside(savestate.Path, SavestatePath))
            return Result.Fail("Savestate non valido.");

        try
        {
            Directory.Delete(savestate.Path, true);
        }
        catch (Exception ex)
        {
            return Result.Fail("Savestate non eliminato: " + ex.Message);
        }

        return Result.Ok("Savestate eliminato.");
    }

    // BACKUP

    private string NewBackupPath(string name)
    {
        return Path.Combine(BackupPath, name + "_" + DateTime.Now.ToString(BackupDateFormat));
    }

    /// <summary>
    /// Backup nella cartella Backup, dal più recente. Name è il motivo
    /// (es. "Prima_di_Profilo_Mario"), vedi BackupReasonText.
    /// </summary>
    public IReadOnlyList<Snapshot> GetBackups()
    {
        var list = new List<Snapshot>();

        foreach (string dir in Directory.GetDirectories(BackupPath))
        {
            string name = Path.GetFileName(dir);
            int dateStart = name.Length - BackupDateFormat.Length;

            if (dateStart < 2 || name[dateStart - 1] != '_' ||
                !DateTime.TryParseExact(name[dateStart..], BackupDateFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                continue;

            list.Add(new Snapshot(dir, date, name[..(dateStart - 1)]));
        }

        return list.OrderByDescending(b => b.Date).ToList();
    }

    /// <summary>
    /// Descrizione leggibile del motivo di un backup.
    /// </summary>
    public static string BackupReasonText(string reason)
    {
        if (reason.StartsWith(ActivateBackupPrefix))
            return "Prima di attivare " + ProfileDisplayName(reason[ActivateBackupPrefix.Length..]);

        if (reason.StartsWith(SaveBackupPrefix))
            return "Profilo " + ProfileDisplayName(reason[SaveBackupPrefix.Length..]) + " prima del salvataggio";

        return reason switch
        {
            "Prima_salvataggio" => "Profilo prima del salvataggio",
            "Prima_creazione" => "Prima di creare un profilo",
            RestoreBackupName => "Prima di un ripristino",
            _ => reason.Replace('_', ' ')
        };
    }

    /// <summary>
    /// Rimette un backup nella cartella del gioco, dopo un nuovo backup.
    /// Il profilo attivo non cambia.
    /// </summary>
    public Result RestoreBackup(Snapshot backup)
    {
        return Finish("Ripristina backup " + backup.Path,
            RestoreToGame(backup, BackupPath, "Backup ripristinato."));
    }

    private Result RestoreToGame(Snapshot snapshot, string root, string message)
    {
        if (IsGameRunning())
            return Result.Fail("Chiudi Control prima di continuare.");

        if (!IsInside(snapshot.Path, root) || !Directory.Exists(snapshot.Path))
            return Result.Fail("Cartella non trovata:\n" + snapshot.Path);

        // La pulizia dei backup avviene solo in Finish, a ripristino concluso:
        // il backup da ripristinare non può sparire durante la copia.
        Result replaced = ReplaceGameSaves(snapshot.Path, RestoreBackupName);

        return replaced.Success ? Result.Ok(message, replaced.BackupPath) : replaced;
    }

    /// <summary>
    /// Imposta quanti backup conservare ed elimina subito quelli in eccesso.
    /// </summary>
    public Result SetBackupsToKeep(int count)
    {
        State.BackupsToKeep = Math.Max(0, count);

        try
        {
            State.Save();
        }
        catch (Exception ex)
        {
            return Result.Fail("Impostazione non salvata: " + ex.Message);
        }

        int deleted = PruneBackups();

        return Result.Ok(deleted == 0
            ? "Impostazione salvata."
            : $"Impostazione salvata. Backup eliminati: {deleted}.");
    }

    /// <summary>
    /// Elimina i backup più vecchi oltre il numero da conservare.
    /// Restituisce quanti ne ha eliminati.
    /// </summary>
    private int PruneBackups()
    {
        if (State.BackupsToKeep <= 0)
            return 0;

        int deleted = 0;

        try
        {
            foreach (Snapshot backup in GetBackups().Skip(State.BackupsToKeep))
            {
                try
                {
                    Directory.Delete(backup.Path, true);
                    deleted++;
                }
                catch (Exception ex)
                {
                    Log.Write("Backup non eliminato " + backup.Path, ex);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Write("Pulizia dei backup", ex);
        }

        if (deleted > 0)
            Log.Write($"Pulizia: eliminati {deleted} backup vecchi");

        return deleted;
    }

    private static bool IsInside(string path, string root)
    {
        string full = Path.GetFullPath(path);
        string parent = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return full.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
    }

    // CONTROLLI

    public static bool IsGameRunning()
    {
        return IsProcessRunning("Control_DX12") ||
               IsProcessRunning("Control_DX11") ||
               IsGameLauncherRunning();
    }

    /// <summary>
    /// Il launcher del gioco si chiama Control.exe, come il Pannello di
    /// controllo di Windows: si scartano gli eseguibili nella cartella di Windows.
    /// </summary>
    private static bool IsGameLauncherRunning()
    {
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        bool found = false;

        foreach (Process process in Process.GetProcessesByName("Control"))
        {
            try
            {
                string? file = process.MainModule?.FileName;

                if (file != null &&
                    !file.StartsWith(windows + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                }
            }
            catch
            {
                // Processo non accessibile (di sistema o già chiuso): non è il gioco.
            }
            finally
            {
                process.Dispose();
            }
        }

        return found;
    }

    /// <summary>
    /// Con Steam aperto, Steam Cloud può risincronizzare i salvataggi
    /// e annullare un cambio di profilo.
    /// </summary>
    public static bool IsSteamRunning()
    {
        return IsProcessRunning("steam");
    }

    private static bool IsProcessRunning(string name)
    {
        Process[] processes = Process.GetProcessesByName(name);

        foreach (Process process in processes)
            process.Dispose();

        return processes.Length > 0;
    }

    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            return false;

        return name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-');
    }
}
