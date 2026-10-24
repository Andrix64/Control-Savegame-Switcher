# Development

Technical notes for people working on the code. The user guide is in the
[README](../README.md).

## Paths

| What | Path |
|---|---|
| Game saves | `<Steam>\userdata\<AccountID>\870780\` |
| App data | `%LOCALAPPDATA%\LSS\` (`AppData.Root`); on Linux `~/.local/share/LSS/` |
| Playthroughs | `<data>\<AccountID>\<AppID>\Profili\Profilo_<name>\` |
| Note and play time | `<data>\<AccountID>\<AppID>\Info\Profilo_<name>.ini` |
| Automatic copies | `<data>\<AccountID>\<AppID>\Backup\<reason>_<date>\` plus `<reason>_<date>.ini` next to it |
| Saved points | `<data>\<AccountID>\<AppID>\Savestate\Profilo_<name>\<date>_<point name>\` |
| State | `<data>\<AccountID>\<AppID>\stato.ini` |
| Log | `<data>\log.txt` (above 1 MB it becomes `log.old.txt`) |
| Settings (last account and game) | `<data>\lss.ini`: `UltimoAccount=`, `UltimoGioco=` |
| Steam Cloud setting | `<Steam>\userdata\<AccountID>\7\remote\sharedconfig.vdf` (read only) |

Data lives in `%LOCALAPPDATA%` rather than Documents because Documents is often
synced with OneDrive, which locks files while uploading them, keeps them only
in the cloud ("files on demand") and creates "conflict" copies: all things
that make copies fail or mix up playthroughs in a save folder. `Local` rather
than `Roaming` because backups can be large.

Data of Control Save Manager is moved here at startup: see
[Compatibility with Control Save Manager](#compatibility-with-control-save-manager).

Single instance: `WindowsPlatform.AcquireSingleInstance` takes the named
mutex `Local\LSS` and, for compatibility, the old one of Control Save
Manager. If either is taken, the app does not start.

## Compatibility with Control Save Manager

LSS started as Control Save Manager (0.9.0, Control only). Everything that
exists only to take over its data is in **`src/Lss.Core/LegacyMigration.cs`**
and its tests in **`tests/Lss.Tests/LegacyMigrationTests.cs`**, so it can be
removed in one go once nobody needs it.

What it does:

- **Data folder.** Control Save Manager kept its data in
  `%LOCALAPPDATA%\ControlSaveManager`, and earlier development versions used
  `Documents\ControlSaves`. At startup `LegacyMigration.Run` moves them to
  `LSS` in two steps: `Documents\ControlSaves` to `ControlSaveManager`, then
  `ControlSaveManager` to `LSS`. Each step goes through
  `LegacyMigration.Move`: on the same drive a single `Directory.Move`; across
  drives a copy into `<destination>.spostamento` (`FileOps.CopyAllVerified`),
  verifying every file, and only then deleting the original. If the
  destination already has data, nothing is moved (`BothExist`).
- **Layout.** `LegacyMigration.RestructureAccounts` moves each account's
  Control data from `<AccountID>\` into `<AccountID>\870780\`, skipping
  whatever is already there, and turns `account.txt` into `lss.ini`. It runs
  at every start, also on the old folder when a move fails (so the app finds
  the playthroughs there), and can be repeated safely.
- **Message.** `LegacyMigration.Notice` writes the outcome to the log and
  returns the text shown once to the user (`DataMoved`, `DataMovedOldLeft`,
  `DataBothExist`, `DataMoveFailed`).
- **Mutex.** `LegacyMigration.MutexName` (`Local\ControlSaveManager`): LSS
  also holds it, so an old Control Save Manager can't run next to it.
- **Exports.** `LegacyMigration.ExportedGame` reads a zip of format 1
  (Control Save Manager, no `AppId`) as Control.

To remove it:

1. Delete `src/Lss.Core/LegacyMigration.cs` and
   `tests/Lss.Tests/LegacyMigrationTests.cs`.
2. Startup (`Program.cs` of the app): remove the call to
   `LegacyMigration.Run()` and the message from `LegacyMigration.Notice`.
   `AppData.Root` stays `AppData.DefaultRoot`.
3. `WindowsPlatform.AcquireSingleInstance`: take only `Local\LSS`; remove
   `InstanceLock` if it holds a single mutex.
4. `SaveManager.ImportProfileCore`: read `AppId` and `Gioco` from
   `partita.ini` directly and refuse zips without `AppId` with
   `Strings.ImportInvalid`. Delete the test `Import_Format1IsControl` in
   `PlaythroughTests.cs`.
5. Texts: delete `DataMoved`, `DataMovedOldLeft`, `DataBothExist`,
   `DataMoveFailed` from both resx files; in `ImportInvalid` remove "or
   Control Save Manager".
6. `FileOps.CopyAllVerified` and `CopyAll`: delete them (and their test in
   `FileOpsTests.cs`) if nothing else uses them.
7. Docs: this section, the "Coming from Control Save Manager" part of the
   README, and the CHANGELOG line about the migration in the next version.
8. `grep -rn "LegacyMigration\|ControlSaveManager\|account.txt" src tests`
   must print nothing.

The Steam folder is read from the registry
(`HKCU\Software\Valve\Steam\SteamPath`), falling back to the standard paths in
`Program Files`.

If more than one Steam account has Control saves, the app asks which one to
use at startup (name read from `userdata\<AccountID>\config\localconfig.vdf`)
and preselects the last one chosen. Each account has its own playthroughs,
copies and saved points.

In the code and on disk the historical names remain: a *profile* is a
playthrough, a *savestate* is a saved point, a *backup* is an automatic copy.
Folder, file and key names on disk are Italian (`Profili`, `stato.ini`,
`ProfiloAttivo`...) and stay so whatever the app's language.

Steam's `remotecache.vdf` file, at the top level of the game folder, is never
copied or deleted. Everything else is copied and verified at any depth: there
are no other exclusions (a file with that name further down is a save like
any other).

## Steam libraries and games

Steam keeps games in the Steam folder and in the other libraries listed in
`steamapps\libraryfolders.vdf`. `SteamLibrary` reads that file (paths are
written with `\\`, which `Vdf` already handles), keeps only the folders that
exist, and looks for each game's `steamapps\appmanifest_<AppID>.acf` in all
of them to get its name, install folder and library. A game without a
manifest keeps its name from a short list (Control) or is shown as `App <AppID>`.

A game is *supported* when its folder `userdata\<AccountID>\<AppID>\` has a file
other than the top-level `remotecache.vdf`. If only `remotecache.vdf` is there,
the game saves elsewhere (Steam Auto-Cloud): *saves elsewhere*. An empty folder
is *no saves*. Supported games come first in the list.

Skipped folders: `7`, `760` and `241100` (Steam settings, screenshots,
controller settings) and any name that is not a number. Manifests of Proton
and of the Steam runtimes are not games.

The images are in `appcache\librarycache`: the 32×32 icon is the `.jpg` named
with 40 hexadecimal characters in `<AppID>\` (or `<AppID>_icon.jpg` in older
versions), the cover is `library_600x900.jpg` (or `<AppID>_library_600x900.jpg`).

## Copy safety

- Every backup and every copy is compared byte by byte with the original.
- Activation and "Save progress" work in two steps: the source is copied to
  `<folder>.nuovo`, verified, and then the folders are swapped (the old one
  goes through `<folder>.vecchio` and is deleted). If the folder is in use and
  can't be moved, it falls back to copying directly from the already verified
  copy. If anything fails, the backup is restored.
- `stato.ini` is written to `stato.ini.tmp` and then replaced.
- Recovery at startup from an interrupted operation (for example a power
  cut): before looking for accounts, for every game folder, and on opening,
  for `Profili` and `Savestate`. If the folder is missing but
  `<folder>.vecchio` exists, that goes back in place (it's the state before
  the operation); if the folder exists, leftover `.nuovo` and `.vecchio`
  folders are deleted. An interrupted case-only rename (`<profile>.rinomina`)
  is completed. A lone `.nuovo` is left alone. Every fix goes to the log.
- An account is recognised if it has the `870780` folder, even without
  `remote` (which is missing after activating a hand-made profile).
- Only one instance of the app at a time (`Mutex`).
- Every operation that touches the game's saves is blocked while Control is
  running, both in the logic (`IsGameRunning` in every operation) and in the
  interface: the main window and History check it every 2 seconds and disable
  the buttons. The launcher is called `Control.exe`, like the Control Panel:
  the one in the Windows folder is ignored, and if the path can't be read
  (process started as administrator) it counts as the game running, to be
  safe.
- At startup the main window reports (and logs) the folders in `Profili`
  ignored because their name is invalid.

## Playthroughs and play sessions

- **Play this** (`MainForm.PlayAsync`): if the game has unsaved progress it
  saves it to the current playthrough, then activates the chosen one and
  starts Control with `steam://rungameid/870780` (`SaveManager.LaunchGame`).
  With `AvviaGioco=0` or "Load without starting" it doesn't start the game and
  offers **Undo**, which does the same steps back to the previous playthrough.
