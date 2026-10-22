using Microsoft.Win32;
using System.Diagnostics;

namespace ControlSaveManager;

/// <summary>
/// Logica dei profili di salvataggio, senza input/output di console.
/// </summary>
public sealed class SaveManager
{
    private const string AppId = "870780";
    private const string ProfilePrefix = "Profilo_";
    private const string DateFormat = "dd/MM/yyyy HH:mm:ss";

    public string SavePath { get; }
    public string ProfilesPath { get; }
    public string BackupPath { get; }
    public AppState State { get; }

    /// <param name="savePath">Cartella dei salvataggi del gioco (…\870780).</param>
    /// <param name="basePath">Cartella dei dati dell'app (…\ControlSaves\IDAccount).</param>
    internal SaveManager(string savePath, string basePath)
    {
        SavePath = savePath;
        ProfilesPath = Path.Combine(basePath, "Profili");
        BackupPath = Path.Combine(basePath, "Backup");
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

    public bool ProfileExists(string profileName)
    {
        return Directory.Exists(Path.Combine(ProfilesPath, profileName));
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
    /// Crea un profilo. Non lo attiva: per farlo chiamare ActivateProfile.
    /// </summary>
    public Result CreateProfile(string name, bool copyCurrentSaves)
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
                return Result.Fail("Errore: " + ex.Message);
            }
        }

        string backup = NewBackupPath("Prima_creazione");

        try
        {
            FileOps.CopyFolder(SavePath, backup);
        }
        catch (Exception ex)
        {
            return Result.Fail("Backup non riuscito: " + ex.Message);
        }

        try
        {
            FileOps.CopyFolder(SavePath, profile);
        }
        catch (Exception ex)
        {
            return Result.Fail("Copia non riuscita: " + ex.Message, backup);
        }

        return Result.Ok("Profilo creato.", backup);
    }

    /// <summary>
    /// Sostituisce i salvataggi del gioco con quelli del profilo,
    /// dopo averne fatto un backup.
    /// </summary>
    public Result ActivateProfile(string profileName)
    {
        if (IsGameRunning())
            return Result.Fail("Chiudi Control prima di continuare.");

        string profile = Path.Combine(ProfilesPath, profileName);

        if (!Directory.Exists(profile))
            return Result.Fail("Profilo non trovato.");

        // Backup dei salvataggi prima della sostituzione.
        string backup = NewBackupPath("Prima_di_" + profileName);

        try
        {
            FileOps.CopyFolder(SavePath, backup);
        }
        catch (Exception ex)
        {
            return Result.Fail("Backup fallito. Cambio annullato.\n" + ex.Message);
        }

        try
        {
            FileOps.MirrorFolder(profile, SavePath);
        }
        catch (Exception ex)
        {
            try
            {
                FileOps.MirrorFolder(backup, SavePath);

                return Result.Fail(
                    "Errore durante l'attivazione: " + ex.Message +
                    "\nI salvataggi precedenti sono stati ripristinati.",
                    backup);
            }
            catch (Exception restoreEx)
            {
                return Result.Fatal(
                    "Errore durante l'attivazione: " + ex.Message +
                    "\nRipristino fallito: " + restoreEx.Message +
                    "\nNON avviare il gioco. Il backup è in:\n" + backup,
                    backup);
            }
        }

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
        string backup = NewBackupPath("Prima_salvataggio");

        try
        {
            FileOps.CopyFolder(profile, backup);
        }
        catch (Exception ex)
        {
            return Result.Fail("Backup fallito. Operazione annullata.\n" + ex.Message);
        }

        try
        {
            FileOps.MirrorFolder(SavePath, profile);
        }
        catch (Exception ex)
        {
            return Result.Fail("Salvataggio fallito: " + ex.Message, backup);
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

    private string NewBackupPath(string name)
    {
        return Path.Combine(BackupPath, name + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
    }

    // CONTROLLI

    public static bool IsGameRunning()
    {
        string[] processNames =
        {
            "Control_DX12",
            "Control_DX11",
            "Control"
        };

        return processNames.Any(IsProcessRunning);
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
