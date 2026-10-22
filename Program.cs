
using Microsoft.Win32;
using System.Diagnostics;

namespace ControlSaveManager;

internal class Program
{
    private static string savePath = "";
    private static string profilesPath = "";
    private static string backupPath = "";
    private static string statePath = "";

    private static string activeProfile = "";
    private static string activeSince = "";
    private static string lastSaved = "";

    static void Main()
    {
        Console.Title = "Control Save Manager";

        if (!Initialize())
        {
            Pause();
            return;
        }

        while (true)
        {
            if (IsGameRunning())
            {
                Console.Clear();
                Console.WriteLine("Control è in esecuzione.");
                Console.WriteLine("Chiudi il gioco prima di continuare.");
                Pause();
                continue;
            }

            LoadState();
            ShowMenu();

            Console.Write("\nScelta: ");
            string choice = Console.ReadLine() ?? "";

            switch (choice)
            {
                case "1":
                    ChangeProfile();
                    break;

                case "2":
                    SaveProgress();
                    break;

                case "3":
                    CreateProfile();
                    break;

                case "4":
                    ShowProfilesFolder();
                    break;

                case "5":
                    return;

                default:
                    Console.WriteLine("Scelta non valida.");
                    Pause();
                    break;
            }
        }
    }

    // INIZIALIZZAZIONE

    private static bool Initialize()
    {
        string steam = FindSteam();

        if (steam == "")
        {
            Console.WriteLine("Steam non trovato.");
            return false;
        }

        string userData = Path.Combine(steam, "userdata");

        if (!Directory.Exists(userData))
        {
            Console.WriteLine("Cartella userdata non trovata.");
            return false;
        }

        // Cerca un account Steam con i salvataggi di Control.
        string? account = Directory.GetDirectories(userData)
            .FirstOrDefault(dir =>
                Directory.Exists(Path.Combine(dir, "870780", "remote")));

        if (account == null)
        {
            Console.WriteLine("Salvataggi di Control non trovati.");
            Console.WriteLine("Avvia il gioco almeno una volta.");
            return false;
        }

        savePath = Path.Combine(account, "870780");

        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        string basePath = Path.Combine(documents, "ControlSaves", Path.GetFileName(account));

        profilesPath = Path.Combine(basePath, "Profili");
        backupPath = Path.Combine(basePath, "Backup");
        statePath = Path.Combine(basePath, "stato.ini");

        Directory.CreateDirectory(profilesPath);
        Directory.CreateDirectory(backupPath);

        LoadState();

        return true;
    }

