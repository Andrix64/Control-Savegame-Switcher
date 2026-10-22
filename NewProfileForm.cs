namespace ControlSaveManager;

/// <summary>
/// Finestra per creare un nuovo profilo.
/// </summary>
internal sealed class NewProfileForm : Form
{
    private const string Hint = "Lettere, cifre, _ e - (massimo 50 caratteri).";

    private readonly Func<string, bool> profileExists;

    private readonly TextBox nameBox = new() { Width = 280 };

    private readonly Label hintLabel = new()
    {
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(3, 0, 3, 10)
    };

    private readonly RadioButton copyRadio = new()
    {
        Text = "Copia i salvataggi attuali del gioco",
        AutoSize = true,
        Checked = true
    };

    private readonly RadioButton emptyRadio = new()
    {
        Text = "Profilo vuoto (nuova partita)",
        AutoSize = true
    };

    private readonly CheckBox activateCheck = new()
    {
        Text = "Attiva subito il profilo",
        AutoSize = true,
        Checked = true,
        Margin = new Padding(3, 10, 3, 3)
    };

    private readonly Button createButton = new()
    {
        Text = "Crea",
        DialogResult = DialogResult.OK,
        Enabled = false
    };

    private readonly Button cancelButton = new()
    {
        Text = "Annulla",
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
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        AcceptButton = createButton;
        CancelButton = cancelButton;

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0)
        };
        buttons.Controls.AddRange(new Control[] { createButton, cancelButton });

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(12)
        };
        layout.Controls.AddRange(new Control[]
        {
            new Label { Text = "Nome del profilo:", AutoSize = true },
            nameBox,
            hintLabel,
            copyRadio,
            emptyRadio,
            activateCheck,
            buttons
        });

        Controls.Add(layout);

        nameBox.TextChanged += (_, _) => Validate();
        Validate();
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
        hintLabel.ForeColor = error == "" ? SystemColors.GrayText : Color.Firebrick;

        createButton.Enabled = name != "" && error == "";
    }
}
