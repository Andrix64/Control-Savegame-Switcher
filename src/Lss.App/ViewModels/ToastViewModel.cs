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

    // Un bool per tipo: la barra prende il colore dallo stile "infobar.<tipo>".
    [ObservableProperty]
    private bool isInfo;

    [ObservableProperty]
    private bool isWarning;

    [ObservableProperty]
    private bool isSuccess;

    [ObservableProperty]
    private bool isError;

    public void Show(InfoKind kind, string text, string? actionText = null, Action? action = null, bool autoHide = true)
    {
        timer.Stop();

        Kind = kind;
        Text = text;
        ActionText = actionText ?? "";
        HasAction = action != null;
        this.action = action;
        IsVisible = true;
        IsInfo = kind == InfoKind.Info;
        IsWarning = kind == InfoKind.Warning;
        IsSuccess = kind == InfoKind.Success;
        IsError = kind == InfoKind.Error;

        if (autoHide)
            timer.Start();
    }

    public void Hide()
    {
        timer.Stop();
        IsVisible = false;

        // Spegne anche il tipo: ogni barra si vede solo con il suo flag.
        IsInfo = false;
        IsWarning = false;
        IsSuccess = false;
        IsError = false;
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