- **Sessions**: the main window checks the game every 2 seconds. When the game
  starts it records the time (the process start time, if readable) and the
  playthrough; when it closes it adds the time with `AddPlayTime` and, with
  `SalvaAllaChiusura=1`, saves the progress (`SaveProgress(onGameExit: true)`).
  Only works while the app is open.
- **Shared options** (`OpzioniCondivise=1`): when activating a playthrough,
  the game's `remote\preferences_data` file is first copied into the
  playthrough; when going back to a point or a copy it's put back afterwards.
- **Copy owner**: every copy has a `.ini` next to it with the playthrough its
  content belongs to, the operation (`BackupKind`) and a detail. A
  playthrough's History shows only its own copies; those without an owner
  (earlier versions, or made with no playthrough in the game) are in a
  separate section and ask for confirmation.
- **Deleted playthroughs**: the folder becomes an `Eliminato_<playthrough>`
  copy with note and play time in its `.ini`; "Recover" puts it back among the
  playthroughs (with a free name if the old one has been reused). Saved points
  are deleted.
- **First start**: if there are no playthroughs but the game has saves,
  `AdoptGameSaves` saves them as the first playthrough and marks it as in the
  game, without touching the game folder.

## Formats

### `stato.ini`

```ini
ProfiloAttivo=Profilo_Mario
AttivatoIl=22/10/2026 18:30:00
SalvatoIl=-
BackupDaConservare=20
AvviaGioco=1
SalvaAllaChiusura=1
OpzioniCondivise=0
```

