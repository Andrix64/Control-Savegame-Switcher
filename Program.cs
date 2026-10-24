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
        (AppData.MoveResult moved, string moveDetail) = AppData.MigrateToLss();

        Log.Write("Avvio versione " + Version);
        ReportDataMove(moved, moveDetail);

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

    /// <summary>
    /// Dice all'utente, una volta sola, che i dati sono stati spostati nella cartella di LSS.
    /// </summary>
    private static void ReportDataMove(AppData.MoveResult result, string detail)
    {
        if (result == AppData.MoveResult.NothingToMove)
            return;

        Log.Write($"Spostamento dei dati da {AppData.LastMoveSource} a {AppData.DefaultRoot}: {result} {detail}");

        (string text, MessageBoxIcon icon) = result switch
        {
            AppData.MoveResult.Moved => (
                Loc.F(Strings.DataMoved, AppData.LastMoveSource, AppData.DefaultRoot) +
                (detail == "" ? "" : Loc.F(Strings.DataMovedOldLeft, detail)),
                MessageBoxIcon.Information),

            AppData.MoveResult.BothExist => (
                Loc.F(Strings.DataBothExist, AppData.LastMoveSource, AppData.DefaultRoot),
                MessageBoxIcon.Warning),

            _ => (
                Loc.F(Strings.DataMoveFailed, AppData.LastMoveSource, AppData.DefaultRoot, detail),
                MessageBoxIcon.Warning)
        };

        MessageDialog.Show(text, Title, MessageBoxButtons.OK, icon);
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
