using System.Drawing.Drawing2D;
using System.Media;

namespace ControlSaveManager;

/// <summary>
/// Finestra di messaggio a tema scuro, al posto di MessageBox.
/// Stessi parametri di MessageBox.Show per i casi usati nell'app.
/// </summary>
internal sealed class MessageDialog : Form
{
    private const int TextWidth = 400;

    private readonly MessageBoxIcon icon;

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
        using var dialog = new MessageDialog(text, title, buttons, icon, defaultButton);

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

    private MessageDialog(string text, string title, MessageBoxButtons buttons,
        MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
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

        var message = new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(TextWidth, 0),
            ForeColor = Theme.Text,
            UseMnemonic = false,
            Margin = new Padding(0, 0, 0, 0)
        };

        var body = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };
        body.Controls.Add(badge);
        body.Controls.Add(message);

        var buttonRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 22, 0, 0)
        };

        List<(string Label, DialogResult Result)> choices = buttons switch
        {
            MessageBoxButtons.YesNo => [(Strings.Yes, DialogResult.Yes), (Strings.No, DialogResult.No)],
            MessageBoxButtons.YesNoCancel =>
                [(Strings.Yes, DialogResult.Yes), (Strings.No, DialogResult.No), (Strings.Cancel, DialogResult.Cancel)],
            MessageBoxButtons.OKCancel => [(Strings.OK, DialogResult.OK), (Strings.Cancel, DialogResult.Cancel)],
            _ => [(Strings.OK, DialogResult.OK)]
        };

        int defaultIndex = defaultButton switch
        {
            MessageBoxDefaultButton.Button2 => 1,
            MessageBoxDefaultButton.Button3 => 2,
            _ => 0
        };
        defaultIndex = Math.Min(defaultIndex, choices.Count - 1);

        var created = new List<FlatButton>();

        for (int i = 0; i < choices.Count; i++)
        {
            var button = new FlatButton(choices[i].Label, i == defaultIndex ? ButtonKind.Primary : ButtonKind.Secondary)
            {
                DialogResult = choices[i].Result,
                Size = new Size(100, 34),
                Margin = new Padding(10, 0, 0, 0)
            };
            created.Add(button);
        }

        // RightToLeft: l'ultimo aggiunto sta a sinistra, quindi si aggiungono al contrario.
        for (int i = created.Count - 1; i >= 0; i--)
            buttonRow.Controls.Add(created[i]);

        AcceptButton = created[defaultIndex];

        // Esc: Annulla se c'è, altrimenti No, altrimenti OK, come MessageBox.
        CancelButton =
            created.FirstOrDefault(b => b.DialogResult == DialogResult.Cancel) ??
            created.FirstOrDefault(b => b.DialogResult == DialogResult.No) ??
            created[0];

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
                    Clipboard.SetText(title + Environment.NewLine + Environment.NewLine + text);
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
