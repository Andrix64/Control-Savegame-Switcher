namespace Lss;

/// <summary>
/// IPlatform per Linux: cartelle di Steam (installazione normale, Flatpak,
/// Snap), registry.vdf, processi da /proc, xdg-open e istanza unica tramite
/// file di lock.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class LinuxPlatform : IPlatform
{
    /// <summary>
    /// Cartelle di Steam trovate nella cartella personale, la principale per
    /// prima. Lista vuota se Steam non è installato.
    /// </summary>
    public IReadOnlyList<string> FindSteamRoots() =>
        FindSteamRoots(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    /// <summary>
    /// Cartelle di Steam sotto <paramref name="home"/>, in ordine: normale,
    /// Flatpak, Snap. Solo quelle con userdata, senza doppioni dovuti ai
    /// collegamenti (~/.steam/steam punta a quella normale).
    /// </summary>
    internal static IReadOnlyList<string> FindSteamRoots(string home)
    {
        string[] candidates =
        {
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".steam", "root"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
            Path.Combine(home, "snap", "steam", "common", ".local", "share", "Steam")
        };

        var roots = new List<string>();

        foreach (string candidate in candidates)
        {
            if (!Directory.Exists(Path.Combine(candidate, "userdata")))
                continue;

            // Il collegamento va risolto fino alla cartella vera, per non contarla due volte.
            string? target = new DirectoryInfo(candidate).ResolveLinkTarget(returnFinalTarget: true)?.FullName;
            string root = Path.GetFullPath(target ?? candidate);

            if (!roots.Contains(root, StringComparer.Ordinal))
                roots.Add(root);
        }

        return roots;
    }

    public bool IsGameRunning(GameInfo game) => throw new NotImplementedException("Fase H, H2–H3.");

    public DateTime? GameStartTime(GameInfo game) => throw new NotImplementedException("Fase H, H3.");

    public bool IsSteamRunning() => throw new NotImplementedException("Fase H, H3.");

    public void OpenSteamUri(string uri) => throw new NotImplementedException("Fase H, H4.");

    public IDisposable? AcquireSingleInstance() => throw new NotImplementedException("Fase H, H4.");
}
