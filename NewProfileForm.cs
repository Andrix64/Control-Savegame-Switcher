namespace ControlSaveManager;

/// <summary>
/// Finestra per creare un nuovo profilo.
/// </summary>
internal sealed class NewProfileForm : Form
{
    private const string Hint = "Lettere, cifre, _ e - (massimo 50 caratteri).";

    private readonly Func<string, bool> profileExists;

    private readonly TextBox nameBox = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Surface,
        ForeColor = Theme.Text,
        Font = new Font("Segoe UI", 11f)
    };

    private readonly CardPanel nameCard = new()
    {
        Size = new Size(340, 38),
        Padding = new Padding(12, 8, 12, 4),
        Margin = new Padding(0, 0, 0, 6),
        BorderColor = Theme.Border
    };

    private readonly Label hintLabel = new()
    {
        AutoSize = true,
        Font = Theme.Small,
        ForeColor = Theme.TextMuted,
        Margin = new Padding(2, 0, 0, 18)
    };

    private readonly RadioButton copyRadio = new()
    {
        Text = "Copia i salvataggi attuali del gioco",
        AutoSize = true,
        Checked = true,
        Margin = new Padding(0, 0, 0, 4)
    };

    private readonly RadioButton emptyRadio = new()
    {
        Text = "Profilo vuoto (nuova partita)",
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 18)
    };

    private readonly CheckBox activateCheck = new()
    {
        Text = "Attiva subito il profilo",
        AutoSize = true,
        Checked = true,
        Margin = new Padding(0)
    };

    private readonly FlatButton createButton = new("Crea", ButtonKind.Primary)
    {
        DialogResult = DialogResult.OK,
        Enabled = false
    };

    private readonly FlatButton cancelButton = new("Annulla")
    {
        DialogResult = DialogResult.Cancel
    };

    public string ProfileName => nameBox.Text.Trim();
    public bool CopyCurrentSaves => copyRadio.Checked;
    public bool ActivateNow => activateCheck.Checked;

    /// <param name="profileExists">Dice se esiste già un profilo con quel nome.</param>
    public NewProfileForm(Func<string, bool> profileExists)
    {
        this.profileExists = profileExists;

        Text = "Nuovo profilo";
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
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        AcceptButton = createButton;
        CancelButton = cancelButton;

        nameCard.Controls.Add(nameBox);

        foreach (FlatButton button in new[] { createButton, cancelButton })
        {
            button.Size = new Size(110, 36);
            button.Margin = new Padding(10, 0, 0, 0);
        }

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 24, 0, 0)
        };
        buttons.Controls.AddRange(new Control[] { createButton, cancelButton });

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(22, 20, 22, 20)
        };
        layout.Controls.AddRange(new Control[]
        {
            MakeCaption("NOME DEL PROFILO"),
            nameCard,
            hintLabel,
            MakeCaption("CONTENUTO"),
            copyRadio,
            emptyRadio,
            activateCheck,
            buttons
        });

        Controls.Add(layout);

        // Bordo rosso sulla casella di testo quando è attiva.
        nameBox.Enter += (_, _) => nameCard.BorderColor = Theme.Accent;
        nameBox.Leave += (_, _) => nameCard.BorderColor = Theme.Border;
        nameBox.TextChanged += (_, _) => Validate();
        Validate();
    }

    private static Label MakeCaption(string text)
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

    private new void Validate()
    {
        string name = ProfileName;
        string error = "";

        if (name != "" && !SaveManager.IsValidName(name))
            error = "Nome non valido. " + Hint;
        else if (name != "" && profileExists(name))
            error = "Esiste già un profilo con questo nome.";

        hintLabel.Text = error == "" ? Hint : error;
        hintLabel.ForeColor = error == "" ? Theme.TextMuted : Theme.Error;

        createButton.Enabled = name != "" && error == "";
    }
}
