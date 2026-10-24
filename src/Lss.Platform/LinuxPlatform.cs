namespace Lss;

/// <summary>
/// IPlatform per Linux: cartelle di Steam (installazione normale, Flatpak,
/// Snap), registry.vdf, processi da /proc, xdg-open e istanza unica tramite
/// file di lock. Niente attributo SupportedOSPlatform: le funzioni pure
/// vanno testate su ogni sistema (CA1416 nei test); la classe si crea solo
/// su Linux, in Program.
/// </summary>
public sealed class LinuxPlatform : IPlatform
{
    /// <summary>
    /// Cartelle di Steam trovate nella cartella personale, la principale per
    /// prima. Lista vuota se Steam non è installato.
    /// </summary>
    public IReadOnlyList<string> FindSteamRoots() => FindSteamRoots(Home);

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

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

    /// <summary>
    /// true se un registry.vdf di Steam segna il gioco come aperto. Il
    /// controllo sui processi si aggiunge in H3.
    /// </summary>
    public bool IsGameRunning(GameInfo game) =>
        RegistryFiles(Home).Any(file => RegistryFileSaysRunning(file, game.AppId));

    /// <summary>
    /// registry.vdf delle installazioni di Steam sotto <paramref name="home"/>
    /// (normale, Flatpak, Snap) che esistono.
    /// </summary>
    internal static IReadOnlyList<string> RegistryFiles(string home)
    {
        string[] paths =
        {
            Path.Combine(home, ".steam", "registry.vdf"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".steam", "registry.vdf"),
            Path.Combine(home, "snap", "steam", "common", ".steam", "registry.vdf")
        };

        return paths.Where(File.Exists).ToList();
    }

    /// <summary>
    /// true se il registry.vdf dice che il gioco è aperto: RunningAppID per
    /// quello in primo piano, Apps\ID\Running per tutti. Steam lo scrive
    /// mentre gira, quindi un errore di lettura vuol dire "non aperto".
    /// </summary>
    internal static bool RegistrySaysRunning(string registryText, string appId)
    {
        try
        {
            Vdf.Node node = Vdf.Parse(registryText);

            return Vdf.Find(node, "Registry", "HKCU", "Software", "Valve", "Steam", "RunningAppID") == appId
                || Vdf.Find(node, "Registry", "HKCU", "Software", "Valve", "Steam", "Apps", appId, "Running") == "1";
        }
        catch
        {
            return false;
        }
    }

    private static bool RegistryFileSaysRunning(string file, string appId)
    {
        try
        {
            // Steam può starlo scrivendo nello stesso momento.
            return RegistrySaysRunning(File.ReadAllText(file), appId);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public DateTime? GameStartTime(GameInfo game) => throw new NotImplementedException("Fase H, H3.");

    public bool IsSteamRunning() => throw new NotImplementedException("Fase H, H3.");

    public void OpenSteamUri(string uri) => throw new NotImplementedException("Fase H, H4.");

    public IDisposable? AcquireSingleInstance() => throw new NotImplementedException("Fase H, H4.");
}
