namespace Lss.Tests;

public sealed class SteamLibraryTests : IDisposable
{
    private readonly TestFolder folder = new();

    private string SteamRoot => Path.Combine(folder.Root, "Steam");

    public void Dispose() => folder.Dispose();

    /// <summary>
    /// libraryfolders.vdf con una voce per cartella, con i percorsi scritti come li scrive Steam.
    /// </summary>
    private static string LibraryFoldersFile(params string[] paths)
    {
        string entries = string.Concat(paths.Select((path, i) =>
        {
            string escaped = path.Replace("\\", "\\\\");
            return $"\t\"{i}\"\n\t{{\n\t\t\"path\"\t\t\"{escaped}\"\n\t}}\n";
        }));

        return "\"libraryfolders\"\n{\n" + entries + "}\n";
    }

    [Fact]
    public void LibraryFolders_WithoutFile_ReturnsSteamRoot()
    {
        Assert.Equal(new[] { SteamRoot }, SteamLibrary.LibraryFolders(SteamRoot).ToArray());
    }

    [Fact]
    public void LibraryFolders_ReadsAllExistingPaths()
    {
        string second = Path.Combine(folder.Root, "Lib2");
        string missing = Path.Combine(folder.Root, "Mancante");
        Directory.CreateDirectory(second);
        TestFolder.Write(
            Path.Combine(SteamRoot, "steamapps", "libraryfolders.vdf"),
            LibraryFoldersFile(SteamRoot, second, missing));

        Assert.Equal(new[] { SteamRoot, second }, SteamLibrary.LibraryFolders(SteamRoot).ToArray());
    }

    [Fact]
    public void LibraryFolders_BrokenFile_ReturnsSteamRoot()
    {
        TestFolder.Write(
            Path.Combine(SteamRoot, "steamapps", "libraryfolders.vdf"),
            "\"libraryfolders\"\n{\n\"0\"\n{");

        Assert.Equal(new[] { SteamRoot }, SteamLibrary.LibraryFolders(SteamRoot).ToArray());
    }
}
