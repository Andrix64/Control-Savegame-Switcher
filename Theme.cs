using System.Drawing.Drawing2D;

namespace ControlSaveManager;

/// <summary>
/// Colori, caratteri e utility grafiche. Tema scuro con accento rosso,
/// ispirato al gioco.
/// </summary>
internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(15, 15, 16);
    public static readonly Color Header = Color.FromArgb(10, 10, 11);
    public static readonly Color Surface = Color.FromArgb(25, 25, 27);
    public static readonly Color SurfaceHover = Color.FromArgb(35, 35, 38);
    public static readonly Color SurfaceSelected = Color.FromArgb(44, 44, 48);
    public static readonly Color Border = Color.FromArgb(46, 46, 51);

    public static readonly Color Text = Color.FromArgb(242, 242, 242);
    public static readonly Color TextMuted = Color.FromArgb(150, 150, 156);
    public static readonly Color TextDisabled = Color.FromArgb(90, 90, 96);

    public static readonly Color Accent = Color.FromArgb(224, 38, 27);
    public static readonly Color AccentHover = Color.FromArgb(240, 60, 48);
    public static readonly Color AccentPressed = Color.FromArgb(184, 30, 21);
    public static readonly Color AccentSoft = Color.FromArgb(62, 24, 22);
    public static readonly Color AccentSoftText = Color.FromArgb(255, 128, 118);

    public static readonly Color Success = Color.FromArgb(76, 195, 138);
    public static readonly Color Warning = Color.FromArgb(245, 184, 61);
    public static readonly Color Error = Color.FromArgb(255, 92, 92);

    public static readonly Font Base = new("Segoe UI", 10f);
    public static readonly Font Small = new("Segoe UI", 9f);
    public static readonly Font Caption = new("Segoe UI", 8f, FontStyle.Bold);
    public static readonly Font Button = new("Segoe UI Semibold", 10f);
    public static readonly Font ItemName = new("Segoe UI Semibold", 11f);
    public static readonly Font Large = new("Segoe UI Semibold", 16f);
    public static readonly Font Title = new("Segoe UI", 16f, FontStyle.Bold);

    private static Icon? appIcon;

    /// <summary>
    /// Icona dell'app, incorporata come risorsa (app.ico).
    /// </summary>
    public static Icon? AppIcon
    {
        get
        {
            if (appIcon != null)
                return appIcon;

            using Stream? stream = typeof(Theme).Assembly
                .GetManifestResourceStream("ControlSaveManager.app.ico");

            if (stream != null)
                appIcon = new Icon(stream);

            return appIcon;
        }
    }

    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2;

        if (d <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        return path;
    }
}
