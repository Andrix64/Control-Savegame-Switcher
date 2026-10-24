using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Lss.App.Services;

/// <summary>
/// Finestre di dialogo vere, aperte sopra la finestra principale.
/// </summary>
public sealed class DialogService(Func<Window?> owner) : IDialogService
{
    public Task<bool> InformAsync(string heading, string text, InfoKind kind, string? actionText = null) =>
        throw new NotImplementedException("Finestra dei messaggi: G4.");

    public Task<bool> ConfirmAsync(string heading, string text, string verb, bool destructive = false, InfoKind kind = InfoKind.Warning) =>
        throw new NotImplementedException("Finestra dei messaggi: G4.");

    // Serve una finestra padre: senza, il dialogo non sarebbe modale.
    public async Task<T?> ShowAsync<T>(Window dialog) =>
        await dialog.ShowDialog<T?>(owner() ?? throw new InvalidOperationException("Nessuna finestra principale."));

    public async Task<string?> PickZipToSaveAsync(string title, string suggestedName, string filterName)
    {
        Window? window = owner();

        if (window == null)
            return null;

        IStorageFile? file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = "zip",
            FileTypeChoices = [new FilePickerFileType(filterName) { Patterns = ["*.zip"] }],
            ShowOverwritePrompt = true,
        });

        return file?.TryGetLocalPath();
    }

    public async Task<string?> PickZipToOpenAsync(string title, string filterName)
    {
        Window? window = owner();

        if (window == null)
            return null;

        IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(filterName) { Patterns = ["*.zip"] }],
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<bool> OpenFolderAsync(string path)
    {
        Window? window = owner();

        if (window == null)
            return false;

        try
        {
            Directory.CreateDirectory(path);
            return await window.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(path));
        }
        catch (Exception ex)
        {
            Log.Write("Apertura cartella fallita", ex);
            return false;
        }
    }
}
