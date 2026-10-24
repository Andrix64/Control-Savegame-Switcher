namespace Lss;

public enum SessionChange { None, Started, Ended }

/// <summary>Una sessione di gioco finita o appena iniziata.</summary>
public sealed record SessionEvent(SessionChange Change, string Profile, DateTime Start, DateTime End);

/// <summary>
/// Segue apertura e chiusura del gioco di un SaveManager: quando parte,
/// quale partita era nel gioco, quando finisce. Come in MainForm.CheckGameAsync.
/// </summary>
public sealed class SessionTracker
{
    public bool Running { get; private set; }

    public DateTime? Start { get; private set; }

    public string Profile { get; private set; } = "";

    /// <param name="running">Se il gioco è aperto adesso.</param>
    /// <param name="activeProfile">Partita nel gioco adesso (State.ActiveProfile).</param>
    /// <param name="startTime">Ora di avvio del processo, chiamata solo all'avvio.</param>
    /// <param name="now">Ora attuale, usata se il processo non dice quando è partito.</param>
    public SessionEvent Update(bool running, string activeProfile, Func<DateTime?> startTime, DateTime now)
    {
        if (running == Running)
            return new SessionEvent(SessionChange.None, Profile, Start ?? now, now);

        if (running)
        {
            Start = startTime() ?? now;
            Profile = activeProfile;
            Running = true;
            return new SessionEvent(SessionChange.Started, Profile, Start.Value, now);
        }

        return End(now);
    }

    /// <summary>
    /// Chiude la sessione se è aperta, per la chiusura dell'app: il tempo fin lì conta.
    /// </summary>
    public SessionEvent EndNow(DateTime now) =>
        Running ? End(now) : new SessionEvent(SessionChange.None, Profile, Start ?? now, now);

    private SessionEvent End(DateTime now)
    {
        var ended = new SessionEvent(SessionChange.Ended, Profile, Start ?? now, now);

        Start = null;
        Profile = "";
        Running = false;

        return ended;
    }
}
