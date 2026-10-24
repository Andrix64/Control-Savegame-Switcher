# Phase F — New data folder and migration

Goal: data goes to `<LocalApplicationData>/LSS/<AccountID>/<AppID>/`. On
first start, the data of Control Save Manager is moved there without
losing anything. Export files carry the game.

New layout:

```
<data>/                          %LOCALAPPDATA%\LSS  or  ~/.local/share/LSS
  lss.ini                        UltimoAccount=..., UltimoGioco=...
  log.txt
  <AccountID>/
    <AppID>/
      stato.ini
      Profili/  Info/  Backup/  Savestate/
```

Old layout (Control Save Manager, `%LOCALAPPDATA%\ControlSaveManager`):

```
<old>/
  account.txt                    id of the last account
  log.txt, log.old.txt
  <AccountID>/
    stato.ini
    Profili/  Info/  Backup/  Savestate/
```

---

## F1 — `AppSettings` (`lss.ini`)

Create `src/Lss.Core/AppSettings.cs`:

```csharp
namespace Lss;

/// <summary>
/// Impostazioni generali dell'app in lss.ini nella cartella dei dati:
/// ultimo account e ultimo gioco usati. Non lancia eccezioni: sono solo
/// comodità.
/// </summary>
internal sealed class AppSettings
{
    private readonly string path;

    public string LastAccount { get; set; } = "";
    public string LastGame { get; set; } = "";

    public AppSettings(string path) { this.path = path; }

    public void Load() { ... }   // IniFile.Read; keys UltimoAccount, UltimoGioco
    public void Save() { ... }   // IniFile.Write; catch everything and Log.Write
}
```

Read `IniFile.cs` first and use `IniFile.Read` / `IniFile.Write` exactly as
`SaveManager.Playthroughs.cs` does for `Info\*.ini`.

Tests `AppSettingsTests`: round trip; missing file → empty values; broken
file (binary garbage) → empty values, no exception.

Commit: `AppSettings in lss.ini`

---

## F2 — Opening a game: `SaveManager.Open`

Add to `SaveManager.cs`:

```csharp
/// <summary>
/// Manager per un gioco di un account: dati in dataRoot/IDAccount/AppID.
/// </summary>
public static Result<SaveManager> Open(string dataRoot, SteamAccount account, GameInfo game)
```

- `game.Support != SupportState.Supported` → `Fail(Loc.F(Strings.GameNotSupported, game.Name))`.
- Otherwise, inside try/catch like `Initialize`:
  `new SaveManager(game.SaveFolder, Path.Combine(dataRoot, account.Id, game.AppId), account.Name, game)`.

New text:

| Key | English | Italian |
|---|---|---|
| `GameNotSupported` | `{0} doesn't keep its saves in Steam's userdata folder: LSS can't manage it.` | `{0} non tiene i salvataggi nella cartella userdata di Steam: LSS non può gestirlo.` |

Tests: `Open_UsesAccountAndGameFolder` (check `ProfilesPath ==
<dataRoot>/<id>/<appid>/Profili`), `Open_RefusesUnsupportedGame`.

Commit: `SaveManager.Open for any game`

---

## F3 — Migration from Control Save Manager

### F3.1 Paths in `AppData`

In `src/Lss.Core/AppData.cs`:

1. `DefaultRoot` becomes `Path.Combine(LocalApplicationData, "LSS")`.
2. Add
   `public static string ControlSaveManagerRoot { get; } = Path.Combine(LocalApplicationData, "ControlSaveManager");`
3. Keep `OldRoot` (`Documents\ControlSaves`) as it is.
4. Update the class XML comment (Italian): data in `LSS`; before, Control
   Save Manager used `ControlSaveManager` and even earlier
   `Documenti\ControlSaves`.

### F3.2 New method

```csharp
/// <summary>
/// Porta i dati di Control Save Manager in LSS: prima
/// Documenti\ControlSaves → ControlSaveManager (vecchio spostamento), poi
/// ControlSaveManager → LSS, poi i dati di ogni account nella
/// sottocartella di Control (870780). Da chiamare all'avvio, prima di
/// scrivere nel registro.
/// </summary>
public static (MoveResult Result, string Detail) MigrateToLss()
```

Steps:

