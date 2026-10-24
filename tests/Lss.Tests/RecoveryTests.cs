namespace Lss.Tests;

/// <summary>
/// Operazioni interrotte (bug 1) e account senza cartella remote (bug 2).
/// </summary>
public sealed class RecoveryTests : IDisposable
{
    private readonly TestFolder folder = new();

    private string UserData => Path.Combine(folder.Root, "userdata");
    private string Game => Path.Combine(UserData, "111", "870780");

    public void Dispose() => folder.Dispose();

    [Fact]
    public void InterruptedSwap_GameFolderIsRestoredAndAccountFound()
    {
        // Spegnimento tra i due spostamenti: restano .vecchio e .nuovo, manca 870780.
        TestFolder.Write(Path.Combine(Game + ".vecchio", "remote", "slot"), "prima");
        TestFolder.Write(Path.Combine(Game + ".nuovo", "remote", "slot"), "dopo");

        Assert.Empty(SaveManager.FindAccounts(UserData));

        SaveManager.RecoverGameFolders(UserData);

        Assert.Equal("prima", File.ReadAllText(Path.Combine(Game, "remote", "slot")));
        Assert.False(Directory.Exists(Game + ".vecchio"));
        Assert.False(Directory.Exists(Game + ".nuovo"));
        Assert.Single(SaveManager.FindAccounts(UserData));
    }

    [Fact]
    public void InterruptedSwap_LeftoversNextToCompleteFolderAreDeleted()
    {
        // Spegnimento dopo lo scambio: 870780 è già quella nuova.
        TestFolder.Write(Path.Combine(Game, "remote", "slot"), "dopo");
        TestFolder.Write(Path.Combine(Game + ".vecchio", "remote", "slot"), "prima");

        SaveManager.RecoverGameFolders(UserData);

        Assert.Equal("dopo", File.ReadAllText(Path.Combine(Game, "remote", "slot")));
        Assert.False(Directory.Exists(Game + ".vecchio"));
    }

    [Fact]
    public void LoneTempFolderIsLeftAlone()
    {
        // Senza 870780 e senza .vecchio, .nuovo potrebbe essere l'unica copia.
        TestFolder.Write(Path.Combine(Game + ".nuovo", "remote", "slot"), "x");

        Assert.False(FileOps.RecoverInterruptedReplace(Game));
        Assert.True(Directory.Exists(Game + ".nuovo"));
    }

    [Fact]
    public void InterruptedProfileSwap_IsRecoveredAtStartup()
    {
        SaveManager manager = folder.CreateManager("A1");
        manager.CreateProfile("Mario", true);
        manager.ActivateProfile("Profilo_Mario");

        // Spegnimento durante "Salva progressi": il profilo è rimasto come .vecchio.
        string profile = Path.Combine(manager.ProfilesPath, "Profilo_Mario");
        Directory.Move(profile, profile + ".vecchio");
        Assert.DoesNotContain("Profilo_Mario", manager.GetProfiles());

        var restarted = new SaveManager(folder.Game, folder.Data);

        Assert.Contains("Profilo_Mario", restarted.GetProfiles());
        Assert.True(restarted.HasActiveProfile);
        Assert.False(Directory.Exists(profile + ".vecchio"));
    }

    [Fact]
    public void InterruptedCaseOnlyRename_IsRecoveredAtStartup()
    {
        SaveManager manager = folder.CreateManager();
        manager.CreateProfile("Mario", true);

        // Spegnimento a metà di Mario -> MARIO: resta solo il nome intermedio.
        string profile = Path.Combine(manager.ProfilesPath, "Profilo_Mario");
        Directory.Move(profile, Path.Combine(manager.ProfilesPath, "Profilo_MARIO.rinomina"));

        var restarted = new SaveManager(folder.Game, folder.Data);

        Assert.Contains("Profilo_MARIO", restarted.GetProfiles());
    }

    [Fact]
    public void AccountWithoutRemoteFolderIsStillFound()
    {
        TestFolder.Write(Path.Combine(Game, "remote", "slot"), "x");
        var manager = new SaveManager(Game, folder.Data);

        // Profilo creato a mano, senza remote: attivandolo il gioco resta senza remote.
        Directory.CreateDirectory(Path.Combine(manager.ProfilesPath, "Profilo_Manuale"));
        Assert.True(manager.ActivateProfile("Profilo_Manuale").Success);
        Assert.False(Directory.Exists(Path.Combine(Game, "remote")));

        SteamAccount account = Assert.Single(SaveManager.FindAccounts(UserData));
        Assert.Equal("111", account.Id);
        Assert.Null(account.LastSaved);
    }

    [Fact]
    public void SteamCacheDoesNotCountAsLastSave()
    {
        TestFolder.Write(Path.Combine(Game, "remotecache.vdf"), "cache");

        Assert.Null(Assert.Single(SaveManager.FindAccounts(UserData)).LastSaved);
    }
}
