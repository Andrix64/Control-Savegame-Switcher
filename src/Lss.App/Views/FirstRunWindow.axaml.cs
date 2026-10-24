using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Lss.App.ViewModels;

namespace Lss.App.Views;

/// <summary>
/// Primo avvio: il nome della prima partita. Il risultato è il nome, o null
/// se si salta.
/// </summary>
public partial class FirstRunWindow : Window
{
    public FirstRunWindow() => InitializeComponent();

    public FirstRunWindow(FirstRunViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private FirstRunViewModel ViewModel => (FirstRunViewModel)DataContext!;

    private void Start_Click(object? sender, RoutedEventArgs e) => Close(ViewModel.PlaythroughName);

    private void Skip_Click(object? sender, RoutedEventArgs e) => Close(null);
}
