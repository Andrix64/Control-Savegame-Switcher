# To do

What's missing, in order of priority. Bugs are in
[KNOWN_BUGS.md](KNOWN_BUGS.md); what's already done is in the
[CHANGELOG](../CHANGELOG.md).

## To do

- [ ] **Code signing. Urgent.** On 2026-10-23, on the development PC with
  Smart App Control on, every new build of the app was blocked: the exe, the
  dll and three regenerated single exes (on 2026-10-22 the single exe
  started). Anyone with Smart App Control on may not be able to start 0.9.0. A
  self-signed certificate isn't enough. Chosen path: free signing by SignPath
  Foundation with the GitHub Actions build, already in place (see
  [DEVELOPMENT.md](DEVELOPMENT.md#code-signing)). Still missing:
  - [x] check that the first build on GitHub succeeds;
  - [x] publish the first version (0.9.0, unsigned);
  - [ ] apply at [signpath.org](https://signpath.org/);
  - [ ] configure project and token on SignPath and the secrets/variables on
    GitHub;
  - [ ] make a tagged release and check that the signed exe starts with Smart
    App Control on.

## Tests with the real game

Tests run on 2026-10-23 on real data, after a verified backup. The app was
started from a test executable with the same code, because Smart App Control
blocked the real one (see above).

Passed:

- [x] **Move from Documents** with real data (2026-10-22, 728 files, outcome
  "Moved" in the log).
- [x] **Move between two real drives** (H: → C:) with fake data: 60 of 60
  files identical, old folder deleted, nothing left behind.
- [x] **Starting Control** with `SaveManager.LaunchGame`
  (`steam://rungameid/870780`): with Steam closed, the game started in 21 s.
- [x] **Play session**: when the game closed, the app recorded 103 s in the
  right playthrough. The game, left at the menu, didn't change the saves and
  the app made no needless saves.
- [x] **Data intact**: after the tests, the app's data was identical to the
  backup (apart from the log and play time).

To do by hand (they need actions inside the game or a choice on Steam):

- [ ] **Save on exit with real progress.** Play until a save, close the game
  and check that the playthrough is updated (so far tested only without
  progress and with automated tests).
- [ ] **Shared options.** `preferences_data` is a 764-byte binary (it contains
  `default` and an identifier) and the two playthroughs have different
  versions of it. Change an option in the game (subtitles, for example), switch
  playthrough with the setting on and check that it stays.
- [ ] **Export and import between two PCs.** It couldn't be tested on real
  data because of Smart App Control blocks; the code path is covered by
  automated tests.
- [ ] **Steam Cloud on.** Deliberately not tested: turning it back on might
  download old saves again. The app currently warns and recommends turning it
  off.
- [ ] **Full round.** Two playthroughs, a saved point before a boss, going
  back to the point, switching playthrough and back: each playthrough must
  find its own control points.

## Ideas

- **Desktop shortcut "Play <playthrough>".** One click loads the playthrough
  and starts the game without opening the app. To be checked with Smart App
  Control.
- **More languages.** Just a `Strings.<code>.resx` with the same keys (see
  [DEVELOPMENT.md](DEVELOPMENT.md#languages)).
