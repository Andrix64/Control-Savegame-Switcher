using System.Globalization;

namespace ControlSaveManager;

/// <summary>
/// Testi tradotti: Strings.resx (inglese) e Strings.it.resx (italiano).
/// La lingua è quella di Windows (CultureInfo.CurrentUICulture).
/// </summary>
internal static class Loc
{
    /// <summary>
    /// Riempie un testo di Strings con i valori {0}, {1}, ...
    /// </summary>
    public static string F(string format, params object?[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }
}
