# Phase G — Avalonia interface

Goal: a new app `src/Lss.App` (Avalonia 12.1.4) with the same functions as
the WinForms app, plus a list of games. When it's complete, the WinForms
app is deleted and `Lss.App` becomes `LSS.exe`.

**How to port a window.** For each view below, open the WinForms file
named in the task and read it whole. Reproduce the same behaviour: same
texts (`Strings.*`), same order of checks, same confirmations, same calls
to `SaveManager`. The task lists the essentials; if the WinForms code does
something the task doesn't mention, do it too and add a line in Notes.
**Don't change `Lss.Core` in this phase** except where a task says so.

The test app in `spike/` shows working Avalonia 12 code (window, styles,
cards, flyout menu, dialog, folder picker, launcher): copy from it.

Packages (exact versions):

| Package | Version |
|---|---|
| `Avalonia` | 12.1.4 |
| `Avalonia.Desktop` | 12.1.4 |
| `Avalonia.Themes.Fluent` | 12.1.4 |
| `Avalonia.Fonts.Inter` | 12.1.4 |
| `CommunityToolkit.Mvvm` | 8.4.2 |
| `Avalonia.Headless.XUnit` (tests only) | 12.1.4 |

---

## G1 — Project skeleton

1. Create `src/Lss.App/Lss.App.csproj`:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">

     <PropertyGroup>
       <OutputType>WinExe</OutputType>
       <TargetFramework>net10.0</TargetFramework>
       <Nullable>enable</Nullable>
       <ImplicitUsings>enable</ImplicitUsings>
       <RootNamespace>Lss.App</RootNamespace>
       <!-- Diventa LSS quando l'app WinForms viene eliminata (G14). -->
       <AssemblyName>Lss.App</AssemblyName>
       <ApplicationIcon>..\..\app.ico</ApplicationIcon>
       <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
       <Version>0.9.0</Version>
       <Product>Local Save Switcher</Product>
       <Company>Andrix64</Company>
       <Authors>Andrix64</Authors>
       <Copyright>© 2026 Andrix64</Copyright>
     </PropertyGroup>

     <ItemGroup>
       <PackageReference Include="Avalonia" Version="12.1.4" />
       <PackageReference Include="Avalonia.Desktop" Version="12.1.4" />
       <PackageReference Include="Avalonia.Themes.Fluent" Version="12.1.4" />
       <PackageReference Include="Avalonia.Fonts.Inter" Version="12.1.4" />
       <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
     </ItemGroup>

     <ItemGroup>
       <ProjectReference Include="..\Lss.Core\Lss.Core.csproj" />
       <ProjectReference Include="..\Lss.Platform\Lss.Platform.csproj" />
     </ItemGroup>

     <ItemGroup>
       <AvaloniaResource Include="Assets\**" />
       <InternalsVisibleTo Include="Lss.Tests" />
     </ItemGroup>

     <!-- I .pdb delle librerie native (100 MB) non servono a chi usa l'app. -->
     <Target Name="RemoveNativePdbs" AfterTargets="Publish">
       <Delete Files="$(PublishDir)libSkiaSharp.pdb;$(PublishDir)libHarfBuzzSharp.pdb" />
     </Target>

   </Project>
   ```

2. Copy `app.ico` to `src/Lss.App/Assets/app.ico` (keep the root one for
   WinForms until G14).
3. `Program.cs` (namespace `Lss.App`):

   ```csharp
   using Avalonia;

   namespace Lss.App;

   internal static class Program
   {
       /// <summary>Versione dell'app (es. "0.9.0"), dal tag Version del .csproj.</summary>
       public static string Version { get; } =
           typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "";

       [STAThread]
       public static int Main(string[] args)
       {
           Platform.Current = OperatingSystem.IsWindows()
               ? new WindowsPlatform()
               : throw new PlatformNotSupportedException();   // Linux: fase H

           AppDomain.CurrentDomain.UnhandledException += (_, e) =>
               Log.Write("Errore imprevisto (chiusura)", (Exception)e.ExceptionObject);

           // Due finestre aperte lavorerebbero sugli stessi file.
           using IDisposable? instance = Platform.Current.AcquireSingleInstance();

           if (instance == null)
           {
               BuildAvaloniaApp().StartWithClassicDesktopLifetime(["--already-running"]);
               return 1;
           }

           return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
       }

       // Usato anche dall'anteprima dell'editor.
       public static AppBuilder BuildAvaloniaApp() =>
           AppBuilder.Configure<App>()
               .UsePlatformDetect()
               .WithInterFont()
               .LogToTrace();
   }
   ```

   `--already-running` is handled in `App` (G3): it shows
   `Strings.AppAlreadyRunning` and exits.
4. `App.axaml` / `App.axaml.cs` like `spike/`, with
   `RequestedThemeVariant="Dark"`, `<FluentTheme />` and
   `<StyleInclude Source="avares://Lss.App/Styles/Theme.axaml" />`. In
   `OnFrameworkInitializationCompleted` for now just open an empty
   `MainWindow` (G3 fills it).
5. Add the project to the solution:
   `dotnet sln LSS.slnx add src/Lss.App/Lss.App.csproj`.
6. In `src/Lss.Core/Lss.Core.csproj` and `src/Lss.Platform/Lss.Platform.csproj`
   check that `InternalsVisibleTo Include="Lss.App"` is there (Core has it
   from C4; add it to Platform).

Check: build; `dotnet run --project src/Lss.App` opens an empty dark
window (only if a screen is available; otherwise skip and note it).

Commit: `Avalonia app skeleton`

---

## G2 — Theme and texts in XAML

### G2.1 `src/Lss.App/Styles/Theme.axaml`

A `<Styles>` file with `<Styles.Resources>` holding one `SolidColorBrush`
per colour of `Theme.cs` (WinForms), same names, values converted to hex:

| Key | Hex | | Key | Hex |
|---|---|---|---|---|
| `Background` | `#0F0F10` | | `Success` | `#4CC38A` |
| `Header` | `#0A0A0B` | | `Warning` | `#F5B83D` |
| `Surface` | `#19191B` | | `Error` | `#FF5C5C` |
| `SurfaceHover` | `#232326` | | `InfoBlue` | `#7FB0F0` |
| `SurfaceSelected` | `#2C2C30` | | `TextSecondary` | `#A8A8AE` |
| `Border` | `#2E2E33` | | `InfoBack` | `#1B2230` |
| `Text` | `#F2F2F2` | | `InfoBorder` | `#34507A` |
| `TextMuted` | `#96969C` | | `WarningBack` | `#2A2110` |
| `TextDisabled` | `#5A5A60` | | `WarningBorder` | `#5C4515` |
| `Accent` | `#E0261B` | | `SuccessBack` | `#18261F` |
| `AccentHover` | `#F03C30` | | `SuccessBorder` | `#2E5C44` |
| `AccentPressed` | `#B81E15` | | | |
| `AccentSoft` | `#3E1816` | | | |
| `AccentSoftText` | `#FF8076` | | | |

