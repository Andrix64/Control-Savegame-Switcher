namespace Lss;

/// <summary>
/// Cartella dei dati dell'app (profili, backup, savestate, registro):
/// %LOCALAPPDATA%\LSS su Windows, ~/.local/share/LSS su Linux. Su Windows
/// OneDrive non la sincronizza. I dati di Control Save Manager vengono
/// portati qui da LegacyMigration.
/// </summary>
internal static class AppData
{
    public static string DefaultRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LSS");

    /// <summary>
    /// Cartella in uso: DefaultRoot, oppure una vecchia cartella se
    /// LegacyMigration non è riuscita a spostarla.
    /// </summary>
    public static string Root { get; internal set; } = DefaultRoot;
}
