namespace Lss;

/// <summary>
/// Implementazione di IPlatform in uso. Program la imposta all'avvio, i test
/// mettono FakePlatform.
/// </summary>
public static class Platform
{
    private static IPlatform? current;

    public static IPlatform Current
    {
        get => current ?? throw new InvalidOperationException("Platform.Current non impostata.");
        set => current = value;
    }
}