Check the values against `Theme.cs` (it may have more colours, e.g. an
error background: add them the same way).

Font sizes (WinForms points × 96/72 = Avalonia pixels):

| Theme.cs font | Avalonia |
|---|---|
| `Base` 10 pt | `FontSize="13"` |
| `Small` 9 pt | `12` |
| `Caption` 8 pt bold | `11`, `FontWeight="Bold"` |
| `Button` 10 pt semibold | `13`, `SemiBold` |
| `ItemName` 11 pt semibold | `15`, `SemiBold` |
| `Large` 16 pt semibold | `21`, `SemiBold` |
| `Title` 16 pt bold | `21`, `Bold` |
| `Heading` 14 pt semibold | `19`, `SemiBold` |
| `CardTitle` 12 pt semibold | `16`, `SemiBold` |
| `Note` 9.5 pt italic | `13`, `FontStyle="Italic"` |

Styles (selectors) to define in the same file, used by all views:

- `Window` background `{StaticResource Background}`, `FontSize` 13.
- `TextBlock.caption` (11, bold, `TextMuted`), `.small` (12,
  `TextSecondary`), `.heading` (19 semibold), `.title` (21 bold),
  `.cardTitle` (16 semibold), `.note` (13 italic, `#D8D8DC`).
- `Button.primary` (Accent background, white text; hover `AccentHover`,
  pressed `AccentPressed`), `Button.secondary` (default Fluent look on
  `SurfaceSelected`), `Button.danger` (Error background), `Button.link`
  (transparent, `AccentSoftText`, underline on hover).
  For hover/pressed in Avalonia 12 use the selector
  `Button.primary:pointerover /template/ ContentPresenter#PART_ContentPresenter`
  and `:pressed` the same way.
- `Border.card` (Surface, 1 px `Border`, `CornerRadius="10"`,
  `Padding="18,14,14,14"`), `Border.card.active` (border `Accent`).
- `Border.pill` (Accent background, `CornerRadius="10"`, `Padding="8,1"`,
  white bold 11 text).
- `Border.infobar.info|warning|success|error` (background/border pairs
  from the table: `InfoBack/InfoBorder`, `WarningBack/WarningBorder`,
  `SuccessBack/SuccessBorder`; error: use `AccentSoft`/`Accent`),
  `CornerRadius="6"`, `Padding="12,8"`.

