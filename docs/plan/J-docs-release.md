# Phase J — Documentation, beta and release

All documentation in English. The user guide is `README.md`; technical
notes are `docs/DEVELOPMENT.md`.

---

## J1 — 👤 Rename the repository (Andrix)

1. GitHub: *Settings → General → Repository name* → `Local-Save-Switcher`.
   GitHub redirects the old links.
2. Forgejo: same name.
3. Locally:

   ```bash
   git remote set-url origin https://github.com/Andrix64/Local-Save-Switcher.git
   ```

4. GitHub *About*: description "Multiple playthroughs for Steam games
   (Windows and Linux)", topics `steam`, `savegame`, `save-manager`,
   `windows`, `linux`, `steam-deck`, `avalonia`.

---

## J2 — Links and README

1. Replace `Andrix64/Control-Savegame-Switcher` with
   `Andrix64/Local-Save-Switcher` in every file (`grep -rn
   "Control-Savegame-Switcher" --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj .`).
2. Rewrite `README.md`. Keep the tone and the structure of the current one
   (short sentences, "you", no marketing words). Sections:
   1. Title `# LSS — Local Save Switcher`, one-line subtitle, screenshot
      `docs/img/main.png`, one paragraph: what it does, for which games
      (saves in Steam's `userdata` folder), Windows and Linux, English and
      Italian.
   2. **Which games work**: how to tell (the app lists them; "Other games"
      shows why a game isn't supported); games tried in
      `docs/COMPATIBILITY.md`; the note about games that also save
      elsewhere.
   3. **Installation — Windows**: zip, extract `LSS.exe`, requirements,
      the digital signature paragraph (from the current README, with
      `LSS.exe`).
   4. **Installation — Linux**: a table "distribution → file → command"
      (Ubuntu/Debian/Mint: `sudo apt install ./lss_<v>_amd64.deb`; Fedora:
      `sudo dnf install ./lss-<v>-1.x86_64.rpm`; Steam Deck and others:
      AppImage, `chmod +x` and run, or `.tar.gz` + `./install.sh`;
      openSUSE: AppImage). A line about Steam installed as Flatpak or Snap
      (supported).
   5. **Before you start: turn off Steam Cloud** for the game (current
      text, generic).
   6. **How to use it**: games list, playthroughs, while you play, history
      and saved points, settings (add "files kept the same"), as in the
      current README but generic.
   7. **Where the files are**: Windows `%LOCALAPPDATA%\LSS`, Linux
      `~/.local/share/LSS`; one folder per account and game.
   8. **Coming from Control Save Manager**: data moved automatically on
      first start (from `%LOCALAPPDATA%\ControlSaveManager`), old exports
      can be imported.
   9. **FAQ** (current ones, generic) + "My game isn't in the list".
   10. **License** and the disclaimer: *Steam is a trademark of Valve
       Corporation. LSS is not affiliated with or endorsed by Valve. Game
       names are trademarks of their owners.*
3. `CODE_SIGNING.md`: product name, `LSS.exe`, data folder, repository
   link.

Commit: `README for LSS`

---

## J3 — Other documents

1. `docs/DEVELOPMENT.md`: update every section to the new layout:
   projects (`src/Lss.Core`, `src/Lss.Platform`, `src/Lss.App`), the code
   structure table (one row per file of the three projects and tests),
   building (`dotnet build LSS.slnx`, publish profiles `win-x64` /
   `linux-x64`), tests (Windows and Linux), Linux notes (H: Steam folders,
   `registry.vdf`, `/proc`, lock file), packages (I), Smart App Control
   section (keep it; add that the Avalonia native dlls are signed by
   Microsoft and AvaloniaUI). Remove every WinForms-only detail.
2. `docs/COMPATIBILITY.md` (new): table `Game | AppID | Windows | Linux |
   Notes`, with Control (870780, tested on Windows), Terraria (105600:
   only cloud characters and worlds are in `userdata`), and an "How to
   report a game" paragraph pointing to the issue template.
3. `docs/TODO.md`: rewrite: what's left after 1.0 (Flathub, AUR, games
   saving elsewhere, macOS, desktop shortcut, more languages), and the
   manual checks still open.
4. `docs/KNOWN_BUGS.md`: log path for both systems.
5. `.github/ISSUE_TEMPLATE/bug.yml` (GitHub issue form): fields system
   (dropdown Windows/Linux/Steam Deck), distribution, how Steam is
   installed (native/Flatpak/Snap), LSS version, steps, log (textarea, with
   the log path for both systems).
6. `.github/ISSUE_TEMPLATE/game-report.yml`: game name, AppID, system,
   works yes/no/partly, notes.
7. Remove `docs/PIANO_LSS.md` if it is still there (superseded by
   `docs/plan/`).

Commit: `Docs for LSS`

---

## J4 — Version `1.0.0-beta.1`

1. `src/Lss.App/Lss.App.csproj`: `<Version>1.0.0-beta.1</Version>`.
2. `Program.Version` must show the beta: use the informational version,
   without the `+commit` suffix the SDK adds:

   ```csharp
   public static string Version { get; } =
       (typeof(Program).Assembly
           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "")
       .Split('+')[0];
   ```

   (`using System.Reflection;`). Check the header shows
   `v1.0.0-beta.1`.
3. `CHANGELOG.md`: turn "Unreleased" into `## 1.0.0-beta.1 — <date>` and
   list, in the existing style (short bullets, grouped): new name; every
   Steam game with saves in `userdata`; games list; Linux (packages);
   new interface; files kept the same per game; data moved to `LSS`;
   export format 2; detection of the running game via Steam.
4. Metainfo: `<release version="1.0.0-beta.1" date="<date>" type="development" />`.

Commit: `Version 1.0.0-beta.1`

---

## J5 — 👤 First beta (Andrix)

1. `git switch main`, `git merge --ff-only lss` (or a pull request from
   `lss`), push.
2. `git tag v1.0.0-beta.1`, `git push origin v1.0.0-beta.1`.
3. On GitHub: the draft release has Windows (unsigned: SignPath isn't set
   up yet) and Linux files. Publish it as **pre-release**.

---

## J6 — 👤 SignPath (Andrix)

Follow `docs/DEVELOPMENT.md` → "Code signing", with the new names: apply
on signpath.org for `Andrix64/Local-Save-Switcher` (link
`CODE_SIGNING.md`), create the project with an artifact configuration for
`LSS.exe`, policy `release-signing`, CI user and token; on GitHub the
secret `SIGNPATH_API_TOKEN` and the variables `SIGNPATH_ORGANIZATION_ID`,
`SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG`. Write in
`PROGRESS.md` when it's approved.

---

## J7 — Further betas

For each fix round: the agent bumps `<Version>` to `1.0.0-beta.N`, adds a
CHANGELOG section, commits (`Version 1.0.0-beta.N`); 👤 Andrix tags,
approves the signature on SignPath, checks the draft says "signed by
SignPath Foundation", tries the signed exe with Smart App Control on, and
publishes as pre-release.

---

## J8 — 1.0.0

Release when, all written in `PROGRESS.md`:

- no open bug in `docs/KNOWN_BUGS.md`;
- G13, H8, I7 done;
- at least 3 games in `docs/COMPATIBILITY.md`;
- tried on Windows 11 (Smart App Control on, signed exe), Ubuntu, Fedora,
  Steam Deck;
- migration from Control Save Manager tried with real data (F4).

Agent: `<Version>1.0.0</Version>`, CHANGELOG `## 1.0.0 — <date>` (summary of
the betas), metainfo `<release version="1.0.0" date="<date>" />` (remove
the beta release lines), commit `Version 1.0.0`.

👤 Andrix: merge to `main`, `git tag v1.0.0`, push, approve the signature,
download every file from the draft and check: `.sha256`, the Windows
signature (`Get-AuthenticodeSignature`), install of each Linux package;
publish. In the old `v0.9.0` release add a line "Continued as LSS — Local
Save Switcher" with the link.

---

## J9 — After 1.0 (separate plan)

Flathub (manifest with `org.freedesktop.Platform`, `--filesystem` for the
Steam folders, `--socket=wayland`, `--socket=fallback-x11`; process
detection works only through `registry.vdf` inside the sandbox), AUR
package `lss-bin`, games that save outside `userdata`, macOS. Write a new
plan for these when 1.0 is out.