    private static string FindSteam()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            string? path = key?.GetValue("SteamPath") as string;

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                return path;
            }
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

    // MENU

    private static void ShowMenu()
    {
        Console.Clear();

        Console.WriteLine("====================================");
        Console.WriteLine("    CONTROL - SAVE MANAGER");
        Console.WriteLine("====================================");
        Console.WriteLine();

        Console.WriteLine("Profilo attivo: " + (activeProfile == "" ? "Nessuno" : activeProfile));

        Console.WriteLine("Attivato il: " + (activeSince == "" ? "-" : activeSince));

        Console.WriteLine("Ultimo salvataggio: " + (lastSaved == "" ? "Mai" : lastSaved));

        Console.WriteLine();
        Console.WriteLine("1. Cambia profilo");
        Console.WriteLine("2. Salva i progressi attuali");
        Console.WriteLine("3. Crea un nuovo profilo");
        Console.WriteLine("4. Apri la cartella dei profili");
        Console.WriteLine("5. Esci");
    }

    // PROFILI

    private static List<string> GetProfiles()
    {
        return Directory.GetDirectories(profilesPath)
            .Select(Path.GetFileName)
            .Where(name =>
                name != null &&
                name.StartsWith("Profilo_") &&
                IsValidName(name[8..]))
            .Select(name => name!)
            .OrderBy(name => name)
            .ToList();
    }

    private static void CreateProfile()
    {
        Console.Clear();
        Console.WriteLine("CREA UN NUOVO PROFILO");
        Console.WriteLine();

        Console.Write("Nome del profilo: ");
        string name = Console.ReadLine() ?? "";

        if (!IsValidName(name))
        {
            Console.WriteLine("Nome non valido.");
            Pause();
            return;
        }

        string profile = Path.Combine(profilesPath, "Profilo_" + name);

        if (Directory.Exists(profile))
        {
            Console.WriteLine("Questo profilo esiste già.");
            Pause();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("1. Copia i salvataggi attuali");
        Console.WriteLine("2. Crea un profilo vuoto");
        Console.Write("Scelta: ");

        string choice = Console.ReadLine() ?? "";

        try
        {
            if (choice == "1")
            {
                string backup = NewBackupPath("Prima_creazione");

                if (!CopyFolder(savePath, backup))
                {
                    Console.WriteLine("Backup non riuscito.");
                    Pause();
                    return;
                }

                if (!CopyFolder(savePath, profile))
                {
                    Console.WriteLine("Copia non riuscita.");
                    Pause();
                    return;
                }

                Console.WriteLine("Profilo creato.");
            }
            else if (choice == "2")
            {
                Directory.CreateDirectory(Path.Combine(profile, "remote"));

                Console.WriteLine("Profilo vuoto creato.");
            }
            else
            {
                Console.WriteLine("Scelta non valida.");
                Pause();
                return;
            }

            Console.Write("Vuoi attivarlo? (s/n): ");

            if ((Console.ReadLine() ?? "").Trim()
                .Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                ChangeToProfile(Path.GetFileName(profile)!);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Errore: " + ex.Message);
        }

        Pause();
    }

    private static void ChangeProfile()
    {
        Console.Clear();
        Console.WriteLine("CAMBIA PROFILO");
        Console.WriteLine();

        List<string> profiles = GetProfiles();

        if (profiles.Count == 0)
        {
            Console.WriteLine("Non ci sono profili.");
            Pause();
            return;
        }

        for (int i = 0; i < profiles.Count; i++)
        {
            string marker = profiles[i] == activeProfile ? " [ATTIVO]" : "";

            Console.WriteLine($"{i + 1}. {profiles[i]}{marker}");
        }

        Console.WriteLine("0. Indietro");
        Console.Write("\nScegli un profilo: ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > profiles.Count)
        {
            Console.WriteLine("Scelta non valida.");
            Pause();
            return;
        }

        if (choice == 0)
            return;

        ChangeToProfile(profiles[choice - 1]);
        Pause();
    }

    private static void ChangeToProfile(string profileName)
    {
        if (IsGameRunning())
        {
            Console.WriteLine("Chiudi Control prima di continuare.");
            return;
        }

        string profile = Path.Combine(profilesPath, profileName);

        if (!Directory.Exists(profile))
        {
            Console.WriteLine("Profilo non trovato.");
            return;
        }

        // Backup dei salvataggi prima della sostituzione.
        string backup = NewBackupPath("Prima_di_" + profileName);

        Console.WriteLine("Creo il backup...");

        if (!CopyFolder(savePath, backup))
        {
            Console.WriteLine("Backup fallito. Cambio annullato.");
            return;
        }

        Console.WriteLine("Attivo il profilo...");

        if (!MirrorFolder(profile, savePath))
        {
            Console.WriteLine("Errore durante l'attivazione.");
            Console.WriteLine("Provo a ripristinare il backup...");

            if (MirrorFolder(backup, savePath))
                Console.WriteLine("Ripristino completato.");
            else
                Console.WriteLine("ATTENZIONE: ripristino fallito. " + "Non avviare il gioco.");

            return;
        }

        activeProfile = profileName;
        activeSince = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        lastSaved = "";

        SaveState();

        Console.WriteLine("Profilo attivato correttamente.");
    }

    // SALVATAGGIO DEI PROGRESSI

    private static void SaveProgress()
    {
        if (activeProfile == "" ||
            !Directory.Exists(Path.Combine(profilesPath, activeProfile)))
        {
            Console.WriteLine("Non c'è un profilo attivo valido.");
            Pause();
            return;
        }

        if (IsGameRunning())
        {
            Console.WriteLine("Chiudi Control prima di continuare.");
            Pause();
            return;
        }

        string profile = Path.Combine(profilesPath, activeProfile);
        string backup = NewBackupPath("Prima_salvataggio");

        Console.WriteLine("Creo il backup del profilo...");

        if (!CopyFolder(profile, backup))
        {
            Console.WriteLine("Backup fallito. Operazione annullata.");
            Pause();
            return;
        }

        Console.WriteLine("Salvo i progressi...");

        if (MirrorFolder(savePath, profile))
        {
            lastSaved = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            SaveState();

            Console.WriteLine("Progressi salvati nel profilo.");
        }
        else
        {
            Console.WriteLine("Salvataggio fallito.");
            Console.WriteLine("Backup disponibile in:");
            Console.WriteLine(backup);
        }

        Pause();
    }

    // FILE E CARTELLE

    private static bool CopyFolder(string source, string destination)
    {
        try
        {
            if (!Directory.Exists(source))
                return false;

            Directory.CreateDirectory(destination);

            foreach (string file in Directory.GetFiles(source))
            {
                if (Path.GetFileName(file) == "remotecache.vdf")
                    continue;

                File.Copy(
                    file,
                    Path.Combine(destination, Path.GetFileName(file)),
                    true);
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string name = Path.GetFileName(dir);

                if (IsExcluded(name))
                    continue;

                CopyFolder(dir, Path.Combine(destination, name));
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Errore di copia: " + ex.Message);
            return false;
        }
    }

    private static bool MirrorFolder(string source, string destination)
    {
        try
        {
            if (!Directory.Exists(source))
                return false;

            Directory.CreateDirectory(destination);

            // Elimina i file che non esistono più nella sorgente.
            foreach (string file in Directory.GetFiles(destination))
            {
                string name = Path.GetFileName(file);

                if (name == "remotecache.vdf")
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

                if (name == "remotecache.vdf")
                    continue;

                File.Copy(file, Path.Combine(destination, name), true);
            }

            // Sincronizza le sottocartelle.
            foreach (string dir in Directory.GetDirectories(source))
            {
                string name = Path.GetFileName(dir);

                if (IsExcluded(name))
                    continue;

                if (!MirrorFolder(dir, Path.Combine(destination, name)))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Errore di sincronizzazione: " + ex.Message);
            return false;
        }
    }

    private static bool IsExcluded(string name)
    {
        return name == "Backup" || name == "Profili" || name.StartsWith("Profilo_");
    }

    private static string NewBackupPath(string name)
    {
        return Path.Combine(backupPath, name + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
    }

    // STATO

    private static void LoadState()
    {
        activeProfile = "";
        activeSince = "";
        lastSaved = "";

        if (!File.Exists(statePath))
            return;

        foreach (string line in File.ReadAllLines(statePath))
        {
            string[] parts = line.Split('=', 2);

            if (parts.Length != 2)
                continue;

            switch (parts[0])
            {
                case "ProfiloAttivo":
                    activeProfile = parts[1] == "-" ? "" : parts[1];
                    break;

                case "AttivatoIl":
                    activeSince = parts[1] == "-" ? "" : parts[1];
                    break;

                case "SalvatoIl":
                    lastSaved = parts[1] == "-" ? "" : parts[1];
                    break;
            }
        }
    }

    private static void SaveState()
    {
        try
        {
            File.WriteAllLines(statePath, new[]
            {
                "ProfiloAttivo=" + (activeProfile == "" ? "-" : activeProfile),
                "AttivatoIl=" + (activeSince == "" ? "-" : activeSince),
                "SalvatoIl=" + (lastSaved == "" ? "-" : lastSaved)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Errore nel salvataggio dello stato: " + ex.Message);
        }
    }

    // CONTROLLI

    private static bool IsGameRunning()
    {
        string[] processNames =
        {
            "Control_DX12",
            "Control_DX11",
            "Control"
        };

        foreach (string name in processNames)
        {
            if (Process.GetProcessesByName(name).Length > 0)
                return true;
        }

        return false;
    }

    private static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > 50)
            return false;

        return name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-');
    }

    private static void ShowProfilesFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = profilesPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Impossibile aprire la cartella: " + ex.Message);
            Pause();
        }
    }

    private static void Pause()
    {
        Console.WriteLine();
        Console.Write("Premi INVIO per continuare...");
        Console.ReadLine();
    }
}