### G2.2 Texts in XAML: `src/Lss.App/Localization/Str.cs`

```csharp
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
```

In XAML: `xmlns:loc="using:Lss.App.Localization"` and
`Text="{loc:Str YourPlaythroughs}"`. A missing key shows `!Key`: search
the running app for `!` during manual checks. Texts with values are built
in the ViewModel with `Loc.F(...)`, never in XAML.

Add a test `tests/Lss.Tests/XamlKeysTests.cs`: read every `*.axaml` under
`src/Lss.App` (path from the test assembly folder: go up until a folder
containing `LSS.slnx` is found), extract `{loc:Str (\w+)}` with a regex, and
assert each key exists in `Strings.ResourceManager.GetString(key,
CultureInfo.InvariantCulture)`. This catches typos at test time.

Commit: `Theme and translated texts for Avalonia`

---

## G3 — Services

All in `src/Lss.App/Services/`.

### G3.1 `IDialogService`

```csharp
namespace Lss.App.Services;

/// <summary>
/// Finestre di dialogo e di sistema, separate dai ViewModel per poterli
/// provare con una versione finta.
/// </summary>
public interface IDialogService
{
    /// <summary>Messaggio con un solo pulsante (Close/OK) e, se c'è, un pulsante d'azione: true se è stato premuto.</summary>
    Task<bool> InformAsync(string heading, string text, InfoKind kind, string? actionText = null);

    /// <summary>Domanda con il verbo sul pulsante: true se l'utente conferma.</summary>
    Task<bool> ConfirmAsync(string heading, string text, string verb, bool destructive = false, InfoKind kind = InfoKind.Warning);

    /// <summary>Mostra una finestra modale e restituisce il suo risultato.</summary>
    Task<T?> ShowAsync<T>(Window dialog);

    Task<string?> PickZipToSaveAsync(string title, string suggestedName, string filterName);

    Task<string?> PickZipToOpenAsync(string title, string filterName);

    /// <summary>Apre una cartella nel file manager. false se non si riesce.</summary>
    Task<bool> OpenFolderAsync(string path);
}
```

`InfoKind` already exists in the WinForms code (`PlaythroughControls.cs`):
create `src/Lss.App/Services/InfoKind.cs` with the same members
(`Info, Success, Warning, Error`; check the WinForms enum and copy it).

### G3.2 `DialogService`

- Constructor takes a `Func<Window?> owner` (the main window).
- `InformAsync` / `ConfirmAsync` open a `MessageWindow` (G4) with
  `ShowDialog<bool>(owner)`.
- `PickZipToSaveAsync`: `owner.StorageProvider.SaveFilePickerAsync(new
  FilePickerSaveOptions { Title, SuggestedFileName = suggestedName,
  DefaultExtension = "zip", FileTypeChoices = [new FilePickerFileType(filterName)
  { Patterns = ["*.zip"] }], ShowOverwritePrompt = true })` →
  `file?.TryGetLocalPath()`.
- `PickZipToOpenAsync`: `OpenFilePickerAsync` with `AllowMultiple = false`
  and the same file type → first file's `TryGetLocalPath()`.
- `OpenFolderAsync`: `Directory.CreateDirectory(path)` then
  `owner.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(path))`, in
  try/catch → false.

**Filter name**: `Strings.ZipFilter` is a WinForms filter
(`"{0} playthrough (*.zip)|*.zip"`). Add a new key `ZipFileType`:
English `{0} playthrough`, Italian `Partita di {0}`, and use
`Loc.F(Strings.ZipFileType, game.Name)`. `ZipFilter` is deleted in G14.

### G3.3 Tests helper

`tests/Lss.Tests/FakeDialogService.cs`: implements `IDialogService`,
records calls in a `List<string> Calls`, returns configurable answers
(`ConfirmAnswer`, `InformAnswer`, `ZipToSave`, `ZipToOpen`,
`ShowResult` of type `object?`). The tests project must reference
`src/Lss.App/Lss.App.csproj` from now on.

Commit: `Dialog service`

---

## G4 — `MessageWindow`

Port `MessageDialog.cs` (WinForms). `Views/MessageWindow.axaml(.cs)`:

- Title `Strings.AppTitle`, no resize, `SizeToContent="Height"`,
  `Width="460"`, centred on owner, icon `Assets/app.ico`.
- Left: an icon glyph by `InfoKind` (use text glyphs, no images:
  Info `ℹ` `InfoBlue`, Success `✓` `Success`, Warning `⚠` `Warning`, Error
  `✕` `Error`).