`-` means an empty value. `BackupDaConservare=0` keeps all copies. Dates are
always written as `dd/MM/yyyy HH:mm:ss` (invariant culture) and shown in the
Windows language's format.

| Key | Meaning |
|---|---|
| `ProfiloAttivo` | Playthrough in the game |
| `AttivatoIl` / `SalvatoIl` | When it was loaded / last saved |
| `BackupDaConservare` | Automatic copies to keep |
| `AvviaGioco` | "Play this" also starts Control |
| `SalvaAllaChiusura` | Save progress when the game closes |
| `OpzioniCondivise` | Same game options for all playthroughs |

### Folder names

- Playthroughs: `Profilo_<name>`, with letters (accented too), digits, spaces,
  `_` and `-`, no leading or trailing spaces, at most 50 characters. Folders in
  `Profili` that don't follow the rule are ignored and reported.
- Copies: `Prima_salvataggio_<playthrough>` holds the playthrough before
  saving, `Eliminato_<playthrough>` a deleted playthrough; the others
  (`Prima_di_<playthrough>`, `Prima_creazione`, `Prima_ripristino`) the game's
  saves. The text shown comes from `SaveManager.BackupDescription`, which uses
  the `.ini` next to the copy or, for copies from earlier versions, the name.

### `.ini` files of a playthrough and of a copy

```ini
; Info\Profilo_Mario.ini
Nota=Polaris mission, before Ahti
TempoDiGioco=45000
UltimaSessione=2026-10-22 22:11:00

; Backup\Prima_di_Profilo_Luigi_2026-10-22_22-11-03-120.ini
Partita=Profilo_Mario
Motivo=attivazione
Dettaglio=Profilo_Luigi
```

`TempoDiGioco` (play time) is in seconds. `Motivo` (reason) is one of
`attivazione` (activation), `salvataggio` (save), `chiusura` (save on game
exit), `ripristino` (restore), `creazione` (creation), `eliminazione`
(deletion).

### Exported playthrough

