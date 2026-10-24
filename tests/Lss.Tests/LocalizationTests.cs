using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace Lss.Tests;

public sealed class LocalizationTests
{
    private static readonly CultureInfo English = new("en-US");
    private static readonly CultureInfo Italian = new("it-IT");

    private static Dictionary<string, string> Read(CultureInfo culture)
    {
        // tryParents: false = solo i testi di quella lingua, senza ripiego sull'inglese.
        ResourceSet set = Strings.ResourceManager.GetResourceSet(
            culture.Name == "en-US" ? CultureInfo.InvariantCulture : culture.Parent,
            createIfNotExists: true, tryParents: false)!;

        return set.Cast<DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string)e.Value!);
    }

    private static string[] Placeholders(string text) =>
        Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();

    [Fact]
    public void EveryTextExistsInBothLanguagesWithTheSamePlaceholders()
    {
        Dictionary<string, string> english = Read(English);
        Dictionary<string, string> italian = Read(Italian);

        Assert.NotEmpty(english);
        Assert.Equal(english.Keys.Order(), italian.Keys.Order());

        foreach ((string key, string text) in english)
        {
            Assert.False(string.IsNullOrWhiteSpace(text), key);
            Assert.False(string.IsNullOrWhiteSpace(italian[key]), key);
            Assert.True(Placeholders(text).SequenceEqual(Placeholders(italian[key])),
                $"{key}: segnaposto diversi tra inglese e italiano.");
        }
    }

    [Fact]
    public void LanguageFollowsWindows()
    {
        CultureInfo saved = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = English;
            Assert.Equal("Before activating Mario", SaveManager.BackupReasonText("Prima_di_Profilo_Mario"));

            // Lingue senza traduzione: inglese.
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            Assert.Equal("Playthrough loaded.", Strings.ProfileActivated);

            CultureInfo.CurrentUICulture = Italian;
            Assert.Equal("Prima di attivare Mario", SaveManager.BackupReasonText("Prima_di_Profilo_Mario"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = saved;
        }
    }

    [Fact]
    public void StoredDatesAreShownInTheCurrentCulture()
    {
        CultureInfo saved = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = English;
            // Su Linux (ICU) prima di AM/PM c'è uno spazio stretto U+202F.
            Assert.Equal("10/22/2026 6:30:00 PM", SaveManager.FormatStoredDate("22/10/2026 18:30:00").Replace(' ', ' '));

            CultureInfo.CurrentCulture = Italian;
            Assert.Equal("22/10/2026 18:30:00", SaveManager.FormatStoredDate("22/10/2026 18:30:00"));

            Assert.Equal("illeggibile", SaveManager.FormatStoredDate("illeggibile"));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
