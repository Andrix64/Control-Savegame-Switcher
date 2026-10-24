# Phase E — `SaveManager` for any game

Goal: `SaveManager` works on any `GameInfo`, not only Control. The WinForms
app still opens Control only (it's replaced in phase G), so its behaviour
doesn't change; but nothing in `Lss.Core` may contain `870780`, `Control.exe`
or `preferences_data` any more, except in `GameInfo.ControlAppId` and in
`GameDefaults`.

Files are in `src/Lss.Core/` and `src/Lss.Platform/` (after phase C). Line
numbers have moved: find methods by name.

---

## E1 — `SaveManager` gets a `GameInfo`

1. In `GameInfo.cs` add:

   ```csharp
   /// <summary>
   /// Control con la cartella dei salvataggi data: usato finché l'interfaccia
   /// WinForms apre solo Control, e dai test.
   /// </summary>
   public static GameInfo Control(string saveFolder) =>
       new(ControlAppId, "Control", "", "", saveFolder, SupportState.Supported);
   ```

2. In `SaveManager.cs`:
   - remove `private const string AppId = "870780";`;
   - add `public GameInfo Game { get; }` next to `SavePath`;
   - change the constructor to
     `internal SaveManager(string savePath, string basePath, string accountName = "", GameInfo? game = null)`
     and set `Game = game ?? GameInfo.Control(savePath);` as first line.
3. Replace each use of `AppId` in **instance** methods with `Game.AppId`
   (`GetCloudState`, `LaunchGame` after E3).
4. Static methods that used `AppId`: give them a parameter
   `string appId` and pass `GameInfo.ControlAppId` from `Initialize`:
   - `internal static void RecoverGameFolders(string userData, string appId)`
   - `internal static IReadOnlyList<SteamAccount> FindAccounts(string userData, string appId)`
   In `Initialize`, `new SaveManager(...)` gets
   `game: GameInfo.Control(Path.Combine(account.Folder, GameInfo.ControlAppId))`.
5. Update the tests that call `FindAccounts(userData)` and
   `RecoverGameFolders(userData)` (`RecoveryTests.cs`,
   `StateAndAccountTests.cs`): add `, "870780"`.

Check: build, tests.

Commit: `SaveManager knows its game`

---

## E2 — Texts with the game name

Every user text that says "Control" now takes the game name. Edit **both**
`src/Lss.Core/Strings.resx` and `src/Lss.Core/Strings.it.resx`, value by
value, exactly as in the table. Keep XML escapes (`&apos;`) as they are in
the files.

| Key | New English | New Italian |
|---|---|---|
| `CloseControlFirst` → rename key to `CloseGameFirst` | `Close {0} before continuing.` | `Chiudi {0} prima di continuare.` |
| `GameRunning` | `{0} is running: close it to switch or save.` | `{0} è in esecuzione: chiudilo per cambiare o salvare.` |
| `GameLaunching` | `Starting {0}...` | `Avvio di {0}...` |
| `CloudBarText` | `When the game starts, Steam might put back saves from another playthrough. Turn it off for {0}.` | `All&apos;avvio del gioco Steam potrebbe rimettere salvataggi di un&apos;altra partita. Disattivalo per {0}.` |
| `CloudBarTitle` | `Steam Cloud is on for {0}` | `Steam Cloud è attivo per {0}` |
| `CloudHelpHeading` | `Turn off Steam Cloud for {0}` | `Disattivare Steam Cloud per {0}` |
| `CloudHelpText` | replace `Control` with `{0}` in the line "Right-click Control..." | same in Italian |
| `CloudConfirmOn` | `Steam Cloud is on for {0}: when the game starts, Steam might put back saves from another playthrough. Turn it off in the game&apos;s properties.` | `Steam Cloud è attivo per {0}: all&apos;avvio del gioco Steam potrebbe rimettere salvataggi di un&apos;altra partita. Disattivalo nelle proprietà del gioco.` |
| `CloudConfirmMaybe` | `We can&apos;t tell whether Steam Cloud is on for {0}. If it is, Steam might put back saves from another playthrough.` | `Non sappiamo se Steam Cloud è attivo per {0}. Se lo è, Steam potrebbe rimettere salvataggi di un&apos;altra partita.` |
| `FirstRunCloudOff` | `Steam Cloud is off for {0}: good` | `Steam Cloud disattivato per {0}: va bene così` |
| `FirstRunCloudOn` | `Steam Cloud is on for {0}: better turn it off (you&apos;ll find how in the main window)` | `Steam Cloud è attivo per {0}: meglio disattivarlo (trovi come nella finestra principale)` |
| `FirstRunCloudUnknown` | `We can&apos;t tell whether Steam Cloud is on for {0}` | `Non sappiamo se Steam Cloud è attivo per {0}` |
| `FirstRunTitle` | `Several {0} playthroughs, one at a time in the game` | `Più partite di {0}, una alla volta nel gioco` |
| `GameBarExit` | `To switch playthrough or save a point, quit {0}: the app notices by itself.` | `Per cambiare partita o salvare un punto, esci da {0}: il programma se ne accorge da solo.` |
| `GameBarOpen` | `{0} is running` | `{0} è aperto` |
| `HistoryGameRunning` | `Quit {0} to save a point or go back.` | `Esci da {0} per salvare un punto o tornare indietro.` |
| `SettingLaunch` | `“Play this” also starts {0}` | `«Gioca questa» avvia anche {0}` |
| `SettingSaveOnExit` | `Save progress when you close {0}` | `Salva i progressi quando chiudi {0}` |
| `StartingWith` | `Starting {0} with “{1}”...` | `Avvio {0} con «{1}»...` |
| `ZipFilter` | `{0} playthrough (*.zip)|*.zip` | `Partita di {0} (*.zip)|*.zip` |
| `LaunchFailed` | `We couldn&apos;t start {0} through Steam: {1}` | `Non siamo riusciti ad avviare {0} tramite Steam: {1}` |
| `SavesNotFound` | `{0} saves not found.` | `Salvataggi di {0} non trovati.` |

**Don't change** `SettingSharePreferencesHint` ("Controls, audio..." means
game controls, not the game Control) and `ImportInvalid`.

