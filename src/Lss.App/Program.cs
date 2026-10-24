using Avalonia;

namespace Lss.App;

internal static class Program
{
    /// <summary>Versione dell'app (es. "0.9.0"), dal tag Version del .csproj.</summary>
    public static string Version { get; } =
        typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "";

    [STAThread]
    public static int Main(string[] args)
    {
        // Per i controlli dei pacchetti: stampa la versione ed esce, senza finestre.
        if (args.Contains("--version"))
        {
            Console.WriteLine("LSS " + Version);
            return 0;
        }

        Platform.Current = OperatingSystem.IsWindows() ? new WindowsPlatform()
            : OperatingSystem.IsLinux() ? new LinuxPlatform()
            : throw new PlatformNotSupportedException();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Write("Errore imprevisto (chiusura)", (Exception)e.ExceptionObject);

        // Due finestre aperte lavorerebbero sugli stessi file.
        using IDisposable? instance = Platform.Current.AcquireSingleInstance();

        if (instance == null)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(["--already-running"]);
            return 1;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Usato anche dall'anteprima dell'editor.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