- Heading (`.heading`), text (wrap; keep `\n` line breaks).
- Buttons on the right: for Confirm → `[Cancel] [verb]` with verb
  `primary` (or `danger` if destructive), Enter = verb, Esc = Cancel; for
  Inform → optional `[actionText]` (secondary) + `[Close]` (primary).
- Result `true` = verb / action pressed.

Commit: `Message window`

---

## G5 — Game sessions in Core: `SessionTracker`

Session logic must be testable, so it goes to `src/Lss.Core/SessionTracker.cs`
(the only Core change in this phase):

```csharp
namespace Lss;

public enum SessionChange { None, Started, Ended }

/// <summary>Una sessione di gioco finita o appena iniziata.</summary>
public sealed record SessionEvent(SessionChange Change, string Profile, DateTime Start, DateTime End);

/// <summary>
/// Segue apertura e chiusura del gioco di un SaveManager: quando parte,
/// quale partita era nel gioco, quando finisce. Come in MainForm.CheckGameAsync.
/// </summary>
public sealed class SessionTracker
{
    public bool Running { get; private set; }
    public DateTime? Start { get; private set; }
    public string Profile { get; private set; } = "";

    /// <param name="running">Se il gioco è aperto adesso.</param>
    /// <param name="activeProfile">Partita nel gioco adesso (State.ActiveProfile).</param>
    /// <param name="startTime">Ora di avvio del processo, chiamata solo all'avvio.</param>
    public SessionEvent Update(bool running, string activeProfile, Func<DateTime?> startTime, DateTime now)
}
```

Rules (from `MainForm.CheckGameAsync`, `EndSession`, `OnGameClosedAsync`):

- `running == Running` → return `new(None, Profile, Start ?? now, now)`.
- not running → running: `Start = startTime() ?? now`,
  `Profile = activeProfile`, `Running = true`, return `Started`.
- running → not running: event `Ended` with the saved `Profile` and
  `Start`, then reset `Start = null`, `Profile = ""`, `Running = false`.
- `EndNow(DateTime now)`: if a session is open, returns `Ended` and resets
  (used when the app closes); otherwise `None`.

Tests `SessionTrackerTests`: start uses process time; start without
process time uses `now`; end returns profile and times; repeated calls
return `None`; `EndNow` with and without session.

Commit: `SessionTracker`

---

## G6 — `GameViewModel`: the playthroughs of one game

`src/Lss.App/ViewModels/GameViewModel.cs`, `partial class GameViewModel :
ObservableObject`. It replaces the logic of `MainForm.cs`. Constructor:
`GameViewModel(SaveManager manager, IDialogService dialogs)`.

Observable state (use `[ObservableProperty]`):

| Property | Meaning |
|---|---|
| `ObservableCollection<PlaythroughViewModel> Playthroughs` | cards, active first, then by name (as `RefreshAll`) |
| `bool IsBusy` | an operation is running |
| `bool GameRunning` | the game is open |
| `CloudState Cloud` | from `manager.GetCloudState()` |
| `bool ShowCloudBar` | `Cloud == CloudState.Enabled` |
| `string GameBarTitle`, `string GameBarText`, `bool ShowGameBar` | as `UpdateControls` |
| `int DeletedCount`, `string DeletedLinkText`, `bool ShowDeletedLink` | as `RefreshAll` |
| `ToastViewModel Toast` | message at the bottom |
| `string EmptyText` | `Strings.NoPlaythroughs` when the list is empty |

Methods/commands — port one by one from `MainForm.cs`, same names without
`Async` suffix for commands (`[RelayCommand]` generates `XxxCommand`):

