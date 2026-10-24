# Progress

Tick a task (`- [x]`) when its commit is done and write the short commit
hash after it. 👤 = done by Andrix, skip it. Rules in [README.md](README.md).

## Done before this plan

- [x] A — Rename to LSS (`552e3c9`, `f5aeaa5`)
- [x] B — Avalonia test app in `spike/` (`71840cb`): builds, ~48 MB single
  exe for Windows and Linux, native dlls already signed, starts with Smart
  App Control on, window checked by Andrix on 2026-10-24.

## Phase C — Core library ([C-core.md](C-core.md))

- [x] C1 — Solution file (bafaacf)
- [x] C2 — `IPlatform`, still inside the WinForms project (035d396)
- [x] C3 — Card texts out of the WinForms control (b80bb70)
- [x] C4 — Projects `Lss.Core` and `Lss.Platform` (af1f6f1)
- [x] C5 — Tests on plain `net10.0` (98d083e)
- [x] C6 — Tests on Linux in GitHub Actions (79683d9)
- [x] C7 — 👤 Check the WinForms app, push, note failing Linux tests
  (2026-10-24: app works; CI run on `lss` by hand: Windows 92/92, Ubuntu
  90/92, the two failures are planned in H7)

## Phase D — Game catalogue ([D-catalog.md](D-catalog.md))

- [x] D1 — `GameInfo` and `SupportState` (c563816)
- [x] D2 — Library folders (e345f9b)
- [x] D3 — Manifests (91efc49)
- [x] D4 — Support state (745b9f8)
- [x] D5 — Images (2220175)
- [x] D6 — List the games of an account (401f983)
- [x] D7 — Docs (2195cef)

## Phase E — Any game ([E-per-game.md](E-per-game.md))

- [x] E1 — `SaveManager` gets a `GameInfo` (79efd2d)
- [x] E2 — Texts with the game name (765edb8)
- [x] E3 — `LaunchGame`, `IsGameRunning`, `GameStartTime` per game (e5f65bc)
- [x] E4 — Shared files per game (c1a2827)
- [x] E5 — Accounts and recovery for every game (f1cc69b)
- [x] E6 — 👤 Check with Control (2026-10-24, Andrix: game bar while Control runs, play time and save on exit when it closes)

## Phase F — Data folder ([F-data.md](F-data.md))

- [x] F1 — `AppSettings` (`lss.ini`) (eaedc98)
- [x] F2 — `SaveManager.Open` (9ecef98)
- [x] F3 — Migration from Control Save Manager (d5d2bba)
- [x] F4 — 👤 Migration with real data (2026-10-24: ControlSaveManager moved to LSS; both playthroughs, notes, saved points and 29 copies under `405015118/870780`)
- [x] F5 — Export format 2 (320fb77)
- [x] F6 — Docs for the data folder (62a5ef5)

## Phase G — Avalonia interface ([G-avalonia.md](G-avalonia.md))

- [x] G1 — Project skeleton (f1a0faa)
- [x] G2 — Theme and texts in XAML (24aa28d)
- [x] G3 — Services (db28682)
- [x] G4 — `MessageWindow` (3e4f19a)
- [x] G5 — `SessionTracker` (a4ce61a)
- [x] G6 — `GameViewModel` (36c5ebd)
- [x] G7 — Playthrough card (d5560fa)
- [x] G8 — Main window, games and monitor (21a0d0e)
- [x] G9 — New playthrough, rename, note (2741435)
- [x] G10 — History (f74f110)
- [x] G11 — Settings and first start (39c8f98)
- [x] G12 — Accounts and deleted playthroughs (396633a)
- [ ] G13 — 👤 Full manual check
- [ ] G14 — Remove WinForms, `Lss.App` becomes `LSS`

## Phase H — Linux ([H-linux.md](H-linux.md))

- [x] H1 — Steam folders (241528a)
- [x] H2 — `registry.vdf` (bdb46ca)
- [x] H3 — Game processes (5870599)
- [x] H4 — Launching and single instance (35fd677)
- [x] H5 — Case of names (34b0517)
- [x] H6 — Paths and permissions (f77a0cb)
- [ ] H7 — Linux tests required in CI
- [ ] H8 — 👤 Real Linux check

## Phase I — Packages ([I-packaging.md](I-packaging.md))

- [ ] I1 — `--version`
- [ ] I2 — Linux desktop files and icons
- [ ] I3 — `scripts/release.sh`
- [ ] I4 — `nfpm.yaml` (deb, rpm)
- [ ] I5 — AppImage
- [ ] I6 — Workflow
- [ ] I7 — 👤 First CI run

## Phase J — Docs and release ([J-docs-release.md](J-docs-release.md))

