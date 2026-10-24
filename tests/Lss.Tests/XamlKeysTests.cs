using System.Globalization;
using System.Text.RegularExpressions;

namespace Lss.Tests;

/// <summary>
/// Ogni {loc:Str Chiave} nei file XAML deve esistere in Strings.resx: un
/// refuso si vede qui e non nell'app con "!Chiave".
/// </summary>
public class XamlKeysTests
{
    private static readonly Regex KeyPattern = new(@"\{loc:Str (\w+)\}");

    [Fact]
    public void EveryXamlKeyExistsInTheResources()
    {
        string root = FindRepositoryRoot();
        string appFolder = Path.Combine(root, "src", "Lss.App");

        var missing = new List<string>();

        foreach (string file in Directory.EnumerateFiles(appFolder, "*.axaml", SearchOption.AllDirectories))
        {
            foreach (Match match in KeyPattern.Matches(File.ReadAllText(file)))
            {
                string key = match.Groups[1].Value;

                if (Strings.ResourceManager.GetString(key, CultureInfo.InvariantCulture) == null)
                    missing.Add(Path.GetFileName(file) + ": " + key);
            }
        }

        Assert.Empty(missing);
    }

    // Risale dalla cartella dei test fino a quella con LSS.slnx.
    private static string FindRepositoryRoot()
    {
        string? folder = AppContext.BaseDirectory;

        while (folder != null && !File.Exists(Path.Combine(folder, "LSS.slnx")))
            folder = Path.GetDirectoryName(folder);

        return folder ?? throw new InvalidOperationException("LSS.slnx non trovato.");
    }
}
