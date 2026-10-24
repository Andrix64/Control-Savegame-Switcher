namespace Lss.Tests;

/// <summary>
/// Testi delle schede nella finestra principale (in italiano, vedi TestSetup).
/// </summary>
public sealed class CardTests
{
    private static CardData Data(TimeSpan playTime, DateTime? saved, int points) =>
        new("Profilo_Mario", false, new PlaythroughInfo("", playTime, null), saved, points);

    [Fact]
    public void DetailsShowPlayTimeLastSaveAndPoints()
    {
        DateTime today = DateTime.Today.AddHours(22).AddMinutes(11);

        string details = PlaythroughCard.Details(Data(new TimeSpan(12, 30, 0), today, 2));

        Assert.Equal("12 h 30 min di gioco  ·  salvata oggi alle 22:11  ·  2 punti salvati", details);
    }

    [Fact]
    public void DetailsForANewPlaythrough()
    {
        Assert.Equal("mai giocata: parte dall'inizio", PlaythroughCard.Details(Data(TimeSpan.Zero, null, 0)));
    }

    [Fact]
    public void DetailsUseSingularAndYesterday()
    {
        DateTime yesterday = DateTime.Today.AddDays(-1).AddHours(18).AddMinutes(40);

        string details = PlaythroughCard.Details(Data(TimeSpan.FromMinutes(45), yesterday, 1));

        Assert.Equal("45 min di gioco  ·  salvata ieri alle 18:40  ·  1 punto salvato", details);
    }
}