A zip with `partita.ini` (`Formato=2`, `AppId`, `Gioco`, `Nome`, `Nota`,
`TempoDiGioco`, `UltimaSessione`), `salvataggi\` (the playthrough folder) and,
if any, `punti\` (the saved points). `AppId` is the Steam number of the game
and `Gioco` its name. Import rejects zips without `salvataggi\`, paths that
leave the folder, and playthroughs of another game (`ImportOtherGame`).
Zip entries always use `/`, so the same zip imports on Windows and Linux.

Format 1 (written by Control Save Manager) has no `AppId`: it is read as
Control (`870780`) by `LegacyMigration.ExportedGame`. Format 2 is the only
one written now.

### Steam Cloud

Steam stores the game's "Keep games saves in the Steam Cloud" setting in
`sharedconfig.vdf`, in the block `apps → 870780 → "cloudenabled"`. The app
reads only that value (`Vdf.cs` is a minimal VDF reader): `"0"` means off; a
missing key or file means on (Steam's default). Steam's global "Enable Steam
Cloud" setting is not read.

With the cloud off, the "Steam is running" warning isn't shown. Steam
regenerates `remotecache.vdf` even with the cloud off: that's normal.

## Languages

Interface texts are in `Strings.resx` (English, the fallback language) and
`Strings.it.resx` (Italian). The language follows Windows
(`CultureInfo.CurrentUICulture`). The `Strings` class is generated by the
build (`StronglyTypedLanguage` in the `.csproj`), no Visual Studio needed.

- Texts with values: `Loc.F(Strings.Key, value1, value2)`.
- To add a text: same key in both files. The `LocalizationTests` test checks
  that every key exists in both languages with the same `{0}`, `{1}`...
  placeholders.
- To add a language: `Strings.<code>.resx` with the same keys.
- The log (`log.txt`), code comments and on-disk names stay in Italian.

## Code structure

| File | Contents |
|---|---|
| `src/Lss.Core/SaveManager.cs` | Core logic: finding Steam and accounts, playthroughs, activation, saving, saved points, automatic copies, Steam Cloud. |
| `src/Lss.Core/SaveManager.Playthroughs.cs` | Note and play time, copy owner, deleted playthroughs, first start, settings, shared options, export, import, starting the game. |
| `src/Lss.Core/SessionTracker.cs` | Game sessions: when the game starts and stops, and the play time. |
| `src/Lss.Core/IPlatform.cs`, `Platform.cs` | What depends on the system (Steam folders, processes, starting the game, single instance), and the implementation in use. |
| `src/Lss.Core/IniFile.cs` | Safe reading and writing of `.ini` files. |
| `src/Lss.Core/FileOps.cs` | Copying, verifying and replacing folders. |
| `src/Lss.Core/AppState.cs` | Reading and writing `stato.ini`. |
| `src/Lss.Core/AppData.cs`, `AppSettings.cs` | App data folder and `lss.ini`. |
| `src/Lss.Core/LegacyMigration.cs` | Everything for Control Save Manager: data migration, its mutex, format 1 exports (removable, see above). |
| `src/Lss.Core/Vdf.cs` | Reader for Steam configuration files. |
| `src/Lss.Core/SteamLibrary.cs` | Steam libraries, manifests, support state, images and the list of an account's games. |
| `src/Lss.Core/GameInfo.cs` | `GameInfo` record and `SupportState`. |
| `src/Lss.Core/Log.cs` | Log of operations and unexpected errors. |
| `src/Lss.Core/Result.cs` | Outcome of operations (message, backup created, serious error). |
| `src/Lss.Core/Loc.cs`, `Strings.resx`, `Strings.it.resx` | Texts in English and Italian. |
| `src/Lss.Platform/WindowsPlatform.cs` | Windows: Steam path from the registry, processes, the single-instance mutex. |
| `src/Lss.Platform/LinuxPlatform.cs` | Linux: Steam folders, `registry.vdf`, processes through `/proc`. |
| `src/Lss.App/` | The Avalonia app, exe `LSS.exe` (`AssemblyName` `LSS`, so resources are `avares://LSS/...`). |
| `src/Lss.App/Program.cs`, `App.axaml(.cs)` | Startup, single instance, the application. |
| `src/Lss.App/Views/` | Windows and the playthrough card (`MainWindow`, `HistoryWindow`, `MessageWindow`...). |
| `src/Lss.App/ViewModels/` | The logic of each window, without controls. |
| `src/Lss.App/Services/` | Dialogs (`DialogService`), the game monitor (`GameMonitor`). |
| `src/Lss.App/Styles/Theme.axaml` | Colours and styles of the dark theme. |
| `src/Lss.App/Assets/app.ico` | Icon of the exe and the windows. |
| `src/Lss.App/Properties/PublishProfiles/` | Publish profiles `win-x64.pubxml` and `linux-x64.pubxml`. |
| `scripts\release.ps1` | Builds the release zip (locally and on GitHub Actions). |
| `.github\workflows\build.yml` | Build, tests, SignPath signing and draft release on GitHub. |
| `CODE_SIGNING.md` | Signing policy required by SignPath. |
| `tests\Lss.Tests\` | xUnit tests (run on Windows and Linux). |
| `tools\UiScreenshots\` | Pictures of every window with fake data (see below). |

The WinForms version was removed in G14 (see git history before that commit).

## Building

Requires the .NET 10 SDK. The solution has all the projects (app, core,
platform, tests and tools):

```bash
dotnet build LSS.slnx -c Release
```

Single exe for 64-bit Windows, with the .NET runtime included (users don't
have to install anything, about 50 MB), in `bin\publish\win-x64\`:

```bash
dotnet publish src/Lss.App/Lss.App.csproj -p:PublishProfile=win-x64
```

For debugging in VS Code there are `.vscode\launch.json` and
`.vscode\tasks.json`.

## Tests

xUnit tests in `tests/Lss.Tests`: they work on fake folders in
`%TEMP%` (they don't touch Steam or the app's real data) and cover copying and
swapping folders, playthroughs, saved points, automatic copies and their
owner, note and play time, deleted playthroughs, export and import, shared
options, first start, `stato.ini`, Steam Cloud, accounts, recovery from
interrupted operations, launcher detection, moving data from Documents,
translated texts and card lines. The tests' log goes to
`%TEMP%\csm_test_log.txt`. Tests run in Italian whatever the Windows language.

```bash
dotnet test tests/Lss.Tests
```

The windows aren't covered by tests. On GitHub the tests run on every push and
pull request (see below); locally `scripts\release.ps1` runs them too.

### Checking on Linux without GitHub

`scripts/linux-check.sh` runs on a Linux machine with Docker, reached over
SSH with a key (no password; the user must be able to run `docker`, root is
not needed). It copies the current commit (`HEAD` only, not uncommitted
changes) to `~/lss-ci` and, in temporary containers only:

1. runs the tests (`mcr.microsoft.com/dotnet/sdk:10.0`);
2. builds the packages with `scripts/release.sh`, as the `linux` job of the
   workflow does, and leaves them in `~/lss-out`;
3. installs each package in a clean distribution and runs `lss --version`:
   deb on Ubuntu 24.04, Ubuntu 22.04 and Debian 13, rpm on Fedora 42, the
   AppImage on Ubuntu 24.04, the tar.gz with `install.sh` on Arch Linux.

```bash
bash scripts/linux-check.sh user@host
```

With `--tests-only` it stops after the tests. It can't replace a check with
the real Steam and a desktop (H8 in the plan).

### Pictures of the windows

`tools/UiScreenshots` draws every window of the Avalonia app (main window,
confirmations, new playthrough, note, History, Settings, first start,
accounts, deleted playthroughs) with fake data and saves them as PNG, to
check the look without opening the app on real saves. It works in a
temporary folder (data and log included) and never touches Steam or
`%LOCALAPPDATA%\LSS`. Second argument: `it` (default) or `en`.

```bash
dotnet run --project tools/UiScreenshots -c Release -- shots it
```

Menus opened from a button (the card's ⋯) are separate popups and are not
in the pictures. If Smart App Control blocks the tool's dll, rebuild it
with `--no-incremental` (see [Smart App Control](#smart-app-control)).

## Build on GitHub

The repository is on [GitHub](https://github.com/Andrix64/Control-Savegame-Switcher)
(needed for free signing, see below). The `.github/workflows/build.yml`
workflow runs on Windows on every push to `main`, every pull request and on
demand (*Run workflow*):

1. on `v*` tags it checks that the tag matches `<Version>`;
2. runs the tests;
3. publishes the single exe and uploads it as the `unsigned-exe` artifact;
4. **on tags only**, if SignPath is configured, sends the exe to SignPath,
   waits for the signature (to be approved manually on SignPath) and checks
   that it's valid;
5. builds the zip with `scripts/release.ps1 -Exe <exe>` and uploads it as the
   `lss` artifact, with the test results;
6. on tags it prepares a **draft** release with the zip and `.sha256`, saying
   whether the exe is signed or not.

Without SignPath the build works anyway: the zip just isn't signed.

## Code signing

Smart App Control and SmartScreen (see below) require a signature from an
authority trusted by Microsoft: a self-signed certificate isn't enough.
Signing is free through [SignPath Foundation](https://signpath.org/), which
signs open source projects with an OSI license (MIT here) built by an
automated build in a public repository. The project's signing policy is in
[CODE_SIGNING.md](../CODE_SIGNING.md).

One-time setup:

1. Publish at least one version (even unsigned) and apply at
   [signpath.org](https://signpath.org/), pointing to the repository and the
   [CODE_SIGNING.md](../CODE_SIGNING.md) page.
2. On SignPath: create the project linked to the GitHub repository, with an
   *artifact configuration* that signs `LSS.exe` (Authenticode)
   and the `release-signing` *signing policy*; create a CI user and its token.
3. On GitHub, in *Settings → Secrets and variables → Actions*:
   - secret `SIGNPATH_API_TOKEN`: the CI user's token;
   - variable `SIGNPATH_ORGANIZATION_ID`: the organization ID;
   - variable `SIGNPATH_PROJECT_SLUG`: the project slug;
   - variable `SIGNPATH_SIGNING_POLICY_SLUG`: optional, `release-signing` by
     default.

SignPath gives the exact names (slug, policy) when it approves the project:
check them and adjust the variables. The name and version of the
`signpath/github-action-submit-signing-request` action should also be checked
against their documentation when setting it up.

## Release

1. Update `<Version>` in `src/Lss.App/Lss.App.csproj` and the
   [CHANGELOG](../CHANGELOG.md), then commit. The version shows in the window
   header, in the exe properties and in the log.
2. Create the version tag and push it to GitHub:

   ```bash
   git tag v0.9.0
   ```

   ```bash
   git push origin main v0.9.0
   ```

3. On SignPath, approve the signing request (it comes from GitHub Actions).
4. On GitHub, in **Releases**, check the draft (it must say the exe is signed
   by SignPath Foundation) and publish it.

Without GitHub, the zip can also be built locally (unsigned):

```bash
powershell -ExecutionPolicy Bypass -File scripts/release.ps1
```

The script refuses to run with uncommitted changes, runs the tests, publishes
the exe and creates in `dist\` the zip `LSS-<version>-win-x64.zip`
(exe, README, CHANGELOG and LICENSE) with its `.sha256` file. With
`-Exe <path>` it uses a ready exe instead of publishing one.

## Smart App Control

Smart App Control (Windows 11) may block a self-built exe, which isn't
signed: the app closes right after starting and the event log shows "An
Application Control policy has blocked this file". It happened with a version
that called `dwmapi.dll` and `uxtheme.dll` directly (dark title bar and
scrollbars); without those calls it started. Better to avoid `DllImport` in
the project.

The verdict is about the single file (its hash), not just the code: a Debug
build was blocked (`FileLoadException ... 0x800711C7`) while the Release build
of the same sources started. A deterministic build always produces the same
file, so rebuilding didn't help. That's why Debug builds are not
deterministic: if one is blocked, rebuild everything (VS Code task `rebuild`,
or `dotnet build --no-incremental`) and the new file usually passes. On
2026-10-23 every new build was blocked, which is why signing is now urgent
(see [TODO](TODO.md)). The event log is
`Microsoft-Windows-CodeIntegrity/Operational` (events 3033 and 3077).

## Other documents

- [Known bugs](KNOWN_BUGS.md)
- [To do](TODO.md)
- [What's new in each version](../CHANGELOG.md)

The original plan for moving from the console to the graphical interface was
completed and deleted; it's in the git history (up to commit `90c00da`).
