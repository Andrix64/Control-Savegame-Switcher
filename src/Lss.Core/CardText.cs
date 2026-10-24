namespace Lss;

/// <summary>
/// Dati mostrati su una scheda partita.
/// </summary>
internal sealed record CardData(
    string ProfileName,
    bool Active,
    PlaythroughInfo Info,
    DateTime? LastSaved,
    int Savestates);

/// <summary>Testi delle schede delle partite (dettagli, tempo di gioco, ultimo salvataggio).</summary>
internal static class CardText
{
    /// <summary>
    /// Riga dei dettagli: tempo di gioco, ultimo salvataggio, punti salvati.
    /// </summary>
    internal static string Details(CardData data)
    {
        var parts = new List<string>();

        if (data.Info.PlayTime >= TimeSpan.FromMinutes(1))
            parts.Add(FormatPlayTime(data.Info.PlayTime));

        if (data.LastSaved is DateTime saved)
            parts.Add(FormatSaved(saved));
        else
            parts.Add(Strings.NeverPlayed);

        if (data.Savestates == 1)
            parts.Add(Strings.OnePoint);
        else if (data.Savestates > 1)
            parts.Add(Loc.F(Strings.PointsCount, data.Savestates));

        return string.Join("  ·  ", parts);
    }

    internal static string FormatPlayTime(TimeSpan time)
    {
        int hours = (int)time.TotalHours;

        return hours > 0
            ? Loc.F(Strings.PlayTimeHours, hours, time.Minutes)
            : Loc.F(Strings.PlayTimeMinutes, Math.Max(1, time.Minutes));
    }

    internal static string FormatSaved(DateTime saved)
    {
        string time = saved.ToString("t");

        if (saved.Date == DateTime.Today)
            return Loc.F(Strings.SavedToday, time);

        if (saved.Date == DateTime.Today.AddDays(-1))
            return Loc.F(Strings.SavedYesterday, time);

        return Loc.F(Strings.SavedOn, saved.ToString("d"));
    }
}
