using System.Diagnostics;

namespace ControlSaveManager;

/// <summary>
/// Interfaccia da console. Tutta la logica è in SaveManager.
/// </summary>
internal class Program
{
    private static SaveManager manager = null!;

    static void Main()
    {
        Console.Title = "Control Save Manager";

        Result<SaveManager> init = SaveManager.Initialize();

        if (!init.Success)
        {
            Console.WriteLine(init.Message);
            Pause();
            return;
        }

        manager = init.Value!;

        while (true)
        {
            if (SaveManager.IsGameRunning())
            {
                Console.Clear();
                Console.WriteLine("Control è in esecuzione.");
                Console.WriteLine("Chiudi il gioco prima di continuare.");
                Pause();
                continue;
            }

            manager.ReloadState();
            ShowMenu();

            Console.Write("\nScelta: ");
            string? choice = Console.ReadLine();

            // Input chiuso: esce invece di ripetere il menu all'infinito.
            if (choice == null)
                return;

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

    // MENU

    private static void ShowMenu()
    {
        AppState state = manager.State;

        Console.Clear();

        Console.WriteLine("====================================");
        Console.WriteLine("    CONTROL - SAVE MANAGER");
        Console.WriteLine("====================================");
        Console.WriteLine();

        Console.WriteLine("Profilo attivo: " + (state.ActiveProfile == "" ? "Nessuno" : state.ActiveProfile));
        Console.WriteLine("Attivato il: " + (state.ActiveSince == "" ? "-" : state.ActiveSince));
        Console.WriteLine("Ultimo salvataggio: " + (state.LastSaved == "" ? "Mai" : state.LastSaved));

        Console.WriteLine();
        Console.WriteLine("1. Cambia profilo");
        Console.WriteLine("2. Salva i progressi attuali");
        Console.WriteLine("3. Crea un nuovo profilo");
        Console.WriteLine("4. Apri la cartella dei profili");
        Console.WriteLine("5. Esci");
    }

    // PROFILI

    private static void CreateProfile()
    {
        Console.Clear();
        Console.WriteLine("CREA UN NUOVO PROFILO");
        Console.WriteLine();

        Console.Write("Nome del profilo: ");
        string name = Console.ReadLine() ?? "";

        if (!SaveManager.IsValidName(name))
        {
            Console.WriteLine("Nome non valido.");
            Pause();
            return;
        }

        if (manager.ProfileExists(SaveManager.ProfileFolderName(name)))
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

        if (choice != "1" && choice != "2")
        {
            Console.WriteLine("Scelta non valida.");
            Pause();
            return;
        }

        Result result = manager.CreateProfile(name, copyCurrentSaves: choice == "1");
        Print(result);

        if (result.Success && Confirm("Vuoi attivarlo?"))
            ActivateProfile(SaveManager.ProfileFolderName(name));

        Pause();
    }

    private static void ChangeProfile()
    {
        Console.Clear();
        Console.WriteLine("CAMBIA PROFILO");
        Console.WriteLine();

        IReadOnlyList<string> profiles = manager.GetProfiles();

        if (profiles.Count == 0)
        {
            Console.WriteLine("Non ci sono profili.");
            Pause();
            return;
        }

        for (int i = 0; i < profiles.Count; i++)
        {
            string marker = profiles[i] == manager.State.ActiveProfile ? " [ATTIVO]" : "";

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

        ActivateProfile(profiles[choice - 1]);
        Pause();
    }

    private static void ActivateProfile(string profileName)
    {
        if (!ConfirmSteamClosed())
            return;

        Console.WriteLine("Creo il backup e attivo il profilo...");
        Print(manager.ActivateProfile(profileName));
    }

    // SALVATAGGIO DEI PROGRESSI

    private static void SaveProgress()
    {
        if (!manager.HasActiveProfile)
        {
            Console.WriteLine("Non c'è un profilo attivo valido.");
            Pause();
            return;
        }

        bool allowEmpty = false;

        if (manager.GameSavesAreEmpty())
        {
            Console.WriteLine("ATTENZIONE: la cartella dei salvataggi del gioco è vuota.");
            Console.WriteLine("Salvando, il profilo " + manager.State.ActiveProfile + " verrà svuotato.");

            if (!Confirm("Continuare comunque?"))
            {
                Pause();
                return;
            }

            allowEmpty = true;
        }

        if (!ConfirmSteamClosed())
        {
            Pause();
            return;
        }

        Console.WriteLine("Creo il backup e salvo i progressi...");
        Print(manager.SaveProgress(allowEmpty));
        Pause();
    }

    // UTILITÀ

    private static bool ConfirmSteamClosed()
    {
        if (!SaveManager.IsSteamRunning())
            return true;

        Console.WriteLine("ATTENZIONE: Steam è aperto.");
        Console.WriteLine("Se Steam Cloud è attivo per Control, può sovrascrivere i salvataggi.");

        return Confirm("Continuare comunque?");
    }

    private static void Print(Result result)
    {
        if (result.Critical)
            Console.WriteLine("ERRORE GRAVE");

        Console.WriteLine(result.Message);

        if (!result.Success && !result.Critical && result.BackupPath != null)
            Console.WriteLine("Backup: " + result.BackupPath);
    }

    private static bool Confirm(string question)
    {
        Console.Write(question + " (s/n): ");

        return (Console.ReadLine() ?? "").Trim()
            .Equals("s", StringComparison.OrdinalIgnoreCase);
    }

    private static void ShowProfilesFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = manager.ProfilesPath,
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
