namespace Lss;

/// <summary>
/// Impostazioni consigliate per i giochi noti.
/// </summary>
internal static class GameDefaults
{
    /// <summary>
    /// File delle opzioni del gioco (comandi, audio...) da tenere uguali in
    /// tutte le partite, relativi alla cartella dei salvataggi, con "/".
    /// </summary>
    public static IReadOnlyList<string> SharedFiles(string appId) => appId switch
    {
        GameInfo.ControlAppId => ["remote/preferences_data"],
        _ => []
    };
}