| WinForms (`MainForm.cs`) | `GameViewModel` |
|---|---|
| `RefreshAll` | `Refresh()` |
| `UpdateControls` | `UpdateState()` (sets the properties above) |
| `ShowToast` / `HideToast` | `Toast.Show(kind, text, actionText, action, autoHide)` / `Toast.Hide()` |
| `PlayAsync(profile, launch, offerUndo)` | `PlayAsync(...)` (private), same logic |
| `LaunchOnly` | `LaunchOnly()` |
| `RenameAsync` | `RenameAsync(profile)` → opens `NewPlaythroughWindow` in rename mode (G9) |
| `EditNote` | `EditNoteAsync(profile)` → `TextPromptWindow` (G9) |
| `ExportAsync` | `ExportAsync(profile)` → `dialogs.PickZipToSaveAsync` |
| `ImportClicked` | `[RelayCommand] ImportAsync()` |
| `DeleteAsync` | `DeleteAsync(profile)` |
| `NewClicked` | `[RelayCommand] NewAsync()` |
| `ShowHistory(focusSave)` | `ShowHistoryAsync(focusSave)` (G10) |
| `ShowSettings` | `[RelayCommand] SettingsAsync()` (G11) |
| `ShowDeleted` | `[RelayCommand] ShowDeletedAsync()` (G12) |
| `ShowCloudHelp` | `[RelayCommand] CloudHelpAsync()` (text `Loc.F(Strings.CloudHelpText, name)`) |
| `OfferFirstRunAsync` | `OfferFirstRunAsync()` (G11) |
| `ReportIgnoredProfileFolders` | `ReportIgnoredProfileFolders()` |
| `RunBusyAsync` | `RunBusyAsync(message, Func<Result>)` (`Task.Run`, same try/catch/finally) |
| `ShowResult`, `ShowError` | same names; "Open backup" uses `dialogs.OpenFolderAsync` |
| `ConfirmCloud(launching)` | `ConfirmCloudAsync(launching)` (async because the dialog is) |
| `CheckGameAsync`, `OnGameClosedAsync`, `EndSession` | `OnGameState(SessionEvent e)` called by the monitor (G8) |

`OnCardActionAsync` becomes the card commands in `PlaythroughViewModel`,
which call back into `GameViewModel` (pass `this` to each card).

`ToastViewModel` (`ViewModels/ToastViewModel.cs`): `Kind`, `Text`,
`ActionText`, `HasAction`, `IsVisible`, `[RelayCommand] Action` (runs the
action then hides), auto-hide after 12 s with a `DispatcherTimer`
(`Avalonia.Threading`), like `toastTimer`.

Tests `GameViewModelTests` with `TestFolder`, `FakeDialogService` and a
manager from `folder.CreateManager()`:

- `Refresh_PutsActivePlaythroughFirst`;
- `Delete_AsksConfirmationAndDeletes` (`ConfirmAnswer = true`);
- `Delete_Cancelled_KeepsPlaythrough` (`ConfirmAnswer = false`);
- `Import_UsesPickedZip`.

ViewModel tests that touch `DispatcherTimer` need the Avalonia dispatcher:
mark those test classes with `[AvaloniaFact]` from
`Avalonia.Headless.XUnit` 12.1.4 (add the package to the tests project and
an `[assembly: AvaloniaTestApplication(typeof(TestApp))]` with a minimal
`TestApp : Application` and `AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions())`).
If this setup fails twice, stop and note it.

Commit: `GameViewModel`

---

## G7 — Playthrough card

`ViewModels/PlaythroughViewModel.cs` built from a `CardData`
(`src/Lss.Core/CardText.cs`) and the owning `GameViewModel`:

| Property | Value |
|---|---|
| `Name` | `SaveManager.ProfileDisplayName(data.ProfileName)` |
| `IsActive` | `data.Active` |
| `Note`, `HasNote` | `data.Info.Note` |
| `Details` | `CardText.Details(data)` |
| `PlayText` | `Strings.PlayButton` if active, else `Strings.PlayThisButton` |
| `CanPlay` | `!game.IsBusy && !game.GameRunning` |
| `ShowPlay` | not active, or active and `State.LaunchGame` |
| `CanDelete` | not active and `CanPlay` |
| `AccessibleName` | name + `Strings.ActiveSuffix` if active |
| `MoreActionsName` | `Loc.F(Strings.MoreActionsFor, Name)` |

Commands: `Play`, `LoadOnly`, `SavePoint`, `History`, `Rename`,
`EditNote`, `Export`, `Delete` → call the `GameViewModel` methods as in
`MainForm.OnCardActionAsync`.

`Views/PlaythroughCardView.axaml` (UserControl), same layout as
`PlaythroughCard` (`PlaythroughControls.cs` lines with `titleRow`, `info`,
`menu`, `actions`):

- active card: name + pill `{loc:Str InGameNow}`; note; details; "Add
  note" link if no note; buttons **below** (Play — secondary —, Save a
  point, History, ⋯);
- other cards: name; note; details; link; buttons **on the right**
  (Play this — primary —, ⋯);
- ⋯ is a `Button` with `MenuFlyout`: Load without starting (only if not
  active), Rename, Edit note, Export, separator, Delete (disabled on the
  active card, with `ToolTip.Tip="{loc:Str ActiveCannotBeDeleted}"`).
- `AutomationProperties.Name` on the card and on ⋯.

Tests `PlaythroughViewModelTests`: texts and flags for active/inactive,
`ShowPlay` false when active and `LaunchGame` off.

Commit: `Playthrough card`

---

## G8 — Main window, games and monitor

### G8.1 `MainWindowViewModel`

