using CommunityToolkit.Mvvm.ComponentModel;

namespace Lss.App.ViewModels;

/// <summary>
/// Primo avvio di un gioco: il gioco ha salvataggi ma nessuna partita. Si
/// sceglie il nome della prima partita (FirstRunForm in WinForms).
/// </summary>
public partial class FirstRunViewModel : ObservableObject
{
    private static readonly string Hint = Strings.PlaythroughNameHint;

    public FirstRunViewModel(SaveManager manager, CloudState cloud)
    {
        string game = manager.Game.Name;
        string account = manager.AccountName == "" ? "—" : manager.AccountName;

        Title = Loc.F(Strings.FirstRunTitle, game);
        Intro = Strings.FirstRunIntro;
        NameCaption = Strings.FirstRunNameCaption;
        AccountLine = "✓  " + Loc.F(Strings.FirstRunAccount, account);
        CloudLine = cloud switch
        {
            CloudState.Disabled => "✓  " + Loc.F(Strings.FirstRunCloudOff, game),
            CloudState.Enabled => "!  " + Loc.F(Strings.FirstRunCloudOn, game),
            _ => "?  " + Loc.F(Strings.FirstRunCloudUnknown, game),
        };
        CloudIsWarning = cloud == CloudState.Enabled;

        name = Strings.FirstPlaythroughName;
        Validate();
    }

    public string Title { get; }

    public string Intro { get; }

    public string NameCaption { get; }

    public string AccountLine { get; }

    public string CloudLine { get; }

    public bool CloudIsWarning { get; }

    public string StartText => Strings.FirstRunSave;

    public string SkipText => Strings.FirstRunSkip;

    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string hintText = Hint;

    [ObservableProperty]
    private bool hintIsError;

    [ObservableProperty]
    private bool canStart;

    /// <summary>Nome della prima partita, senza spazi ai bordi.</summary>
    public string PlaythroughName => Name.Trim();

    partial void OnNameChanged(string value) => Validate();

    private void Validate()
    {
        bool valid = SaveManager.IsValidName(PlaythroughName);

        CanStart = valid;
        HintIsError = !valid;
        HintText = valid ? Hint : Loc.F(Strings.InvalidNameHint, Hint);
    }
}
