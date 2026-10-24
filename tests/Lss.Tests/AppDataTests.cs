namespace Lss.Tests;

public sealed class AppDataTests : IDisposable
{
    private readonly TestFolder folder = new();

    private string Old => Path.Combine(folder.Root, "Documenti", "ControlSaves");
    private string New => Path.Combine(folder.Root, "AppData", "Local", "ControlSaveManager");

    public void Dispose() => folder.Dispose();

    private void WriteOldData()
    {
        TestFolder.Write(Path.Combine(Old, "123", "Profili", "Profilo_Mario", "remote", "slot.sav"), "mario");
        TestFolder.Write(Path.Combine(Old, "123", "stato.ini"), "ProfiloAttivo=Profilo_Mario");
        TestFolder.Write(Path.Combine(Old, "log.txt"), "registro");
    }

    [Fact]
    public void Move_MovesEverythingAndRemovesOldFolder()
    {
        WriteOldData();

        (AppData.MoveResult result, string detail) = AppData.Move(Old, New);

        Assert.Equal(AppData.MoveResult.Moved, result);
        Assert.Equal("", detail);
        Assert.False(Directory.Exists(Old));
        Assert.Equal("mario", File.ReadAllText(Path.Combine(New, "123", "Profili", "Profilo_Mario", "remote", "slot.sav")));
        Assert.Equal("registro", File.ReadAllText(Path.Combine(New, "log.txt")));
    }

    [Fact]
    public void Move_UsesEmptyDestinationFolder()
    {
        WriteOldData();
        Directory.CreateDirectory(New);

        Assert.Equal(AppData.MoveResult.Moved, AppData.Move(Old, New).Result);
        Assert.True(File.Exists(Path.Combine(New, "123", "stato.ini")));
    }

    [Fact]
    public void Move_TouchesNothingWhenBothHaveData()
    {
        WriteOldData();
        TestFolder.Write(Path.Combine(New, "log.txt"), "nuovo");

        Assert.Equal(AppData.MoveResult.BothExist, AppData.Move(Old, New).Result);
        Assert.Equal("registro", File.ReadAllText(Path.Combine(Old, "log.txt")));
        Assert.Equal("nuovo", File.ReadAllText(Path.Combine(New, "log.txt")));
    }

    [Fact]
    public void Move_DoesNothingWithoutOldFolder()
    {
        Assert.Equal(AppData.MoveResult.NothingToMove, AppData.Move(Old, New).Result);
        Assert.False(Directory.Exists(New));
    }

    [Fact]
    public void Move_LeavesOldFolderIntactWhenItFails()
    {
        WriteOldData();

        // Un file aperto impedisce di spostare la cartella.
        using (new FileStream(Path.Combine(Old, "log.txt"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            (AppData.MoveResult result, string detail) = AppData.Move(Old, New);

            Assert.Equal(AppData.MoveResult.Failed, result);
            Assert.NotEqual("", detail);
        }

        Assert.Equal("mario", File.ReadAllText(Path.Combine(Old, "123", "Profili", "Profilo_Mario", "remote", "slot.sav")));
        Assert.False(Directory.Exists(New));
    }

    [Fact]
    public void CopyAllVerified_CopiesExcludedFoldersToo()
    {
        // Lo spostamento tra dischi diversi copia tutto, anche "Backup" e "Profili",
        // che CopyFolder invece salta.
        WriteOldData();
        TestFolder.Write(Path.Combine(Old, "123", "Backup", "Prima_ripristino_x", "a.sav"), "backup");

        FileOps.CopyAllVerified(Old, New);

        Assert.Equal("backup", File.ReadAllText(Path.Combine(New, "123", "Backup", "Prima_ripristino_x", "a.sav")));
        Assert.True(File.Exists(Path.Combine(New, "123", "Profili", "Profilo_Mario", "remote", "slot.sav")));
        Assert.True(Directory.Exists(Old));
    }
}