```csharp
public partial class MainWindowViewModel : ObservableObject
```

- `ObservableCollection<GameItemViewModel> Games` — supported games of the
  account (`SteamLibrary.FindGames(steamRoot, account.Folder)` with
  `Support == Supported`);
- `ObservableCollection<GameItemViewModel> OtherGames` — the others;
- `[ObservableProperty] GameItemViewModel? selectedGame` — on change, get
  or create its `GameViewModel` (keep them in a
  `Dictionary<string, GameViewModel>` by AppID) and set `CurrentGame`;
  save `AppSettings.LastGame`;
- `[ObservableProperty] GameViewModel? currentGame`;
- `bool ShowSidebar` — `Games.Count > 1 || OtherGames.Count > 0`;
- `string AccountText` — account display name;
- `string VersionText` — `"v" + Program.Version`.

`GameItemViewModel`: `GameInfo Info`, `Name`, `Bitmap? Icon` (load
`Info.IconPath` with `new Bitmap(path)` in try/catch → null), `Initial`
(first letter of the name, upper case, for the placeholder),
`SupportText` (`Strings.SupportSavesElsewhere` /
`Strings.SupportNoSaves`, new keys below).

New texts:

| Key | English | Italian |
|---|---|---|
| `GamesCaption` | `GAMES` | `GIOCHI` |
| `OtherGamesCaption` | `Other games` | `Altri giochi` |
| `SupportSavesElsewhere` | `Saves are outside Steam's userdata folder: not supported yet.` | `I salvataggi sono fuori dalla cartella userdata di Steam: per ora non è supportato.` |
| `SupportNoSaves` | `No saves yet: start the game once.` | `Ancora nessun salvataggio: avvia il gioco una volta.` |
| `NoSupportedGames` | `No game with saves in Steam's userdata folder was found for this account.` | `Per questo account non c'è nessun gioco con i salvataggi nella cartella userdata di Steam.` |
| `OnlyUserdataNotice` | `LSS copies only the files in Steam's userdata folder for {0}. If the game also saves elsewhere, those files don't change between playthroughs.` | `LSS copia solo i file di {0} nella cartella userdata di Steam. Se il gioco salva anche altrove, quei file non cambiano tra le partite.` |

### G8.2 Monitor

