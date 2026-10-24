using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lss.App.ViewModels;

/// <summary>
/// Una scheda partita. Le azioni chiamano il GameViewModel che la contiene.
/// </summary>
public partial class PlaythroughViewModel : ObservableObject
{
    private readonly GameViewModel game;

    // CardData è interno a Lss.Core: anche il costruttore resta interno.
    internal PlaythroughViewModel(CardData data, GameViewModel game)
    {
        Data = data;
        this.game = game;
    }

    internal CardData Data { get; }

    public string ProfileName => Data.ProfileName;

    public string Name => SaveManager.ProfileDisplayName(Data.ProfileName);

    public bool IsActive => Data.Active;

    public string Note => Data.Info.Note;

    public bool HasNote => Note != "";

    public string Details => CardText.Details(Data);

    public string PlayText => IsActive ? Strings.PlayButton : Strings.PlayThisButton;

    public bool CanPlay => !game.IsBusy && !game.GameRunning;

    public bool ShowPlay => !IsActive || game.Manager.State.LaunchGame;

    public bool CanDelete => !IsActive && CanPlay;

    public string AccessibleName => Name + (IsActive ? Strings.ActiveSuffix : "");

    public string MoreActionsName => Loc.F(Strings.MoreActionsFor, Name);

    /// <summary>Chiamato dal GameViewModel quando cambia busy o gioco aperto.</summary>
    public void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CanPlay));
        OnPropertyChanged(nameof(ShowPlay));
        OnPropertyChanged(nameof(CanDelete));
    }

    [RelayCommand]
    private Task PlayAsync() => game.PlayCardAsync(this);

    [RelayCommand]
    private Task LoadOnlyAsync() => game.PlayAsync(ProfileName, launch: false);

    [RelayCommand]
    private Task ExportAsync() => game.ExportAsync(ProfileName);

    [RelayCommand]
    private Task DeleteAsync() => game.DeleteAsync(ProfileName);
}
