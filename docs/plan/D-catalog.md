# Phase D — Game catalogue (`SteamLibrary`)

Goal: given a Steam folder and an account, list the games that have a
folder in `userdata/<AccountID>/`, with name, install folder, library,
image and whether LSS can manage them. Nothing in the UI uses it yet
(phase E and G do).

All new files go in `src/Lss.Core/`. Tests in
`tests/Lss.Tests/SteamLibraryTests.cs`.

## Facts about Steam files (checked on a real PC)

`<Steam>/steamapps/libraryfolders.vdf` (paths use `\\` on Windows, `/` on
Linux; `Vdf.Parse` already handles `\\` escapes):

```
"libraryfolders"
{
	"0"
	{
		"path"		"C:\\Program Files (x86)\\Steam"
		"apps"
		{
			"105600"		"803964902"
		}
	}
	"1"
	{
		"path"		"I:\\SteamLibrary"
		...
	}
}
```

`<library>/steamapps/appmanifest_<AppID>.acf`:

```
"AppState"
{
	"appid"		"105600"
	"name"		"Terraria"
	"installdir"		"Terraria"
	...
}
```

`<Steam>/appcache/librarycache/<AppID>/` contains, among others, the
32×32 game icon as a `.jpg` file whose name is 40 hex characters (for
example `858961e95fbf869f136e1770d586e0caefd4cfac.jpg`), `library_600x900.jpg`
(cover, 300×450) and sometimes `header.jpg`. Older Steam versions instead
have `<Steam>/appcache/librarycache/<AppID>_icon.jpg` and
`<AppID>_library_600x900.jpg` directly in `librarycache`.

`userdata/<AccountID>/` contains one folder per AppID plus non-game folders:
`7` (Steam client settings), `760` (screenshots), `241100` (controller
settings), `config`, `ugc`, `gamerecordings`, `inventorymsgcache`,
`ugcmsgcache`, `ugsmsgcache`. A game whose saves are elsewhere (Steam
Auto-Cloud) has only `remotecache.vdf` in its folder.

---

## D1 — `GameInfo` and `SupportState`

Create `src/Lss.Core/GameInfo.cs`:

```csharp
namespace Lss;

/// <summary>
/// Se LSS può gestire i salvataggi di un gioco.
/// </summary>
public enum SupportState
{
    /// <summary>Ci sono salvataggi in userdata: LSS li gestisce.</summary>
    Supported,

    /// <summary>Solo remotecache.vdf: il gioco salva altrove (Steam Auto-Cloud).</summary>
    SavesElsewhere,

    /// <summary>Cartella vuota: nessun salvataggio.</summary>
    NoSaves
}

/// <summary>
/// Gioco Steam con una cartella in userdata.
/// </summary>
/// <param name="AppId">Numero del gioco su Steam (es. "870780").</param>
/// <param name="Name">Nome dal manifest, o quello noto, o "App 870780".</param>
/// <param name="InstallDir">Cartella in steamapps/common, o "" se non è installato.</param>
/// <param name="LibraryPath">Libreria Steam che lo contiene, o "" se non è installato.</param>
/// <param name="SaveFolder">Cartella userdata/IDAccount/AppID.</param>
/// <param name="Support">Se LSS può gestirlo.</param>
/// <param name="IconPath">Icona 32×32, o "" se non c'è.</param>
/// <param name="CoverPath">Copertina 300×450, o "" se non c'è.</param>
public sealed record GameInfo(
    string AppId,
    string Name,
    string InstallDir,
    string LibraryPath,
    string SaveFolder,
    SupportState Support,
    string IconPath = "",
    string CoverPath = "")
{
    public const string ControlAppId = "870780";

    /// <summary>true se il gioco è installato in una libreria trovata.</summary>
    public bool IsInstalled => InstallDir != "" && LibraryPath != "";

    /// <summary>Cartella del gioco installato, o "" se non è installato.</summary>
    public string InstallPath => IsInstalled
        ? Path.Combine(LibraryPath, "steamapps", "common", InstallDir)
        : "";
}
```

No test needed for this task alone (it is data). Commit:
`GameInfo and SupportState`

---

## D2 — Library folders

Create `src/Lss.Core/SteamLibrary.cs` with
`internal static class SteamLibrary` and the first method:

```csharp
/// <summary>
/// Librerie di Steam da steamapps/libraryfolders.vdf, la cartella di Steam
/// per prima. Solo le cartelle che esistono, senza doppioni.
/// </summary>
public static IReadOnlyList<string> LibraryFolders(string steamRoot)
```

Algorithm:

1. `var result = new List<string> { steamRoot };`
2. File `Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf")`. If it
   doesn't exist, return `result`.
