using System.Diagnostics;

namespace Lss;

/// <summary>
/// Finestra principale: una scheda per partita, con le sue azioni visibili,
/// barre per gli stati di tutta l'app (Control aperto, Steam Cloud attivo) e
/// un avviso in basso per l'esito delle operazioni, con "Annulla" dopo un cambio.
/// </summary>
internal sealed class MainForm : Form
{
    private readonly SaveManager manager;

    private readonly InfoBar cloudBar = new() { Visible = false };
    private readonly InfoBar gameBar = new() { Visible = false };
    private readonly InfoBar toast = new() { Visible = false };

    private readonly FlowLayoutPanel cardsHost = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Margin = new Padding(0)
    };

    private readonly Label emptyLabel = new()
    {
        AutoSize = true,
        ForeColor = Theme.TextMuted,
        Text = Strings.NoPlaythroughs,
        Margin = new Padding(2, 8, 0, 0)
    };

    private readonly LinkLabel deletedLink = new()
    {
        AutoSize = true,
        Font = Theme.Small,
        LinkColor = Theme.TextSecondary,
        ActiveLinkColor = Color.White,
        LinkBehavior = LinkBehavior.HoverUnderline,
        Margin = new Padding(2, 4, 0, 6),
        Visible = false
    };

    private readonly FlatButton newButton = new FlatButton(Strings.NewPlaythroughButton).FitText();
    private readonly FlatButton importButton = new FlatButton(Strings.ImportButton, ButtonKind.Ghost).FitText();
    private readonly FlatButton settingsButton = new FlatButton(Strings.SettingsButton, ButtonKind.Ghost).FitText(34);

    private readonly System.Windows.Forms.Timer gameTimer = new() { Interval = 2000 };
    private readonly System.Windows.Forms.Timer toastTimer = new() { Interval = 12000 };

    private readonly List<PlaythroughCard> cards = new();

    // true mentre un'operazione sui file è in corso.
    private bool busy;
    private bool gameRunning;
    private bool checkingGame;
    private bool cloudWarningAccepted;
    private CloudState cloudState = CloudState.Unknown;

    // Sessione di gioco in corso: quando è partita e con quale partita.
    private DateTime? sessionStart;
    private string sessionProfile = "";

    public MainForm(SaveManager manager)
    {
        this.manager = manager;

        Text = manager.AccountName == ""
            ? Strings.AppTitle
            : Strings.AppTitle + "  ·  " + manager.AccountName;
        Icon = Theme.AppIcon;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        Font = Theme.Base;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        ClientSize = new Size(760, 720);
        MinimumSize = new Size(640, 560);

        BuildLayout();

        newButton.Click += NewClicked;
        importButton.Click += ImportClicked;
        settingsButton.Click += (_, _) => ShowSettings();
        deletedLink.LinkClicked += (_, _) => ShowDeleted();
        cardsHost.Resize += (_, _) => FitCards();

        gameTimer.Tick += async (_, _) => await CheckGameAsync();
        toastTimer.Tick += (_, _) => HideToast();

        Load += async (_, _) =>
        {
            RefreshAll();
            await OfferFirstRunAsync();
            ReportIgnoredProfileFolders();
            await CheckGameAsync();
            gameTimer.Start();
        };

        FormClosing += (_, e) =>
        {
            // Non interrompere una copia a metà.
            if (busy)
            {
                e.Cancel = true;
                MessageDialog.Inform(this, Strings.WaitForOperation, "", MessageBoxIcon.Information);
            }
        };

        // Sessione in corso alla chiusura dell'app: il tempo fin qui conta.
        FormClosed += (_, _) => EndSession();
    }

    // LAYOUT

    private void BuildLayout()
    {
        var header = new HeaderPanel("CONTROL", "SAVE MANAGER  ·  v" + Program.Version);
        // Il pulsante sta a destra e lascia libera la riga in fondo all'intestazione.
        settingsButton.BackColor = Theme.Header;
        header.Controls.Add(settingsButton);
        header.Resize += (_, _) => settingsButton.Location = new Point(
            header.ClientSize.Width - settingsButton.Width - 16,
            (header.ClientSize.Height - settingsButton.Height) / 2);

        var titleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            Margin = new Padding(0, 4, 0, 10)
        };
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titleRow.Controls.Add(new Label
        {
            Text = Strings.YourPlaythroughs,
            AutoSize = true,
            Font = Theme.Heading,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0)
        }, 0, 0);
        importButton.Margin = new Padding(0, 0, 8, 0);
        newButton.Margin = new Padding(0);
        titleRow.Controls.Add(importButton, 1, 0);
        titleRow.Controls.Add(newButton, 2, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 12),
            ColumnCount = 1,
            RowCount = 6
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        toast.Margin = new Padding(0, 6, 0, 0);

        root.Controls.Add(cloudBar, 0, 0);
        root.Controls.Add(gameBar, 0, 1);
        root.Controls.Add(titleRow, 0, 2);
        root.Controls.Add(cardsHost, 0, 3);
        root.Controls.Add(deletedLink, 0, 4);
        root.Controls.Add(toast, 0, 5);

        // L'ultimo aggiunto viene ancorato per primo.
        Controls.Add(root);
        Controls.Add(header);
    }

    /// <summary>
    /// Le schede occupano tutta la larghezza disponibile.
    /// </summary>
    private void FitCards()
    {
        int width = cardsHost.ClientSize.Width - 2;

        if (width < 200)
            return;

        foreach (PlaythroughCard card in cards)
        {
            card.MinimumSize = new Size(width, 0);
            card.MaximumSize = new Size(width, 0);
        }
    }

    // AGGIORNAMENTO

    /// <summary>
    /// Rilegge stato e partite dal disco e ricostruisce le schede.
    /// </summary>
    private void RefreshAll()
    {
        try
        {
            manager.ReloadState();
        }
        catch (Exception ex)
        {
            ShowToast(InfoKind.Error, Loc.F(Strings.CannotReadState, ex.Message));
        }

        cloudState = manager.GetCloudState();

        cardsHost.SuspendLayout();

        foreach (PlaythroughCard card in cards)
            card.Dispose();

        cards.Clear();
        cardsHost.Controls.Clear();

        try
        {
            string active = manager.State.ActiveProfile;

            // La partita nel gioco per prima, poi le altre in ordine di nome.
            foreach (string profile in manager.GetProfiles().OrderBy(p => p != active).ThenBy(p => p))
            {
                var card = new PlaythroughCard(new CardData(
                    profile,
                    profile == active,
                    manager.GetInfo(profile),
                    manager.GetProfileLastModified(profile),
                    manager.CountSavestates(profile)));

                card.ActionRequested += async (c, action) => await OnCardActionAsync(c, action);
                cards.Add(card);
                cardsHost.Controls.Add(card);
            }
        }
        catch (Exception ex)
        {
            ShowToast(InfoKind.Error, Loc.F(Strings.CannotReadProfiles, ex.Message));
        }

        if (cards.Count == 0)
            cardsHost.Controls.Add(emptyLabel);

        cardsHost.ResumeLayout();
        FitCards();

        int deleted = 0;

        try
        {
            deleted = manager.GetDeletedProfiles().Count;
        }
        catch
        {
            // Senza l'elenco il collegamento resta nascosto.
        }

        deletedLink.Text = Loc.F(Strings.DeletedLink, deleted);
        deletedLink.Visible = deleted > 0;

        UpdateControls();
    }

    private void UpdateControls()
    {
        newButton.Enabled = !busy;
        importButton.Enabled = !busy;
        settingsButton.Enabled = !busy;

        foreach (PlaythroughCard card in cards)
        {
            card.Enabled = !busy;
            card.SetGameRunning(gameRunning, manager.State.LaunchGame);
        }

        if (cloudState == CloudState.Enabled)
        {
            cloudBar.Show(InfoKind.Warning, Strings.CloudBarTitle, Strings.CloudBarText,
                Strings.CloudBarAction, ShowCloudHelp);
        }
        else
        {
            cloudBar.Visible = false;
        }

        if (gameRunning)
        {
            string active = manager.HasActiveProfile
                ? SaveManager.ProfileDisplayName(manager.State.ActiveProfile)
                : "";

            string text = manager.State.SaveOnGameExit && active != ""
                ? Strings.GameBarSaveOnExit + " " + Strings.GameBarExit
                : Strings.GameBarExit;

            if (sessionStart is DateTime start && DateTime.Now - start >= TimeSpan.FromMinutes(1))
                text += "\n" + Loc.F(Strings.GameBarSession, PlaythroughCard.FormatPlayTime(DateTime.Now - start));

            gameBar.Show(InfoKind.Info,
                active == "" ? Strings.GameBarOpen : Loc.F(Strings.GameBarPlaying, active), text);
        }
        else
        {
            gameBar.Visible = false;
        }

        UseWaitCursor = busy;
    }

    private void ShowToast(InfoKind kind, string text, string? actionText = null, Action? action = null,
        bool autoHide = true)
    {
        toastTimer.Stop();
        toast.Show(kind, "", text, actionText, action == null ? null : () =>
        {
            HideToast();
            action();
        });

        if (autoHide)
            toastTimer.Start();
    }

    private void HideToast()
    {
        toastTimer.Stop();
        toast.Visible = false;
    }

    // GIOCO APERTO E SESSIONI

    private async Task CheckGameAsync()
    {
        if (checkingGame)
            return;

        checkingGame = true;

        try
        {
            bool running = await Task.Run(SaveManager.IsGameRunning);

            if (running == gameRunning)
            {
                // Aggiorna i minuti della sessione nella barra.
                if (running && !busy)
                    UpdateControls();

                return;
            }

            gameRunning = running;

            if (running)
            {
                sessionStart = await Task.Run(SaveManager.GameStartTime) ?? DateTime.Now;
                sessionProfile = manager.State.ActiveProfile;
                HideToast();
                UpdateControls();
            }
            else
            {
                await OnGameClosedAsync();
            }
        }
        finally
        {
            checkingGame = false;
        }
    }

    /// <summary>
    /// Registra il tempo della sessione appena finita.
    /// </summary>
    private void EndSession()
    {
        if (sessionStart is DateTime start && sessionProfile != "")
            manager.AddPlayTime(sessionProfile, start, DateTime.Now);

        sessionStart = null;
    }

    /// <summary>
    /// Il gioco si è chiuso: tempo di gioco e, se attivo, salvataggio dei progressi.
    /// </summary>
    private async Task OnGameClosedAsync()
    {
        string profile = sessionProfile;
        EndSession();

        if (busy || !manager.State.SaveOnGameExit || profile == "" ||
            manager.State.ActiveProfile != profile || !manager.HasActiveProfile)
        {
            RefreshAll();
            return;
        }

        string name = SaveManager.ProfileDisplayName(profile);

        Result result = await RunBusyAsync(Loc.F(Strings.BusySavingOnExit, name), () =>
            manager.HasUnsavedProgress()
                ? manager.SaveProgress(onGameExit: true)
                : Result.Ok(""));

        if (!result.Success)
            ShowError(result);
        else if (result.Message != "")
            ShowToast(InfoKind.Success, Loc.F(Strings.SavedOnExit, name));
    }

    // AZIONI DELLE SCHEDE

    private async Task OnCardActionAsync(PlaythroughCard card, CardAction action)
    {
        if (busy)
            return;

        string profile = card.Data.ProfileName;

        switch (action)
        {
            case CardAction.Play:
                if (card.Data.Active)
                    LaunchOnly();
                else
                    await PlayAsync(profile, manager.State.LaunchGame);
                break;

            case CardAction.LoadOnly:
                await PlayAsync(profile, launch: false);
                break;

            case CardAction.SavePoint:
                ShowHistory(focusSave: true);
                break;

            case CardAction.History:
                ShowHistory(focusSave: false);
                break;

            case CardAction.Rename:
                await RenameAsync(profile);
                break;

            case CardAction.EditNote:
                EditNote(card.Data);
                break;

            case CardAction.Export:
                await ExportAsync(profile);
                break;

            case CardAction.Delete:
                await DeleteAsync(profile);
                break;
        }
    }

    /// <summary>
    /// Carica una partita senza chiedere conferme: i progressi non salvati di
    /// quella nel gioco vengono salvati prima, ed entrambe le operazioni fanno
    /// una copia. Poi avvia il gioco, oppure offre "Annulla".
    /// </summary>
    /// <param name="offerUndo">false per il cambio fatto da "Annulla", che non si annulla.</param>
    private async Task PlayAsync(string profileName, bool launch, bool offerUndo = true)
    {
        if (busy || gameRunning || !ConfirmCloud(launch))
            return;

        string name = SaveManager.ProfileDisplayName(profileName);
        string previous = manager.State.ActiveProfile;
        bool hadPrevious = manager.HasActiveProfile && previous != profileName;
        string previousName = SaveManager.ProfileDisplayName(previous);

        Result result = await RunBusyAsync(Loc.F(Strings.BusyLoading, name), () =>
        {
            bool saved = false;

            if (manager.HasUnsavedProgress())
            {
                Result save = manager.SaveProgress();

                if (!save.Success)
                    return save with { Message = Loc.F(Strings.SwitchSaveFailed, previousName, save.Message) };

                saved = true;
            }

            Result activation = manager.ActivateProfile(profileName);

            if (!activation.Success)
                return activation;

            return activation with
            {
                Message = saved
                    ? Loc.F(Strings.LoadedAndSaved, name, previousName)
                    : Loc.F(Strings.Loaded, name)
            };
        });

        if (!result.Success)
        {
            ShowError(result);
            return;
        }

        if (launch)
        {
            Result launched = SaveManager.LaunchGame();

            if (launched.Success)
                ShowToast(InfoKind.Info, Loc.F(Strings.StartingWith, name));
            else
                ShowError(launched);

            return;
        }

        if (offerUndo && hadPrevious)
        {
            ShowToast(InfoKind.Success, result.Message, Strings.Undo,
                () => _ = PlayAsync(previous, launch: false, offerUndo: false));
        }
        else
        {
            ShowToast(InfoKind.Success, result.Message);
        }
    }

    /// <summary>
    /// "Gioca" sulla partita già nel gioco: avvia solo Control.
    /// </summary>
    private void LaunchOnly()
    {
        if (busy || gameRunning || !ConfirmCloud(launching: true))
            return;

        Result launched = SaveManager.LaunchGame();

        if (launched.Success)
            ShowToast(InfoKind.Info, Loc.F(Strings.StartingWith,
                SaveManager.ProfileDisplayName(manager.State.ActiveProfile)));
        else
            ShowError(launched);
    }

    private async Task RenameAsync(string profile)
    {
        using var dialog = new NewProfileForm(
            name => manager.ProfileExists(SaveManager.ProfileFolderName(name)),
            SaveManager.ProfileDisplayName(profile));

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string newName = dialog.ProfileName;

        Result result = await RunBusyAsync(Strings.BusyRenaming,
            () => manager.RenameProfile(profile, newName));

        ShowResult(result);
    }

    private void EditNote(CardData data)
    {
        using var dialog = new TextPromptForm(Strings.NoteTitle,
            Loc.F(Strings.NoteCaption, SaveManager.ProfileDisplayName(data.ProfileName)).ToUpperInvariant(),
            data.Info.Note, Strings.NoteHint, SaveManager.MaxNoteLength, Strings.SaveButton);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        Result result = manager.SetNote(data.ProfileName, dialog.Value);

        if (!result.Success)
            ShowError(result);

        RefreshAll();
    }

    private async Task ExportAsync(string profile)
    {
        string name = SaveManager.ProfileDisplayName(profile);

        using var dialog = new SaveFileDialog
        {
            Title = Strings.ExportTitle,
            Filter = Strings.ZipFilter,
            FileName = name + ".zip",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string path = dialog.FileName;

        Result result = await RunBusyAsync(Loc.F(Strings.BusyExporting, name),
            () => manager.ExportProfile(profile, path));

        ShowResult(result);
    }

    private async void ImportClicked(object? sender, EventArgs e)
    {
        if (busy)
            return;

        using var dialog = new OpenFileDialog
        {
            Title = Strings.ImportTitle,
            Filter = Strings.ZipFilter
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string path = dialog.FileName;
        string imported = "";

        Result result = await RunBusyAsync(Strings.BusyImporting, () =>
        {
            Result<string> import = manager.ImportProfile(path);

            if (!import.Success)
                return Result.Fail(import.Message);

            imported = import.Value!;
            return Result.Ok(Loc.F(Strings.Imported, SaveManager.ProfileDisplayName(imported)));
        });

        ShowResult(result);
    }

    /// <summary>
    /// Elimina una partita: finisce tra le copie automatiche, da cui si può recuperare.
    /// </summary>
    private async Task DeleteAsync(string profile)
    {
        string name = SaveManager.ProfileDisplayName(profile);
        string text = Loc.F(Strings.ConfirmDeleteText, manager.State.BackupsToKeep == 0
            ? Strings.ConfirmDeleteKeepAll
            : Loc.F(Strings.ConfirmDeleteKeepN, manager.State.BackupsToKeep));

        int points = manager.CountSavestates(profile);

        if (points > 0)
            text += "\n\n" + Loc.F(Strings.ConfirmDeletePoints, points);

        if (!MessageDialog.Confirm(this, Loc.F(Strings.ConfirmDeleteHeading, name), text,
                Strings.DeletePlaythrough, destructive: true, MessageBoxIcon.Warning))
            return;

        Result result = await RunBusyAsync(Strings.BusyDeleting, () => manager.DeleteProfile(profile));

        ShowResult(result);
    }

    private async void NewClicked(object? sender, EventArgs e)
    {
        if (busy)
            return;

        using var dialog = new NewProfileForm(name =>
            manager.ProfileExists(SaveManager.ProfileFolderName(name)));

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string name = dialog.ProfileName;
        bool copy = dialog.CopyCurrentSaves;
        bool playNow = dialog.ActivateNow;

        Result result = await RunBusyAsync(Strings.BusyCreating,
            () => manager.CreateProfile(name, copy));

        ShowResult(result);

        if (result.Success && playNow)
            await PlayAsync(SaveManager.ProfileFolderName(name), manager.State.LaunchGame);
    }

    // ALTRE FINESTRE

    private void ShowHistory(bool focusSave)
    {
        if (!manager.HasActiveProfile)
            return;

        using var dialog = new HistoryForm(manager, focusSave, () => ConfirmCloud(launching: false));
        dialog.ShowDialog(this);

        if (dialog.Changed)
            RefreshAll();
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsForm(manager);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            RefreshAll();
            ShowToast(InfoKind.Success, Strings.SettingSaved);
        }
    }

    private void ShowDeleted()
    {
        using var dialog = new DeletedForm(manager);
        dialog.ShowDialog(this);

        if (dialog.Changed)
            RefreshAll();
    }

    private void ShowCloudHelp()
    {
        MessageDialog.Inform(this, Strings.CloudHelpHeading, Strings.CloudHelpText, MessageBoxIcon.Information);
        RefreshAll();
    }

    /// <summary>
    /// Primo avvio: nessuna partita, ma il gioco ha dei salvataggi.
    /// </summary>
    private async Task OfferFirstRunAsync()
    {
        try
        {
            if (manager.GetProfiles().Count > 0 || manager.GameSavesAreEmpty())
                return;
        }
        catch
        {
            return;
        }

        using var dialog = new FirstRunForm(manager, cloudState);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string name = dialog.PlaythroughName;
        Result result = await RunBusyAsync(Strings.BusyCreating, () => manager.AdoptGameSaves(name));

        ShowResult(result);
    }

    /// <summary>
    /// Dice all'avvio quali cartelle in Profili non compaiono come partite e perché.
    /// </summary>
    private void ReportIgnoredProfileFolders()
    {
        IReadOnlyList<string> ignored;

        try
        {
            ignored = manager.GetIgnoredProfileFolders();
        }
        catch
        {
            return;
        }

        if (ignored.Count == 0)
            return;

        string names = string.Join(", ", ignored);
        Log.Write("Cartelle ignorate in Profili (nome non valido): " + names);
        ShowToast(InfoKind.Warning, Loc.F(Strings.IgnoredProfileFolders, names), autoHide: false);
    }

    // OPERAZIONI E MESSAGGI

    /// <summary>
    /// Esegue un'operazione sui file in background, con la finestra bloccata.
    /// </summary>
    private async Task<Result> RunBusyAsync(string message, Func<Result> action)
    {
        busy = true;
        ShowToast(InfoKind.Info, message, autoHide: false);
        UpdateControls();

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
            HideToast();
            RefreshAll();
        }
    }

    private void ShowResult(Result result)
    {
        if (result.Success)
            ShowToast(InfoKind.Success, result.Message);
        else
            ShowError(result);
    }

    /// <summary>
    /// Errore in una finestra: cosa è successo e, se c'è, il backup da aprire.
    /// </summary>
    private void ShowError(Result result)
    {
        bool hasBackup = result.BackupPath != null && Directory.Exists(result.BackupPath);

        bool openBackup = MessageDialog.Inform(this,
            result.Critical ? Strings.TitleSeriousError : Strings.TitleOperationFailed,
            result.Message,
            result.Critical ? MessageBoxIcon.Error : MessageBoxIcon.Warning,
            hasBackup ? Strings.OpenBackup : null);

        if (openBackup)
            OpenFolder(result.BackupPath!);
    }

    /// <summary>
    /// Avviso su Steam Cloud, una sola volta per sessione: prima di avviare il
    /// gioco, o prima di toccare i salvataggi con Steam aperto. Con il cloud
    /// disattivato per Control non serve.
    /// </summary>
    private bool ConfirmCloud(bool launching)
    {
        if (cloudWarningAccepted || cloudState == CloudState.Disabled)
            return true;

        if (!launching && !SaveManager.IsSteamRunning())
            return true;

        cloudWarningAccepted = MessageDialog.Confirm(this, Strings.CloudConfirmHeading,
            cloudState == CloudState.Enabled ? Strings.CloudConfirmOn : Strings.CloudConfirmMaybe,
            launching ? Strings.StartAnyway : Strings.ContinueAnyway,
            icon: MessageBoxIcon.Warning);

        return cloudWarningAccepted;
    }

    private void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageDialog.Inform(this, Strings.TitleOperationFailed, Loc.F(Strings.CannotOpenFolder, ex.Message),
                MessageBoxIcon.Warning);
        }
    }
}
