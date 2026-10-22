using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ControlSaveManager;

/// <summary>
/// Finestra principale: lista dei profili e azioni.
/// </summary>
internal sealed class MainForm : Form
{
    private const int ItemHeight = 58;

    private readonly SaveManager manager;

    private readonly Label activeLabel = MakeLabel(Theme.Large, Theme.Text);
    private readonly Label sinceLabel = MakeLabel(Theme.Base, Theme.Text);
    private readonly Label savedLabel = MakeLabel(Theme.Base, Theme.Text);
    private readonly Label profilesCaption = MakeLabel(Theme.Caption, Theme.TextMuted);

    private readonly ListBox profileList = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        BackColor = Theme.Surface,
        ForeColor = Theme.Text
    };

    private readonly Label emptyLabel = new()
    {
        Dock = DockStyle.Fill,
        Text = "Nessun profilo.\nCreane uno con «Nuovo profilo».",
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Theme.TextMuted,
        Visible = false
    };

    private readonly FlatButton activateButton = new("Attiva profilo", ButtonKind.Primary);
    private readonly FlatButton saveButton = new("Salva progressi");
    private readonly FlatButton newButton = new("Nuovo profilo...");
    private readonly FlatButton folderButton = new("Apri cartella profili", ButtonKind.Ghost);

    private readonly StatusBar statusBar = new();

    private readonly System.Windows.Forms.Timer gameTimer = new() { Interval = 2000 };

    // true mentre un'operazione sui file è in corso.
    private bool busy;
    private bool gameRunning;
    private bool checkingGame;
    private string lastMessage = "Pronto.";
    private StatusKind lastKind = StatusKind.Info;

    public MainForm(SaveManager manager)
    {
        this.manager = manager;

        Text = "Control Save Manager";
        Icon = Theme.AppIcon;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        Font = Theme.Base;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        ClientSize = new Size(620, 520);
        MinimumSize = new Size(540, 480);

        BuildLayout();

        profileList.DrawItem += DrawProfileItem;
        profileList.DpiChangedAfterParent += (_, _) =>
            profileList.ItemHeight = profileList.LogicalToDeviceUnits(ItemHeight);
        profileList.SelectedIndexChanged += (_, _) => UpdateControls();
        profileList.DoubleClick += (_, _) =>
        {
            if (activateButton.Enabled)
                activateButton.PerformClick();
        };

        activateButton.Click += ActivateClicked;
        saveButton.Click += SaveClicked;
        newButton.Click += NewClicked;
        folderButton.Click += (_, _) => OpenFolder(manager.ProfilesPath);

        gameTimer.Tick += async (_, _) => await CheckGameAsync();

        Load += async (_, _) =>
        {
            profileList.ItemHeight = profileList.LogicalToDeviceUnits(ItemHeight);
            RefreshAll();
            await CheckGameAsync();
            gameTimer.Start();
        };

        FormClosing += (_, e) =>
        {
            // Non interrompere una copia a metà.
            if (busy)
            {
                e.Cancel = true;
                MessageBox.Show(this,
                    "Attendi la fine dell'operazione in corso.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
    }

    // LAYOUT

    private void BuildLayout()
    {
        // Scheda del profilo attivo.
        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4
        };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        Label activeCaption = MakeCaption("PROFILO ATTIVO");
        info.Controls.Add(activeCaption, 0, 0);
        info.SetColumnSpan(activeCaption, 2);

        activeLabel.Margin = new Padding(0, 0, 0, 10);
        info.Controls.Add(activeLabel, 0, 1);
        info.SetColumnSpan(activeLabel, 2);

        info.Controls.Add(MakeCaption("ATTIVATO IL"), 0, 2);
        info.Controls.Add(MakeCaption("ULTIMO SALVATAGGIO"), 1, 2);
        info.Controls.Add(sinceLabel, 0, 3);
        info.Controls.Add(savedLabel, 1, 3);

        var infoCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18, 14, 18, 16),
            Margin = new Padding(0, 0, 0, 18)
        };
        infoCard.Controls.Add(info);

        // Lista dei profili.
        profilesCaption.Margin = new Padding(2, 0, 0, 8);

        var listCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6),
            Margin = new Padding(0)
        };
        listCard.Controls.Add(profileList);
        listCard.Controls.Add(emptyLabel);

        // Pulsanti.
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(16, 0, 0, 0)
        };

        foreach (FlatButton button in new[] { activateButton, saveButton, newButton, folderButton })
        {
            button.Size = new Size(190, 40);
            button.Margin = new Padding(0, 0, 0, 10);
            buttons.Controls.Add(button);
        }

        folderButton.Margin = new Padding(0, 14, 0, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 18, 20, 18),
            ColumnCount = 2,
            RowCount = 3
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(infoCard, 0, 0);
        root.SetColumnSpan(infoCard, 2);
        root.Controls.Add(profilesCaption, 0, 1);
        root.Controls.Add(listCard, 0, 2);
        root.Controls.Add(buttons, 1, 2);

        // L'ultimo aggiunto viene ancorato per primo.
        Controls.Add(root);
        Controls.Add(statusBar);
        Controls.Add(new HeaderPanel("CONTROL", "SAVE MANAGER"));
    }

    private static Label MakeLabel(Font font, Color color)
    {
        return new Label
        {
            AutoSize = true,
            Font = font,
            ForeColor = color,
            Margin = new Padding(0)
        };
    }

    private static Label MakeCaption(string text)
    {
        Label label = MakeLabel(Theme.Caption, Theme.TextMuted);
        label.Text = text;
        label.Margin = new Padding(0, 0, 0, 3);
        return label;
    }

    // LISTA DEI PROFILI

    private sealed record ProfileItem(string FolderName, bool Active, DateTime? Modified)
    {
        public string Details => Modified == null
            ? "Nessun salvataggio"
            : "Ultima modifica: " + Modified.Value.ToString("dd/MM/yyyy HH:mm");

        // Usato dagli screen reader.
        public override string ToString() =>
            SaveManager.ProfileDisplayName(FolderName) + (Active ? ", attivo" : "");
    }

    private void DrawProfileItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= profileList.Items.Count)
            return;

        var item = (ProfileItem)profileList.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var back = new SolidBrush(Theme.Surface))
            g.FillRectangle(back, e.Bounds);

        int Px(int value) => profileList.LogicalToDeviceUnits(value);

        Rectangle inner = Rectangle.Inflate(e.Bounds, -Px(2), -Px(2));

        if (selected)
        {
            using GraphicsPath path = Theme.RoundedRect(inner, Px(6));
            using var brush = new SolidBrush(Theme.SurfaceSelected);
            g.FillPath(brush, path);
        }

        // Barra rossa a sinistra del profilo attivo.
        if (item.Active)
        {
            using var accent = new SolidBrush(Theme.Accent);
            g.FillRectangle(accent,
                inner.X, inner.Y + Px(10), Px(3), inner.Height - Px(20));
        }

        int pad = Px(16);
        int right = inner.Right - pad;

        if (item.Active)
        {
            const string badge = "ATTIVO";
            Size textSize = TextRenderer.MeasureText(g, badge, Theme.Caption, Size.Empty, TextFormatFlags.NoPadding);
            var badgeRect = new Rectangle(0, 0, textSize.Width + Px(16), textSize.Height + Px(8));
            badgeRect.Location = new Point(right - badgeRect.Width, inner.Y + (inner.Height - badgeRect.Height) / 2);

            using (GraphicsPath path = Theme.RoundedRect(badgeRect, badgeRect.Height / 2f))
            using (var brush = new SolidBrush(Theme.AccentSoft))
                g.FillPath(brush, path);

            TextRenderer.DrawText(g, badge, Theme.Caption, badgeRect, Theme.AccentSoftText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            right = badgeRect.Left - Px(10);
        }

        const TextFormatFlags flags =
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix;

        int x = inner.X + pad;
        int middle = inner.Y + inner.Height / 2;

        TextRenderer.DrawText(g, SaveManager.ProfileDisplayName(item.FolderName), Theme.ItemName,
            new Rectangle(x, middle - Px(22), right - x, Px(24)), Theme.Text, flags);
        TextRenderer.DrawText(g, item.Details, Theme.Small,
            new Rectangle(x, middle + Px(1), right - x, Px(20)), Theme.TextMuted, flags);
    }

    // AGGIORNAMENTO

    /// <summary>
    /// Rilegge stato e profili dal disco e aggiorna tutta la finestra.
    /// </summary>
    private void RefreshAll()
    {
        try
        {
            manager.ReloadState();
        }
        catch (Exception ex)
        {
            SetStatus("Impossibile leggere lo stato: " + ex.Message, StatusKind.Error);
        }

        AppState state = manager.State;

        string? selected = (profileList.SelectedItem as ProfileItem)?.FolderName;

        profileList.BeginUpdate();
        profileList.Items.Clear();

        try
        {
            foreach (string profile in manager.GetProfiles())
            {
                var item = new ProfileItem(
                    profile,
                    profile == state.ActiveProfile,
                    manager.GetProfileLastModified(profile));

                profileList.Items.Add(item);

                if (profile == selected)
                    profileList.SelectedItem = item;
            }
        }
        catch (Exception ex)
        {
            SetStatus("Impossibile leggere i profili: " + ex.Message, StatusKind.Error);
        }

        profileList.EndUpdate();

        int count = profileList.Items.Count;
        profilesCaption.Text = count == 0 ? "PROFILI" : $"PROFILI  ·  {count}";
        emptyLabel.Visible = count == 0;
        profileList.Visible = count > 0;

        bool hasActive = state.ActiveProfile != "";

        activeLabel.Text = hasActive
            ? SaveManager.ProfileDisplayName(state.ActiveProfile)
            : "Nessuno";
        activeLabel.ForeColor = hasActive ? Theme.Text : Theme.TextMuted;

        sinceLabel.Text = state.ActiveSince == "" ? "—" : state.ActiveSince;

        savedLabel.Text = state.LastSaved == "" ? "Mai" : state.LastSaved;
        savedLabel.ForeColor = state.LastSaved == "" ? Theme.TextMuted : Theme.Text;

        UpdateControls();
    }

    private void UpdateControls()
    {
        bool canAct = !busy && !gameRunning;
        var selected = profileList.SelectedItem as ProfileItem;

        activateButton.Enabled = canAct && selected != null && !selected.Active;
        saveButton.Enabled = canAct && manager.HasActiveProfile;
        newButton.Enabled = canAct;

        if (gameRunning && !busy)
            statusBar.SetStatus("Control è in esecuzione: chiudilo per cambiare o salvare.", StatusKind.Warning);
        else
            statusBar.SetStatus(lastMessage, lastKind);

        UseWaitCursor = busy;
    }

    private void SetStatus(string message, StatusKind kind)
    {
        lastMessage = message;
        lastKind = kind;
        UpdateControls();
    }

    private async Task CheckGameAsync()
    {
        if (checkingGame)
            return;

        checkingGame = true;

        try
        {
            bool running = await Task.Run(SaveManager.IsGameRunning);

            if (running != gameRunning)
            {
                gameRunning = running;
                UpdateControls();
            }
        }
        finally
        {
            checkingGame = false;
        }
    }

    // AZIONI

    private async void ActivateClicked(object? sender, EventArgs e)
    {
        if (profileList.SelectedItem is not ProfileItem item)
            return;

        await ActivateAsync(item.FolderName, confirm: true);
    }

    /// <summary>
    /// Attiva un profilo, offrendo prima di salvare i progressi non salvati
    /// del profilo attivo.
    /// </summary>
    /// <param name="confirm">
    /// Chiede conferma anche quando non ci sono progressi da salvare.
    /// </param>
    private async Task ActivateAsync(string profileName, bool confirm)
    {
        string name = SaveManager.ProfileDisplayName(profileName);
        bool saveFirst = false;

        if (await CheckUnsavedProgressAsync())
        {
            string active = SaveManager.ProfileDisplayName(manager.State.ActiveProfile);

            DialogResult answer = MessageBox.Show(this,
                $"I salvataggi del gioco contengono progressi non salvati nel profilo \"{active}\".\n\n" +
                $"Salvarli in \"{active}\" prima di attivare \"{name}\"?\n\n" +
                "Sì: salva i progressi, poi attiva.\n" +
                "No: attiva senza salvare (i progressi restano solo nel backup).\n" +
                "Annulla: non fare nulla.",
                Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

            if (answer == DialogResult.Cancel)
                return;

            saveFirst = answer == DialogResult.Yes;
        }
        else if (confirm &&
                 !Ask($"Attivare il profilo \"{name}\"?\n\n" +
                      "I salvataggi attuali del gioco verranno sostituiti.\n" +
                      "Prima viene creato un backup."))
        {
            return;
        }

        if (!ConfirmSteamClosed())
            return;

        if (saveFirst)
        {
            Result saved = await RunBusyAsync("Salvo i progressi...",
                () => manager.SaveProgress());

            ShowResult(saved);

            if (!saved.Success)
                return;
        }

        Result result = await RunBusyAsync("Attivo il profilo...",
            () => manager.ActivateProfile(profileName));

        ShowResult(result);
    }

    private async Task<bool> CheckUnsavedProgressAsync()
    {
        string previousMessage = lastMessage;
        StatusKind previousKind = lastKind;

        busy = true;
        SetStatus("Controllo i progressi...", StatusKind.Busy);

        try
        {
            return await Task.Run(manager.HasUnsavedProgress);
        }
        finally
        {
            busy = false;
            SetStatus(previousMessage, previousKind);
        }
    }

    private async void SaveClicked(object? sender, EventArgs e)
    {
        if (!manager.HasActiveProfile)
            return;

        string name = SaveManager.ProfileDisplayName(manager.State.ActiveProfile);
        bool allowEmpty = false;

        if (manager.GameSavesAreEmpty())
        {
            if (!Ask("La cartella dei salvataggi del gioco è vuota.\n" +
                     $"Salvando, il profilo \"{name}\" verrà svuotato.\n\n" +
                     "Continuare comunque?",
                     MessageBoxIcon.Warning))
                return;

            allowEmpty = true;
        }
        else if (!Ask($"Sovrascrivere il profilo \"{name}\" con i salvataggi attuali del gioco?\n\n" +
                      "Prima viene creato un backup del profilo."))
        {
            return;
        }

        if (!ConfirmSteamClosed())
            return;

        Result result = await RunBusyAsync("Salvo i progressi...",
            () => manager.SaveProgress(allowEmpty));

        ShowResult(result);
    }

    private async void NewClicked(object? sender, EventArgs e)
    {
        using var dialog = new NewProfileForm(name =>
            manager.ProfileExists(SaveManager.ProfileFolderName(name)));

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string name = dialog.ProfileName;
        bool copy = dialog.CopyCurrentSaves;
        bool activate = dialog.ActivateNow;

        Result result = await RunBusyAsync("Creo il profilo...",
            () => manager.CreateProfile(name, copy));

        ShowResult(result);

        if (!result.Success)
            return;

        string profileName = SaveManager.ProfileFolderName(name);
        SelectProfile(profileName);

        if (activate)
            await ActivateAsync(profileName, confirm: false);
    }

    /// <summary>
    /// Esegue un'operazione sui file in background, con la finestra bloccata.
    /// </summary>
    private async Task<Result> RunBusyAsync(string message, Func<Result> action)
    {
        busy = true;
        SetStatus(message, StatusKind.Busy);

        try
        {
            return await Task.Run(action);
        }
        catch (Exception ex)
        {
            return Result.Fail("Errore imprevisto: " + ex.Message);
        }
        finally
        {
            busy = false;
            RefreshAll();
        }
    }

    // MESSAGGI

    private void ShowResult(Result result)
    {
        SetStatus(result.Message.Split('\n')[0],
            result.Success ? StatusKind.Success : StatusKind.Error);

        if (result.Success)
            return;

        string title = result.Critical ? "Errore grave" : "Operazione non riuscita";
        MessageBoxIcon icon = result.Critical ? MessageBoxIcon.Error : MessageBoxIcon.Warning;

        if (result.BackupPath == null || !Directory.Exists(result.BackupPath))
        {
            MessageBox.Show(this, result.Message, title, MessageBoxButtons.OK, icon);
            return;
        }

        string text = result.Message;

        if (!result.Critical)
            text += "\n\nBackup:\n" + result.BackupPath;

        text += "\n\nAprire la cartella del backup?";

        if (MessageBox.Show(this, text, title, MessageBoxButtons.YesNo, icon) == DialogResult.Yes)
            OpenFolder(result.BackupPath);
    }

    private bool ConfirmSteamClosed()
    {
        if (!SaveManager.IsSteamRunning())
            return true;

        return Ask("Steam è aperto.\n\n" +
                   "Se Steam Cloud è attivo per Control, potrebbe sovrascrivere " +
                   "i salvataggi dopo l'operazione.\n\n" +
                   "Continuare comunque?",
                   MessageBoxIcon.Warning);
    }

    private bool Ask(string question, MessageBoxIcon icon = MessageBoxIcon.Question)
    {
        // Con un avviso il pulsante predefinito è "No".
        MessageBoxDefaultButton defaultButton = icon == MessageBoxIcon.Warning
            ? MessageBoxDefaultButton.Button2
            : MessageBoxDefaultButton.Button1;

        return MessageBox.Show(this, question, Text,
            MessageBoxButtons.YesNo, icon, defaultButton) == DialogResult.Yes;
    }

    private void SelectProfile(string profileName)
    {
        foreach (ProfileItem item in profileList.Items)
        {
            if (item.FolderName == profileName)
            {
                profileList.SelectedItem = item;
                return;
            }
        }
    }

    private void OpenFolder(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossibile aprire la cartella: " + ex.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            gameTimer.Dispose();

        base.Dispose(disposing);
    }
}
