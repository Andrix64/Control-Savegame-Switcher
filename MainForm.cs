using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ControlSaveManager;

/// <summary>
/// Finestra principale: lista dei profili e azioni.
/// </summary>
internal sealed class MainForm : Form
{
    private const int ItemHeight = 58;
    private static readonly string ReadyMessage = Strings.ReadyHint;

    private readonly SaveManager manager;

    private readonly Label activeLabel = MakeLabel(Theme.Large, Theme.Text);
    private readonly Label sinceLabel = MakeLabel(Theme.Base, Theme.Text);
    private readonly Label savedLabel = MakeLabel(Theme.Base, Theme.Text);
    private readonly Label cloudLabel = MakeLabel(Theme.Base, Theme.Text);
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
        Text = Strings.NoProfiles,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Theme.TextMuted,
        Visible = false
    };

    private readonly FlatButton activateButton = new(Strings.ActivateProfile, ButtonKind.Primary);
    private readonly FlatButton saveButton = new(Strings.SaveProgress);
    private readonly FlatButton savestateButton = new(Strings.SavestatesButton);
    private readonly FlatButton newButton = new(Strings.NewProfileButton);
    private readonly FlatButton backupButton = new(Strings.BackupsButton, ButtonKind.Ghost);
    private readonly FlatButton folderButton = new(Strings.OpenProfilesFolder, ButtonKind.Ghost);

    private readonly ContextMenuStrip profileMenu = new()
    {
        Renderer = new ThemedMenuRenderer(),
        ShowImageMargin = false,
        Font = Theme.Base
    };

    private readonly ToolStripMenuItem renameItem = new(Strings.RenameMenu) { ShortcutKeyDisplayString = "F2" };
    private readonly ToolStripMenuItem deleteItem = new(Strings.DeleteMenu) { ShortcutKeyDisplayString = Strings.DeleteKey };

    private readonly StatusBar statusBar = new();

    private readonly System.Windows.Forms.Timer gameTimer = new() { Interval = 2000 };

    // true mentre un'operazione sui file è in corso.
    private bool busy;
    private bool gameRunning;
    private bool checkingGame;
    private bool steamWarningAccepted;
    private CloudState cloudState = CloudState.Unknown;
    private string lastMessage = ReadyMessage;
    private StatusKind lastKind = StatusKind.Info;

    public MainForm(SaveManager manager)
    {
        this.manager = manager;

        Text = manager.AccountName == ""
            ? "Control Save Manager"
            : "Control Save Manager  ·  " + manager.AccountName;
        Icon = Theme.AppIcon;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        Font = Theme.Base;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        ClientSize = new Size(620, 600);
        MinimumSize = new Size(540, 560);

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

        // Menu del tasto destro: prima seleziona il profilo sotto il mouse.
        profileMenu.Items.AddRange(new ToolStripItem[] { renameItem, deleteItem });
        profileList.ContextMenuStrip = profileMenu;
        profileList.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right)
                return;

            int index = profileList.IndexFromPoint(e.Location);

            if (index >= 0)
                profileList.SelectedIndex = index;
        };
        profileMenu.Opening += (_, e) =>
        {
            if (busy || profileList.SelectedItem is not ProfileItem item)
            {
                e.Cancel = true;
                return;
            }

            deleteItem.Enabled = !item.Active;
            deleteItem.ToolTipText = item.Active ? Strings.ActiveCannotBeDeleted : "";
        };
        profileList.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F2)
                RenameClicked(null, EventArgs.Empty);
            else if (e.KeyCode == Keys.Delete)
                DeleteClicked(null, EventArgs.Empty);
        };
        renameItem.Click += RenameClicked;
        deleteItem.Click += DeleteClicked;

        activateButton.Click += ActivateClicked;
        saveButton.Click += SaveClicked;
        newButton.Click += NewClicked;
        savestateButton.Click += (_, _) => ShowListDialog(new SavestateForm(manager, ConfirmSteamClosed));
        backupButton.Click += (_, _) => ShowListDialog(new BackupForm(manager, ConfirmSteamClosed));
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
                MessageDialog.Show(this,
                    Strings.WaitForOperation,
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
            ColumnCount = 3,
            RowCount = 4
        };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));

        Label activeCaption = MakeCaption(Strings.CaptionActiveProfile);
        info.Controls.Add(activeCaption, 0, 0);
        info.SetColumnSpan(activeCaption, 3);

        activeLabel.Margin = new Padding(0, 0, 0, 10);
        info.Controls.Add(activeLabel, 0, 1);
        info.SetColumnSpan(activeLabel, 3);

        info.Controls.Add(MakeCaption(Strings.CaptionActivatedOn), 0, 2);
        info.Controls.Add(MakeCaption(Strings.CaptionLastSaved), 1, 2);
        info.Controls.Add(MakeCaption(Strings.CaptionSteamCloud), 2, 2);
        info.Controls.Add(sinceLabel, 0, 3);
        info.Controls.Add(savedLabel, 1, 3);
        info.Controls.Add(cloudLabel, 2, 3);

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

        foreach (FlatButton button in new[]
                 {
                     activateButton, saveButton, savestateButton, newButton, backupButton, folderButton
                 })
        {
            button.Size = new Size(190, 40);
            button.Margin = new Padding(0, 0, 0, 10);
            buttons.Controls.Add(button);
        }

        backupButton.Margin = new Padding(0, 14, 0, 4);
        folderButton.Margin = new Padding(0);

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
        Controls.Add(new HeaderPanel("CONTROL", "SAVE MANAGER  ·  v" + Program.Version));
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
            ? Strings.NoSaves
            : Loc.F(Strings.LastModified, Modified.Value.ToString("g"));

        // Usato dagli screen reader.
        public override string ToString() =>
            SaveManager.ProfileDisplayName(FolderName) + (Active ? Strings.ActiveSuffix : "");
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
            string badge = Strings.ActiveBadge;
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
            SetStatus(Loc.F(Strings.CannotReadState, ex.Message), StatusKind.Error);
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
            SetStatus(Loc.F(Strings.CannotReadProfiles, ex.Message), StatusKind.Error);
        }

        profileList.EndUpdate();

        int count = profileList.Items.Count;
        profilesCaption.Text = count == 0 ? Strings.CaptionProfiles : $"{Strings.CaptionProfiles}  ·  {count}";
        emptyLabel.Visible = count == 0;
        profileList.Visible = count > 0;

        bool hasActive = state.ActiveProfile != "";

        activeLabel.Text = hasActive
            ? SaveManager.ProfileDisplayName(state.ActiveProfile)
            : Strings.NoneValue;
        activeLabel.ForeColor = hasActive ? Theme.Text : Theme.TextMuted;

        sinceLabel.Text = state.ActiveSince == "" ? "—" : SaveManager.FormatStoredDate(state.ActiveSince);

        savedLabel.Text = state.LastSaved == "" ? Strings.Never : SaveManager.FormatStoredDate(state.LastSaved);
        savedLabel.ForeColor = state.LastSaved == "" ? Theme.TextMuted : Theme.Text;

        cloudState = manager.GetCloudState();

        (cloudLabel.Text, cloudLabel.ForeColor) = cloudState switch
        {
            CloudState.Disabled => (Strings.CloudOff, Theme.Success),
            CloudState.Enabled => (Strings.CloudOn, Theme.Warning),
            _ => (Strings.CloudUnknown, Theme.TextMuted)
        };

        UpdateControls();
    }

    private void UpdateControls()
    {
        bool canAct = !busy && !gameRunning;
        var selected = profileList.SelectedItem as ProfileItem;

        activateButton.Enabled = canAct && selected != null && !selected.Active;
        saveButton.Enabled = canAct && manager.HasActiveProfile;
        savestateButton.Enabled = canAct && manager.HasActiveProfile;
        newButton.Enabled = canAct;
        backupButton.Enabled = canAct;

        if (gameRunning && !busy)
            statusBar.SetStatus(Strings.GameRunning, StatusKind.Warning);
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

        await ActivateAsync(item.FolderName);
    }

    /// <summary>
    /// Attiva un profilo senza chiedere conferme: se i salvataggi del gioco
    /// contengono progressi non salvati, li salva prima nel profilo attivo.
    /// Entrambe le operazioni fanno un backup.
    /// </summary>
    private async Task ActivateAsync(string profileName)
    {
        if (!ConfirmSteamClosed())
            return;

        string name = SaveManager.ProfileDisplayName(profileName);
        string active = SaveManager.ProfileDisplayName(manager.State.ActiveProfile);

        Result result = await RunBusyAsync(Loc.F(Strings.BusyActivating, name), () =>
        {
            bool saved = false;

            if (manager.HasUnsavedProgress())
            {
                Result save = manager.SaveProgress();

                if (!save.Success)
                {
                    return save with
                    {
                        Message = Loc.F(Strings.SwitchSaveFailed, active, save.Message)
                    };
                }

                saved = true;
            }

            Result activation = manager.ActivateProfile(profileName);

            if (!activation.Success)
                return activation;

            return activation with
            {
                Message = saved
                    ? Loc.F(Strings.SavedAndActivated, active, name)
                    : Loc.F(Strings.ActivatedNamed, name)
            };
        });

        ShowResult(result);
    }

    private async void SaveClicked(object? sender, EventArgs e)
    {
        if (!manager.HasActiveProfile)
            return;

        string name = SaveManager.ProfileDisplayName(manager.State.ActiveProfile);
        bool allowEmpty = false;

        if (manager.GameSavesAreEmpty())
        {
            if (!Ask(Loc.F(Strings.ConfirmSaveEmpty, name), MessageBoxIcon.Warning))
                return;

            allowEmpty = true;
        }

        if (!ConfirmSteamClosed())
            return;

        Result result = await RunBusyAsync(Strings.BusySavingProgress,
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

        Result result = await RunBusyAsync(Strings.BusyCreatingProfile,
            () => manager.CreateProfile(name, copy));

        ShowResult(result);

        if (!result.Success)
            return;

        string profileName = SaveManager.ProfileFolderName(name);
        SelectProfile(profileName);

        if (activate)
            await ActivateAsync(profileName);
    }

    private async void RenameClicked(object? sender, EventArgs e)
    {
        if (busy || profileList.SelectedItem is not ProfileItem item)
            return;

        using var dialog = new NewProfileForm(
            name => manager.ProfileExists(SaveManager.ProfileFolderName(name)),
            SaveManager.ProfileDisplayName(item.FolderName));

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string newName = dialog.ProfileName;

        Result result = await RunBusyAsync(Strings.BusyRenamingProfile,
            () => manager.RenameProfile(item.FolderName, newName));

        ShowResult(result);

        if (result.Success)
            SelectProfile(SaveManager.ProfileFolderName(newName));
    }

    /// <summary>
    /// Elimina il profilo selezionato, che finisce nei backup.
    /// </summary>
    private async void DeleteClicked(object? sender, EventArgs e)
    {
        if (busy || profileList.SelectedItem is not ProfileItem item)
            return;

        string name = SaveManager.ProfileDisplayName(item.FolderName);

        if (item.Active)
        {
            SetStatus(Loc.F(Strings.ActiveCannotDeleteStatus, name),
                StatusKind.Warning);
            return;
        }

        string question = Loc.F(Strings.ConfirmDeleteProfile, name);

        int savestates = manager.CountSavestates(item.FolderName);

        if (savestates > 0)
            question += Loc.F(Strings.ConfirmDeleteProfileSavestates, savestates);

        if (!Ask(question, MessageBoxIcon.Warning))
            return;

        Result result = await RunBusyAsync(Strings.BusyDeletingProfile,
            () => manager.DeleteProfile(item.FolderName));

        ShowResult(result);
    }

    /// <summary>
    /// Apre la finestra dei savestate o dei backup e poi aggiorna la finestra.
    /// </summary>
    private void ShowListDialog(ListDialog dialog)
    {
        using (dialog)
            dialog.ShowDialog(this);

        if (dialog.Changed)
        {
            SetStatus(ReadyMessage, StatusKind.Info);
            RefreshAll();
        }
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
            Log.Write("Errore imprevisto (" + message + ")", ex);
            return Result.Fail(Loc.F(Strings.UnexpectedError, ex.Message, Log.FilePath));
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

        string title = result.Critical ? Strings.TitleSeriousError : Strings.TitleOperationFailed;
        MessageBoxIcon icon = result.Critical ? MessageBoxIcon.Error : MessageBoxIcon.Warning;

        if (result.BackupPath == null || !Directory.Exists(result.BackupPath))
        {
            MessageDialog.Show(this, result.Message, title, MessageBoxButtons.OK, icon);
            return;
        }

        string text = result.Message;

        if (!result.Critical)
            text += Loc.F(Strings.BackupLine, result.BackupPath);

        text += Strings.AskOpenBackupFolder;

        if (MessageDialog.Show(this, text, title, MessageBoxButtons.YesNo, icon) == DialogResult.Yes)
            OpenFolder(result.BackupPath);
    }

    /// <summary>
    /// Avvisa che Steam è aperto, una sola volta per sessione. Con Steam Cloud
    /// disattivato per Control l'avviso non serve: Steam non tocca i salvataggi.
    /// </summary>
    private bool ConfirmSteamClosed()
    {
        if (steamWarningAccepted || cloudState == CloudState.Disabled || !SaveManager.IsSteamRunning())
            return true;

        string cloud = cloudState == CloudState.Enabled
            ? Strings.CloudOnWarning
            : Strings.CloudMaybeWarning;

        steamWarningAccepted = Ask(Loc.F(Strings.SteamRunningWarning, cloud), MessageBoxIcon.Warning);

        return steamWarningAccepted;
    }

    private bool Ask(string question, MessageBoxIcon icon = MessageBoxIcon.Question)
    {
        // Con un avviso il pulsante predefinito è "No".
        MessageBoxDefaultButton defaultButton = icon == MessageBoxIcon.Warning
            ? MessageBoxDefaultButton.Button2
            : MessageBoxDefaultButton.Button1;

        return MessageDialog.Show(this, question, Text,
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
            MessageDialog.Show(this, Loc.F(Strings.CannotOpenFolder, ex.Message),
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
