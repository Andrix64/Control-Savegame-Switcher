namespace Lss;

/// <summary>
/// Finestra con una casella di testo, per la nota di una partita.
/// </summary>
internal sealed class TextPromptForm : Form
{
    private readonly TextBox box = new();

    public string Value => box.Text.Trim();

    /// <param name="validate">Restituisce l'errore da mostrare, o "" se il testo va bene.</param>
    public TextPromptForm(string title, string caption, string initial, string hint,
        int maxLength, string confirmText, Func<string, string>? validate = null)
    {
        Theme.ApplyDialog(this, title);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        box.Text = initial;
        box.MaxLength = maxLength;

        var hintLabel = new Label
        {
            Text = hint,
            AutoSize = true,
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            MaximumSize = new Size(400, 0),
            Margin = new Padding(2, 0, 0, 0)
        };

        var confirm = new FlatButton(confirmText, ButtonKind.Primary) { DialogResult = DialogResult.OK };
        confirm.FitText(36, 100);
        var cancel = new FlatButton(Strings.Cancel) { DialogResult = DialogResult.Cancel };
        cancel.FitText(36, 100);
        confirm.Margin = cancel.Margin = new Padding(10, 0, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 22, 0, 0)
        };
        buttons.Controls.AddRange(new Control[] { cancel, confirm });

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(22, 20, 22, 20)
        };
        layout.Controls.AddRange(new Control[]
        {
            Theme.SectionLabel(caption),
            Theme.TextBoxCard(box, 400),
            hintLabel,
            buttons
        });
        Controls.Add(layout);

        AcceptButton = confirm;
        CancelButton = cancel;

        box.TextChanged += (_, _) =>
        {
            string error = validate?.Invoke(Value) ?? "";
            hintLabel.Text = error == "" ? hint : error;
            hintLabel.ForeColor = error == "" ? Theme.TextMuted : Theme.Error;
            confirm.Enabled = error == "";
        };

        Shown += (_, _) =>
        {
            box.Focus();
            box.SelectAll();
        };
    }
}

/// <summary>
/// Impostazioni: avvio del gioco, salvataggio alla chiusura, opzioni
/// condivise, copie automatiche, cartella dei dati e account.
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly SaveManager manager;

    private readonly CheckBox launchCheck = MakeCheck(Strings.SettingLaunch);
    private readonly CheckBox saveOnExitCheck = MakeCheck(Strings.SettingSaveOnExit);
    private readonly CheckBox shareCheck = MakeCheck(Strings.SettingSharePreferences);

    private readonly NumericUpDown keepBox = new()
    {
        Minimum = 0,
        Maximum = 999,
        Width = 70,
        BackColor = Theme.Surface,
        ForeColor = Theme.Text,
        BorderStyle = BorderStyle.FixedSingle,
        TextAlign = HorizontalAlignment.Center,
        Margin = new Padding(10, 0, 10, 0)
    };

    /// <summary>true se le impostazioni sono state salvate.</summary>
    public bool Changed { get; private set; }

    public SettingsForm(SaveManager manager)
    {
        this.manager = manager;

        Theme.ApplyDialog(this, Strings.SettingsTitle);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        AppState state = manager.State;
        launchCheck.Checked = state.LaunchGame;
        saveOnExitCheck.Checked = state.SaveOnGameExit;
        shareCheck.Checked = state.SharePreferences;
        keepBox.Value = Math.Min(keepBox.Maximum, state.BackupsToKeep);

        var options = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 14)
        };
        options.Controls.AddRange(new Control[]
        {
            launchCheck, Hint(Strings.SettingLaunchHint),
            saveOnExitCheck, Hint(Strings.SettingSaveOnExitHint),
            shareCheck, Hint(Strings.SettingSharePreferencesHint)
        });

        var keepRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 18)
        };
        keepRow.Controls.Add(new Label { Text = Strings.SettingKeepCopies, AutoSize = true, Margin = new Padding(0, 4, 0, 0) });
        keepRow.Controls.Add(keepBox);
        keepRow.Controls.Add(new Label
        {
            Text = Strings.SettingKeepCopiesHint,
            AutoSize = true,
            ForeColor = Theme.TextMuted,
            Margin = new Padding(0, 4, 0, 0)
        });

        var folderRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
        var openFolder = new FlatButton(Strings.OpenFolder).FitText(32);
        openFolder.Margin = new Padding(12, 0, 0, 0);
        openFolder.Click += (_, _) => OpenFolder(Path.GetDirectoryName(manager.ProfilesPath)!);
        folderRow.Controls.Add(new Label
        {
            Text = Loc.F(Strings.SettingDataFolder, AppData.Root),
            AutoSize = true,
            MaximumSize = new Size(380, 0),
            ForeColor = Theme.TextSecondary,
            Margin = new Padding(0, 6, 0, 0)
        });
        folderRow.Controls.Add(openFolder);

        var about = new Label
        {
            Text = Loc.F(Strings.SettingAbout,
                manager.AccountName == "" ? "—" : manager.AccountName, Program.Version),
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            ForeColor = Theme.TextMuted,
            Font = Theme.Small,
            Margin = new Padding(0, 6, 0, 0)
        };

        var save = new FlatButton(Strings.SaveButton, ButtonKind.Primary);
        save.FitText(36, 100);
        var cancel = new FlatButton(Strings.Cancel) { DialogResult = DialogResult.Cancel };
        cancel.FitText(36, 100);
        save.Margin = cancel.Margin = new Padding(10, 0, 0, 0);
        save.Click += (_, _) => SaveAndClose();

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 22, 0, 0)
        };
        buttons.Controls.AddRange(new Control[] { cancel, save });

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(24, 20, 24, 18)
        };
        layout.Controls.AddRange(new Control[]
        {
            Theme.SectionLabel(Strings.CaptionGame), options,
            Theme.SectionLabel(Strings.CaptionCopies), keepRow,
            Theme.SectionLabel(Strings.CaptionData), folderRow, about,
            buttons
        });
        Controls.Add(layout);

        AcceptButton = save;
        CancelButton = cancel;
    }

    private static CheckBox MakeCheck(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Strong,
        ForeColor = Theme.Text,
        Margin = new Padding(0, 4, 0, 0)
    };

    private static Label Hint(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(500, 0),
        Font = Theme.Small,
        ForeColor = Theme.TextMuted,
        Margin = new Padding(20, 0, 0, 8)
    };

    private void SaveAndClose()
    {
        int keep = (int)keepBox.Value;
        int excess = keep == 0 ? 0 : Math.Max(0, manager.GetBackups().Count - keep);

        // Eliminare copie è definitivo: si chiede prima.
        if (excess > 0 &&
            !MessageDialog.Confirm(this, Strings.ConfirmPruneHeading, Loc.F(Strings.ConfirmPrune, excess),
                Strings.DeleteCopies, destructive: true, MessageBoxIcon.Warning))
            return;

        Result result = manager.SaveSettings(launchCheck.Checked, saveOnExitCheck.Checked, shareCheck.Checked, keep);

        if (!result.Success)
        {
            MessageDialog.Inform(this, Strings.TitleOperationFailed, result.Message, MessageBoxIcon.Warning);
            return;
        }

        Changed = true;
        DialogResult = DialogResult.OK;
    }

    private void OpenFolder(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageDialog.Inform(this, Strings.TitleOperationFailed, Loc.F(Strings.CannotOpenFolder, ex.Message),
                MessageBoxIcon.Warning);
        }
    }
}

