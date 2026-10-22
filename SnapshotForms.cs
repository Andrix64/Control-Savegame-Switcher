using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ControlSaveManager;

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

    // true mentre un'operazione sui file è in corso.
    protected bool Busy { get; private set; }

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

        Load += (_, _) => Reload();

        FormClosing += (_, e) =>
        {
            // Non interrompere una copia a metà.
            if (Busy)
                e.Cancel = true;
        };
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

    protected void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
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
/// Savestate del profilo attivo: crea, ripristina, elimina.
/// </summary>
internal sealed class SavestateForm : ListDialog
{
    private static readonly string Hint = Strings.SavestateHint;

    private readonly TextBox nameBox;
    private readonly CardPanel nameCard;
    private readonly Label hintLabel;
    private readonly FlatButton createButton;

    private readonly FlatButton restoreButton;
    private readonly FlatButton deleteButton;

    public SavestateForm(SaveManager manager, Func<bool> confirmSteamClosed)
        : this(manager, confirmSteamClosed, BuildTop(out TextBox box, out CardPanel card,
            out Label hint, out FlatButton create), box, card, hint, create)
    {
    }

    private SavestateForm(SaveManager manager, Func<bool> confirmSteamClosed, Control top,
        TextBox nameBox, CardPanel nameCard, Label hintLabel, FlatButton createButton)
        : base(manager, Strings.SavestatesTitle,
            Strings.NoSavestates,
            top, confirmSteamClosed)
    {
        this.nameBox = nameBox;
        this.nameCard = nameCard;
        this.hintLabel = hintLabel;
        this.createButton = createButton;

        restoreButton = AddButton(Strings.Restore, ButtonKind.Primary, RestoreAsync);
        deleteButton = AddButton(Strings.Delete, ButtonKind.Secondary, DeleteAsync);
        FlatButton folderButton = AddButton(Strings.OpenFolder, ButtonKind.Ghost, () =>
        {
            OpenFolder(Manager.ActiveSavestatePath);
            return Task.CompletedTask;
        });
        folderButton.Margin = new Padding(0, 14, 0, 0);

        AcceptButton = createButton;

        createButton.Click += async (_, _) =>
        {
            if (!Busy)
                await CreateAsync();
        };

        List.DoubleClick += async (_, _) =>
        {
            if (restoreButton.Enabled)
                await RestoreAsync();
        };

        nameBox.Enter += (_, _) => nameCard.BorderColor = Theme.Accent;
        nameBox.Leave += (_, _) => nameCard.BorderColor = Theme.Border;
        nameBox.TextChanged += (_, _) => UpdateButtons();
    }

    private static Control BuildTop(out TextBox nameBox, out CardPanel nameCard,
        out Label hintLabel, out FlatButton createButton)
    {
        nameBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI", 11f),
            MaxLength = 50
        };

        nameCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Height = 38,
            Padding = new Padding(12, 8, 12, 4),
            Margin = new Padding(0),
            BorderColor = Theme.Border
        };
        nameCard.Controls.Add(nameBox);

        createButton = new FlatButton(Strings.CreateSavestate, ButtonKind.Primary)
        {
            Size = new Size(170, 38),
            Margin = new Padding(16, 0, 0, 0)
        };

        hintLabel = new Label
        {
            AutoSize = true,
            Text = Hint,
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            Margin = new Padding(2, 6, 0, 18)
        };

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0)
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        Label caption = MakeCaption(Strings.CaptionNewSavestate);
        top.Controls.Add(caption, 0, 0);
        top.SetColumnSpan(caption, 2);
        top.Controls.Add(nameCard, 0, 1);
        top.Controls.Add(createButton, 1, 1);
        top.Controls.Add(hintLabel, 0, 2);
        top.SetColumnSpan(hintLabel, 2);

        return top;
    }

    protected override IEnumerable<ItemList.Row> LoadRows()
    {
        return Manager.GetSavestates()
            .Select(s => ItemList.Row.For(s, s.Name == "" ? Strings.Unnamed : s.Name));
    }

    protected override string CaptionText(int count)
    {
        string profile = SaveManager.ProfileDisplayName(Manager.State.ActiveProfile).ToUpperInvariant();
        string caption = Loc.F(Strings.CaptionSavestatesOf, profile);
        return count == 0 ? caption : $"{caption}  ·  {count}";
    }

    protected override void UpdateButtons()
    {
        // Chiamato anche dal costruttore della base, prima che i campi esistano.
        if (createButton == null)
            return;

        string name = nameBox.Text.Trim();
        bool valid = SaveManager.IsValidSavestateName(name);

        hintLabel.Text = valid ? Hint : Strings.InvalidSavestateName;
        hintLabel.ForeColor = valid ? Theme.TextMuted : Theme.Error;

        bool selected = List.SelectedSnapshot != null;

        createButton.Enabled = !Busy && valid && Manager.HasActiveProfile;
        restoreButton.Enabled = !Busy && selected;
        deleteButton.Enabled = !Busy && selected;
    }

    private async Task CreateAsync()
    {
        string name = nameBox.Text.Trim();

        Result result = await RunAsync(Strings.BusyCreatingSavestate, () => Manager.CreateSavestate(name));

        if (result.Success)
        {
            nameBox.Clear();
            List.SelectedIndex = List.Items.Count > 0 ? 0 : -1;
        }
    }

    /// <summary>
    /// Nessuna conferma: i salvataggi attuali vanno comunque in un backup.
    /// </summary>
    private async Task RestoreAsync()
    {
        if (List.SelectedSnapshot is not Snapshot savestate || !ConfirmSteamClosed())
            return;

        await RunAsync(Strings.BusyRestoringSavestate, () => Manager.RestoreSavestate(savestate));
    }

    private async Task DeleteAsync()
    {
        if (List.SelectedSnapshot is not Snapshot savestate)
            return;

        string name = savestate.Name == "" ? Strings.UnnamedLower : $"\"{savestate.Name}\"";

        if (!Ask(Loc.F(Strings.ConfirmDeleteSavestate, name, savestate.Date.ToString("g")),
                 MessageBoxIcon.Warning))
            return;

        await RunAsync(Strings.BusyDeletingSavestate, () => Manager.DeleteSavestate(savestate));
    }
}