1. `(r1, d1) = Move(OldRoot, ControlSaveManagerRoot)`. If `r1 == Failed`,
   set `Root = OldRoot` and return `(Failed, d1)`: nothing else is touched.
2. `(r2, d2) = Move(ControlSaveManagerRoot, DefaultRoot)`.
   - `Failed` → `Root = ControlSaveManagerRoot`, return `(Failed, d2)`.
   - `BothExist` → keep `Root = DefaultRoot`, return `(BothExist, d2)`
     (the old folder stays; the user is told once, see F4).
3. `RestructureAccounts(Root)` (below) — **always**, also when r2 is
   `NothingToMove`: it completes an interrupted earlier run.
4. Return `r2 == Moved || r1 == Moved ? Moved : r2` with the details joined.

Read `Move(oldRoot, newRoot)` first: it already moves on the same drive
with `Directory.Move` and across drives with copy + verify into
`<new>.spostamento`. Don't change it, only the path it receives. The
temporary name in the comments and docs becomes `LSS.spostamento` because
it is `newRoot + ".spostamento"`: check that this is how `Move` builds it;
if it hard-codes `ControlSaveManager.spostamento`, change it to
`newRoot + ".spostamento"`.

```csharp
/// <summary>
/// Sposta Profili, Info, Backup, Savestate e stato.ini di ogni account in
/// IDAccount\870780. Si può ripetere: salta quello che è già a posto.
/// </summary>
internal static void RestructureAccounts(string root)
```

For each subfolder of `root` whose name is all digits (an account):

- `target = Path.Combine(account, GameInfo.ControlAppId)`;
- for each name in `["Profili", "Info", "Backup", "Savestate"]`: if
  `account/name` exists as a folder:
  - if `target/name` doesn't exist: `Directory.CreateDirectory(target)` and
    `Directory.Move(account/name, target/name)`; log
    `"Spostato " + name + " in " + target`;
  - if it exists: don't touch either; log
    `"Non spostato, esiste già: " + target/name`.
- same for the file `stato.ini` with `File.Move` (no overwrite).
- Each item in its own try/catch with `Log.Write(context, ex)`: one failure
  doesn't stop the others.

`account.txt` in `root`: if it exists and `lss.ini` doesn't, write
`lss.ini` with `UltimoAccount = <content trimmed>` and
`UltimoGioco = 870780` (use `AppSettings`), then delete `account.txt`.

### F3.3 Startup

`src/Lss.Core/AppData.cs` `MoveFromDocuments()` stays for now (WinForms
calls it). In `Program.cs` (WinForms) replace the call to
`AppData.MoveFromDocuments()` with `AppData.MigrateToLss()`; the result
type is the same. `Program.ReportDataMove` builds its messages with
`AppData.OldRoot` as the source folder: add to `AppData`
`public static string LastMoveSource { get; private set; } = "";`, set it
in `MigrateToLss` to the root that was actually moved
(`ControlSaveManagerRoot` if it existed, otherwise `OldRoot`), and use
`AppData.LastMoveSource` instead of `AppData.OldRoot` in
`ReportDataMove`. Then delete `MoveFromDocuments()` if nothing else uses
it.

`SaveManager.Initialize` (used by WinForms) must now use
`Path.Combine(appData, account.Id, GameInfo.ControlAppId)` as data folder
and `AppSettings` instead of `account.txt` (`ReadLastAccount` /
`WriteLastAccount` become `AppSettings.LastAccount` + `Save()`). Delete
`ReadLastAccount` and `WriteLastAccount`.

### F3.4 Single instance

In `WindowsPlatform.AcquireSingleInstance()` take **two** mutexes, both
must be free: `Local\ControlSaveManager` (blocks an old Control Save
Manager still installed) and `Local\LSS`. Return an object that disposes
both (a small private class `InstanceLock : IDisposable` holding a list).
If the second fails, dispose the first and return `null`. Update the
comment.

### F3.5 Tests (`AppDataTests.cs`)

The existing tests for `Move` must still pass. Add, using temporary roots
(the methods must therefore take the roots as parameters: split
`MigrateToLss()` into a public no-argument wrapper and an
`internal static (MoveResult, string) MigrateToLss(string documentsRoot, string csmRoot, string lssRoot)`
that does the work; the wrapper sets `Root` from the result):

