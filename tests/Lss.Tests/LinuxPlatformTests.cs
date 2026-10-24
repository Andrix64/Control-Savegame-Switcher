namespace Lss.Tests;

/// <summary>
/// Funzioni di LinuxPlatform. I test girano solo su Linux: collegamenti
/// simbolici e percorsi di Steam sono diversi sugli altri sistemi.
/// </summary>
public sealed class LinuxPlatformTests : IDisposable
{
    private readonly TestFolder folder = new();

    public void Dispose() => folder.Dispose();

    [Fact]
    public void SteamLinkIsNotCountedTwice()
    {
        if (!OperatingSystem.IsLinux()) return;

        string home = Path.Combine(folder.Root, "home");
        string native = Path.Combine(home, ".local", "share", "Steam");
        Directory.CreateDirectory(Path.Combine(native, "userdata"));
        Directory.CreateDirectory(Path.Combine(home, ".steam"));
        Directory.CreateSymbolicLink(Path.Combine(home, ".steam", "steam"), native);

        IReadOnlyList<string> roots = LinuxPlatform.FindSteamRoots(home);

        Assert.Single(roots);
        Assert.Equal(Path.GetFullPath(native), roots[0]);
    }

    [Fact]
    public void NativeComesBeforeFlatpak()
    {
        if (!OperatingSystem.IsLinux()) return;

        string home = Path.Combine(folder.Root, "home");
        string native = Path.Combine(home, ".local", "share", "Steam");
        string flatpak = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");
        Directory.CreateDirectory(Path.Combine(flatpak, "userdata"));
        Directory.CreateDirectory(Path.Combine(native, "userdata"));

        IReadOnlyList<string> roots = LinuxPlatform.FindSteamRoots(home);

        Assert.Equal(2, roots.Count);
        Assert.Equal(Path.GetFullPath(native), roots[0]);
        Assert.Equal(Path.GetFullPath(flatpak), roots[1]);
    }

    [Fact]
    public void FolderWithoutUserdataIsSkipped()
    {
        if (!OperatingSystem.IsLinux()) return;

        string home = Path.Combine(folder.Root, "home");
        Directory.CreateDirectory(Path.Combine(home, ".local", "share", "Steam"));

        Assert.Empty(LinuxPlatform.FindSteamRoots(home));
    }

    [Fact]
    public void RunningByRunningAppId()
    {
        Assert.True(LinuxPlatform.RegistrySaysRunning(Registry("105600", "0"), "105600"));
    }

    [Fact]
    public void RunningByAppsRunningFlag()
    {
        Assert.True(LinuxPlatform.RegistrySaysRunning(Registry("0", "1"), "105600"));
    }

    [Fact]
    public void NotRunning()
    {
        Assert.False(LinuxPlatform.RegistrySaysRunning(Registry("0", "0"), "105600"));
    }

    [Fact]
    public void OtherGameRunning()
    {
        Assert.False(LinuxPlatform.RegistrySaysRunning(Registry("730", "0"), "105600"));
    }

    [Fact]
    public void BrokenRegistryIsNotRunning()
    {
        Assert.False(LinuxPlatform.RegistrySaysRunning("\"Registry\" { \"HKCU\" {", "105600"));
    }

    [Fact]
    public void RegistryFilesAreTheExistingOnes()
    {
        string home = Path.Combine(folder.Root, "home");
        TestFolder.Write(Path.Combine(home, ".steam", "registry.vdf"), "");
        TestFolder.Write(Path.Combine(home, "snap", "steam", "common", ".steam", "registry.vdf"), "");

        IReadOnlyList<string> files = LinuxPlatform.RegistryFiles(home);

        Assert.Equal(
            new[] { Path.Combine(home, ".steam", "registry.vdf"), Path.Combine(home, "snap", "steam", "common", ".steam", "registry.vdf") },
            files);
    }

    /// <summary>registry.vdf con il gioco 105600 e un RunningAppID dato.</summary>
    private static string Registry(string runningAppId, string running) => $$"""
        "Registry"
        {
            "HKCU"
            {
                "Software"
                {
                    "Valve"
                    {
                        "Steam"
                        {
                            "RunningAppID"		"{{runningAppId}}"
                            "Apps"
                            {
                                "105600"
                                {
                                    "Running"		"{{running}}"
                                }
                            }
                        }
                    }
                }
            }
        }
        """;
}
