using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace Lss;

/// <summary>
/// Lista a tema (savestate, backup, account): titolo e dettagli su due righe.
/// </summary>
internal sealed class ItemList : ListBox
{
    private const int RowHeight = 50;

    /// <param name="Item">Oggetto rappresentato dalla riga.</param>
    /// <param name="Title">Riga principale.</param>
    /// <param name="Details">Riga secondaria.</param>
    public sealed record Row(object Item, string Title, string Details)
    {
        /// <summary>
        /// Riga di un savestate o backup, con la data come dettaglio.
        /// </summary>
        public static Row For(Snapshot snapshot, string title) =>
            new(snapshot, title, snapshot.Date.ToString("G"));

        // Usato dagli screen reader.
        public override string ToString() => Title + ", " + Details;
    }

    public ItemList()
    {
        Dock = DockStyle.Fill;
        IntegralHeight = false;
        BorderStyle = BorderStyle.None;
        DrawMode = DrawMode.OwnerDrawFixed;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
    }

    public object? SelectedRowItem => (SelectedItem as Row)?.Item;

    public Snapshot? SelectedSnapshot => SelectedRowItem as Snapshot;

    public void SetRows(IEnumerable<Row> rows)
    {
        object? selected = SelectedRowItem;

        BeginUpdate();
        Items.Clear();

        foreach (Row row in rows)
        {
            Items.Add(row);

            if (row.Item.Equals(selected))
                SelectedItem = row;
        }

        EndUpdate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ItemHeight = LogicalToDeviceUnits(RowHeight);
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ItemHeight = LogicalToDeviceUnits(RowHeight);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count)
            return;

        var row = (Row)Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var back = new SolidBrush(Theme.Surface))
            g.FillRectangle(back, e.Bounds);

        int Px(int value) => LogicalToDeviceUnits(value);

        Rectangle inner = Rectangle.Inflate(e.Bounds, -Px(2), -Px(2));

        if (selected)
        {
            using GraphicsPath path = Theme.RoundedRect(inner, Px(6));
            using var brush = new SolidBrush(Theme.SurfaceSelected);
            g.FillPath(brush, path);
        }

        const TextFormatFlags flags =
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix;

        int x = inner.X + Px(14);
        int width = inner.Right - Px(14) - x;
        int middle = inner.Y + inner.Height / 2;

        TextRenderer.DrawText(g, row.Title, Theme.ItemName,
            new Rectangle(x, middle - Px(21), width, Px(22)), Theme.Text, flags);
        TextRenderer.DrawText(g, row.Details, Theme.Small,
            new Rectangle(x, middle + Px(1), width, Px(19)), Theme.TextMuted, flags);
    }
}

/// <summary>
/// Base delle finestre con una lista di savestate o backup e i pulsanti a destra.
/// </summary>
internal abstract class ListDialog : Form
{
    protected readonly SaveManager Manager;
    protected readonly ItemList List = new();

    private readonly Func<bool> confirmSteamClosed;

    private readonly Label caption = MakeCaption("");
    private readonly Label emptyLabel;
    private readonly FlowLayoutPanel buttons;

    private readonly Label statusLabel = new()
    {
        AutoSize = true,
        Font = Theme.Small,
        ForeColor = Theme.TextMuted,
        Margin = new Padding(2, 12, 0, 0)
    };

    private readonly FlatButton closeButton = new(Strings.Close)
    {
        DialogResult = DialogResult.Cancel
    };

    // Come nella finestra principale: il gioco viene controllato ogni 2 secondi.
    private readonly System.Windows.Forms.Timer gameTimer = new() { Interval = 2000 };
    private bool checkingGame;

    // true mentre un'operazione sui file è in corso.
    protected bool Busy { get; private set; }

    /// <summary>
    /// true mentre Control è in esecuzione: le operazioni sui salvataggi del
    /// gioco vanno disattivate.
    /// </summary>
    protected bool GameRunning { get; private set; }

    /// <summary>
    /// true se si possono toccare i salvataggi del gioco.
    /// </summary>
    protected bool CanTouchGame => !Busy && !GameRunning;

