using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lss.App.Services;

namespace Lss.App.ViewModels;

/// <summary>
/// Avviso in fondo alla finestra, come il toast di MainForm: si nasconde da
/// solo dopo 12 secondi, a meno che non sia un'operazione in corso.
/// </summary>
public partial class ToastViewModel : ObservableObject
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(12) };

    private Action? action;

    public ToastViewModel()
    {
        timer.Tick += (_, _) => Hide();
    }

    [ObservableProperty]
    private InfoKind kind;

    [ObservableProperty]
    private string text = "";

    [ObservableProperty]
    private string actionText = "";

    [ObservableProperty]
    private bool hasAction;

    [ObservableProperty]
    private bool isVisible;

    public void Show(InfoKind kind, string text, string? actionText = null, Action? action = null, bool autoHide = true)
    {
        timer.Stop();

        Kind = kind;
        Text = text;
        ActionText = actionText ?? "";
        HasAction = action != null;
        this.action = action;
        IsVisible = true;

        if (autoHide)
            timer.Start();
    }

    public void Hide()
    {
        timer.Stop();
        IsVisible = false;
    }

    // Prima si nasconde, poi fa l'azione (come HideToast e poi action in MainForm).
    [RelayCommand]
    private void RunAction()
    {
        Action? run = action;
        Hide();
        run?.Invoke();
    }
}
