namespace ControlSaveManager;

/// <summary>
/// Stato dell'applicazione, salvato in stato.ini.
/// </summary>
public sealed class AppState
{
    private const string EmptyValue = "-";

    private readonly string path;

    public string ActiveProfile { get; set; } = "";
    public string ActiveSince { get; set; } = "";
    public string LastSaved { get; set; } = "";

    /// <summary>
    /// Quanti backup conservare (i più recenti). 0 = tutti.
    /// </summary>
    public int BackupsToKeep { get; set; } = DefaultBackupsToKeep;

    public const int DefaultBackupsToKeep = 20;

    public AppState(string path)
    {
        this.path = path;
    }

    public void Load()
    {
        ActiveProfile = "";
        ActiveSince = "";
        LastSaved = "";
        BackupsToKeep = DefaultBackupsToKeep;

        if (!File.Exists(path))
            return;

        foreach (string line in File.ReadAllLines(path))
        {
            string[] parts = line.Split('=', 2);

            if (parts.Length != 2)
                continue;

            string value = parts[1] == EmptyValue ? "" : parts[1];

            switch (parts[0])
            {
                case "ProfiloAttivo":
                    ActiveProfile = value;
                    break;

                case "AttivatoIl":
                    ActiveSince = value;
                    break;

                case "SalvatoIl":
                    LastSaved = value;
                    break;

                case "BackupDaConservare":
                    if (int.TryParse(value, out int keep) && keep >= 0)
                        BackupsToKeep = keep;
                    break;
            }
        }
    }

    /// <summary>
    /// Scrive lo stato su disco. Lancia un'eccezione in caso di errore.
    /// Scrive prima un file temporaneo e poi sostituisce stato.ini, così
    /// un'interruzione non lo lascia vuoto o a metà.
    /// </summary>
    public void Save()
    {
        string temp = path + ".tmp";

        using (FileStream stream = new(temp, FileMode.Create, FileAccess.Write))
        using (StreamWriter writer = new(stream))
        {
            writer.WriteLine("ProfiloAttivo=" + Format(ActiveProfile));
            writer.WriteLine("AttivatoIl=" + Format(ActiveSince));
            writer.WriteLine("SalvatoIl=" + Format(LastSaved));
            writer.WriteLine("BackupDaConservare=" + BackupsToKeep);

            writer.Flush();
            stream.Flush(flushToDisk: true);
        }

        File.Move(temp, path, overwrite: true);
    }

    private static string Format(string value)
    {
        return value == "" ? EmptyValue : value;
    }
}
