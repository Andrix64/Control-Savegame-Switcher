using Avalonia.Threading;
using Lss.App.ViewModels;

namespace Lss.App.Services;

/// <summary>
/// Controlla ogni 2 secondi se i giochi sono aperti (CheckGameAsync di
/// MainForm): chi parte o chiude il gioco aggiorna la sua scheda.
/// </summary>
public sealed class GameMonitor
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };

    private readonly Func<IEnumerable<GameViewModel>> games;

    // Evita controlli sovrapposti se un giro è ancora in corso.
    private bool checking;

    public GameMonitor(Func<IEnumerable<GameViewModel>> games)
    {
        this.games = games;
        timer.Tick += async (_, _) => await CheckAsync();
    }

    public void Start()
    {
        _ = CheckAsync();
        timer.Start();
    }

    public void Stop() => timer.Stop();

    private async Task CheckAsync()
    {
        if (checking)
            return;

        checking = true;

        try
        {
            foreach (GameViewModel game in games().ToList())
            {
                bool running = await Task.Run(game.Manager.IsGameRunning);

                SessionEvent e = game.Session.Update(running, game.Manager.State.ActiveProfile,
                    game.Manager.GameStartTime, DateTime.Now);

                await game.OnGameState(e);
            }
        }
        finally
        {
            checking = false;
        }
    }
}
