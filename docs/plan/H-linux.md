# Phase H — Linux

Goal: the same app runs on Linux x64 with the native Steam client, Steam
installed as Flatpak or as Snap, for native games and Proton games.

New file: `src/Lss.Platform/LinuxPlatform.cs`,
`[SupportedOSPlatform("linux")] public sealed class LinuxPlatform : IPlatform`.
Pure helper functions are `internal static` so tests can call them on any
system. Tests in `tests/Lss.Tests/LinuxPlatformTests.cs`.

**Running Linux tests locally**: if `wsl --status` works on the PC, run
`wsl -e dotnet test tests/Lss.Tests -c Release` from the repository folder
(the SDK must be installed inside WSL: if it isn't, don't install it, use
CI). Otherwise the Linux tests run only on GitHub Actions (👤 push by
Andrix).

## Facts about Steam on Linux

| Install | Steam root | `registry.vdf` |
|---|---|---|
| Native (deb, rpm, Arch, SteamOS) | `~/.local/share/Steam` (also reached through the link `~/.steam/steam`, and on some systems `~/.steam/root`) | `~/.steam/registry.vdf` |
| Flatpak | `~/.var/app/com.valvesoftware.Steam/.local/share/Steam` | `~/.var/app/com.valvesoftware.Steam/.steam/registry.vdf` |
| Snap | `~/snap/steam/common/.local/share/Steam` | `~/snap/steam/common/.steam/registry.vdf` |

`registry.vdf` is the Linux copy of the Windows registry keys:

```
"Registry"
{
	"HKCU"
	{
		"Software"
		{
			"Valve"
			{
				"Steam"
				{
					"RunningAppID"		"0"
					"Apps"
					{
						"105600"
						{
							"Running"		"0"
						}
					}
				}
			}
		}
	}
}
```

**Not yet verified** that Steam updates `Running`/`RunningAppID` there
while a game runs (task H8 checks it). The process check (H3) works
anyway, so the app is safe even if it doesn't.

Games started by Steam (native and Proton) have the environment variables
`SteamAppId=<AppID>` and `SteamGameId=<AppID>`, readable in
`/proc/<pid>/environ` (NUL-separated) for processes of the same user.

---

## H1 — Steam folders

```csharp
/// <summary>
/// Cartelle di Steam su Linux, in ordine: installazione normale, Flatpak,
/// Snap. Solo quelle con userdata, senza doppioni dovuti ai collegamenti.
/// </summary>
internal static IReadOnlyList<string> FindSteamRoots(string home)
```

Candidates in this order: `home/.local/share/Steam`, `home/.steam/steam`,
`home/.steam/root`, `home/.var/app/com.valvesoftware.Steam/.local/share/Steam`,
`home/snap/steam/common/.local/share/Steam`. For each: skip if
`Path.Combine(candidate, "userdata")` doesn't exist; resolve links with
`Path.GetFullPath(new DirectoryInfo(candidate).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? candidate)`;
skip if the resolved path is already in the list (ordinal comparison).

Interface method: `FindSteamRoots() =>
FindSteamRoots(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))`.

Tests (Linux only: start each with `if (!OperatingSystem.IsLinux()) return;`
because links behave differently on Windows): a fake home in `TestFolder`
with a native install and `~/.steam/steam` as a symbolic link to it
(`File.CreateSymbolicLink` / `Directory.CreateSymbolicLink`) → one root;
plus a Flatpak install → two roots, native first; a candidate without
`userdata` → skipped.

Commit: `Linux: Steam folders`

---

## H2 — `registry.vdf`

```csharp
/// <summary>true se registry.vdf dice che il gioco è aperto.</summary>
internal static bool RegistrySaysRunning(string registryText, string appId)
```

Parse with `Vdf.Parse`; true if
`Vdf.Find(node, "Registry", "HKCU", "Software", "Valve", "Steam", "RunningAppID") == appId`
or `Vdf.Find(node, "Registry", "HKCU", "Software", "Valve", "Steam", "Apps", appId, "Running") == "1"`.
Any exception → false.

Registry files to read: for each Steam root found, the matching
`registry.vdf` from the table (native → `~/.steam/registry.vdf`; Flatpak
and Snap → their own). Write a helper
`internal static IReadOnlyList<string> RegistryFiles(string home)` that
returns the existing ones among the three paths. Read them with
`File.ReadAllText` in try/catch (Steam may be writing it).

