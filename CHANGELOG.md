# Changelog

All notable changes, newest first. Versions follow
[Semantic Versioning](https://semver.org/).

## 0.9.0 — 2026-10-23

First public release.

### Playthroughs

- Several Control playthroughs on the same account, one at a time in the game.
- **Play this**: saves the progress of the current playthrough, loads the
  chosen one and starts Control through Steam. It can also just load, with
  **Undo** right after.
- Note and play time for each playthrough; play time is measured
  automatically while the app is open.
- Progress is saved automatically when the game closes (can be turned off).
- Option to keep the same game options in all playthroughs.
- New playthrough (empty or a copy of the current saves), rename, export and
  import as a zip file.
- A deleted playthrough can be recovered from “Deleted playthroughs”.
- On first start, the playthrough already in the game is saved under a name of
  your choice.
- If the PC has more than one Steam account with Control, you choose which
  one to use at startup.
- Playthrough names with spaces and accented letters. Folders in `Profili`
  with an invalid name are reported at startup.

### History

- Saved points (for example before a boss) and automatic copies made before
  every operation, per playthrough, with **Go back here**.
- The last 20 automatic copies are kept (configurable).

### Save safety

- Every copy is verified byte by byte.
- Two-step replacement: the new saves are prepared and verified in a separate
  folder before the swap; if anything fails, the copy is restored.
- If an operation is interrupted (for example by a power cut), the folders go
  back to their previous state on the next start.
- Operations are blocked while Control is running; only one instance of the
  app at a time.
- Data in `%LOCALAPPDATA%\ControlSaveManager`, which OneDrive doesn't sync.
  Data from test versions in `Documents\ControlSaves` is moved automatically on
  first start, without ever losing anything.
- Operation log in `log.txt`.

### Steam Cloud

- The app reads whether Steam Cloud is on for Control and shows a bar with
  instructions to turn it off. With the cloud off it no longer warns.

### Interface

- One card per playthrough with its actions visible, bars for app-wide states
  (game running, Steam Cloud) and confirmations with verbs ("Delete
  playthrough", "Cancel") only for what can't be undone. Designed following
  the Nielsen Norman Group heuristics and Microsoft's Windows guidelines.
- Dark theme, message boxes included.
- English and Italian: language and date format follow Windows.
- Single exe for 64-bit Windows, no installation.
