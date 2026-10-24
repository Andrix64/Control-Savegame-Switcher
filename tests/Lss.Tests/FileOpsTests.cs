namespace Lss.Tests;

public sealed class FileOpsTests : IDisposable
{
    private readonly TestFolder folder = new();

    private string Source => Path.Combine(folder.Root, "sorgente");
    private string Destination => Path.Combine(folder.Root, "destinazione");

    public void Dispose() => folder.Dispose();

    [Fact]
    public void CopyAllVerified_CopiesExcludedFoldersToo()
    {
        // Lo spostamento tra dischi diversi copia tutto, anche "Backup" e "Profili",
        // che CopyFolder invece salta.
        TestFolder.Write(Path.Combine(Source, "123", "Profili", "Profilo_Mario", "remote", "slot.sav"), "mario");
        TestFolder.Write(Path.Combine(Source, "123", "Backup", "Prima_ripristino_x", "a.sav"), "backup");

        FileOps.CopyAllVerified(Source, Destination);

        Assert.Equal("backup", File.ReadAllText(Path.Combine(Destination, "123", "Backup", "Prima_ripristino_x", "a.sav")));
        Assert.True(File.Exists(Path.Combine(Destination, "123", "Profili", "Profilo_Mario", "remote", "slot.sav")));
        Assert.True(Directory.Exists(Source));
    }

    [Fact]
    public void ReplaceFolder_MakesDestinationEqualAndKeepsSteamCache()
    {
        TestFolder.Write(Path.Combine(Source, "remote", "a.sav"), "nuovo");
        TestFolder.Write(Path.Combine(Source, "remote", "sub", "b.sav"), "nuovo B");
        TestFolder.Write(Path.Combine(Destination, "remote", "a.sav"), "vecchio");
        TestFolder.Write(Path.Combine(Destination, "remote", "extra.sav"), "da cancellare");
        TestFolder.Write(Path.Combine(Destination, "remotecache.vdf"), "cache");

        FileOps.ReplaceFolder(Source, Destination);

        Assert.True(FileOps.FoldersAreEqual(Source, Destination));
        Assert.False(File.Exists(Path.Combine(Destination, "remote", "extra.sav")));
        Assert.Equal("cache", File.ReadAllText(Path.Combine(Destination, "remotecache.vdf")));
        Assert.False(Directory.Exists(Destination + ".nuovo"));
        Assert.False(Directory.Exists(Destination + ".vecchio"));
    }

    [Fact]
    public void ReplaceFolder_FallsBackToDirectCopyWhenDestinationIsInUse()
    {
        TestFolder.Write(Path.Combine(Source, "remote", "a.sav"), "nuovo");
        TestFolder.Write(Path.Combine(Destination, "remotecache.vdf"), "cache");

        // Un file aperto impedisce di spostare la cartella.
        using (new FileStream(Path.Combine(Destination, "remotecache.vdf"),
                   FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            FileOps.ReplaceFolder(Source, Destination);
        }

        Assert.True(FileOps.FoldersAreEqual(Source, Destination));
    }

    [Fact]
    public void ReplaceFolder_RecoversFromInterruptedSwap()
    {
        TestFolder.Write(Path.Combine(Source, "remote", "a.sav"), "nuovo");
        TestFolder.Write(Path.Combine(Destination + ".vecchio", "remotecache.vdf"), "cache");

        FileOps.ReplaceFolder(Source, Destination);

        Assert.True(FileOps.FoldersAreEqual(Source, Destination));
        Assert.True(File.Exists(Path.Combine(Destination, "remotecache.vdf")));
    }

    [Fact]
    public void ReplaceFolder_LeavesDestinationIntactWhenSourceIsMissing()
    {
        TestFolder.Write(Path.Combine(Destination, "remote", "a.sav"), "vecchio");

        Assert.Throws<DirectoryNotFoundException>(() =>
            FileOps.ReplaceFolder(Path.Combine(folder.Root, "manca"), Destination));

        Assert.Equal("vecchio", File.ReadAllText(Path.Combine(Destination, "remote", "a.sav")));
    }

    [Fact]
    public void FoldersAreEqual_DetectsDifferentContent()
    {
        TestFolder.Write(Path.Combine(Source, "a.sav"), "uno");
        TestFolder.Write(Path.Combine(Destination, "a.sav"), "due");

        Assert.False(FileOps.FoldersAreEqual(Source, Destination));
    }

    [Fact]
    public void FoldersAreEqual_IgnoresSteamCache()
    {
        TestFolder.Write(Path.Combine(Source, "a.sav"), "uno");
        TestFolder.Write(Path.Combine(Destination, "a.sav"), "uno");
        TestFolder.Write(Path.Combine(Destination, "remotecache.vdf"), "cache");

        Assert.True(FileOps.FoldersAreEqual(Source, Destination));
    }

    [Fact]
    public void HasNoSaveFiles_IgnoresSteamCache()
    {
        TestFolder.Write(Path.Combine(Source, "remotecache.vdf"), "cache");
        Directory.CreateDirectory(Path.Combine(Source, "remote"));

        Assert.True(FileOps.HasNoSaveFiles(Source));
    }

    [Fact]
    public void CopyKeepsUnixPermissions()
    {
        if (!OperatingSystem.IsLinux()) return;

        // Un file eseguibile (mode 0o755) resta eseguibile nella copia.
        const UnixFileMode mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

        string file = Path.Combine(Source, "remote", "avvia.sh");
        TestFolder.Write(file, "#!/bin/sh");
        File.SetUnixFileMode(file, mode);

        FileOps.CopyFolderVerified(Source, Destination);

        Assert.Equal(mode, File.GetUnixFileMode(Path.Combine(Destination, "remote", "avvia.sh")));
    }
}
