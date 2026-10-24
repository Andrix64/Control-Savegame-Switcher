using Avalonia.Markup.Xaml;

namespace Lss.App.Localization;

/// <summary>
/// Testo tradotto in XAML: Text="{loc:Str Undo}". La lingua è quella del
/// sistema, letta all'avvio.
/// </summary>
public sealed class Str : MarkupExtension
{
    public Str(string key) { Key = key; }

    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        Strings.ResourceManager.GetString(Key, Strings.Culture) ?? "!" + Key;
}
