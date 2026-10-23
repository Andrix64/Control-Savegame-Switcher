# Development

Technical notes for people working on the code. The user guide is in the
[README](../README.md).

## Paths

| What | Path |
|---|---|
| Game saves | `<Steam>\userdata\<AccountID>\870780\` |
| App data | `%LOCALAPPDATA%\ControlSaveManager\` (`AppData.Root`) |
| Playthroughs | `<data>\<AccountID>\Profili\Profilo_<name>\` |
| Note and play time | `<data>\<AccountID>\Info\Profilo_<name>.ini` |
| Automatic copies | `<data>\<AccountID>\Backup\<reason>_<date>\` plus `<reason>_<date>.ini` next to it |
| Saved points | `<data>\<AccountID>\Savestate\Profilo_<name>\<date>_<point name>\` |
| State | `<data>\<AccountID>\stato.ini` |
| Log | `<data>\log.txt` (above 1 MB it becomes `log.old.txt`) |
| Last account used | `<data>\account.txt` |
| Steam Cloud setting | `<Steam>\userdata\<AccountID>\7\remote\sharedconfig.vdf` (read only) |

Data lives in `%LOCALAPPDATA%` rather than Documents because Documents is often
synced with OneDrive, which locks files while uploading them, keeps them only
in the cloud ("files on demand") and creates "conflict" copies: all things
that make copies fail or mix up playthroughs in a save folder. `Local` rather
than `Roaming` because backups can be large.

Earlier development versions used `Documents\ControlSaves`. At startup
`AppData.MoveFromDocuments` moves that folder: on the same drive with a single
`Directory.Move`; across drives by copying into `ControlSaveManager.spostamento`,
verifying every file, and only then deleting the original. If both folders
contain data it touches nothing and uses the new one; if the move fails it
keeps using the old one. The outcome goes to the log and is shown once to the
user.

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

A zip with `partita.ini` (`Formato=1`, `Nome`, `Nota`, `TempoDiGioco`,
`UltimaSessione`), `salvataggi\` (the playthrough folder) and, if any,
`punti\` (the saved points). Import rejects zips without `salvataggi\` and
paths that leave the folder.

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
| `SaveManager.cs` | Core logic: finding Steam and accounts, playthroughs, activation, saving, saved points, automatic copies, Steam Cloud. |
| `SaveManager.Playthroughs.cs` | Note and play time, copy owner, deleted playthroughs, first start, settings, shared options, export, import, starting the game. |
| `IniFile.cs` | Safe reading and writing of `.ini` files. |
| `FileOps.cs` | Copying, verifying and replacing folders. |
| `AppState.cs` | Reading and writing `stato.ini`. |
| `AppData.cs` | App data folder and move from Documents. |
| `Vdf.cs` | Reader for Steam configuration files. |
| `Log.cs` | Log of operations and unexpected errors. |
| `Result.cs` | Outcome of operations (message, backup created, serious error). |
| `Loc.cs`, `Strings.resx`, `Strings.it.resx` | Texts in English and Italian. |
| `MainForm.cs` | Main window: playthrough cards, bars, notices, play sessions. |
| `PlaythroughControls.cs` | Info bar (`InfoBar`), pill and playthrough card. |
| `HistoryForm.cs` | A playthrough's History: saved points and automatic copies. |
| `PromptForms.cs` | Note, Settings and First start windows. |
| `NewProfileForm.cs` | "New playthrough" window, also used to rename. |
| `AccountForm.cs` | Steam account choice at startup. |
| `SnapshotForms.cs` | Themed list, base of the list windows and the deleted playthroughs window. |
| `MessageDialog.cs` | Themed message boxes; `Confirm` uses verbs on the buttons. |
| `Theme.cs` | Colours, fonts and icon of the dark theme. |
| `ThemedControls.cs` | Hand-drawn controls: buttons, cards, header, menus. |
| `app.ico` | Icon of the exe and the windows. |
| `Program.cs` | Application startup and version. |
| `Properties\PublishProfiles\win-x64.pubxml` | Publish profile for the single exe. |
| `scripts\release.ps1` | Builds the release zip (locally and on GitHub Actions). |
| `.github\workflows\build.yml` | Build, tests, SignPath signing and draft release on GitHub. |
| `CODE_SIGNING.md` | Signing policy required by SignPath. |
| `tests\ControlSaveManager.Tests\` | xUnit tests. |

The old console version is in the git history (commit `17cd279`).

## Building

Requires the .NET 10 SDK.

```bash
dotnet build ControlSaveManager.csproj -c Release
```

Single exe for 64-bit Windows, with the .NET runtime included (users don't
have to install anything, about 50 MB), in `bin\publish\win-x64\`:

```bash
dotnet publish ControlSaveManager.csproj -p:PublishProfile=win-x64
```

For debugging in VS Code there are `.vscode\launch.json` and
`.vscode\tasks.json`.

## Tests

xUnit tests in `tests/ControlSaveManager.Tests`: they work on fake folders in
`%TEMP%` (they don't touch Steam or the app's real data) and cover copying and
swapping folders, playthroughs, saved points, automatic copies and their
owner, note and play time, deleted playthroughs, export and import, shared
options, first start, `stato.ini`, Steam Cloud, accounts, recovery from
interrupted operations, launcher detection, moving data from Documents,
translated texts and card lines. The tests' log goes to
`%TEMP%\csm_test_log.txt`. Tests run in Italian whatever the Windows language.

```bash
dotnet test tests/ControlSaveManager.Tests
```

The windows aren't covered by tests. On GitHub the tests run on every push and
pull request (see below); locally `scripts\release.ps1` runs them too.

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
   `control-save-manager` artifact, with the test results;
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
   *artifact configuration* that signs `ControlSaveManager.exe` (Authenticode)
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

1. Update `<Version>` in `ControlSaveManager.csproj` and the
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

For Forgejo, or without GitHub, the zip can also be built locally (unsigned):

```bash
powershell -ExecutionPolicy Bypass -File scripts/release.ps1
```

The script refuses to run with uncommitted changes, runs the tests, publishes
the exe and creates in `dist\` the zip `ControlSaveManager-<version>-win-x64.zip`
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
