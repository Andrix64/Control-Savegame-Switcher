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
- [ ] F4 — 👤 Migration with real data
- [ ] F5 — Export format 2
- [ ] F6 — Docs for the data folder

## Phase G — Avalonia interface ([G-avalonia.md](G-avalonia.md))

- [ ] G1 — Project skeleton
- [ ] G2 — Theme and texts in XAML
- [ ] G3 — Services
- [ ] G4 — `MessageWindow`
- [ ] G5 — `SessionTracker`
- [ ] G6 — `GameViewModel`
- [ ] G7 — Playthrough card
- [ ] G8 — Main window, games and monitor
- [ ] G9 — New playthrough, rename, note
- [ ] G10 — History
- [ ] G11 — Settings and first start
- [ ] G12 — Accounts and deleted playthroughs
- [ ] G13 — 👤 Full manual check
- [ ] G14 — Remove WinForms, `Lss.App` becomes `LSS`

## Phase H — Linux ([H-linux.md](H-linux.md))

- [ ] H1 — Steam folders
- [ ] H2 — `registry.vdf`
- [ ] H3 — Game processes
- [ ] H4 — Launching and single instance
- [ ] H5 — Case of names
- [ ] H6 — Paths and permissions
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
