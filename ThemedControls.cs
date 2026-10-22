using System.Drawing.Drawing2D;

namespace ControlSaveManager;

internal enum ButtonKind
{
    Primary,
    Secondary,
    Ghost
}

/// <summary>
/// Pulsante piatto con angoli arrotondati.
/// </summary>
internal sealed class FlatButton : Button
{
    private bool hover;
    private bool pressed;

    public ButtonKind Kind { get; }

    public FlatButton(string text, ButtonKind kind = ButtonKind.Secondary)
    {
        Text = text;
        Kind = kind;
        Font = Theme.Button;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hover = false;
        pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        if (!Enabled)
        {
            hover = false;
            pressed = false;
        }

        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        (Color back, Color border, Color fore) = GetColors();

        var rect = new RectangleF(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = Theme.RoundedRect(rect, LogicalToDeviceUnits(6));

        if (back != Color.Empty)
        {
            using var brush = new SolidBrush(back);
            g.FillPath(brush, path);
        }

        // Con la tastiera il pulsante attivo ha il bordo chiaro.
        if (Focused && ShowFocusCues)
            border = Theme.Text;

        if (border != Color.Empty)
        {
            using var pen = new Pen(border);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, fore,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis);
    }

    private (Color Back, Color Border, Color Fore) GetColors()
    {
        if (!Enabled)
        {
            return Kind == ButtonKind.Ghost
                ? (Color.Empty, Color.Empty, Theme.TextDisabled)
                : (Theme.Surface, Theme.Border, Theme.TextDisabled);
        }

        return Kind switch
        {
            ButtonKind.Primary => (
                pressed ? Theme.AccentPressed : hover ? Theme.AccentHover : Theme.Accent,
                Color.Empty,
                Color.White),

            ButtonKind.Secondary => (
                pressed ? Theme.SurfaceSelected : hover ? Theme.SurfaceHover : Theme.Surface,
                Theme.Border,
                Theme.Text),

            _ => (
                pressed ? Theme.SurfaceSelected : hover ? Theme.SurfaceHover : Color.Empty,
                Color.Empty,
                hover ? Theme.Text : Theme.TextMuted)
        };
    }
}

/// <summary>
/// Pannello con sfondo arrotondato ("scheda").
/// </summary>
internal sealed class CardPanel : Panel
{
    private Color borderColor = Color.Empty;

    /// <summary>Bordo della scheda; Color.Empty per nessun bordo.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;
            Invalidate();
        }
    }

    public CardPanel()
    {
        BackColor = Theme.Surface;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = Theme.RoundedRect(rect, LogicalToDeviceUnits(8));
        using var brush = new SolidBrush(BackColor);
        g.FillPath(brush, path);

        if (borderColor != Color.Empty)
        {
            using var pen = new Pen(borderColor);
            g.DrawPath(pen, path);
        }
    }
}

/// <summary>
/// Intestazione della finestra: titolo, sottotitolo e barra rossa.
/// </summary>
internal sealed class HeaderPanel : Control
{
    private readonly string title;
    private readonly string subtitle;

    public HeaderPanel(string title, string subtitle)
    {
        this.title = title;
        this.subtitle = subtitle;

        Dock = DockStyle.Top;
        Height = 68;
        BackColor = Theme.Header;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        int pad = LogicalToDeviceUnits(20);
        int barWidth = LogicalToDeviceUnits(4);
        int gap = LogicalToDeviceUnits(12);

        Size titleSize = TextRenderer.MeasureText(g, title, Theme.Title, Size.Empty, TextFormatFlags.NoPadding);
        Size subtitleSize = TextRenderer.MeasureText(g, subtitle, Theme.Caption, Size.Empty, TextFormatFlags.NoPadding);

        int spacing = LogicalToDeviceUnits(2);
        int blockHeight = titleSize.Height + spacing + subtitleSize.Height;
        int top = (Height - blockHeight) / 2;

        using (var accent = new SolidBrush(Theme.Accent))
            g.FillRectangle(accent, pad, top, barWidth, blockHeight);

        int x = pad + barWidth + gap;

        TextRenderer.DrawText(g, title, Theme.Title, new Point(x, top), Theme.Text,
            TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, subtitle, Theme.Caption,
            new Point(x + LogicalToDeviceUnits(1), top + titleSize.Height + spacing),
            Theme.TextMuted, TextFormatFlags.NoPadding);

        using var line = new Pen(Theme.Border);
        g.DrawLine(line, 0, Height - 1, Width, Height - 1);
    }
}

internal enum StatusKind
{
    Info,
    Busy,
    Success,
    Warning,
    Error
}

/// <summary>
/// Barra di stato in basso: pallino colorato più messaggio.
/// </summary>
internal sealed class StatusBar : Control
{
    private StatusKind kind;

    public StatusBar()
    {
        Dock = DockStyle.Bottom;
        Height = 34;
        BackColor = Theme.Header;
        Font = Theme.Small;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    public void SetStatus(string message, StatusKind kind)
    {
        if (Text == message && this.kind == kind)
            return;

        // Text è anche il nome letto dagli screen reader.
        Text = message;
        this.kind = kind;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;

        using (var line = new Pen(Theme.Border))
            g.DrawLine(line, 0, 0, Width, 0);

        int pad = LogicalToDeviceUnits(20);
        int dot = LogicalToDeviceUnits(8);

        Color dotColor = kind switch
        {
            StatusKind.Busy => Theme.Accent,
            StatusKind.Success => Theme.Success,
            StatusKind.Warning => Theme.Warning,
            StatusKind.Error => Theme.Error,
            _ => Theme.TextDisabled
        };

        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var brush = new SolidBrush(dotColor))
            g.FillEllipse(brush, pad, (Height - dot) / 2f, dot, dot);

        int x = pad + dot + LogicalToDeviceUnits(10);
        var textRect = new Rectangle(x, 0, Width - x - pad, Height);

        TextRenderer.DrawText(g, Text, Font, textRect,
            kind == StatusKind.Info ? Theme.TextMuted : Theme.Text,
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }
}
