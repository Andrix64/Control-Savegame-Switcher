using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Lss.App.ViewModels;

namespace Lss.App.Views;

/// <summary>
/// Impostazioni. Il risultato è true se sono state salvate.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow() => InitializeComponent();

    public SettingsWindow(SettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext!;

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (await ViewModel.TrySaveAsync())
            Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