Tests: running by `RunningAppID`, running by `Apps/<id>/Running`, not
running, other game running, broken text → false.

Commit: `Linux: registry.vdf`

---

## H3 — Game processes

```csharp
/// <summary>true se l'ambiente del processo (contenuto di /proc/PID/environ) è di quel gioco.</summary>
internal static bool EnvironMatches(ReadOnlySpan<byte> environ, string appId)
```

Split on byte `0`, decode each entry as UTF-8, true if an entry is exactly
`SteamAppId=<appId>` or `SteamGameId=<appId>`.

```csharp
private List<(int Id, DateTime? Start)> FindGameProcesses(GameInfo game)
```

1. For each directory in `/proc` whose name is all digits (`pid`):
   - skip our own process (`Environment.ProcessId`);
   - cached result per pid (`Dictionary<int, bool>`, same idea as
     `WindowsPlatform.processPaths`; remove pids that disappeared);
   - if not cached: matches if `EnvironMatches(File.ReadAllBytes($"/proc/{pid}/environ"), game.AppId)`,
     or if `game.InstallPath != ""` and the link target of
     `/proc/{pid}/exe` (`new FileInfo(path).LinkTarget`) starts with
     `Path.GetFullPath(game.InstallPath) + "/"`. Every read in try/catch
     (permission denied, process ended) → not a match.
   - if it matches: start time with
     `using Process p = Process.GetProcessById(pid); p.StartTime` in
     try/catch → `null`.
2. Lock the cache like on Windows.

Interface:

- `IsGameRunning(game)`: any registry file says running **or**
  `FindGameProcesses(game).Count > 0`.
- `GameStartTime(game)`: earliest start, or `null`.
- `IsSteamRunning()`: `Process.GetProcessesByName("steam")` (dispose all)
  has at least one, or `steamwebhelper`.

Tests: `EnvironMatches` with `SteamAppId`, with `SteamGameId`, with a
similar but different id (`SteamAppId=1056001` must not match `105600`),
with an empty span.

Commit: `Linux: running game`

---

## H4 — Launching and single instance

- `OpenSteamUri(string uri)`:
  `Process.Start(new ProcessStartInfo("xdg-open", uri) { UseShellExecute = false })?.Dispose();`.
  `xdg-open` hands `steam://` links to the native or Flatpak Steam.
- `AcquireSingleInstance()`: lock file
  `Path.Combine(Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 } d ? d : Path.GetTempPath(), "lss.lock")`:

  ```csharp
  try
  {
      // FileShare.None su Linux prende un lock esclusivo (flock): una
      // seconda istanza non riesce ad aprirlo.
      return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
  }
  catch (IOException)
  {
      return null;
  }
  ```

- In `src/Lss.App/Program.cs` replace the `throw new PlatformNotSupportedException()`
  with `OperatingSystem.IsLinux() ? new LinuxPlatform() : throw new PlatformNotSupportedException()`.

Put the `try/catch` above in a helper
`internal static IDisposable? Lock(string path)` and make
`AcquireSingleInstance()` call it with the path above.

Test (Linux only), on the helper with a path in `TestFolder`: two calls →
first not null, second null; after disposing the first, a third is not
null.

Commit: `Linux: launch and single instance`

---

## H5 — Case of names

On Linux `Profilo_Mario` and `Profilo_mario` are two different folders. The
rule for LSS is: **two playthroughs can't have names that differ only by
case, on every system** (exports stay portable).

1. `SaveManager.ProfileExists(string profileName)` today is
   `Directory.Exists(Path.Combine(ProfilesPath, profileName))`, which on
   Linux is case-sensitive. Replace the body: true if any folder name in
   `ProfilesPath` (`Directory.GetDirectories` + `Path.GetFileName`, not
   only the valid ones of `GetProfiles()`) equals `profileName` with
   `StringComparison.OrdinalIgnoreCase`.
2. `UniqueName` (used by import and recovery) must use `ProfileExists`;
   check it does.
3. `RenameProfileCore`: the "case only" branch (`caseOnly`) must be taken
   when the names are equal ignoring case: check the comparison is
   `OrdinalIgnoreCase`, not a filesystem check.
4. `Info/<profile>.ini` and `Savestate/<profile>` follow the same rename
   (they do via `MoveInfo`): no change.

Tests (run on every system): create `Mario`, then creating `mario` fails
with `ProfileExists`; import of a zip named `mario` while `Mario` exists
gets the free name `mario 2` (`UniqueName` adds " 2", " 3"...);
rename `Mario` → `MARIO` works and moves note and points.

