namespace ControlSaveManager;

internal static class Program
{
    private const string Title = "Control Save Manager";

    /// <summary>
    /// Versione dell'app (es. "0.9.0"), dal tag Version del .csproj.
    /// </summary>
    public static string Version { get; } =
        typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "";

    [STAThread]
    static void Main()
    {
        // Due finestre aperte lavorerebbero sugli stessi file.
        using Mutex mutex = new(true, @"Local\ControlSaveManager", out bool firstInstance);

        if (!firstInstance)
        {
            MessageBox.Show(
                "Control Save Manager è già aperto.",
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowUnexpectedError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Write("Errore imprevisto (chiusura)", (Exception)e.ExceptionObject);

        ApplicationConfiguration.Initialize();

        Log.Write("Avvio versione " + Version);

        Result<SaveManager> init = SaveManager.Initialize(AccountForm.Choose);

        if (!init.Success)
        {
            // Messaggio vuoto: l'utente ha chiuso la scelta dell'account.
            if (init.Message == "")
                return;

            Log.Write("Avvio fallito: " + init.Message);

            MessageBox.Show(
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

        MessageBox.Show(
            "Si è verificato un errore imprevisto:\n" + ex.Message +
            "\n\nI dettagli sono nel registro:\n" + Log.FilePath,
            Title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