- [ ] J1 — 👤 Rename the repository
- [ ] J2 — Links and README
- [ ] J3 — Other documents
- [ ] J4 — Version `1.0.0-beta.1`
- [ ] J5 — 👤 First beta
- [ ] J6 — 👤 SignPath
- [ ] J7 — Further betas
- [ ] J8 — 1.0.0

## Notes

Write here anything that didn't match the plan, decisions taken with
Andrix, and results of the 👤 checks. Newest first, with the date.

- 2026-10-24 — G13, first round (Andrix + Opus): decisions: in
  confirmations the **verb is the default button** (Enter = confirm, Esc =
  cancel, Cancel on the left); the **Open folder** button stays in the
  deleted playthroughs window. Fixed after looking at every window with the
  new `tools/UiScreenshots`: text centred in all buttons, ⋯ button same
  height as the others with drawn dots, darker red for destructive buttons,
  hover on secondary/danger, app colours for list selection and for the
  Fluent accent (checkboxes, radio buttons), italic note no longer cut,
  History's Delete button styled. The rest of G13 (every function by hand)
  is still to do.
- 2026-10-24 — Phase G review (Opus): the main window was never shown
  (assigned to `desktop.MainWindow` after startup without `Show()`), and an
  exception in `StartAsync` was lost; both fixed in `01344b5`, with texts
  using `MaxWidth` left-aligned. Window seen with real data: games list
  (Control Ultimate Edition, Terraria), cards and buttons OK.
- 2026-10-24 — Fase G (G1–G12, Haiku): visual checks not done (no window was looked at).
  Andrix's G13 list: the dialogs' button order (see the G4 note below), hover colours of
  `Button.primary` (`/template/ ContentPresenter`, may not show with the Fluent template),
  the Italian texts (`!Key` would show a missing key), the game-card buttons, the Settings
  header button, the deleted-playthroughs link, the wait cursor while busy, the toast colours.
  The app was started only for 10 s after G1 (empty window): it was never run with real
  data, because the startup calls `LegacyMigration.Run()` on the real folders.
- G4/G3 — Deviation from the plan (kept WinForms): in confirmations the default button is
  **Cancel** (Enter and Esc cancel), the verb is secondary or danger. The plan said verb
  primary and Enter = verb; for deletions the WinForms choice is safer. `DialogService.Inform`
  / `Confirm` were a placeholder in G3 and became real in G4 (same commit series).
  WinForms' sound of the message box (`SystemSounds`) is not ported.
- G6 — Test setup deviation: `Avalonia.Headless.XUnit` 12.1.4 brings xunit.v3 and conflicts
  with xunit 2.9.2 of the test project (CS0433 on every `[Fact]`). Used `Avalonia.Headless`
  12.1.4 with `TestApp.Ensure()` (`SetupWithoutStarting`) in the constructor of the tests that
  need the dispatcher, and plain `[Fact]` instead of `[AvaloniaFact]`. That counts as the
  one failure the plan allows for the headless setup; the workaround passed on its first run.
- G6/G7 — `PlaythroughViewModel` is written in G6 (GameViewModel needs its type); G7 holds
  the card view and its tests. The commands Rename, Edit note, Save a point and History were
  placeholders in G7 (`NotImplementedException`) and are real since G9/G10.
- G8 — The header Settings button, the New playthrough button and the deleted link were
  added in G9, G11 and G12 (the plan puts them there). `OfferFirstRunAsync` and the list of
  ignored folders run when the window opens (first game) or at the first selection of each game
  after that (`MainWindowViewModel.OnOpenedAsync`).
- G8/G12 — Several accounts: G8 used the last one or the first one; G12 added AccountWindow
  and the startup now shows it when there is more than one account.
- Keys added that the plan did not list: `AppSubtitle` (the WinForms header wrote
  "LOCAL SAVE SWITCHER" as a literal), `AccountIntroAny` (the plan's `AccountIntro` names
  Control; the account window must not), `SharedFilesCaption`, `AddSharedFile`,
  `RemoveSharedFile`, `SharedFileOutside` (G11 list), `GamesCaption` etc. (G8 list).
- G3/G11 — `IDialogService.PickFileAsync` added for the shared-files picker (G11); the plan
  did not list it.
- G12 — The "Open folder" button in the deleted-playthroughs window is there because the
  plan lists it; the WinForms `DeletedForm` has no such button. Check with Andrix.
- G4–G12 — `CannotOpenFolder` receives the folder path, not the exception message (the
  `DialogService.OpenFolderAsync` returns only true/false).
- G2 — Colours not in the plan's table (`ErrorBack`, `ErrorBorder`, `DangerBorder`,
  `DangerText`) were added as the plan allows. Font: Inter (`WithInterFont`), not Segoe UI.
