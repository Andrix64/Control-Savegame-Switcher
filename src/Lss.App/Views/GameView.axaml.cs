using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Lss.App.ViewModels;

namespace Lss.App.Views;

/// <summary>
/// Le partite di un gioco: barre, elenco delle schede, avviso in fondo.
/// </summary>
public partial class GameView : UserControl
{
    public GameView()
    {
        InitializeComponent();

        // Cursore di attesa mentre un'operazione è in corso.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is GameViewModel game)
            {
                game.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(GameViewModel.IsBusy))
                        Cursor = game.IsBusy ? new Cursor(StandardCursorType.Wait) : null;
                };
            }
        };
    }
}
