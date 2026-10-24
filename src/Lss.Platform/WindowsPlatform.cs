using Microsoft.Win32;
using System.Diagnostics;

namespace Lss;

/// <summary>
/// IPlatform per Windows: cartella di Steam dal registro, processi e istanza
/// unica tramite mutex.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class WindowsPlatform : IPlatform
{
    /// <summary>
    /// Cartella di Steam dal registro o dai percorsi standard. Lista vuota se
    /// Steam non è installato.
    /// </summary>
    public IReadOnlyList<string> FindSteamRoots()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            string? path = key?.GetValue("SteamPath") as string;

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                return [path];
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

        return paths.Where(Directory.Exists).Take(1).ToList();
    }

    public bool IsGameRunning()
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
                if (IsGameLauncher(process.MainModule?.FileName, windows))
                    found = true;
            }
            catch (Exception) when (!HasExited(process))
            {
                // Percorso non leggibile, per esempio perché il processo è avviato
                // come amministratore: nel dubbio si considera il gioco aperto.
                found = true;
            }
            catch
            {
                // Processo già chiuso.
            }
            finally
            {
                process.Dispose();
            }
        }

        return found;
    }

    /// <summary>
    /// true se l'eseguibile non è nella cartella di Windows. Un percorso
    /// sconosciuto (null) conta come gioco: meglio bloccare per errore che
    /// cambiare profilo con il gioco aperto.
    /// </summary>
    internal static bool IsGameLauncher(string? file, string windowsFolder)
    {
        return file == null ||
               !file.StartsWith(windowsFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch
        {
            // Nemmeno lo stato è leggibile: il processo esiste ancora.
            return false;
        }
    }

    /// <summary>
    /// Con Steam aperto, Steam Cloud può risincronizzare i salvataggi
    /// e annullare un cambio di profilo.
    /// </summary>
    public bool IsSteamRunning()
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

    /// <summary>
    /// Quando è partito il gioco in esecuzione, se si riesce a leggerlo.
    /// </summary>
    public DateTime? GameStartTime()
    {
        foreach (string name in new[] { "Control_DX12", "Control_DX11" })
        {
            Process[] processes = Process.GetProcessesByName(name);

            try
            {
                foreach (Process process in processes)
                {
                    try
                    {
                        return process.StartTime;
                    }
                    catch
                    {
                        // Processo non leggibile: si prova il prossimo.
                    }
                }
            }
            finally
            {
                foreach (Process process in processes)
                    process.Dispose();
            }
        }

        return null;
    }

    public void OpenSteamUri(string uri)
    {
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true })?.Dispose();
    }

    public IDisposable? AcquireSingleInstance()
    {
        // Il nome resta quello di Control Save Manager finché i dati sono nella
        // sua cartella: così la vecchia versione e LSS non possono essere aperte
        // insieme.
        var mutex = new Mutex(true, @"Local\ControlSaveManager", out bool first);

        if (first)
            return mutex;

        mutex.Dispose();
        return null;
    }
}