Then search both files for any remaining `Control` (excluding
`Control Save Manager`, `Controls` and `Comandi`):

```bash
grep -n "Control" src/Lss.Core/Strings.resx src/Lss.Core/Strings.it.resx
```

If another value still names the game, change it the same way and add it
to the Notes in `PROGRESS.md`.

Update **every call site** (use Grep for each key in `*.cs`): a text that
was used directly (`Strings.GameRunning`) becomes
`Loc.F(Strings.GameRunning, <game name>)`; one that was already in
`Loc.F` gets the game name as first argument and the old arguments after
it. The game name is:

- in `SaveManager`: `Game.Name`;
- in the WinForms forms: `manager.Game.Name` (or `Manager.Game.Name` in
  `ListDialog` subclasses); `SavesNotFound` in `Initialize`: `"Control"`
  (only Control there).

`CloseControlFirst` is renamed: update its uses to `CloseGameFirst`.

Tests: `LocalizationTests` must pass. Fix tests that compare texts (search
tests for "Control" in Italian strings, e.g. `"Chiudi Control"`): the
expected text stays the same because the game is still Control.

Commit: `Texts take the game name`

---

## E3 — `LaunchGame`, `IsGameRunning`, `GameStartTime` per game

### E3.1 Interface

Change `IPlatform` (in `src/Lss.Core/IPlatform.cs`):

```csharp
/// <summary>true se il gioco è aperto (nel dubbio true).</summary>
bool IsGameRunning(GameInfo game);

/// <summary>Quando è partito il gioco aperto, se si riesce a leggerlo.</summary>
DateTime? GameStartTime(GameInfo game);
```

Update `FakePlatform`: keep `GameRunning`/`StartTime` properties, ignore the
parameter.

### E3.2 `SaveManager`

Replace the static methods with instance methods:

```csharp
public bool IsGameRunning() => Platform.Current.IsGameRunning(Game);

public DateTime? GameStartTime() => Platform.Current.GameStartTime(Game);

public Result LaunchGame() { ... }   // was static; uses Game.AppId and Game.Name
```

`LaunchGame` body: `Platform.Current.OpenSteamUri("steam://rungameid/" + Game.AppId);`,
log `"Avvio di " + Game.Name + " tramite Steam"`, return
`Result.Ok(Loc.F(Strings.GameLaunching, Game.Name))`; on exception
`Loc.F(Strings.LaunchFailed, Game.Name, ex.Message)`.

Every `operation` in `SaveManager` that checks the game (`IsGameRunning()`
inside `ActivateProfileCore`, `SaveProgressCore`, etc.: Grep
`IsGameRunning`) now calls the instance method: nothing to change except
removing `SaveManager.` if it was written.

### E3.3 WinForms call sites

- `MainForm.cs`: `Task.Run(SaveManager.IsGameRunning)` →
  `Task.Run(manager.IsGameRunning)`; `Task.Run(SaveManager.GameStartTime)`
  → `Task.Run(manager.GameStartTime)`; `SaveManager.LaunchGame()` →
  `manager.LaunchGame()`.
