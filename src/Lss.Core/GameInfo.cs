namespace Lss;

/// <summary>
/// Se LSS può gestire i salvataggi di un gioco.
/// </summary>
public enum SupportState
{
    /// <summary>Ci sono salvataggi in userdata: LSS li gestisce.</summary>
    Supported,

    /// <summary>Solo remotecache.vdf: il gioco salva altrove (Steam Auto-Cloud).</summary>
    SavesElsewhere,

    /// <summary>Cartella vuota: nessun salvataggio.</summary>
    NoSaves
}

/// <summary>
/// Gioco Steam con una cartella in userdata.
/// </summary>
/// <param name="AppId">Numero del gioco su Steam (es. "870780").</param>
/// <param name="Name">Nome dal manifest, o quello noto, o "App 870780".</param>
/// <param name="InstallDir">Cartella in steamapps/common, o "" se non è installato.</param>
/// <param name="LibraryPath">Libreria Steam che lo contiene, o "" se non è installato.</param>
/// <param name="SaveFolder">Cartella userdata/IDAccount/AppID.</param>
/// <param name="Support">Se LSS può gestirlo.</param>
/// <param name="IconPath">Icona 32×32, o "" se non c'è.</param>
/// <param name="CoverPath">Copertina 300×450, o "" se non c'è.</param>
public sealed record GameInfo(
    string AppId,
    string Name,
    string InstallDir,
    string LibraryPath,
    string SaveFolder,
    SupportState Support,
    string IconPath = "",
    string CoverPath = "")
{
    public const string ControlAppId = "870780";

    /// <summary>true se il gioco è installato in una libreria trovata.</summary>
    public bool IsInstalled => InstallDir != "" && LibraryPath != "";

    /// <summary>Cartella del gioco installato, o "" se non è installato.</summary>
    public string InstallPath => IsInstalled
        ? Path.Combine(LibraryPath, "steamapps", "common", InstallDir)
        : "";

    /// <summary>
    /// Control con la cartella dei salvataggi data: usato finché l'interfaccia
    /// WinForms apre solo Control, e dai test.
    /// </summary>
    public static GameInfo Control(string saveFolder) =>
        new(ControlAppId, "Control", "", "", saveFolder, SupportState.Supported);
}