- `Migrate_MovesControlSaveManagerAndRestructures`: csm root with
  `account.txt` ("111"), `111/stato.ini`, `111/Profili/Profilo_Mario/remote/a.sav`,
  `111/Backup/x/a`, `log.txt` → lss root has `111/870780/stato.ini`,
  `111/870780/Profili/Profilo_Mario/remote/a.sav`, `111/870780/Backup/x/a`,
  `lss.ini` with `UltimoAccount=111`, no `account.txt`, no csm root.
- `Migrate_FromDocumentsThroughToLss`: only the documents root has data →
  ends in lss root, restructured.
- `Migrate_BothExist_TouchesNothing`: csm root and lss root both with data
  → both unchanged, result `BothExist`.
- `Restructure_IsRepeatable`: run `RestructureAccounts` twice → same
  result, no exception; with `111/870780/Profili` already present and
  `111/Profili` too, both stay and nothing is lost.
- `Restructure_SkipsNonAccountFolders`: a folder `logs` or `abc` at the
  root is not touched.

Commit: `Data moves to LSS, one folder per game`

---

## F4 — 👤 Migration with real data (Andrix)

Back up `%LOCALAPPDATA%\ControlSaveManager` (copy it somewhere else). Start
the WinForms app: the outcome message says the data was moved;
`%LOCALAPPDATA%\LSS\<account>\870780\` has the playthroughs with notes,
play time and saved points; `log.txt` lists every step.

---

## F5 — Export format 2

In `SaveManager.Playthroughs.cs`:

1. `ExportProfileCore`: `new("Formato", "2")`, and add
   `new("AppId", Game.AppId)`, `new("Gioco", Game.Name)` after `Formato`.
2. `ImportProfileCore`, after reading `values`:

   ```csharp
   // Le partite esportate da Control Save Manager (formato 1) non hanno AppId: sono di Control.
   string appId = values.GetValueOrDefault("AppId", GameInfo.ControlAppId);
   string gameName = values.GetValueOrDefault("Gioco", "Control");

   if (appId != Game.AppId)
       return Result<string>.Fail(Loc.F(Strings.ImportOtherGame, gameName, Game.Name));
   ```

3. Zip entries must use `/` on every system (the same zip must import on
   Windows and Linux). After `ZipFile.CreateFromDirectory`, the existing
   check opens the zip: also check
   `archive.Entries.All(e => !e.FullName.Contains('\\'))`, throw
   `IOException("Percorsi con \\ nello zip.")` otherwise.
4. Temporary folder names `csm_esporta_` / `csm_importa_` → `lss_esporta_`
   / `lss_importa_`.

New text:

| Key | English | Italian |
|---|---|---|
| `ImportOtherGame` | `This playthrough is from {0}, not from {1}.` | `Questa partita è di {0}, non di {1}.` |

Update `ZipFilter` users if any test checks it (none should).

Tests (`PlaythroughTests.cs`, export/import section):

- existing round trip still passes;
- `Export_WritesFormat2WithGame`: open the zip, read `partita.ini`,
  check `Formato=2`, `AppId=870780`, `Gioco=Control`;
- `Import_Format1IsControl`: build a zip by hand with `Formato=1` and no
  `AppId` → import into a Control manager works; into a `123450` manager
  fails with the `ImportOtherGame` text (Italian: "Questa partita è di
  Control, non di Test.");
- `Import_OtherGameRefused`: zip exported from a `123450` manager imported
  into a Control manager fails;
- `Export_UsesForwardSlashes`: every entry `FullName` has no `\`.

In `docs/DEVELOPMENT.md` section "Exported playthrough": document
`Formato=2`, `AppId`, `Gioco`, and that format 1 is read as Control.

Commit: `Export format 2 with the game`

---

## F6 — Docs for the data folder

Update `docs/DEVELOPMENT.md` (English): the "Paths" table (data in
`%LOCALAPPDATA%\LSS` / `~/.local/share/LSS`, one folder per account and
game, `lss.ini`), the migration paragraph (Documents → ControlSaveManager →
LSS, restructure per account, repeatable), the single-instance mutexes.

Commit: `Docs: data folder and migration`
