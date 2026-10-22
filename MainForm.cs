using System.Diagnostics;

namespace ControlSaveManager;

/// <summary>
/// Finestra principale: lista dei profili e azioni.
/// </summary>
internal sealed class MainForm : Form
{
    private readonly SaveManager manager;

    private readonly Label activeLabel = new() { AutoSize = true };
    private readonly Label sinceLabel = new() { AutoSize = true };
    private readonly Label savedLabel = new() { AutoSize = true };

    private readonly ListBox profileList = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false
    };

    private readonly Button activateButton = MakeButton("Attiva");
    private readonly Button saveButton = MakeButton("Salva progressi");
    private readonly Button newButton = MakeButton("Nuovo...");
    private readonly Button folderButton = MakeButton("Apri cartella");

    private readonly ToolStripStatusLabel statusLabel = new()
    {
        Spring = true,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly System.Windows.Forms.Timer gameTimer = new() { Interval = 2000 };

    // true mentre un'operazione sui file è in corso.
    private bool busy;
    private bool gameRunning;
    private bool checkingGame;
    private string lastMessage = "Pronto.";

    public MainForm(SaveManager manager)
    {
        this.manager = manager;

        Text = "Control Save Manager";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 380);
        MinimumSize = new Size(440, 340);

        BuildLayout();

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
        activeLabel.Font = new Font(Font, FontStyle.Bold);

        var info = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            WrapContents = false
        };
        info.Controls.AddRange(new Control[] { activeLabel, sinceLabel, savedLabel });

        var profilesBox = new GroupBox
        {
            Text = "Profili",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        profilesBox.Controls.Add(profileList);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(8, 6, 0, 0)
        };
        buttons.Controls.AddRange(new Control[] { activateButton, saveButton, newButton, folderButton });

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(info, 0, 0);
        root.SetColumnSpan(info, 2);
        root.Controls.Add(profilesBox, 0, 1);
        root.Controls.Add(buttons, 1, 1);

        var status = new StatusStrip { SizingGrip = false };
        status.Items.Add(statusLabel);

        Controls.Add(root);
        Controls.Add(status);
    }

    private static Button MakeButton(string text)
    {
        return new Button
        {
            Text = text,
            Size = new Size(140, 32),
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    // AGGIORNAMENTO

    private sealed record ProfileItem(string FolderName, bool Active)
    {
        public override string ToString() =>
            SaveManager.ProfileDisplayName(FolderName) + (Active ? "   ✔ attivo" : "");
    }

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
            lastMessage = "Impossibile leggere lo stato: " + ex.Message;
        }

        AppState state = manager.State;

        string? selected = (profileList.SelectedItem as ProfileItem)?.FolderName;

        profileList.BeginUpdate();
        profileList.Items.Clear();

        try
        {
            foreach (string profile in manager.GetProfiles())
            {
                var item = new ProfileItem(profile, profile == state.ActiveProfile);
                profileList.Items.Add(item);

                if (profile == selected)
                    profileList.SelectedItem = item;
            }
        }
        catch (Exception ex)
        {
            lastMessage = "Impossibile leggere i profili: " + ex.Message;
        }

        profileList.EndUpdate();

        activeLabel.Text = "Profilo attivo: " + (state.ActiveProfile == ""
            ? "nessuno"
            : SaveManager.ProfileDisplayName(state.ActiveProfile));
        sinceLabel.Text = "Attivato il: " + (state.ActiveSince == "" ? "-" : state.ActiveSince);
        savedLabel.Text = "Ultimo salvataggio: " + (state.LastSaved == "" ? "mai" : state.LastSaved);

        UpdateControls();
    }

    private void UpdateControls()
    {
        bool canAct = !busy && !gameRunning;
        var selected = profileList.SelectedItem as ProfileItem;

        activateButton.Enabled = canAct && selected != null && !selected.Active;
        saveButton.Enabled = canAct && manager.HasActiveProfile;
        newButton.Enabled = canAct;
        profileList.Enabled = !busy;

        statusLabel.Text = gameRunning && !busy
            ? "⚠ Control è in esecuzione: chiudilo per cambiare o salvare."
            : lastMessage;

        UseWaitCursor = busy;
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

        busy = true;
        lastMessage = "Controllo i progressi...";
        UpdateControls();

        try
        {
            return await Task.Run(manager.HasUnsavedProgress);
        }
        finally
        {
            busy = false;
            lastMessage = previousMessage;
            UpdateControls();
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
        lastMessage = message;
        UpdateControls();

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
        lastMessage = (result.Success ? "✔ " : "✖ ") + result.Message.Split('\n')[0];
        UpdateControls();

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