/// <summary>
/// Backup automatici: ripristino e numero da conservare.
/// </summary>
internal sealed class BackupForm : ListDialog
{
    private readonly NumericUpDown keepBox;
    private readonly FlatButton applyButton;
    private readonly FlatButton restoreButton;

    public BackupForm(SaveManager manager, Func<bool> confirmSteamClosed)
        : this(manager, confirmSteamClosed,
            BuildTop(out NumericUpDown keep, out FlatButton apply), keep, apply)
    {
    }

    private BackupForm(SaveManager manager, Func<bool> confirmSteamClosed, Control top,
        NumericUpDown keepBox, FlatButton applyButton)
        : base(manager, Strings.BackupsTitle, Strings.NoBackups, top, confirmSteamClosed)
    {
        this.keepBox = keepBox;
        this.applyButton = applyButton;

        restoreButton = AddButton(Strings.Restore, ButtonKind.Primary, RestoreAsync);
        FlatButton folderButton = AddButton(Strings.OpenFolder, ButtonKind.Ghost, () =>
        {
            OpenFolder(List.SelectedSnapshot?.Path ?? Manager.BackupPath);
            return Task.CompletedTask;
        });
        folderButton.Margin = new Padding(0, 14, 0, 0);

        keepBox.Value = Math.Min(keepBox.Maximum, Manager.State.BackupsToKeep);
        keepBox.ValueChanged += (_, _) => UpdateButtons();

        applyButton.Click += async (_, _) =>
        {
            if (!Busy)
                await ApplyKeepAsync();
        };
    }

    private static Control BuildTop(out NumericUpDown keepBox, out FlatButton applyButton)
    {
        applyButton = new FlatButton(Strings.Apply)
        {
            Size = new Size(90, 30),
            Margin = new Padding(8, 0, 0, 0)
        };

        keepBox = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 999,
            Width = 70,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Center,
            Margin = new Padding(8, 0, 8, 0)
        };

        var top = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 16)
        };

        Label MakeText(string text) => new()
        {
            Text = text,
            AutoSize = true,
            ForeColor = Theme.Text,
            Margin = new Padding(0, 3, 0, 0)
        };

        top.Controls.Add(MakeText(Strings.KeepLast));
        top.Controls.Add(keepBox);
        top.Controls.Add(MakeText(Strings.KeepLastSuffix));
        top.Controls.Add(applyButton);

        return top;
    }

    protected override IEnumerable<ItemList.Row> LoadRows()
    {
        return Manager.GetBackups()
            .Select(b => ItemList.Row.For(b, SaveManager.BackupReasonText(b.Name)));
    }

    protected override string CaptionText(int count)
    {
        return count == 0 ? Strings.CaptionBackups : $"{Strings.CaptionBackups}  ·  {count}";
    }

    protected override void UpdateButtons()
    {
        // Chiamato anche dal costruttore della base, prima che i campi esistano.
        if (restoreButton == null)
            return;

        restoreButton.Enabled = !Busy && List.SelectedSnapshot != null;
        keepBox.Enabled = !Busy;
        applyButton.Enabled = !Busy && (int)keepBox.Value != Manager.State.BackupsToKeep;
    }

    private async Task ApplyKeepAsync()
    {
        int keep = (int)keepBox.Value;
        int excess = keep == 0 ? 0 : Math.Max(0, List.Items.Count - keep);

        // Conferma solo se si perde qualcosa: l'eliminazione è definitiva.
        if (excess > 0 &&
            !Ask(Loc.F(Strings.ConfirmPrune, excess),
                 MessageBoxIcon.Warning))
            return;

        await RunAsync(Strings.BusySavingSetting, () => Manager.SetBackupsToKeep(keep));
    }

    private async Task RestoreAsync()
    {
        if (List.SelectedSnapshot is not Snapshot backup)
            return;

        string active = Manager.HasActiveProfile
            ? Loc.F(Strings.RestoreGoesToActive, SaveManager.ProfileDisplayName(Manager.State.ActiveProfile))
            : Strings.NoActiveProfile;

        // Qui la conferma serve: il backup può essere di un altro profilo.
        if (!Ask(Loc.F(Strings.ConfirmRestoreBackup,
                     SaveManager.BackupReasonText(backup.Name), backup.Date.ToString("g"), active)))
            return;

        if (!ConfirmSteamClosed())
            return;

        await RunAsync(Strings.BusyRestoringBackup, () => Manager.RestoreBackup(backup));
    }
}
