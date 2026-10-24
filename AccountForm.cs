namespace Lss;

/// <summary>
/// Scelta dell'account Steam all'avvio, quando più account hanno i salvataggi di Control.
/// </summary>
internal sealed class AccountForm : Form
{
    private readonly ItemList list = new();

    private readonly FlatButton okButton = new(Strings.UseThisAccount, ButtonKind.Primary)
    {
        DialogResult = DialogResult.OK
    };

    private readonly FlatButton cancelButton = new(Strings.Exit)
    {
        DialogResult = DialogResult.Cancel
    };

    private SteamAccount? SelectedAccount => list.SelectedRowItem as SteamAccount;

    /// <summary>
    /// Mostra la finestra e restituisce l'account scelto, o null se l'utente esce.
    /// </summary>
    public static SteamAccount? Choose(IReadOnlyList<SteamAccount> accounts, string? lastId)
    {
        using var form = new AccountForm(accounts, lastId);
        return form.ShowDialog() == DialogResult.OK ? form.SelectedAccount : null;
    }

    private AccountForm(IReadOnlyList<SteamAccount> accounts, string? lastId)
    {
        Text = "Local Save Switcher";
        Icon = Theme.AppIcon;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Font;
        Font = Theme.Base;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        ClientSize = new Size(480, 400);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        var intro = new Label
        {
            Text = Strings.AccountIntro,
            AutoSize = true,
            ForeColor = Theme.Text,
            Margin = new Padding(0, 0, 0, 14)
        };

        var caption = new Label
        {
            Text = Strings.CaptionSteamAccount,
            AutoSize = true,
            Font = Theme.Caption,
            ForeColor = Theme.TextMuted,
            Margin = new Padding(2, 0, 0, 8)
        };

        var listCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6),
            Margin = new Padding(0)
        };
        listCard.Controls.Add(list);

        foreach (FlatButton button in new[] { okButton, cancelButton })
            button.Margin = new Padding(10, 0, 0, 0);

        okButton.Size = new Size(170, 36);
        cancelButton.Size = new Size(100, 36);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 16, 0, 0)
        };
        buttons.Controls.AddRange(new Control[] { okButton, cancelButton });

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 18, 20, 16),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(intro, 0, 0);
        root.Controls.Add(caption, 0, 1);
        root.Controls.Add(listCard, 0, 2);
        root.Controls.Add(buttons, 0, 3);

        Controls.Add(root);

        list.SetRows(accounts.Select(a => new ItemList.Row(a, a.DisplayName, Details(a))));

        // Preseleziona l'ultimo account usato, altrimenti il più recente.
        int last = accounts.ToList().FindIndex(a => a.Id == lastId);
        list.SelectedIndex = last >= 0 ? last : 0;

        list.SelectedIndexChanged += (_, _) => okButton.Enabled = SelectedAccount != null;
        list.DoubleClick += (_, _) =>
        {
            if (SelectedAccount != null)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };
    }

    private static string Details(SteamAccount account)
    {
        string saved = account.LastSaved == null
            ? Strings.AccountNoSaves
            : Loc.F(Strings.AccountLastSaved, account.LastSaved.Value.ToString("g"));

        return $"ID {account.Id}  ·  {saved}";
    }
}
