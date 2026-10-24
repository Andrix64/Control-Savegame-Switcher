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
}
