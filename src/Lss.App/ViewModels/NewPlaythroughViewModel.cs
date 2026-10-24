using CommunityToolkit.Mvvm.ComponentModel;

namespace Lss.App.ViewModels;

/// <summary>Esito della finestra: nome e scelte. Null se annullata.</summary>
public sealed record NewPlaythroughResult(string Name, bool CopyCurrentSaves, bool ActivateNow);

/// <summary>
/// Nuova partita o rinomina (NewProfileForm in WinForms): nome con controllo
/// in tempo reale, copia o partita vuota, attiva subito.
/// </summary>
public partial class NewPlaythroughViewModel : ObservableObject
{
    private static readonly string Hint = Strings.ProfileNameHint;

    private readonly Func<string, bool> profileExists;

    // Nome attuale del profilo da rinominare, null per una partita nuova.
    private readonly string? currentName;

    /// <param name="profileExists">Dice se esiste già una partita con quel nome.</param>
    /// <param name="currentName">Nome da mostrare per la rinomina; null per una partita nuova.</param>
    public NewPlaythroughViewModel(Func<string, bool> profileExists, string? currentName = null)
    {
        this.profileExists = profileExists;
        this.currentName = currentName;

        name = currentName ?? "";
        Validate();
    }

    [ObservableProperty]
    private string name = "";

    [NotifyPropertyChangedFor(nameof(EmptyProfile))]
    [ObservableProperty]
    private bool copyCurrentSaves = true;

    [ObservableProperty]
    private bool activateNow = true;

    [ObservableProperty]
    private string hintText = Hint;

    [ObservableProperty]
    private bool hintIsError;

    [ObservableProperty]
    private bool canCreate;

    public bool IsRename => currentName != null;

    public string Title => IsRename ? Strings.RenameProfileTitle : Strings.NewProfileTitle;

    public string CreateText => IsRename ? Strings.Rename : Strings.Create;

    // Il pulsante "partita vuota" della coppia di scelte: l'opposto di CopyCurrentSaves.
    public bool EmptyProfile
    {
        get => !CopyCurrentSaves;
        set => CopyCurrentSaves = !value;
    }

    public NewPlaythroughResult CreateResult() =>
        new(Name.Trim(), CopyCurrentSaves, ActivateNow);

    partial void OnNameChanged(string value) => Validate();

    private void Validate()
    {
        string trimmed = Name.Trim();
        string error = "";

        if (trimmed != "" && !SaveManager.IsValidName(trimmed))
            error = Loc.F(Strings.InvalidNameHint, Hint);
        else if (trimmed != "" && profileExists(trimmed) && !IsCurrentName(trimmed))
            error = Strings.ProfileExists;

        HintText = error == "" ? Hint : error;
        HintIsError = error != "";

        // Con la rinomina, il nome invariato non serve.
        CanCreate = trimmed != "" && error == "" && trimmed != currentName;
    }

    // Nel rinomina il nome attuale è valido anche con maiuscole diverse.
    private bool IsCurrentName(string name) =>
        currentName != null && string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase);
}
