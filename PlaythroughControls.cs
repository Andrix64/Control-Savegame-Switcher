using System.Drawing.Drawing2D;

namespace Lss;

internal enum InfoKind
{
    Info,
    Warning,
    Success,
    Error
}

/// <summary>
/// Barra informativa (come l'InfoBar di Windows): icona, titolo, testo e un
/// pulsante facoltativo. Per gli stati di tutta l'app (Control aperto, Steam
/// Cloud attivo) e per l'esito delle operazioni.
/// </summary>
internal sealed class InfoBar : TableLayoutPanel
{
    private const int IconColumn = 34;

    private readonly Label titleLabel = new()
    {
        AutoSize = true,
        Font = Theme.Strong,
        ForeColor = Theme.Text,
        UseMnemonic = false,
        Margin = new Padding(0, 0, 0, 2)
    };

    private readonly Label textLabel = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(216, 216, 220),
        UseMnemonic = false,
        Margin = new Padding(0)
    };

    private readonly FlatButton actionButton = new("", ButtonKind.Ghost)
    {
        Visible = false,
        Anchor = AnchorStyles.Right,
        Margin = new Padding(10, 0, 0, 0)
    };

    private InfoKind kind;
    private Action? action;

    public InfoBar()
    {
        // Larghezza della cella, altezza calcolata dal testo (GetPreferredSize).
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        ColumnCount = 3;
        RowCount = 2;
        Padding = new Padding(IconColumn + 8, 10, 10, 10);
        Margin = new Padding(0, 0, 0, 10);
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(titleLabel, 0, 0);
        Controls.Add(textLabel, 0, 1);
        Controls.Add(actionButton, 1, 0);
        SetRowSpan(actionButton, 2);

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        actionButton.Click += (_, _) => action?.Invoke();
    }

    /// <param name="actionText">Testo del pulsante, o null per nessun pulsante.</param>
    public void Show(InfoKind kind, string title, string text, string? actionText = null, Action? action = null)
    {
        this.kind = kind;
        this.action = action;

        // Etichette e pulsante prendono il colore della barra.
        BackColor = kind switch
        {
            InfoKind.Warning => Theme.WarningBack,
            InfoKind.Success => Theme.SuccessBack,
            InfoKind.Error => Theme.ErrorBack,
            _ => Theme.InfoBack
        };

        titleLabel.Text = title;
        titleLabel.Visible = title != "";
        textLabel.Text = text;
        textLabel.Visible = text != "";

        actionButton.Visible = actionText != null;

        if (actionText != null)
        {
            actionButton.Text = actionText;
            actionButton.FitText(32);
        }

        Visible = true;
        FitLabels(Width);
        Parent?.PerformLayout();
        Invalidate();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        FitLabels(Width);
    }

    /// <summary>
    /// Fa andare a capo i testi lunghi alla larghezza data.
    /// </summary>
    /// <returns>L'altezza che serve alla barra per mostrare tutto il testo.</returns>
    private int FitLabels(int width)
    {
        int available = width - Padding.Horizontal
                        - (actionButton.Visible ? actionButton.Width + actionButton.Margin.Horizontal : 0) - 4;

        if (available <= 50)
            return Padding.Vertical + LogicalToDeviceUnits(22);

        var max = new Size(available, 0);
        titleLabel.MaximumSize = max;
        textLabel.MaximumSize = max;

        int text = 0;

        if (titleLabel.Visible)
            text += titleLabel.GetPreferredSize(max).Height + titleLabel.Margin.Vertical;

        if (textLabel.Visible)
            text += textLabel.GetPreferredSize(max).Height + textLabel.Margin.Vertical;

        int content = Math.Max(text, actionButton.Visible ? actionButton.Height : 0);
        return Math.Max(content, LogicalToDeviceUnits(22)) + Padding.Vertical;
    }

    /// <summary>
    /// La larghezza la decide la cella della tabella; l'altezza segue il testo
    /// a quella larghezza.
    /// </summary>
    public override Size GetPreferredSize(Size proposedSize)
    {
        int width = proposedSize.Width > 0 && proposedSize.Width < 100000 ? proposedSize.Width : Width;
        return new Size(width, FitLabels(width));
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        (Color back, Color border, Color icon) = kind switch
        {
            InfoKind.Warning => (Theme.WarningBack, Theme.WarningBorder, Theme.Warning),
            InfoKind.Success => (Theme.SuccessBack, Theme.SuccessBorder, Theme.Success),
            InfoKind.Error => (Theme.ErrorBack, Theme.ErrorBorder, Theme.Error),
            _ => (Theme.InfoBack, Theme.InfoBorder, Theme.InfoBlue)
        };

        var rect = new RectangleF(0, 0, Width - 1, Height - 1);

        using (GraphicsPath path = Theme.RoundedRect(rect, LogicalToDeviceUnits(8)))
        using (var brush = new SolidBrush(back))
        using (var pen = new Pen(border))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        // Icona: cerchio con simbolo, allineato alla prima riga di testo.
        int size = LogicalToDeviceUnits(20);
        var circle = new Rectangle(LogicalToDeviceUnits(14), Padding.Top + 1, size, size);

        using (var pen = new Pen(icon, LogicalToDeviceUnits(2)))
            g.DrawEllipse(pen, circle);

        string symbol = kind switch
        {
            InfoKind.Warning => "!",
            InfoKind.Success => "✓",
            InfoKind.Error => "×",
            _ => "i"
        };

        TextRenderer.DrawText(g, symbol, Theme.Strong, circle, icon,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

/// <summary>
/// Etichetta a pillola, per "NEL GIOCO ADESSO".
/// </summary>
internal sealed class Pill : Control
{
    public Pill(string text)
    {
        Text = text;
        Font = Theme.Caption;
        ForeColor = Theme.AccentSoftText;
        BackColor = Theme.AccentSoft;
        Margin = new Padding(10, 3, 0, 0);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

        Size size = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPadding);
        Size = new Size(size.Width + 18, size.Height + 8);
        AccessibleRole = AccessibleRole.StaticText;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Surface);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (GraphicsPath path = Theme.RoundedRect(new RectangleF(0, 0, Width - 1, Height - 1), (Height - 1) / 2f))
        using (var brush = new SolidBrush(BackColor))
            g.FillPath(brush, path);

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

/// <summary>
/// Azioni di una scheda partita.
/// </summary>
internal enum CardAction
{
    /// <summary>Carica la partita e avvia il gioco (o avvia il gioco, se è già caricata).</summary>
    Play,
    LoadOnly,
    SavePoint,
    History,
    Rename,
    EditNote,
    Export,
    Delete
}

/// <summary>
/// Scheda di una partita nella finestra principale: nome, nota, dettagli e
/// le sue azioni, tutte visibili (il resto nel menu "⋯").
/// </summary>
internal sealed class PlaythroughCard : TableLayoutPanel
{
    private readonly FlatButton playButton;
    private readonly FlatButton? savePointButton;
    private readonly ToolStripMenuItem loadItem;
    private readonly ToolStripMenuItem deleteItem;

    public CardData Data { get; }

    public event Action<PlaythroughCard, CardAction>? ActionRequested;

    public PlaythroughCard(CardData data)
    {
        Data = data;

        BackColor = Theme.Surface;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(18, 14, 14, 14);
        Margin = new Padding(0, 0, 0, 10);
        ColumnCount = 2;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        string name = SaveManager.ProfileDisplayName(data.ProfileName);
        AccessibleName = data.Active ? name + Strings.ActiveSuffix : name;

        // Titolo e, per la partita nel gioco, la pillola.
        var titleRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 4)
        };
        titleRow.Controls.Add(new Label
        {
            Text = name,
            AutoSize = true,
            Font = Theme.CardTitle,
            ForeColor = Theme.Text,
            UseMnemonic = false,
            Margin = new Padding(0)
        });

        if (data.Active)
            titleRow.Controls.Add(new Pill(Strings.InGameNow));

        var info = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0)
        };
        info.Controls.Add(titleRow);

        if (data.Info.Note != "")
        {
            info.Controls.Add(new Label
            {
                Text = data.Info.Note,
                AutoSize = true,
                Font = Theme.Note,
                ForeColor = Color.FromArgb(216, 216, 220),
                UseMnemonic = false,
                MaximumSize = new Size(420, 0),
                Margin = new Padding(0, 0, 0, 2)
            });
        }

        info.Controls.Add(new Label
        {
            Text = CardText.Details(data),
            AutoSize = true,
            Font = Theme.Small,
            ForeColor = Theme.TextSecondary,
            UseMnemonic = false,
            MaximumSize = new Size(440, 0),
            Margin = new Padding(0)
        });

        if (data.Info.Note == "")
        {
            var addNote = new LinkLabel
            {
                Text = Strings.AddNote,
                AutoSize = true,
                Font = Theme.Small,
                LinkColor = Theme.AccentSoftText,
                ActiveLinkColor = Color.White,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Margin = new Padding(0, 2, 0, 0)
            };
            addNote.LinkClicked += (_, _) => Raise(CardAction.EditNote);
            info.Controls.Add(addNote);
        }

        Controls.Add(info, 0, 0);

        // Menu "⋯".
        var menu = new ContextMenuStrip
        {
            Renderer = new ThemedMenuRenderer(),
            ShowImageMargin = false,
            Font = Theme.Base
        };
        loadItem = new ToolStripMenuItem(Strings.LoadWithoutStarting);
        loadItem.Click += (_, _) => Raise(CardAction.LoadOnly);
        deleteItem = new ToolStripMenuItem(Strings.DeletePlaythroughMenu);
        deleteItem.Click += (_, _) => Raise(CardAction.Delete);

        if (!data.Active)
            menu.Items.Add(loadItem);

        menu.Items.Add(new ToolStripMenuItem(Strings.RenameMenu, null, (_, _) => Raise(CardAction.Rename)));
        menu.Items.Add(new ToolStripMenuItem(Strings.EditNote, null, (_, _) => Raise(CardAction.EditNote)));
        menu.Items.Add(new ToolStripMenuItem(Strings.ExportMenu, null, (_, _) => Raise(CardAction.Export)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(deleteItem);

        if (data.Active)
        {
            deleteItem.Enabled = false;
            deleteItem.ToolTipText = Strings.ActiveCannotBeDeleted;
        }

        var moreButton = new FlatButton("⋯")
        {
            Size = new Size(38, 36),
            Margin = new Padding(8, 0, 0, 0),
            AccessibleName = Loc.F(Strings.MoreActionsFor, name)
        };
        moreButton.Click += (_, _) => menu.Show(moreButton, new Point(moreButton.Width - menu.Width, moreButton.Height + 4));
        menu.Opening += (_, _) => menu.PerformLayout();

        // Azioni: per la partita nel gioco sotto i dati, per le altre a destra.
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Right
        };

        playButton = new FlatButton(data.Active ? Strings.PlayButton : Strings.PlayThisButton,
            data.Active ? ButtonKind.Secondary : ButtonKind.Primary).FitText(36, 96);
        playButton.Margin = new Padding(0);
        playButton.Click += (_, _) => Raise(CardAction.Play);

        if (data.Active)
        {
            savePointButton = new FlatButton(Strings.SavePointButton).FitText();
            savePointButton.Margin = new Padding(8, 0, 0, 0);
            savePointButton.Click += (_, _) => Raise(CardAction.SavePoint);

            var historyButton = new FlatButton(Strings.HistoryButton).FitText();
            historyButton.Margin = new Padding(8, 0, 0, 0);
            historyButton.Click += (_, _) => Raise(CardAction.History);

            actions.Controls.AddRange(new Control[] { playButton, savePointButton, historyButton, moreButton });
            actions.Anchor = AnchorStyles.Left;
            actions.Margin = new Padding(0, 10, 0, 0);

            RowCount = 2;
            Controls.Add(actions, 0, 1);
            SetColumnSpan(actions, 2);
        }
        else
        {
            actions.Controls.AddRange(new Control[] { playButton, moreButton });
            Controls.Add(actions, 1, 0);
        }
    }

    /// <summary>
    /// Disattiva le azioni che toccano i salvataggi del gioco.
    /// </summary>
    public void SetGameRunning(bool running, bool launchGame)
    {
        playButton.Enabled = !running;
        loadItem.Enabled = !running;

        // Con l'avvio spento "Gioca" sulla partita nel gioco non ha niente da fare.
        if (Data.Active)
            playButton.Visible = launchGame;

        if (savePointButton != null)
            savePointButton.Enabled = !running;

        if (!Data.Active)
            deleteItem.Enabled = !running;
    }

    private void Raise(CardAction action) => ActionRequested?.Invoke(this, action);

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = Theme.RoundedRect(rect, LogicalToDeviceUnits(10));
        using var brush = new SolidBrush(BackColor);
        using var pen = new Pen(Data.Active ? Theme.Accent : Theme.Border);

        g.FillPath(brush, path);
        g.DrawPath(pen, path);
    }
}