`Services/GameMonitor.cs`: a `DispatcherTimer` with `Interval = 2 s`. On
each tick, for **every** `GameViewModel` created so far **and** every
supported game whose data folder already has `stato.ini` (create their
`GameViewModel`s at startup so they're monitored):

1. `bool running = await Task.Run(vm.Manager.IsGameRunning);`
2. `SessionEvent e = vm.Session.Update(running, vm.Manager.State.ActiveProfile, vm.Manager.GameStartTime, DateTime.Now);`
   (`Session` is a `SessionTracker` field of `GameViewModel`).
3. `vm.OnGameState(e)`: `Started` → hide toast, update; `Ended` →
   `manager.AddPlayTime(e.Profile, e.Start, e.End)` then the save-on-exit
   logic of `OnGameClosedAsync`; `None` → if running and not busy,
   `UpdateState()` (session minutes in the bar).
4. A flag prevents overlapping ticks (`checkingGame` in WinForms).

On window close: for each `GameViewModel`, `Session.EndNow(DateTime.Now)`
and `AddPlayTime` if `Ended`. Closing is cancelled while any game
`IsBusy`, with `Strings.WaitForOperation` (as `FormClosing`).

### G8.3 `MainWindow.axaml`

- Header (`Header` background, 68 px): "LSS" (`.title`) and
  "LOCAL SAVE SWITCHER · v…" (`.caption`) on the left with a 4 px accent
  bar, Settings button on the right (`{loc:Str SettingsButton}`, bound to
  `CurrentGame.SettingsCommand`).
- Left sidebar (240 px, visible if `ShowSidebar`): caption
  `GamesCaption`, a `ListBox` of `Games` (icon 32×32 or initial, name),
  `SelectedItem` bound to `SelectedGame`; then an `Expander` "Other
  games" with `OtherGames` (name + `SupportText`, greyed, not selectable).
- Right: a `ContentControl Content="{Binding CurrentGame}"` with a
  `DataTemplate` for `GameViewModel` → `GameView` (G8.4). If there is no
  supported game, show `Strings.NoSupportedGames`.

### G8.4 `GameView.axaml` (UserControl)

Rows, as `MainForm.BuildLayout`: cloud bar (infobar warning, title
`Loc.F(CloudBarTitle, name)`, text, action button `CloudBarAction` →
`CloudHelpCommand`), game bar (infobar info), title row
(`YourPlaythroughs` heading, Import, New playthrough), the notice
`OnlyUserdataNotice` (small, `TextMuted`), the cards (`ScrollViewer` +
`ItemsControl` of `PlaythroughCardView`, spacing 10), the empty text,
the deleted link (`Button.link`), the toast (infobar with optional action
button). Everything disabled while `IsBusy`; the window shows a wait
cursor (`Cursor="Wait"` on the root when busy).

### G8.5 Startup (`App.OnFrameworkInitializationCompleted`)

1. If `--already-running`: show `MessageWindow` with
   `Strings.AppAlreadyRunning` (no owner) and shut down.
2. `LegacyMigration.Run()`, then `Log.Write("Avvio versione " + version)`,
   then, if `LegacyMigration.Notice(result, detail)` is not null, show its
   text in a `MessageWindow` (warning or info by `IsWarning`), as the
   WinForms `Program.cs` does. Keep these lines together under the comment
   `// Compatibilità con Control Save Manager (vedi LegacyMigration).` and
   put no other Control Save Manager code anywhere in `Lss.App`: it must be
   removable by deleting these lines (see `docs/DEVELOPMENT.md`,
   "Compatibility with Control Save Manager").
3. Steam root: `Platform.Current.FindSteamRoots()`; none →
   `Strings.SteamNotFound` and exit. `userdata` missing →
   `Strings.UserDataNotFound`.
4. `SaveManager.RecoverAllGameFolders(userData)`.
5. Accounts: `SaveManager.FindAccountsWithGames(userData)`; none → show
   the new key `NoAccountWithGames` (English `No Steam account on this PC has games
   with saves in the userdata folder.`, Italian `Nessun account Steam su
   questo PC ha giochi con i salvataggi nella cartella userdata.`) and show
   it. One account → use it; more → `AccountWindow` (G12) with
   `AppSettings.LastAccount` preselected; save the choice.
6. Build `MainWindowViewModel`, select `AppSettings.LastGame` if it is in
   `Games`, else the first game. Show `MainWindow`.
7. For the selected game: `OfferFirstRunAsync()` then
   `ReportIgnoredProfileFolders()` (as `MainForm.Load`). Do the same the
   first time each other game is selected.
8. Unhandled UI exceptions: `Dispatcher.UIThread.UnhandledException` →
   log and show `Strings.UnexpectedError`, like `Program.ShowUnexpectedError`
   (read and port it), `e.Handled = true`.

Commit: `Main window with games`

---

## G9 — New playthrough, rename, note

- `NewPlaythroughWindow` + `NewPlaythroughViewModel`: port
  `NewProfileForm.cs` (both modes: new and rename). Name box with
  validation (`SaveManager.IsValidName`, `Strings.InvalidNameHint`,
  `Strings.ProfileExists` via the `exists` function), radio buttons copy
  current saves / empty (`CopyCurrentSaves`, `EmptyProfileOption`), check
  "Activate now". Result: a record `NewPlaythroughResult(string Name, bool
  CopyCurrentSaves, bool ActivateNow)` or `null`.
- `TextPromptWindow` + VM: port `TextPromptForm` (`PromptForms.cs`): title,
  caption, value, hint, max length (`SaveManager.MaxNoteLength`), verb.
  Result: `string?`.

Tests: VM validation (invalid name disables Create, existing name shows
`ProfileExists`, rename mode allows the same name with different case if
WinForms allows it — check `NewProfileForm`).

Commit: `New playthrough and note windows`

---

## G10 — History

`HistoryWindow` + `HistoryViewModel`: port `HistoryForm.cs` whole —
sections "my points", "automatic copies", "copies from earlier versions"
(only for copies without owner, with `ConfirmLegacyHeading` /
`ConfirmLegacyText`), save a point (name box, `IsValidSavestateName`,
`InvalidSavestateName`), "Go back here", delete point with confirmation,
keep-copies text with the Change link to Settings, its own 2-second game
check (`HistoryGameRunning` with the game name), status line, `Changed`
flag returned to the caller so the main window refreshes. `focusSave`
puts the focus on the point name box.

Commit: `History window`

---

## G11 — Settings and first start

- `SettingsWindow` + VM: port `SettingsForm` (`PromptForms.cs`): launch
  game, save on exit, shared options, copies to keep (with the
  `ConfirmPrune` confirmation when lowering), data folder with Open, about
  line (account, version). **New**: under shared options, a list of shared
  files (`manager.SharedFiles`) with Add (file picker starting in
  `manager.SavePath`; store the path relative to `SavePath` with `/`;
  refuse files outside it) and Remove. Saved with
  `SaveSettings(..., sharedFiles)`.
  New texts: `SharedFilesCaption` (`FILES KEPT THE SAME` / `FILE UGUALI IN
  TUTTE LE PARTITE`), `AddSharedFile` (`Add file...` / `Aggiungi file...`),
  `RemoveSharedFile` (`Remove` / `Rimuovi`), `SharedFileOutside` (`Choose a
  file inside the game's save folder.` / `Scegli un file dentro la cartella
  dei salvataggi del gioco.`).
- `FirstRunWindow` + VM: port `FirstRunForm`, texts with the game name.

Commit: `Settings and first start`

---

## G12 — Accounts and deleted playthroughs

- `AccountWindow` + VM: port `AccountForm.cs` (list with name and last
  save, Use this account, Exit).
- `DeletedWindow` + VM: port `DeletedForm` and the parts of `ListDialog`
  it uses (`SnapshotForms.cs`): list, Recover, Open folder, its own game
  check, `Changed` flag.

Commit: `Account and deleted playthroughs windows`

---

## G13 — 👤 Full manual check (Andrix)

Back up `%LOCALAPPDATA%\LSS`. Run `dotnet run --project src/Lss.App -c
Release` and go through: games list (Control and Terraria supported,
others in "Other games" with the reason), every card action, new/import/
export/delete/recover, history (save a point, go back, delete), settings
(shared files), Steam Cloud bar and help, start a game from "Play this",
close it and check play time and save on exit. Check the Italian texts
(Windows in Italian) and look for `!Key` texts. Note problems in
`PROGRESS.md`; the agent fixes them as extra tasks `G13.n`.

**For the agent, on every `G13.n` that changes how a window looks:** after
the fix run `tools/UiScreenshots` (see `docs/DEVELOPMENT.md` → "Pictures of
the windows") into a folder outside the repository, open the PNG of the
window you changed with the Read tool and check it. Use the shared styles
of `src/Lss.App/Styles/Theme.axaml` (`primary`, `secondary`, `danger`,
`link`, `more`) for every button: no button without a class, no fixed
`Width`/`Height` on buttons. If you add a window, add it to
`tools/UiScreenshots/Program.cs` too.

---

## G14 — Remove WinForms, `Lss.App` becomes `LSS`

Only after G13 is ticked.

1. `git rm` the WinForms files at the root: `AccountForm.cs`,
   `HistoryForm.cs`, `MainForm.cs`, `MessageDialog.cs`,
   `NewProfileForm.cs`, `PlaythroughControls.cs`, `Program.cs`,
   `PromptForms.cs`, `SnapshotForms.cs`, `Theme.cs`, `ThemedControls.cs`,
   `LSS.csproj`, `Properties/PublishProfiles/win-x64.pubxml`. Move
   `app.ico` to `src/Lss.App/Assets/app.ico` (it's already copied: delete
   the root one).
2. `dotnet sln LSS.slnx remove LSS.csproj`.
3. In `src/Lss.App/Lss.App.csproj`: `<AssemblyName>LSS</AssemblyName>`,
   remove the comment about G14, `ApplicationIcon` →
   `Assets\app.ico`, and move the version there (it's the only `<Version>`
   now). In `Lss.Core.csproj` the `InternalsVisibleTo Include="LSS"` now
   points to the Avalonia app: keep it, remove `Lss.App`.
4. Create `src/Lss.App/Properties/PublishProfiles/win-x64.pubxml` and
   `linux-x64.pubxml` from the old Windows one (same properties;
   `RuntimeIdentifier` and `PublishDir` `bin\publish\<rid>\` relative to
   the repository root: `..\..\bin\publish\<rid>\`).
5. Delete the key `ZipFilter` from both resx files (unused now; check with
   Grep).
6. Delete `spike/` and its line in `DefaultItemExcludes` (there is no root
   csproj any more, so nothing to change there).
7. `.vscode/launch.json` and `tasks.json`: program
   `${workspaceFolder}/src/Lss.App/bin/Debug/net10.0/LSS.dll`, build
   `${workspaceFolder}/LSS.slnx`.
8. `scripts/release.ps1`: `$project` = `src\Lss.App\Lss.App.csproj`,
   `dotnet publish $project -p:PublishProfile=win-x64`.
9. Workflow `build.yml`: same paths (`src/Lss.App/Lss.App.csproj`, version
   read from it).

Check: build, tests, `powershell -ExecutionPolicy Bypass -File
scripts/release.ps1 -AllowDirty -SkipTests` creates
`dist/LSS-<version>-win-x64.zip` with `LSS.exe` and **no `.pdb`** files
next to it.

Commit: `Avalonia replaces WinForms`
