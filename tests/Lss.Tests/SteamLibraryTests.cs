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

    /// <summary>
    /// Manifest appmanifest_&lt;AppID&gt;.acf con nome e cartella di installazione.
    /// </summary>
    private static string Manifest(string appId, string name, string installDir) =>
        $"\"AppState\"\n{{\n\t\"appid\"\t\t\"{appId}\"\n\t\"name\"\t\t\"{name}\"\n\t\"installdir\"\t\t\"{installDir}\"\n}}\n";

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

    [Fact]
    public void ReadManifest_FindsGameInSecondLibrary()
    {
        string second = Path.Combine(folder.Root, "Lib2");
        TestFolder.Write(
            Path.Combine(second, "steamapps", "appmanifest_105600.acf"),
            Manifest("105600", "Terraria", "Terraria"));
        string[] libraries = [SteamRoot, second];

        var manifest = SteamLibrary.ReadManifest(libraries, "105600");

        Assert.NotNull(manifest);
        Assert.Equal("Terraria", manifest.Value.Name);
        Assert.Equal("Terraria", manifest.Value.InstallDir);
        Assert.Equal(second, manifest.Value.Library);
    }

    [Fact]
    public void ReadManifest_MissingGame_ReturnsNull()
    {
        string[] libraries = [SteamRoot];

        Assert.Null(SteamLibrary.ReadManifest(libraries, "999999"));
    }

    [Fact]
    public void ReadManifest_BrokenManifest_IsSkipped()
    {
        string second = Path.Combine(folder.Root, "Lib2");
        TestFolder.Write(
            Path.Combine(SteamRoot, "steamapps", "appmanifest_105600.acf"),
            "\"AppState\"\n{\n\t\"name\"");
        TestFolder.Write(
            Path.Combine(second, "steamapps", "appmanifest_105600.acf"),
            Manifest("105600", "Terraria", "Terraria"));
        string[] libraries = [SteamRoot, second];

        var manifest = SteamLibrary.ReadManifest(libraries, "105600");

        Assert.NotNull(manifest);
        Assert.Equal(second, manifest.Value.Library);
    }

    [Fact]
    public void FindImages_NewLayout_FindsIconAndCoverInGameFolder()
    {
        string folder = Path.Combine(SteamRoot, "appcache", "librarycache", "870780");
        string icon = Path.Combine(folder, "858961e95fbf869f136e1770d586e0caefd4cfac.jpg");
        string cover = Path.Combine(folder, "library_600x900.jpg");
        TestFolder.Write(icon, "");
        TestFolder.Write(cover, "");
        TestFolder.Write(Path.Combine(folder, "header.jpg"), "");
        TestFolder.Write(Path.Combine(folder, "logo.png"), "");

        var images = SteamLibrary.FindImages(SteamRoot, "870780");

        Assert.Equal(icon, images.Icon);
        Assert.Equal(cover, images.Cover);
    }

    [Fact]
    public void FindImages_OldLayout_FindsFilesInLibraryCache()
    {
        string cache = Path.Combine(SteamRoot, "appcache", "librarycache");
        TestFolder.Write(Path.Combine(cache, "870780_icon.jpg"), "");
        TestFolder.Write(Path.Combine(cache, "870780_library_600x900.jpg"), "");

        var images = SteamLibrary.FindImages(SteamRoot, "870780");

        Assert.Equal(Path.Combine(cache, "870780_icon.jpg"), images.Icon);
        Assert.Equal(Path.Combine(cache, "870780_library_600x900.jpg"), images.Cover);
    }

    [Fact]
    public void FindImages_NothingInCache_ReturnsEmptyPaths()
    {
        Assert.Equal(("", ""), SteamLibrary.FindImages(SteamRoot, "870780"));
    }

    [Fact]
    public void GetSupport_FilesInRemote_IsSupported()
    {
        TestFolder.Write(folder.SaveFile, "partita");
        TestFolder.Write(folder.CacheFile, "cache");

        Assert.Equal(SupportState.Supported, SteamLibrary.GetSupport(folder.Game));
    }

    [Fact]
    public void GetSupport_OnlyRemoteCache_IsSavesElsewhere()
    {
        TestFolder.Write(folder.CacheFile, "cache");

        Assert.Equal(SupportState.SavesElsewhere, SteamLibrary.GetSupport(folder.Game));
    }

    [Fact]
    public void GetSupport_EmptyFolder_IsNoSaves()
    {
        Directory.CreateDirectory(folder.Game);

        Assert.Equal(SupportState.NoSaves, SteamLibrary.GetSupport(folder.Game));
    }

    [Fact]
    public void GetSupport_MissingFolder_IsNoSaves()
    {
        Assert.Equal(SupportState.NoSaves, SteamLibrary.GetSupport(folder.Game));
    }

    [Fact]
    public void GetSupport_NestedRemoteCache_IsSupported()
    {
        TestFolder.Write(Path.Combine(folder.Game, "remote", "remotecache.vdf"), "cache");

        Assert.Equal(SupportState.Supported, SteamLibrary.GetSupport(folder.Game));
    }
}
