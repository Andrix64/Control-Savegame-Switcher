namespace Lss.Tests;

/// <summary>
/// IPlatform per i test: niente registro, processi o Steam veri. I campi
/// si impostano dal test.
/// </summary>
public sealed class FakePlatform : IPlatform
{
    public List<string> SteamRoots { get; } = [];
    public bool GameRunning { get; set; }
    public DateTime? StartTime { get; set; }
    public bool SteamRunning { get; set; }
    public List<string> OpenedUris { get; } = [];

    public IReadOnlyList<string> FindSteamRoots() => SteamRoots;
    public bool IsGameRunning() => GameRunning;
    public DateTime? GameStartTime() => StartTime;
    public bool IsSteamRunning() => SteamRunning;
    public void OpenSteamUri(string uri) => OpenedUris.Add(uri);
    public IDisposable? AcquireSingleInstance() => new MemoryStream();
}