    /// <summary>
    /// true se è stata fatta almeno un'operazione: la finestra principale va aggiornata.
    /// </summary>
    public bool Changed { get; private set; }

    /// <param name="top">Controlli sopra la lista (facoltativi).</param>
    /// <param name="confirmSteamClosed">Avviso di Steam aperto della finestra principale.</param>
    protected ListDialog(SaveManager manager, string title, string emptyText,
        Control? top, Func<bool> confirmSteamClosed)
    {
        Manager = manager;
        this.confirmSteamClosed = confirmSteamClosed;

        Text = title;
        Icon = Theme.AppIcon;
        ShowIcon = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        Font = Theme.Base;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        ClientSize = new Size(600, 470);
        CancelButton = closeButton;

        emptyLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = emptyText,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.TextMuted,
            Visible = false
        };

        var listCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6),
            Margin = new Padding(0)
        };
        listCard.Controls.Add(List);
        listCard.Controls.Add(emptyLabel);

        buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(16, 0, 0, 0)
        };

        closeButton.Size = new Size(110, 36);
        closeButton.Margin = new Padding(0);
        closeButton.Anchor = AnchorStyles.Right;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 18, 20, 16),
            ColumnCount = 2,
            RowCount = 5
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        if (top != null)
        {
            root.Controls.Add(top, 0, 0);
            root.SetColumnSpan(top, 2);
        }

        caption.Margin = new Padding(2, 0, 0, 8);
        root.Controls.Add(caption, 0, 1);
        root.Controls.Add(listCard, 0, 2);
        root.Controls.Add(buttons, 1, 2);
        root.Controls.Add(statusLabel, 0, 3);
        root.SetColumnSpan(statusLabel, 2);
        root.Controls.Add(closeButton, 1, 4);

        Controls.Add(root);

        List.SelectedIndexChanged += (_, _) => UpdateButtons();

        gameTimer.Tick += async (_, _) => await CheckGameAsync();

        Load += async (_, _) =>
        {
            Reload();
            await CheckGameAsync();
            gameTimer.Start();
        };

        FormClosing += (_, e) =>
        {
            // Non interrompere una copia a metà.
            if (Busy)
                e.Cancel = true;
        };

        FormClosed += (_, _) => gameTimer.Dispose();
    }

    private async Task CheckGameAsync()
    {
        if (checkingGame)
            return;

        checkingGame = true;

        try
        {
            bool running = await Task.Run(SaveManager.IsGameRunning);

            if (running == GameRunning || IsDisposed)
                return;

            GameRunning = running;

            // Il messaggio sul gioco aperto prende il posto dell'ultimo esito.
            if (!Busy)
                SetStatus(running ? Loc.F(Strings.GameRunning, Manager.Game.Name) : "", running ? Theme.Warning : Theme.TextMuted);

            UpdateButtons();
        }
        finally
        {
            checkingGame = false;
        }
    }

    protected static Label MakeCaption(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = Theme.Caption,
            ForeColor = Theme.TextMuted,
            Margin = new Padding(2, 0, 0, 6)
        };
    }

    protected FlatButton AddButton(string text, ButtonKind kind, Func<Task> onClick)
    {
        var button = new FlatButton(text, kind)
        {
            Size = new Size(170, 38),
            Margin = new Padding(0, 0, 0, 10)
        };

        button.Click += async (_, _) =>
        {
            if (!Busy)
                await onClick();
        };

        buttons.Controls.Add(button);
        return button;
    }

    /// <summary>
    /// Elementi da mostrare, dal più recente.
    /// </summary>
    protected abstract IEnumerable<ItemList.Row> LoadRows();

    protected abstract string CaptionText(int count);

    /// <summary>
    /// Abilita i pulsanti in base alla selezione e allo stato.
    /// </summary>
    protected abstract void UpdateButtons();

    protected void Reload()
    {
        try
        {
            List.SetRows(LoadRows());
        }
        catch (Exception ex)
        {
            SetStatus(Loc.F(Strings.CannotReadList, ex.Message), Theme.Error);
        }

        int count = List.Items.Count;
        caption.Text = CaptionText(count);
        emptyLabel.Visible = count == 0;
        List.Visible = count > 0;

        UpdateButtons();
    }

    protected bool ConfirmSteamClosed() => confirmSteamClosed();

    protected bool Ask(string question, MessageBoxIcon icon = MessageBoxIcon.Question)
    {
        // Con un avviso il pulsante predefinito è "No".
        MessageBoxDefaultButton defaultButton = icon == MessageBoxIcon.Warning
            ? MessageBoxDefaultButton.Button2
            : MessageBoxDefaultButton.Button1;

        return MessageDialog.Show(this, question, Text,
            MessageBoxButtons.YesNo, icon, defaultButton) == DialogResult.Yes;
    }

    /// <summary>
    /// Esegue un'operazione sui file in background e ne mostra l'esito.
    /// </summary>
    protected async Task<Result> RunAsync(string message, Func<Result> action)
    {
        Busy = true;
        UseWaitCursor = true;
        SetStatus(message, Theme.TextMuted);
        UpdateButtons();

        Result result;

        try
        {
            result = await Task.Run(action);
        }
        catch (Exception ex)
        {
            Log.Write("Errore imprevisto (" + message + ")", ex);
            result = Result.Fail(Loc.F(Strings.UnexpectedError, ex.Message, Log.FilePath));
        }
        finally
        {
            Busy = false;
            UseWaitCursor = false;
            Changed = true;
        }

        Reload();
        ShowResult(result);
        return result;
    }

    private void ShowResult(Result result)
    {
        SetStatus(result.Message.Split('\n')[0], result.Success ? Theme.Success : Theme.Error);

        if (result.Success)
            return;

        MessageDialog.Show(this, result.Message,
            result.Critical ? Strings.TitleSeriousError : Strings.TitleOperationFailed,
            MessageBoxButtons.OK,
            result.Critical ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
    }

    protected void SetStatus(string message, Color color)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = color;
    }

    /// <param name="createIfMissing">
    /// true solo per le cartelle radice (Backup, Savestate del profilo): un
    /// singolo backup o savestate sparito non va ricreato vuoto.
    /// </param>
    protected void OpenFolder(string path, bool createIfMissing)
    {
        if (path == "")
            return;

        try
        {
            if (!Directory.Exists(path))
            {
                if (!createIfMissing)
                {
                    // Eliminata da fuori mentre la finestra era aperta.
                    Reload();
                    SetStatus(Loc.F(Strings.FolderNotFound, path).Replace('\n', ' '), Theme.Error);
                    return;
                }

                Directory.CreateDirectory(path);
            }

            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageDialog.Show(this, Loc.F(Strings.CannotOpenFolder, ex.Message),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

/// <summary>
/// Partite eliminate che si possono ancora recuperare (finché la pulizia
/// delle copie automatiche non le elimina).
/// </summary>
internal sealed class DeletedForm : ListDialog
{
    private readonly FlatButton recoverButton;

    public DeletedForm(SaveManager manager)
        : base(manager, Strings.DeletedTitle, Strings.NoDeleted, null, () => true)
    {
        recoverButton = AddButton(Strings.Recover, ButtonKind.Primary, RecoverAsync);
        List.DoubleClick += async (_, _) =>
        {
            if (recoverButton.Enabled)
                await RecoverAsync();
        };
    }

    protected override IEnumerable<ItemList.Row> LoadRows()
    {
        return Manager.GetDeletedProfiles()
            .Select(d => new ItemList.Row(d, SaveManager.ProfileDisplayName(d.Owner),
                Loc.F(Strings.DeletedOn, d.Date.ToString("g"))));
    }

    protected override string CaptionText(int count)
    {
        return count == 0 ? Strings.CaptionDeleted : $"{Strings.CaptionDeleted}  ·  {count}";
    }

    protected override void UpdateButtons()
    {
        // Chiamato anche dal costruttore della base, prima che i campi esistano.
        if (recoverButton == null)
            return;

        recoverButton.Enabled = !Busy && List.SelectedSnapshot != null;
    }

    private async Task RecoverAsync()
    {
        if (List.SelectedSnapshot is not Snapshot deleted)
            return;

        await RunAsync(Strings.BusyRecovering, () => Manager.RecoverDeletedProfile(deleted));
    }
}
