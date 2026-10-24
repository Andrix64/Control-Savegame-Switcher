namespace Lss;

/// <summary>
/// File di testo "Chiave=Valore", come stato.ini. La scrittura passa da un
/// file temporaneo, così un'interruzione non lo lascia a metà.
/// </summary>
internal static class IniFile
{
    /// <summary>
    /// Valori del file, o un dizionario vuoto se il file non c'è o non si legge.
    /// </summary>
    public static Dictionary<string, string> Read(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!File.Exists(path))
                return values;

            foreach (string line in File.ReadAllLines(path))
            {
                string[] parts = line.Split('=', 2);

                if (parts.Length == 2)
                    values[parts[0].Trim()] = parts[1];
            }
        }
        catch
        {
            // Un file illeggibile vale come vuoto: sono solo informazioni in più.
        }

        return values;
    }

    /// <summary>
    /// Scrive i valori. Lancia un'eccezione in caso di errore.
    /// </summary>
    public static void Write(string path, IEnumerable<KeyValuePair<string, string>> values)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string temp = path + ".tmp";

        using (FileStream stream = new(temp, FileMode.Create, FileAccess.Write))
        using (StreamWriter writer = new(stream))
        {
            foreach ((string key, string value) in values)
                writer.WriteLine(key + "=" + value.Replace("\r", "").Replace("\n", " "));

            writer.Flush();
            stream.Flush(flushToDisk: true);
        }

        File.Move(temp, path, overwrite: true);
    }
}