Commit: `Playthrough names ignore case on every system`

---

## H6 — Paths and permissions

1. Search Core and Platform for hard-coded Windows separators:

   ```bash
   grep -rn '\\\\' src/Lss.Core src/Lss.Platform --include=*.cs
   ```

   Every `\` used to build a path (not inside a comment, a log text, a
   regex or `WindowsPlatform`) must become `Path.Combine` or
   `Path.DirectorySeparatorChar`. List the changes in the commit message
   body.
2. Test (Linux only) in `FileOpsTests`: a file with mode `0o755`
   (`File.SetUnixFileMode`) copied with `FileOps.CopyFolderVerified` keeps
   the same mode (`File.GetUnixFileMode`). If it fails, add after each
   `File.Copy` in `FileOps` (only `if (!OperatingSystem.IsWindows())`)
   `File.SetUnixFileMode(destination, File.GetUnixFileMode(source));`.

Commit: `Linux: paths and file permissions`

---

## H7 — Linux tests required in CI

First fix the two tests that failed on Ubuntu in the C7 run (2026-10-24,
90 passed, 2 failed). Both are differences of the system, not bugs of the
app:

1. `AppDataTests.Move_LeavesOldFolderIntactWhenItFails` ("Expected:
   Failed, Actual: Moved"). The test blocks the move by keeping `log.txt`
   open with `FileShare.None`: on Linux an open file doesn't stop
   `Directory.Move`. Keep the current body for Windows and add, at the top
   of the method, a Linux variant that makes the move fail by putting a
   **file** where the parent of the new folder should be (then
   `Directory.CreateDirectory` inside `AppData.Move` throws, which is
   inside its `try`, so the result is `Failed` on every system):

   ```csharp
   if (!OperatingSystem.IsWindows())
   {
       WriteOldData();

       // Su Linux un file aperto non blocca lo spostamento: al posto della
       // cartella che deve contenere quella nuova c'è un file.
       string blocker = Path.Combine(folder.Root, "bloccata");
       File.WriteAllText(blocker, "");
       string target = Path.Combine(blocker, "LSS");

       (AppData.MoveResult result, string detail) = AppData.Move(Old, target);

       Assert.Equal(AppData.MoveResult.Failed, result);
       Assert.NotEqual("", detail);
       Assert.Equal("mario", File.ReadAllText(Path.Combine(Old, "123", "Profili", "Profilo_Mario", "remote", "slot.sav")));
       Assert.False(Directory.Exists(target));
       return;
   }
   ```

   Check the names `folder`, `Old` and `WriteOldData` against the test
   class before pasting.
2. `LocalizationTests.StoredDatesAreShownInTheCurrentCulture` (strings
   differ at position 18). On Linux .NET formats dates with ICU, which puts
   a narrow no-break space (U+202F) before `PM`. In the English assertion
   compare after normalising spaces:

   ```csharp
   // Su Linux (ICU) prima di AM/PM c'è uno spazio stretto U+202F.
   Assert.Equal("10/22/2026 6:30:00 PM",
       SaveManager.FormatStoredDate("22/10/2026 18:30:00").Replace(' ', ' '));
   ```

   Leave the app code as it is: the narrow space is correct output.

Then, in `.github/workflows/build.yml`, job `test`: remove the
`continue-on-error` line and its comment. If other tests fail on Ubuntu
by then, fix them the same way (often a path with `\` in the test itself:
use `Path.Combine`).

Commit: `CI: Linux tests required`

---

## H8 — 👤 Real Linux check (Andrix)

On a Linux PC or a VM (Ubuntu 24.04 and Fedora) and on the Steam Deck
(desktop mode), with Steam installed and a game with saves in `userdata`
(Terraria works on Linux natively):

- publish with `dotnet publish src/Lss.App -p:PublishProfile=linux-x64`
  and run `bin/publish/linux-x64/LSS`;
- the game appears, two playthroughs, switch, "Play this" starts the game
  through Steam, the game bar appears while it runs, play time and save on
  exit when it closes;
- with a Proton game: same;
- while a game runs, `grep -A3 '"Apps"' ~/.steam/registry.vdf` — write in
  `PROGRESS.md` whether `Running` is `1` (this confirms or not the fact
  marked "not yet verified");
- Steam as Flatpak, if possible.