3. `Vdf.Node root = Vdf.Parse(File.ReadAllText(file));` inside a
   `try { } catch (Exception ex) { Log.Write("Librerie di Steam non lette", ex); return result; }`.
4. Take `root["libraryfolders"]` as `Vdf.Node` (if missing or not a Node,
   return `result`). For each value that is a `Vdf.Node`, read its `"path"`
   string. Skip empty ones.
5. Add the path if `Directory.Exists(path)` and it is not already in the
   list. Compare with `NormalizePath(a) == NormalizePath(b)` where

   ```csharp
   private static string NormalizePath(string path) =>
       Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
   ```

   and the comparison is `StringComparison.OrdinalIgnoreCase` on Windows,
   `StringComparison.Ordinal` elsewhere (`OperatingSystem.IsWindows()`).
6. Return `result`.

Check `Log.Write(string, Exception)` exists in `Log.cs` (it does in the
current code: `Log.Write("...", ex)`).

Tests (`SteamLibraryTests`, class with a `TestFolder folder` field and
`Dispose` like `ProfileTests`):

- `LibraryFolders_WithoutFile_ReturnsSteamRoot`: only the Steam folder.
- `LibraryFolders_ReadsAllExistingPaths`: write a `libraryfolders.vdf` with
  three entries: the Steam root itself, an existing second folder
  (`folder.Root/Lib2`, create it), a missing folder. Expect
  `[steamRoot, lib2]`. Write paths in the file with
  `path.Replace("\\", "\\\\")` so it is valid VDF on Windows.
- `LibraryFolders_BrokenFile_ReturnsSteamRoot`: file content
  `"libraryfolders" { "0" {` → `[steamRoot]`, no exception.

Commit: `SteamLibrary: library folders`

---

## D3 — Manifests

Add to `SteamLibrary`:

```csharp
/// <summary>
/// Nome e cartella di installazione dal manifest del gioco, cercato in
/// tutte le librerie. null se il gioco non è installato.
/// </summary>
public static (string Name, string InstallDir, string Library)? ReadManifest(
    IReadOnlyList<string> libraries, string appId)
```

For each library: file
`Path.Combine(library, "steamapps", "appmanifest_" + appId + ".acf")`. If it
exists, parse it (try/catch → log and continue with the next library), read
`Vdf.Find(node, "AppState", "name")` and
`Vdf.Find(node, "AppState", "installdir")`. If `name` is not empty, return
`(name, installdir ?? "", library)`. If no library has it, return `null`.

Tests:

- `ReadManifest_FindsGameInSecondLibrary`
- `ReadManifest_MissingGame_ReturnsNull`
- `ReadManifest_BrokenManifest_IsSkipped` (broken in library 1, good in
  library 2 → returns library 2).

Commit: `SteamLibrary: manifests`

---

## D4 — Support state

Add to `SteamLibrary`:

```csharp
/// <summary>
/// Se LSS può gestire la cartella dei salvataggi di un gioco. Conta ogni
/// file a qualsiasi profondità, tranne remotecache.vdf al primo livello
/// (come FileOps.HasNoSaveFiles).
/// </summary>
public static SupportState GetSupport(string saveFolder)
```

Rules:

1. Folder missing or no files at all → `NoSaves`.
2. `FileOps.HasNoSaveFiles(saveFolder)` is `true` but the folder has
   `remotecache.vdf` at the top level → `SavesElsewhere`.
3. `FileOps.HasNoSaveFiles(saveFolder)` is `false` → `Supported`.
4. Any exception reading the folder → log
   (`Log.Write("Cartella non leggibile: " + saveFolder, ex)`) and return
   `NoSaves`.

First read `FileOps.HasNoSaveFiles` in `FileOps.cs` to confirm it ignores
only the top-level `remotecache.vdf`. If it doesn't, stop and write a note.

Tests (one each): `GetSupport_FilesInRemote_IsSupported`,
`GetSupport_OnlyRemoteCache_IsSavesElsewhere`,
`GetSupport_EmptyFolder_IsNoSaves`, `GetSupport_MissingFolder_IsNoSaves`,
`GetSupport_NestedRemoteCache_IsSupported` (a file
`remote/remotecache.vdf` counts as a save).

Commit: `SteamLibrary: support state`

---

## D5 — Images

Add to `SteamLibrary`:

```csharp
/// <summary>
/// Icona (32×32) e copertina (300×450) del gioco dalla cache di Steam.
/// "" se non ci sono.
/// </summary>
public static (string Icon, string Cover) FindImages(string steamRoot, string appId)
```

Look in `cache = Path.Combine(steamRoot, "appcache", "librarycache")`:

