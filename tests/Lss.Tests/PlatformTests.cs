namespace Lss.Tests;

/// <summary>
/// Avvio del gioco e di Steam, con la piattaforma finta.
/// </summary>
public sealed class PlatformTests
{
    [Fact]
    public void LaunchGameOpensSteamUri()
    {
        Result result = SaveManager.LaunchGame();

        Assert.True(result.Success);
        Assert.Contains("steam://rungameid/870780", TestSetup.Fake.OpenedUris);
    }
}
