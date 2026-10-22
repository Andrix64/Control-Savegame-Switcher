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

    public AppState(string path)
    {
        this.path = path;
    }

    public void Load()
    {
        ActiveProfile = "";
        ActiveSince = "";
        LastSaved = "";

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
            }
        }
    }

    /// <summary>
    /// Scrive lo stato su disco. Lancia un'eccezione in caso di errore.
    /// </summary>
    public void Save()
    {
        File.WriteAllLines(path, new[]
        {
            "ProfiloAttivo=" + Format(ActiveProfile),
            "AttivatoIl=" + Format(ActiveSince),
            "SalvatoIl=" + Format(LastSaved)
        });
    }

    private static string Format(string value)
    {
        return value == "" ? EmptyValue : value;
    }
}