- **Icon**, first match wins:
  1. in folder `Path.Combine(cache, appId)`, the first `*.jpg` whose file
     name without extension is exactly 40 characters, all in `0-9a-f`
     (order the candidates by name to be deterministic);
  2. `Path.Combine(cache, appId + "_icon.jpg")`.
- **Cover**, first match wins:
  1. `Path.Combine(cache, appId, "library_600x900.jpg")`;
  2. `Path.Combine(cache, appId + "_library_600x900.jpg")`.
- Everything inside `try/catch` → `("", "")` on error, no log needed.

Tests: new layout (folder with a 40-hex jpg, a `logo.png` and
`library_600x900.jpg`), old layout (files in `librarycache`), nothing →
`("", "")`. The files can be empty: only names matter.

Commit: `SteamLibrary: icon and cover`

---

## D6 — List the games of an account

Add to `SteamLibrary`:

```csharp
// Cartelle di userdata che non sono giochi.
private static readonly HashSet<string> SystemApps = ["7", "760", "241100"];

// Nomi dei manifest che non sono giochi (runtime di Steam e di Proton).
private static readonly string[] SystemNamePrefixes =
[
    "Proton", "Steam Linux Runtime", "Steamworks Common Redistributables"
];

// Nomi noti dei giochi senza manifest (disinstallati ma con i salvataggi).
private static readonly Dictionary<string, string> KnownNames = new()
{
    [GameInfo.ControlAppId] = "Control"
};

/// <summary>
/// Giochi con una cartella in userdata/IDAccount, i supportati per primi,
/// poi in ordine di nome.
/// </summary>
public static IReadOnlyList<GameInfo> FindGames(string steamRoot, string accountFolder)
```

Algorithm:

1. `libraries = LibraryFolders(steamRoot)`.
2. For each subfolder of `accountFolder` (`Directory.GetDirectories`):
   - `appId = Path.GetFileName(dir)`; skip if it isn't all digits
     (`appId.All(char.IsAsciiDigit)`), if it is in `SystemApps`, or if it
     ends with `FileOps.OldSuffix` / `FileOps.TempSuffix` (impossible for
     digits only, but keep the digits check first).
   - `manifest = ReadManifest(libraries, appId)`.
   - If the manifest name starts with one of `SystemNamePrefixes`
     (`StringComparison.OrdinalIgnoreCase`), skip.
   - `name = manifest?.Name ?? KnownNames.GetValueOrDefault(appId) ?? Loc.F(Strings.UnknownGameName, appId)`.
   - `(icon, cover) = FindImages(steamRoot, appId)`.
   - Add `new GameInfo(appId, name, manifest?.InstallDir ?? "", manifest?.Library ?? "", dir, GetSupport(dir), icon, cover)`.
3. Return the list ordered by `Support != SupportState.Supported`, then by
   `Name` with `StringComparer.CurrentCultureIgnoreCase`.
4. If `accountFolder` doesn't exist, return an empty list.

New text, in both resx files. The keys in the resx files are in
alphabetical order: insert the new `<data>` line at its alphabetical place,
with the same one-line format as the lines around it:

| Key | English | Italian |
|---|---|---|
| `UnknownGameName` | `App {0}` | `App {0}` |

Tests:

- `FindGames_ListsGamesWithNamesAndSupport`: account folder with
  `105600/remote/a.sav` (manifest "Terraria" in the Steam library),
  `814380/remotecache.vdf` (manifest "Sekiro" in a second library),
  `7/remote/sharedconfig.vdf`, `config/localconfig.vdf`, `241100/x.vdf`.
  Expect two games: Terraria (Supported, `InstallDir` from manifest) first,
  Sekiro (SavesElsewhere) second.
- `FindGames_UsesKnownNameWithoutManifest`: folder `870780/remote/x` and no
  manifest → `Name == "Control"`, `IsInstalled == false`.
- `FindGames_UnknownGameWithoutManifest`: folder `123450/remote/x` → name
  `"App 123450"`.
- `FindGames_SkipsProtonAndRuntimes`: folder `1391110/remote/x` with
  manifest name `Steam Linux Runtime 2.0 (soldier)` → not listed.

Commit: `SteamLibrary: games of an account`

---

## D7 — Docs

In `docs/DEVELOPMENT.md`, after the section "Paths", add a section
"## Steam libraries and games" (English) explaining in 10–15 lines:
`libraryfolders.vdf`, `appmanifest_<AppID>.acf`, the support rule, the
skipped folders and the image cache, as in "Facts" above. Add
`SteamLibrary.cs` and `GameInfo.cs` to the "Code structure" table with the
path `src/Lss.Core/`.

Commit: `Docs: Steam libraries and games`