- `HistoryForm.cs`: `Task.Run(SaveManager.IsGameRunning)` → use its
  manager field (read the file to find its name).
- `SnapshotForms.cs`: `Task.Run(SaveManager.IsGameRunning)` →
  `Task.Run(Manager.IsGameRunning)`.

### E3.4 `WindowsPlatform`

Remove the Control-specific code (`IsGameLauncherRunning`,
`IsGameLauncher`, the `Control_DX12`/`Control_DX11` names) and write:

```csharp
// Percorso dell'eseguibile per ogni processo già visto: leggerlo è lento e
// il controllo gira ogni 2 secondi. null = percorso non leggibile.
private readonly Dictionary<int, string?> processPaths = [];
private readonly object processLock = new();

public bool IsGameRunning(GameInfo game) =>
    SteamSaysRunning(game.AppId) || FindGameProcesses(game).Count > 0;
```

`SteamSaysRunning(string appId)`: open `HKCU\Software\Valve\Steam`; true
if value `RunningAppID` (DWORD) as string equals `appId`, or if subkey
`Apps\<appId>` has DWORD `Running` == 1. Any exception → `false`.

`FindGameProcesses(GameInfo game)` → `List<(int Id, DateTime? Start)>`:

1. If `game.InstallPath == ""` return empty list.
2. `prefix = Path.GetFullPath(game.InstallPath).TrimEnd('\\') + "\\"`.
3. `Process[] all = Process.GetProcesses();` For each process (always
   `Dispose` it in `finally`):
   - path: `processPaths.TryGetValue(p.Id, out path)`; if not cached, try
     `p.MainModule?.FileName`, catch any exception → `null`, store in the
     cache;
   - if `path != null && path.StartsWith(prefix, OrdinalIgnoreCase)`, add
     `(p.Id, TryStartTime(p))` where `TryStartTime` returns `p.StartTime`
     or `null` on exception.
4. Remove from the cache the ids that are no longer in `all`.
5. Guard the cache with `lock (processLock)`.

`GameStartTime(GameInfo game)`: the earliest non-null `Start` of
`FindGameProcesses(game)`, or `null`.

Processes whose path can't be read are **ignored** (before, for
`Control.exe`, they counted as the game running). This is safe because
Steam's `Running` value covers games started as administrator. Write this
in the XML comment of `IsGameRunning`.

### E3.5 Tests

- Delete the test `GameLauncherIsRecognized` from `MinorBugTests.cs`
  (the method it tested is gone).
- In `PlatformTests.cs` change `SaveManager.LaunchGame()` to
  `folder.CreateManager().LaunchGame()` (add a `TestFolder`) and assert
  the URI `steam://rungameid/870780`.
- Add `LaunchGame_UsesTheGameAppId`: manager created with
  `new SaveManager(folder.Game, folder.Data, "", new GameInfo("123450", "Test", "", "", folder.Game, SupportState.Supported))`
  → `OpenedUris` contains `steam://rungameid/123450` and the result
  message contains `Test`.
- Add `SaveManagerTests.ActivateProfile_RefusedWhileGameRunning`? **No**:
  `FakePlatform.GameRunning` is shared by parallel tests. Don't write
  tests that change it.

Check: build, tests. Then `grep -rn "870780\|Control_DX\|\"Control\"" src/`
→ only `GameInfo.cs` (and `SteamLibrary.KnownNames`).

Commit: `Running game and launch work for any game`

---

## E4 — Shared files per game

### E4.1 Defaults

Create `src/Lss.Core/GameDefaults.cs`:

```csharp
namespace Lss;

/// <summary>
/// Impostazioni consigliate per i giochi noti.
/// </summary>
internal static class GameDefaults
{
    /// <summary>
    /// File delle opzioni del gioco (comandi, audio...) da tenere uguali in
    /// tutte le partite, relativi alla cartella dei salvataggi, con "/".
    /// </summary>
    public static IReadOnlyList<string> SharedFiles(string appId) => appId switch
    {
        GameInfo.ControlAppId => ["remote/preferences_data"],
        _ => []
    };
}
```

### E4.2 `AppState`

Add:

```csharp
/// <summary>
/// File da tenere uguali in tutte le partite (FileCondivisi in stato.ini),
/// o null se non è mai stato impostato: allora valgono quelli di GameDefaults.
/// </summary>
public IReadOnlyList<string>? SharedFiles { get; set; }
```

- `Load()`: reset to `null` with the other values; new `case "FileCondivisi":`
  → `SharedFiles = value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);`
  (`value` is already `""` when the file says `-`).
