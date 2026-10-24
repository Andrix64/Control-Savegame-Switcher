namespace Lss;

/// <summary>
/// Cronologia della partita nel gioco: i punti salvati dall'utente (prima di
/// un boss, per esempio) e le copie automatiche fatte prima di ogni
/// operazione. Ogni riga ha "Torna qui".
/// </summary>
internal sealed class HistoryForm : Form
{
    private const int ListWidth = 620;

    private readonly SaveManager manager;
    private readonly string profileName;
    private readonly Func<bool> confirmCloud;

    private readonly TextBox nameBox = new() { MaxLength = 50 };
    private readonly FlatButton saveButton = new FlatButton(Strings.SavePointNow, ButtonKind.Primary).FitText(38, 120);

    private readonly Label saveHint = new()
    {
        AutoSize = true,
        Font = Theme.Small,
        ForeColor = Theme.TextMuted,
        MaximumSize = new Size(ListWidth, 0),
        Margin = new Padding(2, 0, 0, 14)
    };

    private readonly FlowLayoutPanel list = new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Size = new Size(ListWidth, 330),
        Margin = new Padding(0)
    };

    private readonly Label statusLabel = new()
    {
        AutoSize = true,
        Font = Theme.Small,
        ForeColor = Theme.TextMuted,
        MaximumSize = new Size(ListWidth, 0),
        Margin = new Padding(2, 10, 0, 0)
    };

    private readonly Label keepLabel = new()
    {
        AutoSize = true,
        Font = Theme.Small,
        ForeColor = Theme.TextMuted,
        Margin = new Padding(0, 10, 0, 0)
    };

    private readonly System.Windows.Forms.Timer gameTimer = new() { Interval = 2000 };

    private readonly List<FlatButton> gameButtons = new();

    private bool busy;
    private bool gameRunning;
    private bool checkingGame;

    /// <summary>true se è stata fatta almeno un'operazione.</summary>
    public bool Changed { get; private set; }

    /// <param name="focusSave">true per mettere subito il cursore nel nome del nuovo punto.</param>
    /// <param name="confirmCloud">Avviso su Steam Cloud della finestra principale.</param>
    public HistoryForm(SaveManager manager, bool focusSave, Func<bool> confirmCloud)
    {
        this.manager = manager;
        this.confirmCloud = confirmCloud;
        profileName = manager.State.ActiveProfile;

        Theme.ApplyDialog(this, Strings.HistoryTitle);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var title = new Label
        {
            Text = Loc.F(Strings.HistoryHeading, SaveManager.ProfileDisplayName(profileName)),
            AutoSize = true,
            Font = Theme.Heading,
            UseMnemonic = false,
            Margin = new Padding(0, 0, 0, 4)
        };

        var subtitle = new Label
        {
            Text = Strings.HistoryIntro,
            AutoSize = true,
            MaximumSize = new Size(ListWidth, 0),
            ForeColor = Theme.TextSecondary,
            Margin = new Padding(0, 0, 0, 16)
        };

        var saveRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
        saveButton.Margin = new Padding(10, 0, 0, 0);
        saveButton.Click += async (_, _) => await SavePointAsync();
        saveRow.Controls.Add(Theme.TextBoxCard(nameBox, ListWidth - saveButton.Width - 12));
        saveRow.Controls.Add(saveButton);
        gameButtons.Add(saveButton);

        var changeKeep = new LinkLabel
        {
            Text = Strings.Change,
            AutoSize = true,
            Font = Theme.Small,
            LinkColor = Theme.AccentSoftText,
            ActiveLinkColor = Color.White,
            Margin = new Padding(4, 10, 0, 0)
        };
        changeKeep.LinkClicked += (_, _) => OpenSettings();

        var close = new FlatButton(Strings.Close) { DialogResult = DialogResult.Cancel };
        close.FitText(36, 100);
        close.Margin = new Padding(0);

        var footer = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 3,
            Width = ListWidth,
            Margin = new Padding(0, 6, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(keepLabel, 0, 0);
        footer.Controls.Add(changeKeep, 1, 0);
        footer.Controls.Add(close, 2, 0);

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(24, 20, 24, 16)
        };
        layout.Controls.AddRange(new Control[]
        {
            title, subtitle,
            Theme.SectionLabel(Strings.CaptionSavePoint), saveRow, saveHint,
            list, statusLabel, footer
        });
        Controls.Add(layout);

        AcceptButton = saveButton;
        CancelButton = close;

        gameTimer.Tick += async (_, _) => await CheckGameAsync();

        Load += async (_, _) =>
        {
            Reload();
            await CheckGameAsync();
            gameTimer.Start();
        };

        Shown += (_, _) =>
        {
            if (focusSave)
                nameBox.Focus();
        };

        FormClosing += (_, e) =>
        {
            // Non interrompere una copia a metà.
            if (busy)
                e.Cancel = true;
        };

        FormClosed += (_, _) => gameTimer.Dispose();

        nameBox.TextChanged += (_, _) => UpdateControls();
    }

    // ELENCO

    private void Reload()
    {
        list.SuspendLayout();
        list.Controls.Clear();
        gameButtons.RemoveAll(b => b != saveButton);

        try
        {
            IReadOnlyList<Snapshot> points = manager.GetSavestates();
            IReadOnlyList<Snapshot> copies = manager.GetBackupsOf(profileName);
            IReadOnlyList<Snapshot> legacy = manager.GetBackupsWithoutOwner();

            list.Controls.Add(Section(Strings.CaptionMyPoints));

            if (points.Count == 0)
                list.Controls.Add(Empty(Strings.NoPoints));

            foreach (Snapshot point in points)
            {
                list.Controls.Add(Row(point.Name == "" ? Strings.Unnamed : point.Name, point.Date, strong: true,
                    () => RestoreAsync(point, isPoint: true, legacy: false),
                    () => DeletePointAsync(point)));
            }

            list.Controls.Add(Section(Strings.CaptionAutomaticCopies));

            if (copies.Count == 0)
                list.Controls.Add(Empty(Strings.NoCopies));

            foreach (Snapshot copy in copies)
            {
                list.Controls.Add(Row(SaveManager.BackupDescription(copy), copy.Date, strong: false,
                    () => RestoreAsync(copy, isPoint: false, legacy: false), null));
            }

            if (legacy.Count > 0)
            {
                list.Controls.Add(Section(Strings.CaptionLegacyCopies));

                foreach (Snapshot copy in legacy)
                {
                    list.Controls.Add(Row(SaveManager.BackupDescription(copy), copy.Date, strong: false,
                        () => RestoreAsync(copy, isPoint: false, legacy: true), null));
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus(Loc.F(Strings.CannotReadList, ex.Message), Theme.Error);
        }

        list.ResumeLayout();

        keepLabel.Text = manager.State.BackupsToKeep == 0
            ? Strings.KeepAllCopies
            : Loc.F(Strings.KeepCopies, manager.State.BackupsToKeep);

        UpdateControls();
    }

    private int RowWidth => ListWidth - SystemInformation.VerticalScrollBarWidth - 4;

    private Label Section(string text)
    {
        Label label = Theme.SectionLabel(text);
        label.Margin = new Padding(2, list.Controls.Count == 0 ? 0 : 14, 0, 6);
        return label;
    }

    private static Label Empty(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.TextMuted,
        Margin = new Padding(2, 0, 0, 4)
    };

    /// <param name="delete">Azione del pulsante "Elimina", o null per nessun pulsante.</param>
    private Control Row(string title, DateTime date, bool strong, Func<Task> restore, Func<Task>? delete)
    {
        var row = new TableLayoutPanel
        {
            // Larghezza fissa: AutoSize regola solo l'altezza.
            MinimumSize = new Size(RowWidth, 0),
            MaximumSize = new Size(RowWidth, 0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            BackColor = strong ? Theme.Surface : Theme.Background,
            Padding = new Padding(12, 8, 8, 8),
            Margin = new Padding(0, 0, 0, 6)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var text = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Left
        };
        text.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = strong ? Theme.Strong : Theme.Base,
            ForeColor = Theme.Text,
            UseMnemonic = false,
            MaximumSize = new Size(RowWidth - 230, 0),
            Margin = new Padding(0)
        });
        text.Controls.Add(new Label
        {
            Text = date.ToString("g"),
            AutoSize = true,
            Font = Theme.Small,
            ForeColor = Theme.TextSecondary,
            Margin = new Padding(0, 1, 0, 0)
        });
        row.Controls.Add(text, 0, 0);

        var back = new FlatButton(Strings.GoBackHere, strong ? ButtonKind.Secondary : ButtonKind.Ghost).FitText(32);
        back.Margin = new Padding(8, 0, 0, 0);
        back.Anchor = AnchorStyles.Right;
        back.Click += async (_, _) =>
        {
            if (!busy)
                await restore();
        };
        row.Controls.Add(back, 1, 0);
        gameButtons.Add(back);

        if (delete != null)
        {
            var remove = new FlatButton(Strings.Delete, ButtonKind.Ghost).FitText(32);
            remove.Margin = new Padding(4, 0, 0, 0);
            remove.Anchor = AnchorStyles.Right;
            remove.AccessibleName = Loc.F(Strings.DeletePointNamed, title);
            remove.Click += async (_, _) =>
            {
                if (!busy)
                    await delete();
            };
            row.Controls.Add(remove, 2, 0);
        }

        return row;
    }

    // AZIONI

    private async Task SavePointAsync()
    {
        string name = nameBox.Text.Trim();

        if (busy || gameRunning || !SaveManager.IsValidSavestateName(name))
            return;

        Result result = await RunAsync(Strings.BusySavingPoint, () => manager.CreateSavestate(name));

        if (result.Success)
            nameBox.Clear();
    }

    /// <summary>
    /// Torna a un punto o a una copia. Nessuna conferma: la situazione attuale
    /// viene comunque copiata prima. Solo le copie delle versioni precedenti,
    /// che potrebbero essere di un'altra partita, chiedono conferma.
    /// </summary>
    private async Task RestoreAsync(Snapshot snapshot, bool isPoint, bool legacy)
    {
        if (busy || gameRunning)
            return;

        if (legacy &&
            !MessageDialog.Confirm(this, Strings.ConfirmLegacyHeading,
                Loc.F(Strings.ConfirmLegacyText, SaveManager.ProfileDisplayName(profileName)),
                Strings.GoBackHere, icon: MessageBoxIcon.Warning))
            return;

        if (!confirmCloud())
            return;

        await RunAsync(Strings.BusyGoingBack, () => isPoint
            ? manager.RestoreSavestate(snapshot)
            : manager.RestoreBackup(snapshot));
    }

    private async Task DeletePointAsync(Snapshot point)
    {
        string name = point.Name == "" ? Strings.UnnamedLower : $"«{point.Name}»";

        if (!MessageDialog.Confirm(this, Loc.F(Strings.ConfirmDeletePointHeading, name),
                Strings.ConfirmDeletePointText, Strings.DeletePoint, destructive: true))
            return;

        await RunAsync(Strings.BusyDeletingPoint, () => manager.DeleteSavestate(point));
    }

    private void OpenSettings()
    {
        using var settings = new SettingsForm(manager);

        if (settings.ShowDialog(this) == DialogResult.OK)
        {
            Changed = true;
            Reload();
        }
    }

    private async Task<Result> RunAsync(string message, Func<Result> action)
    {
        busy = true;
        UseWaitCursor = true;
        SetStatus(message, Theme.TextMuted);
        UpdateControls();

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
            busy = false;
            UseWaitCursor = false;
            Changed = true;
        }

        if (IsDisposed)
            return result;

        Reload();
        SetStatus(result.Message.Split('\n')[0], result.Success ? Theme.Success : Theme.Error);

        if (!result.Success)
        {
            MessageDialog.Inform(this,
                result.Critical ? Strings.TitleSeriousError : Strings.TitleOperationFailed,
                result.Message, result.Critical ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
        }

        return result;
    }

    // STATO

    private async Task CheckGameAsync()
    {
        if (checkingGame)
            return;

        checkingGame = true;

        try
        {
            bool running = await Task.Run(SaveManager.IsGameRunning);

            if (running == gameRunning || IsDisposed)
                return;

            gameRunning = running;
            UpdateControls();
        }
        finally
        {
            checkingGame = false;
        }
    }

    private void UpdateControls()
    {
        bool canTouchGame = !busy && !gameRunning;

        foreach (FlatButton button in gameButtons)
            button.Enabled = canTouchGame;

        string name = nameBox.Text.Trim();
        bool valid = SaveManager.IsValidSavestateName(name);
        saveButton.Enabled = canTouchGame && valid;

        if (gameRunning)
        {
            saveHint.Text = Loc.F(Strings.HistoryGameRunning, manager.Game.Name);
            saveHint.ForeColor = Theme.Warning;
        }
        else if (!valid)
        {
            saveHint.Text = Strings.InvalidSavestateName;
            saveHint.ForeColor = Theme.Error;
        }
        else
        {
            saveHint.Text = Strings.SavePointHint;
            saveHint.ForeColor = Theme.TextMuted;
        }

        UseWaitCursor = busy;
    }

    private void SetStatus(string message, Color color)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = color;
    }
}