- G9 — Name box: the WinForms rule kept as is (rename with the same name in other case is
  allowed, the same name exactly is not).
- 2026-10-24 — Decision with Andrix: nobody has used Control Save Manager
  yet, so everything for its compatibility (data migration, its mutex,
  format 1 exports, the "data moved" message) is now in
  `src/Lss.Core/LegacyMigration.cs`, tests in `LegacyMigrationTests.cs`
  (was `AppDataTests.cs`), removable with the checklist in
  `docs/DEVELOPMENT.md` → "Compatibility with Control Save Manager". Rule
  added to README.md.
- 2026-10-24 — F3 review (Opus): (b) fixed: when ControlSaveManager → LSS
  finds LSS already with data the result is `BothExist`, even if Documents
  → ControlSaveManager moved first. (c)+(d): when a move fails the app stays
  in the old folder, and `RestructureAccounts` now runs there too, so the
  playthroughs are found under `IDAccount\870780` (before, the app would
  have seen none). (a), (e), (f) are fine as they are. New test
  `Migrate_FromDocumentsButLssHasData_IsBothExist`; 129 tests.
- 2026-10-24 — F3, to check with Andrix before F4: (a) `AppData.MigrateToLss`
  is `internal` with three roots and returns four values (result, detail,
  root in use, source of the move): the public wrapper needs the last two to
  set `Root` and `LastMoveSource`, and the tests need the roots as
  parameters. (b) Step 4 is applied as written, which gives `Moved` also when
  Documents → ControlSaveManager moved and ControlSaveManager → LSS found LSS
  already with data (`BothExist`): then ControlSaveManager's data stays out of
  use and the message says it was moved to LSS. Maybe it should say
  `BothExist` instead. (c) If the first step fails, `ReportDataMove` still names
  LSS as the destination. (d) `RestructureAccounts` also runs when the result
  is `BothExist` (only inside LSS). (e) `account.txt` is deleted only when
  `lss.ini` was written; if `lss.ini` already exists, `account.txt` is left.
  (f) `Move` already builds `newRoot + ".spostamento"`: unchanged.
  `SaveManager.IsNumeric` became `internal` so `AppData` can use it.
- 2026-10-24 — F2/F5: `ImportOtherGame` and `GameNotSupported` texts added as
  the plan says. Zip entries: the export check for `\` is `Any` instead of the
  plan's `All(!…)`, same meaning. Tests: 113 → 128.
- 2026-10-24 — E3: the check `grep -rn "870780\|Control_DX\|\"Control\""
  src/` lists, besides `GameInfo.cs` and `SteamLibrary.cs`, the literal
  `"Control"` of `SavesNotFound` in `SaveManager.Initialize` (E2 keeps it on
  purpose: that entry point is Control-only until phase G). `AccountForm.cs`
  (outside `src/`) has the same literal. `MinorBugTests.GameLauncherIsRecognized`
  is deleted with `IsGameLauncher` (its 5 cases: 110 → 106 tests, plus
  `LaunchGame_UsesTheGameAppId`).
- 2026-10-24 — E2: `AccountIntro` ("…with Control.") names the game too but
  is not in the table: changed the same way to `{0}`. Its call site
  (`AccountForm`, no `SaveManager` there) and `SavesNotFound` in `Initialize`
  pass `"Control"`. Until E3, the static `LaunchGame` also passes `"Control"`
  to `GameLaunching` and `LaunchFailed`.
- 2026-10-24 — E1: until E3, the static `LaunchGame` uses
  `GameInfo.ControlAppId` (it can't use `Game.AppId`: no instance yet).

- 2026-10-24 — CI run on `lss` at `683f447` (end of phase D): on Ubuntu
  only the two known failures of H7; every `SteamLibrary` test passes on
  Linux too.
- 2026-10-24 — CI is triggered automatically only by pushes to `main` and
  `v*` tags: on `lss` Andrix starts it by hand (*Actions → Build → Run
  workflow*). `lss` is merged into `main` only at the end.
- 2026-10-24 — C7, Ubuntu: `AppDataTests.Move_LeavesOldFolderIntactWhenItFails`
  (an open file doesn't block a move on Linux) and
  `LocalizationTests.StoredDatesAreShownInTheCurrentCulture` (ICU puts
  U+202F before PM). Fixes written in H7.
- 2026-10-24 — C4: the check "grep for Microsoft.Win32 / System.Windows.Forms /
  Process.Start in src/Lss.Core" also matches the standard resx header lines
  (`ResXResourceReader, System.Windows.Forms`) in `Strings.resx` and
  `Strings.it.resx`. Restricted to `*.cs` the grep prints nothing: the code
  is clean and the build has 0 warnings.
- 2026-10-24 — Plan written. Open questions for Andrix: maintainer email
  for the Linux packages (I4).