- `Save()`: only if `SharedFiles != null`, write
  `"FileCondivisi=" + Format(string.Join("|", SharedFiles))`.

### E4.3 `SaveManager`

1. Remove `PreferencesFile`.
2. Add

   ```csharp
   /// <summary>File da tenere uguali in tutte le partite, relativi a SavePath.</summary>
   public IReadOnlyList<string> SharedFiles => State.SharedFiles ?? GameDefaults.SharedFiles(Game.AppId);
   ```

3. `CopyPreferencesTo(string profile)`: loop over `SharedFiles`; for each
   `relative`, `source = Path.Combine(SavePath, ToLocal(relative))`, same
   body as now for one file. `ToLocal(string relative) =>
   relative.Replace('/', Path.DirectorySeparatorChar)`.
4. `ReadPreferences()` returns `Dictionary<string, byte[]>` (relative path →
   content) with the files that exist; `WritePreferences(Dictionary<string, byte[]>)`
   writes them all. In `RestoreToGame` (where `ReadPreferences()` is
   called) change the type of the local variable: `null` when
   `State.SharePreferences` is false, as now.
5. Reject unsafe paths: a relative path is used only if
   `IsInside(Path.Combine(SavePath, ToLocal(relative)), SavePath)` (method
   already in `SaveManager.cs`). Skip others with a log line
   `"File condiviso fuori dalla cartella del gioco: " + relative`.
6. `SaveSettings(...)` gets a new last parameter
   `IReadOnlyList<string>? sharedFiles = null`; if not null, set
   `State.SharedFiles = sharedFiles`. The WinForms Settings window doesn't
   pass it.

### E4.4 Tests

In `PlaythroughTests.cs` the existing shared-options tests use
`remote/preferences_data`: they must still pass unchanged (Control
default). Add:

- `SharedFiles_DefaultForControl` → `["remote/preferences_data"]`;
- `SharedFiles_EmptyForOtherGames` (manager with AppID `123450`);
- `SharedFiles_FromStateWinOverDefaults`: set `State.SharedFiles =
  ["remote/a.cfg"]`, `State.Save()`, `ReloadState()` → `["remote/a.cfg"]`;
- `SharedFiles_SeveralFilesKeptAcrossActivation`: game `123450`, shared
  files `["remote/a.cfg", "b.ini"]`, `SharePreferences = true`, two
  profiles; change both files in the game, activate the other profile →
  the game has the changed files;
- `SharedFiles_PathOutsideGameIsIgnored`: `["../evil.txt"]` → no file
  written outside `folder.Game`.

Commit: `Shared files chosen per game`

---

## E5 — Accounts and recovery for every game

Prepare the API that phase F and G need.

1. In `SaveManager.cs` add

   ```csharp
   /// <summary>
   /// Account in userdata che hanno almeno un gioco con salvataggi in
   /// userdata (SupportState.Supported), dal più usato di recente.
   /// </summary>
   internal static IReadOnlyList<SteamAccount> FindAccountsWithGames(string userData)
   ```

   An account folder is a subfolder of `userData` whose name is all digits.
   It counts if at least one of its numeric subfolders not in
   `SteamLibrary`'s system list has `SteamLibrary.GetSupport(dir) ==
   SupportState.Supported`. `LastSaved` is the newest write time of any
   file in those supported game folders, excluding top-level
   `remotecache.vdf`. Make `SteamLibrary.SystemApps` `internal` so it can
   be used here. Name: `ReadPersonaName(dir)` as in `FindAccounts`.
2. Add `internal static void RecoverAllGameFolders(string userData)`: for
   every account folder and every subfolder whose name is
   `<digits>.vecchio` or `<digits>.nuovo`, call
   `FileOps.RecoverInterruptedReplace(<account>/<digits>)` once per
   target (use a `HashSet` of targets), with the same logging and
   try/catch as `RecoverGameFolders`.
3. Tests in `RecoveryTests.cs` / `StateAndAccountTests.cs`:
   - `FindAccountsWithGames_SkipsAccountsWithoutSupportedGames`
     (account A with `105600/remote/x`, account B with only
     `7/remote/sharedconfig.vdf` and `814380/remotecache.vdf` → only A);
   - `RecoverAllGameFolders_RestoresAnyGame` (account with
     `105600.vecchio` and no `105600` → `105600` is back), like the
     existing Control test.

Commit: `Accounts and recovery for every game`

---

## E6 — 👤 Check with Control (Andrix)

Run the WinForms app with real data (after a backup of
`%LOCALAPPDATA%\ControlSaveManager`): start Control from "Play this", check
that the app sees it running (game bar) and notices when it closes. This
checks the new registry/process detection.