/// <summary>
/// Primo avvio: due righe su cosa fa l'app e il nome per la partita che è
/// già nel gioco. OK = salva la partita con Name.
/// </summary>
internal sealed class FirstRunForm : Form
{
    private readonly TextBox nameBox = new() { Text = Strings.FirstPlaythroughName, MaxLength = 50 };

    public string PlaythroughName => nameBox.Text.Trim();

    public FirstRunForm(SaveManager manager, CloudState cloud)
    {
        Theme.ApplyDialog(this, Strings.AppTitle);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var title = new Label
        {
            Text = Strings.FirstRunTitle,
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Font = Theme.Large,
            Margin = new Padding(0, 0, 0, 8)
        };

        var intro = new Label
        {
            Text = Strings.FirstRunIntro,
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            ForeColor = Color.FromArgb(216, 216, 220),
            Margin = new Padding(0, 0, 0, 20)
        };

        var hint = new Label
        {
            Text = Strings.PlaythroughNameHint,
            AutoSize = true,
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            Margin = new Padding(2, 0, 0, 18)
        };

        string cloudText = cloud switch
        {
            CloudState.Disabled => "✓  " + Strings.FirstRunCloudOff,
            CloudState.Enabled => "!  " + Strings.FirstRunCloudOn,
            _ => "?  " + Strings.FirstRunCloudUnknown
        };

        var checks = new Label
        {
            Text = "✓  " + Loc.F(Strings.FirstRunAccount, manager.AccountName == "" ? "—" : manager.AccountName) +
                   "\n" + cloudText,
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            ForeColor = cloud == CloudState.Enabled ? Theme.Warning : Theme.TextSecondary,
            Margin = new Padding(0, 0, 0, 8)
        };

        var start = new FlatButton(Strings.FirstRunSave, ButtonKind.Primary) { DialogResult = DialogResult.OK };
        start.FitText(40, 140);
        var skip = new LinkLabel
        {
            Text = Strings.FirstRunSkip,
            AutoSize = true,
            LinkColor = Theme.AccentSoftText,
            ActiveLinkColor = Color.White,
            Margin = new Padding(16, 11, 0, 0)
        };
        skip.LinkClicked += (_, _) => DialogResult = DialogResult.Cancel;

        var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 14, 0, 0) };
        buttons.Controls.AddRange(new Control[] { start, skip });

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(36, 30, 36, 28)
        };
        layout.Controls.AddRange(new Control[]
        {
            title, intro,
            Theme.SectionLabel(Strings.FirstRunNameCaption),
            Theme.TextBoxCard(nameBox, 420),
            hint, checks, buttons
        });
        Controls.Add(layout);

        AcceptButton = start;

        nameBox.TextChanged += (_, _) =>
        {
            bool valid = SaveManager.IsValidName(PlaythroughName);
            start.Enabled = valid;
            hint.Text = valid ? Strings.PlaythroughNameHint : Loc.F(Strings.InvalidNameHint, Strings.PlaythroughNameHint);
            hint.ForeColor = valid ? Theme.TextMuted : Theme.Error;
        };
    }
}
