namespace ControlSaveManager;

/// <summary>
/// Registro delle operazioni in Documenti\ControlSaves\log.txt.
/// Non lancia mai eccezioni: un registro non scrivibile non deve bloccare l'app.
/// </summary>
internal static class Log
{
    // Oltre questa dimensione il registro diventa log.old.txt e si riparte.
    private const long MaxSize = 1024 * 1024;

    private static readonly object Lock = new();

    // Impostabile per i test.
    public static string FilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ControlSaves", "log.txt");

    public static void Write(string message)
    {
        string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " +
                      message.Replace("\r", "").Replace("\n", " | ");

        lock (Lock)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxSize)
                    File.Move(FilePath, Path.ChangeExtension(FilePath, ".old.txt"), true);

                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
            catch
            {
                // Registro non scrivibile: l'operazione continua comunque.
            }
        }
    }

    /// <summary>
    /// Scrive l'esito di un'operazione e lo restituisce invariato.
    /// </summary>
    public static Result Write(string operation, Result result)
    {
        string outcome = result.Success ? "OK" : result.Critical ? "ERRORE GRAVE" : "ERRORE";
        string backup = result.BackupPath != null ? "  Backup: " + result.BackupPath : "";

        Write($"{operation}  {outcome}  {result.Message}{backup}");
        return result;
    }

    public static void Write(string context, Exception ex)
    {
        Write(context + ": " + ex);
    }
}
