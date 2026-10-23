using System.Drawing.Drawing2D;
using System.Media;

namespace ControlSaveManager;

/// <summary>
/// Finestra di messaggio a tema scuro, al posto di MessageBox.
/// Le conferme usano verbi sui pulsanti ("Elimina partita" / "Annulla"), come
/// chiedono le linee guida di Windows; Show resta per i messaggi semplici.
/// </summary>
internal sealed class MessageDialog : Form
{
    private const int TextWidth = 420;

    private readonly MessageBoxIcon icon;

    /// <summary>Un pulsante: testo, risultato e stile.</summary>
    private sealed record Choice(string Label, DialogResult Result, ButtonKind Kind);

    /// <summary>
    /// Messaggio senza finestra proprietaria (all'avvio).
    /// </summary>
    public static DialogResult Show(string text, string title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        return Show(null, text, title, buttons, icon);
    }

    public static DialogResult Show(
        IWin32Window? owner,
        string text,
        string title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
    {
        List<Choice> choices = buttons switch
        {
            MessageBoxButtons.YesNo =>
                [new(Strings.Yes, DialogResult.Yes, ButtonKind.Secondary), new(Strings.No, DialogResult.No, ButtonKind.Secondary)],
            MessageBoxButtons.YesNoCancel =>
                [new(Strings.Yes, DialogResult.Yes, ButtonKind.Secondary), new(Strings.No, DialogResult.No, ButtonKind.Secondary),
                 new(Strings.Cancel, DialogResult.Cancel, ButtonKind.Secondary)],
            MessageBoxButtons.OKCancel =>
                [new(Strings.OK, DialogResult.OK, ButtonKind.Secondary), new(Strings.Cancel, DialogResult.Cancel, ButtonKind.Secondary)],
            _ => [new(Strings.OK, DialogResult.OK, ButtonKind.Secondary)]
        };

        int defaultIndex = Math.Min(choices.Count - 1, defaultButton switch
        {
            MessageBoxDefaultButton.Button2 => 1,
            MessageBoxDefaultButton.Button3 => 2,
            _ => 0
        });

        // Il predefinito ha l'aspetto principale.
        choices[defaultIndex] = choices[defaultIndex] with { Kind = ButtonKind.Primary };

        return Run(owner, "", text, title, choices, defaultIndex, icon);
    }

    /// <summary>
    /// Conferma con un verbo: "do it" a sinistra, "Annulla" a destra e come
    /// scelta predefinita (Invio ed Esc annullano). true se l'utente conferma.
    /// </summary>
    /// <param name="heading">Domanda in grassetto ("Eliminare «Speedrun»?").</param>
    /// <param name="destructive">true se l'azione elimina qualcosa.</param>
    public static bool Confirm(IWin32Window? owner, string heading, string text,
        string confirmText, bool destructive = false, MessageBoxIcon icon = MessageBoxIcon.None)
    {
        var choices = new List<Choice>
        {
            new(confirmText, DialogResult.OK, destructive ? ButtonKind.Danger : ButtonKind.Secondary),
            new(Strings.Cancel, DialogResult.Cancel, ButtonKind.Primary)
        };

        return Run(owner, heading, text, Strings.AppTitle, choices, 1, icon) == DialogResult.OK;
    }

    /// <summary>
    /// Messaggio con titolo in grassetto e un solo pulsante.
    /// </summary>
    /// <param name="extraText">Secondo pulsante facoltativo (es. "Apri backup").</param>
    /// <returns>true se l'utente ha scelto il secondo pulsante.</returns>
    public static bool Inform(IWin32Window? owner, string heading, string text,
        MessageBoxIcon icon = MessageBoxIcon.None, string? extraText = null)
    {
        var choices = new List<Choice>();

        if (extraText != null)
            choices.Add(new Choice(extraText, DialogResult.Yes, ButtonKind.Secondary));

        choices.Add(new Choice(Strings.Close, DialogResult.OK, ButtonKind.Primary));

        return Run(owner, heading, text, Strings.AppTitle, choices, choices.Count - 1, icon) == DialogResult.Yes;
    }

    private static DialogResult Run(IWin32Window? owner, string heading, string text, string title,
        List<Choice> choices, int defaultIndex, MessageBoxIcon icon)
    {
        using var dialog = new MessageDialog(heading, text, title, choices, defaultIndex, icon);

        // Senza proprietario (all'avvio) la finestra va al centro dello schermo
        // e nella barra delle applicazioni, altrimenti si perde.
        if (owner == null)
        {
            dialog.StartPosition = FormStartPosition.CenterScreen;
            dialog.ShowInTaskbar = true;
        }

        PlaySound(icon);

        return owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
    }

    private MessageDialog(string heading, string text, string title, List<Choice> choices,
        int defaultIndex, MessageBoxIcon icon)
    {
        this.icon = icon;

        Text = title;
        Icon = Theme.AppIcon;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Font;
        Font = Theme.Base;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        var badge = new IconBadge(icon)
        {
            Size = new Size(36, 36),
            Margin = new Padding(0, 2, 16, 0),
            Visible = icon != MessageBoxIcon.None
        };

        var texts = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        if (heading != "")
        {
            texts.Controls.Add(new Label
            {
                Text = heading,
                AutoSize = true,
                MaximumSize = new Size(TextWidth, 0),
                Font = Theme.Heading,
                ForeColor = Theme.Text,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, 10)
            });
        }

        texts.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(TextWidth, 0),
            ForeColor = heading != "" ? Color.FromArgb(216, 216, 220) : Theme.Text,
            UseMnemonic = false,
            Margin = new Padding(0)
        });

        var body = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };
        body.Controls.Add(badge);
        body.Controls.Add(texts);

        var buttonRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 22, 0, 0)
        };

        var created = new List<FlatButton>();

        foreach (Choice choice in choices)
        {
            var button = new FlatButton(choice.Label, choice.Kind)
            {
                DialogResult = choice.Result,
                Margin = new Padding(10, 0, 0, 0)
            };
            button.FitText(36, 100);
            created.Add(button);
        }

        // RightToLeft: l'ultimo aggiunto sta a sinistra, quindi si aggiungono al contrario.
        for (int i = created.Count - 1; i >= 0; i--)
            buttonRow.Controls.Add(created[i]);

        AcceptButton = created[defaultIndex];

        // Esc: Annulla se c'è, altrimenti No, altrimenti l'ultimo pulsante.
        CancelButton =
            created.FirstOrDefault(b => b.DialogResult == DialogResult.Cancel) ??
            created.FirstOrDefault(b => b.DialogResult == DialogResult.No) ??
            created[^1];

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(24, 22, 24, 18)
        };
        layout.Controls.Add(body, 0, 0);
        layout.Controls.Add(buttonRow, 0, 1);

        Controls.Add(layout);

        Shown += (_, _) => created[defaultIndex].Focus();

        // Ctrl+C copia il messaggio, come in MessageBox.
        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                try
                {
                    Clipboard.SetText((heading != "" ? heading + Environment.NewLine + Environment.NewLine : "") + text);
                }
                catch
                {
                    // Appunti occupati: non importa.
                }

                e.Handled = true;
            }
        };
    }

    private static void PlaySound(MessageBoxIcon icon)
    {
        SystemSound? sound = icon switch
        {
            MessageBoxIcon.Error => SystemSounds.Hand,
            MessageBoxIcon.Warning => SystemSounds.Exclamation,
            MessageBoxIcon.Information => SystemSounds.Asterisk,
            MessageBoxIcon.Question => SystemSounds.Question,
            _ => null
        };

        sound?.Play();
    }

    /// <summary>
    /// Cerchio colorato con il simbolo del tipo di messaggio.
    /// </summary>
    private sealed class IconBadge : Control
    {
        private readonly MessageBoxIcon icon;

        public IconBadge(MessageBoxIcon icon)
        {
            this.icon = icon;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            (Color color, string symbol) = icon switch
            {
                MessageBoxIcon.Error => (Theme.Error, "×"),
                MessageBoxIcon.Warning => (Theme.Warning, "!"),
                MessageBoxIcon.Question => (Theme.Text, "?"),
                _ => (Theme.TextMuted, "i")
            };

            var circle = new Rectangle(1, 1, Width - 3, Height - 3);

            using (var fill = new SolidBrush(Color.FromArgb(40, color)))
                g.FillEllipse(fill, circle);

            using (var pen = new Pen(color, LogicalToDeviceUnits(2)))
                g.DrawEllipse(pen, circle);

            using var font = new Font("Segoe UI Semibold", 13f);
            TextRenderer.DrawText(g, symbol, font, circle, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
