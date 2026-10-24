namespace Lss;

/// <summary>
/// Esito di un'operazione: l'interfaccia decide come mostrarlo.
/// </summary>
/// <param name="Success">true se l'operazione è riuscita.</param>
/// <param name="Message">Messaggio da mostrare all'utente.</param>
/// <param name="BackupPath">Backup creato durante l'operazione, se c'è.</param>
/// <param name="Critical">
/// true se i salvataggi del gioco possono essere in uno stato inconsistente.
/// </param>
public sealed record Result(
    bool Success,
    string Message,
    string? BackupPath = null,
    bool Critical = false)
{
    public static Result Ok(string message, string? backupPath = null) =>
        new(true, message, backupPath);

    public static Result Fail(string message, string? backupPath = null) =>
        new(false, message, backupPath);

    public static Result Fatal(string message, string? backupPath = null) =>
        new(false, message, backupPath, true);
}

/// <summary>
/// Esito di un'operazione che produce un valore.
/// </summary>
public sealed record Result<T>(T? Value, string Message) where T : class
{
    public bool Success => Value != null;

    public static Result<T> Ok(T value) => new(value, "");

    public static Result<T> Fail(string message) => new(null, message);
}
