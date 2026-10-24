using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Lss.App.Services;

namespace Lss.App.Views;

/// <summary>
/// Finestra di messaggio a tema scuro, al posto di MessageBox (MessageDialog.cs).
/// Conferme con il verbo sul pulsante; messaggi con un pulsante e un secondo
/// facoltativo. Il risultato è true se è stato premuto il verbo o il secondo.
/// </summary>
public partial class MessageWindow : Window
{
    private readonly string copyText;

    public MessageWindow() : this("", "", InfoKind.Info) { }

    private MessageWindow(string heading, string text, InfoKind kind)
    {
        InitializeComponent();

        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://LSS/Assets/app.ico")));

        copyText = (heading != "" ? heading + Environment.NewLine + Environment.NewLine : "") + text;

        HeadingText.Text = heading;
        HeadingText.IsVisible = heading != "";
        BodyText.Text = text;
        ShowKind(kind);
    }

    /// <summary>Messaggio con un pulsante Chiudi e, se c'è, un secondo pulsante d'azione.</summary>
    public static MessageWindow Inform(string heading, string text, InfoKind kind, string? actionText = null)
    {
        var window = new MessageWindow(heading, text, kind);

        if (actionText != null)
            window.AddButton(actionText, "secondary", result: true);

        window.AddButton(Strings.Close, "primary", result: false, isDefault: true, isCancel: true);
        return window;
    }

    /// <summary>
    /// Conferma con un verbo: "Annulla" a sinistra, il verbo a destra. Il
    /// verbo è il pulsante principale (Invio), Esc annulla.
    /// </summary>
    public static MessageWindow Confirm(string heading, string text, string verb, bool destructive, InfoKind kind)
    {
        var window = new MessageWindow(heading, text, kind);
        window.AddButton(Strings.Cancel, "secondary", result: false, isCancel: true);
        window.AddButton(verb, destructive ? "danger" : "primary", result: true, isDefault: true);
        return window;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Ctrl+C copia il messaggio, come in MessageBox.
        if (e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control)
        {
            _ = TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(copyText);
            e.Handled = true;
        }
    }

    private void AddButton(string label, string style, bool result, bool isDefault = false, bool isCancel = false)
    {
        var button = new Button
        {
            Content = label,
            Classes = { style },
            IsDefault = isDefault,
            IsCancel = isCancel,
            MinWidth = 100,
        };

        button.Click += (_, _) => Close(result);
        ButtonRow.Children.Add(button);
    }

    private void ShowKind(InfoKind kind)
    {
        (string glyph, string resource) = kind switch
        {
            InfoKind.Warning => ("⚠", "Warning"),
            InfoKind.Success => ("✓", "Success"),
            InfoKind.Error => ("✕", "Error"),
            _ => ("ℹ", "InfoBlue"),
        };

        var brush = (IBrush)this.FindResource(resource)!;
        Badge.BorderBrush = brush;
        Glyph.Text = glyph;
        Glyph.Foreground = brush;
    }
}
