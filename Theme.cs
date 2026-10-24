using System.Drawing.Drawing2D;

namespace Lss;

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
    public static readonly Color InfoBlue = Color.FromArgb(127, 176, 240);

    // Testo secondario più leggibile di TextMuted, per le righe di dettaglio.
    public static readonly Color TextSecondary = Color.FromArgb(168, 168, 174);

    // Sfondo e bordo delle barre informative (InfoBar).
    public static readonly Color InfoBack = Color.FromArgb(27, 34, 48);
    public static readonly Color InfoBorder = Color.FromArgb(52, 80, 122);
    public static readonly Color WarningBack = Color.FromArgb(42, 33, 16);
    public static readonly Color WarningBorder = Color.FromArgb(92, 69, 21);
    public static readonly Color SuccessBack = Color.FromArgb(24, 38, 31);
    public static readonly Color SuccessBorder = Color.FromArgb(46, 92, 68);
    public static readonly Color ErrorBack = Color.FromArgb(46, 23, 23);
    public static readonly Color ErrorBorder = Color.FromArgb(107, 43, 43);

    // Pulsanti che eliminano qualcosa.
    public static readonly Color DangerBorder = Color.FromArgb(122, 42, 36);
    public static readonly Color DangerText = Color.FromArgb(255, 180, 173);

    public static readonly Font Base = new("Segoe UI", 10f);
    public static readonly Font Small = new("Segoe UI", 9f);
    public static readonly Font Caption = new("Segoe UI", 8f, FontStyle.Bold);
    public static readonly Font Button = new("Segoe UI Semibold", 10f);
    public static readonly Font ItemName = new("Segoe UI Semibold", 11f);
    public static readonly Font Large = new("Segoe UI Semibold", 16f);
    public static readonly Font Title = new("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font Heading = new("Segoe UI Semibold", 14f);
    public static readonly Font CardTitle = new("Segoe UI Semibold", 12f);
    public static readonly Font Note = new("Segoe UI", 9.5f, FontStyle.Italic);
    public static readonly Font Strong = new("Segoe UI Semibold", 10f);

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
                .GetManifestResourceStream("Lss.app.ico");

            if (stream != null)
                appIcon = new Icon(stream);

            return appIcon;
        }
    }

    /// <summary>
    /// Aspetto comune delle finestre secondarie: tema scuro, dimensione fissa,
    /// centrate sulla finestra principale.
    /// </summary>
    public static void ApplyDialog(Form form, string title)
    {
        form.Text = title;
        form.Icon = AppIcon;
        form.ShowIcon = false;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterParent;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ShowInTaskbar = false;
        form.AutoScaleMode = AutoScaleMode.Font;
        form.Font = Base;
        form.BackColor = Background;
        form.ForeColor = Text;
    }

    /// <summary>
    /// Etichetta di sezione in maiuscoletto grigio.
    /// </summary>
    public static Label SectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = Caption,
            ForeColor = TextMuted,
            UseMnemonic = false,
            Margin = new Padding(2, 0, 0, 6)
        };
    }

    /// <summary>
    /// Casella di testo a tema dentro una scheda col bordo che si accende.
    /// </summary>
    public static CardPanel TextBoxCard(TextBox box, int width)
    {
        box.Dock = DockStyle.Fill;
        box.BorderStyle = BorderStyle.None;
        box.BackColor = Surface;
        box.ForeColor = Text;
        box.Font = new Font("Segoe UI", 11f);

        var card = new CardPanel
        {
            Size = new Size(width, 38),
            Padding = new Padding(12, 8, 12, 4),
            Margin = new Padding(0, 0, 0, 6),
            BorderColor = Border
        };
        card.Controls.Add(box);

        box.Enter += (_, _) => card.BorderColor = Accent;
        box.Leave += (_, _) => card.BorderColor = Border;

        return card;
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
