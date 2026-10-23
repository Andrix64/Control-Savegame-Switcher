using System.Drawing.Drawing2D;

namespace ControlSaveManager;

internal enum ButtonKind
{
    Primary,
    Secondary,
    Ghost,

    /// <summary>Azioni che eliminano qualcosa.</summary>
    Danger
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
        UseMnemonic = false;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
    }

    /// <summary>
    /// Dà al pulsante la larghezza del testo più i margini, con un minimo.
    /// </summary>
    public FlatButton FitText(int height = 36, int minWidth = 0)
    {
        int width = TextRenderer.MeasureText(Text, Font).Width + 32;
        Size = new Size(Math.Max(width, minWidth), height);
        return this;
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

            ButtonKind.Danger => (
                pressed ? Theme.AccentPressed : hover ? Color.FromArgb(82, 30, 27) : Theme.AccentSoft,
                Theme.DangerBorder,
                hover ? Color.White : Theme.DangerText),

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

/// <summary>
/// Menu contestuali con i colori del tema scuro.
/// </summary>
internal sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
{
    public ThemedMenuRenderer() : base(new Colors())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.Text : Theme.TextDisabled;
        base.OnRenderItemText(e);
    }

    private sealed class Colors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Surface;
        public override Color ImageMarginGradientBegin => Theme.Surface;
        public override Color ImageMarginGradientMiddle => Theme.Surface;
        public override Color ImageMarginGradientEnd => Theme.Surface;
        public override Color MenuBorder => Theme.Border;
        public override Color MenuItemBorder => Theme.SurfaceSelected;
        public override Color MenuItemSelected => Theme.SurfaceSelected;
        public override Color SeparatorDark => Theme.Border;
        public override Color SeparatorLight => Theme.Border;
    }
}
