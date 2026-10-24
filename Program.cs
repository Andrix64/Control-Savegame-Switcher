namespace Lss;

internal static class Program
{
    private const string Title = "Local Save Switcher";

    /// <summary>
    /// Versione dell'app (es. "0.9.0"), dal tag Version del .csproj.
    /// </summary>
    public static string Version { get; } =
        typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "";

    [STAThread]
    static void Main()
    {
        Platform.Current = new WindowsPlatform();

        // Vanno chiamati prima di creare qualsiasi finestra.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowUnexpectedError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Write("Errore imprevisto (chiusura)", (Exception)e.ExceptionObject);

        ApplicationConfiguration.Initialize();

        // Due finestre aperte lavorerebbero sugli stessi file.
        using IDisposable? instance = Platform.Current.AcquireSingleInstance();

        if (instance == null)
        {
            MessageDialog.Show(
                Strings.AppAlreadyRunning,
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Prima di qualsiasi lettura o scrittura dei dati, registro compreso.
        (LegacyMigration.MoveResult moved, string moveDetail) = LegacyMigration.Run();

        Log.Write("Avvio versione " + Version);

        if (LegacyMigration.Notice(moved, moveDetail) is (string text, bool isWarning))
            MessageDialog.Show(text, Title, MessageBoxButtons.OK,
                isWarning ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        Result<SaveManager> init = SaveManager.Initialize(AccountForm.Choose);

        if (!init.Success)
        {
            // Messaggio vuoto: l'utente ha chiuso la scelta dell'account.
            if (init.Message == "")
                return;

            Log.Write("Avvio fallito: " + init.Message);

            MessageDialog.Show(
                init.Message,
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm(init.Value!));
    }

    private static void ShowUnexpectedError(Exception ex)
    {
        Log.Write("Errore imprevisto", ex);

        MessageDialog.Show(
            Loc.F(Strings.UnexpectedErrorWithLog, ex.Message, Log.FilePath),
            Title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
