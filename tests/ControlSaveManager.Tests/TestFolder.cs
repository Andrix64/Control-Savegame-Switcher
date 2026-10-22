using System.Runtime.CompilerServices;

namespace ControlSaveManager.Tests;

/// <summary>
/// Cartella temporanea con una finta cartella del gioco (…\870780) e una
/// finta cartella dei dati dell'app. Viene eliminata alla fine del test.
/// </summary>
public sealed class TestFolder : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "csm_test_" + Guid.NewGuid().ToString("N"));

    public string Game => Path.Combine(Root, "870780");
    public string Data => Path.Combine(Root, "data");

    /// <summary>
    /// File di salvataggio del gioco usato nei test.
    /// </summary>
    public string SaveFile => Path.Combine(Game, "remote", "slot.sav");

    public string CacheFile => Path.Combine(Game, "remotecache.vdf");

    public TestFolder()
    {
        Directory.CreateDirectory(Root);
    }

    public static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// Crea i salvataggi del gioco e un manager che li usa.
    /// </summary>
    public SaveManager CreateManager(string saveContent = "partita")
    {
        Write(SaveFile, saveContent);
        Write(CacheFile, "cache");
        return new SaveManager(Game, Data);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, true);
        }
        catch
        {
            // Cartella temporanea: se resta, non è un problema.
        }
    }
}

internal static class TestSetup
{
    /// <summary>
    /// Il registro dei test non va nel log.txt vero.
    /// </summary>
    [ModuleInitializer]
    internal static void Init()
    {
        Log.FilePath = Path.Combine(Path.GetTempPath(), "csm_test_log.txt");
    }
}
