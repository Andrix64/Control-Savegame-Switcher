namespace Lss;

/// <summary>
/// Tutto quello che dipende dal sistema operativo: Steam, processi, avvio
/// del gioco, istanza unica. Un'implementazione per sistema
/// (WindowsPlatform, poi LinuxPlatform) e una finta per i test.
/// </summary>
public interface IPlatform
{
    /// <summary>
    /// Cartelle di Steam trovate, la principale per prima. Vuoto se Steam
    /// non è installato.
    /// </summary>
    IReadOnlyList<string> FindSteamRoots();

    /// <summary>true se il gioco è aperto (nel dubbio true).</summary>
    bool IsGameRunning(GameInfo game);

    /// <summary>Quando è partito il gioco aperto, se si riesce a leggerlo.</summary>
    DateTime? GameStartTime(GameInfo game);

    bool IsSteamRunning();

    /// <summary>Apre un indirizzo steam:// (Steam si apre da solo se è chiuso).</summary>
    void OpenSteamUri(string uri);

    /// <summary>
    /// Prende il diritto di essere l'unica istanza dell'app. null se un'altra
    /// istanza è già aperta; altrimenti un oggetto da tenere fino alla chiusura.
    /// </summary>
    IDisposable? AcquireSingleInstance();
}
