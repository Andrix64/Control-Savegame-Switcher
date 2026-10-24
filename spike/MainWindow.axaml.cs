using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace LssSpike;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    // Finestra di sistema: su Linux passa dal portale del desktop.
    private async void PickFolder_Click(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Pick a folder" });

        ViewModel.Status = folders.Count > 0
            ? "Picked: " + folders[0].TryGetLocalPath()
            : "No folder picked.";
    }

    // Su Windows Esplora file, su Linux xdg-open.
    private async void OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        bool opened = await Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(Path.GetTempPath()));
        ViewModel.Status = opened ? "Folder opened." : "Couldn't open the folder.";
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: PlaythroughViewModel playthrough })
            return;

        bool confirmed = await Confirm(
            $"Delete the playthrough \"{playthrough.Name}\"? You can recover it from Deleted playthroughs.",
            "Delete");

        ViewModel.Status = confirmed ? $"{playthrough.Name} deleted (not really: this is a test)." : "Cancelled.";
    }

    // Dialogo modale con i verbi sui pulsanti, come MessageDialog.Confirm.
    private async Task<bool> Confirm(string message, string verb)
    {
        var dialog = new Window
        {
            Title = "Local Save Switcher",
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Background,
        };

        var yes = new Button { Content = verb, Classes = { "primary" }, IsDefault = true };
        var no = new Button { Content = "Cancel", IsCancel = true };
        yes.Click += (_, _) => dialog.Close(true);
        no.Click += (_, _) => dialog.Close(false);

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { no, yes },
                },
            },
        };

        return await dialog.ShowDialog<bool>(this);
    }
}
