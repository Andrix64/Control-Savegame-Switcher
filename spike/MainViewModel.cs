using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LssSpike;

public partial class PlaythroughViewModel : ObservableObject
{
    public required string Name { get; init; }
    public required string Note { get; init; }
    public required string PlayTime { get; init; }

    [ObservableProperty]
    private bool isActive;
}

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<PlaythroughViewModel> Playthroughs { get; } =
    [
        new() { Name = "Mario", Note = "Polaris mission, before Ahti", PlayTime = "12 h 30 min", IsActive = true },
        new() { Name = "Luigi", Note = "Second run, hard", PlayTime = "3 h 05 min" },
        new() { Name = "Peach", Note = "", PlayTime = "0 min" },
    ];

    // Cosa serve sapere dalla prova: sistema, runtime, versione di Avalonia.
    public string SystemInfo { get; } =
        $"{RuntimeInformation.OSDescription} · {RuntimeInformation.OSArchitecture} · " +
        $".NET {Environment.Version} · Avalonia {typeof(Avalonia.Application).Assembly.GetName().Version?.ToString(3)}";

    [ObservableProperty]
    private string status = "Ready.";

    // Lavoro lungo in background: l'interfaccia non deve bloccarsi.
    [RelayCommand]
    private async Task PlayAsync(PlaythroughViewModel playthrough)
    {
        Status = $"Loading {playthrough.Name}...";
        await Task.Delay(1500);

        foreach (PlaythroughViewModel p in Playthroughs)
            p.IsActive = p == playthrough;

        Status = $"{playthrough.Name} is in the game now.";
    }
}
