# To do

What's missing, in order of priority. Bugs are in
[KNOWN_BUGS.md](KNOWN_BUGS.md); what's already done is in the
[CHANGELOG](../CHANGELOG.md).

## Before 1.0

- [ ] **Code signing.** Urgent. On 2026-10-23, on the development PC with Smart
  App Control on, every new build of Control Save Manager was blocked, so
  anyone with Smart App Control on may not be able to start an unsigned
  version. A self-signed certificate isn't enough. Chosen path: free signing
  by SignPath Foundation with the GitHub Actions build, already in place (see
  [DEVELOPMENT.md](DEVELOPMENT.md#code-signing)). Still missing:
  - [x] check that the first build on GitHub succeeds;
  - [x] publish the first version (0.9.0, unsigned);
  - [ ] apply at [signpath.org](https://signpath.org/);
  - [ ] configure project and token on SignPath and the secrets/variables on
    GitHub;
  - [ ] make a tagged release and check that the signed exe starts with Smart
    App Control on.
- [ ] **First beta** (`1.0.0-beta.1`): tag, pre-release, see the plan in
  `docs/plan/`.
- [ ] **Real Linux check.** The packages are installed and started in
  containers on Ubuntu, Debian, Fedora and Arch, but Steam and a desktop were
  never used. Check on a real desktop: a game started and stopped from LSS,
  the running-game detection, and the Steam installs (native, Flatpak, Snap).
- [ ] **Steam Deck** (desktop mode): the AppImage starts, the games list is
  right, and a playthrough switch works.

## Manual checks with the real game

Done in 0.9.0 (on 2026-10-22 and 2026-10-23): moving the data from Documents,
moving data between two drives, starting the game from the app, recording the
play time, and the data staying intact.

Still to do, because they need actions inside the game or a choice on Steam:

- [ ] **Save on exit with real progress.** Play until a save, close the game
  and check that the playthrough is updated (so far tested only without
  progress and with automated tests).
- [ ] **Files kept the same.** `preferences_data` of Control is a 764-byte
  binary (it contains `default` and an identifier) and the two playthroughs
  have different versions of it. Change an option in the game (subtitles, for
  example), switch playthrough with the setting on and check that it stays.
- [ ] **Export and import between two computers.**
- [ ] **Steam Cloud on.** Deliberately not tested: turning it back on might
  download old saves again. The app warns and recommends turning it off.
- [ ] **Full round.** Two playthroughs, a saved point before a boss, going
  back to the point, switching playthrough and back: each playthrough must
  find its own saved points.

## After 1.0

- [ ] **Flathub**: manifest with `org.freedesktop.Platform`, `--filesystem`
  for the Steam folders, `--socket=wayland`, `--socket=fallback-x11`. Process
  detection works only through `registry.vdf` inside the sandbox.
- [ ] **AUR** package `lss-bin`.
- [ ] **Games that save outside `userdata`** (for example in Documents or in the
  game folder): today LSS lists them as *Other games* and doesn't manage them.
- [ ] **macOS.**
- [ ] **Desktop shortcut "Play <playthrough>".** One click loads the playthrough
  and starts the game without opening the app. To be checked with Smart App
  Control.
- [ ] **More languages.** Just a `Strings.<code>.resx` with the same keys (see
  [DEVELOPMENT.md](DEVELOPMENT.md#languages)).
