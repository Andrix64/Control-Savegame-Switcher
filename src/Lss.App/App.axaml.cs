using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Lss.App.Services;
using Lss.App.ViewModels;
using Lss.App.Views;

namespace Lss.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Le finestre di avviso si chiudono prima che l'app sia pronta: non deve uscire.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                Log.Write("Errore imprevisto", e.Exception);
                e.Handled = true;

                _ = ShowStandaloneAsync(Loc.F(Strings.UnexpectedErrorWithLog, e.Exception.Message, Log.FilePath), InfoKind.Error);
            };

            _ = StartAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (desktop.Args?.Contains("--already-running") == true)
        {
            await ShowStandaloneAsync(Strings.AppAlreadyRunning, InfoKind.Info);
            desktop.Shutdown();
            return;
        }

        // Compatibilità con Control Save Manager (vedi LegacyMigration).
        (LegacyMigration.MoveResult moved, string moveDetail) = LegacyMigration.Run();

        Log.Write("Avvio versione " + Program.Version);

        if (LegacyMigration.Notice(moved, moveDetail) is (string text, bool isWarning))
            await ShowStandaloneAsync(text, isWarning ? InfoKind.Warning : InfoKind.Info);

        string? steam = Platform.Current.FindSteamRoots().FirstOrDefault();

        if (steam == null)
        {
            await ShowStandaloneAsync(Strings.SteamNotFound, InfoKind.Error);
            desktop.Shutdown();
            return;
        }

        string userData = Path.Combine(steam, "userdata");

        if (!Directory.Exists(userData))
        {
            await ShowStandaloneAsync(Strings.UserDataNotFound, InfoKind.Error);
            desktop.Shutdown();
            return;
        }

        // Prima di cercare gli account: una cartella rimasta come 870780.vecchio le farebbe sparire.
        SaveManager.RecoverAllGameFolders(userData);

        IReadOnlyList<SteamAccount> accounts = SaveManager.FindAccountsWithGames(userData);

        if (accounts.Count == 0)
        {
            await ShowStandaloneAsync(Strings.NoAccountWithGames, InfoKind.Warning);
            desktop.Shutdown();
            return;
        }

        var settings = new AppSettings(Path.Combine(AppData.Root, "lss.ini"));
        settings.Load();

        // Con più account la scelta arriva con AccountWindow (G12): per ora si usa l'ultimo o il primo.
        SteamAccount account = accounts.FirstOrDefault(a => a.Id == settings.LastAccount) ?? accounts[0];

        if (accounts.Count > 1)
        {
            settings.LastAccount = account.Id;
            settings.Save();
        }

        IReadOnlyList<GameInfo> games = SteamLibrary.FindGames(steam, account.Folder);

        var window = new MainWindow
        {
            DataContext = new MainWindowViewModel(account, games, settings, new DialogService(() => desktop.MainWindow)),
        };

        window.Closed += (_, _) => desktop.Shutdown();
        desktop.MainWindow = window;
    }

    // Messaggio senza finestra proprietaria (all'avvio): al centro dello schermo, nella barra delle applicazioni.
    private static async Task ShowStandaloneAsync(string text, InfoKind kind)
    {
        MessageWindow window = MessageWindow.Inform("", text, kind);
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.ShowInTaskbar = true;

        var closed = new TaskCompletionSource();
        window.Closed += (_, _) => closed.TrySetResult();
        window.Show();

        await closed.Task;
    }
}
