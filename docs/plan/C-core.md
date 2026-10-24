# Phase C — Core library and platform layer

Goal: the logic (`SaveManager` & co.) lives in a library `Lss.Core` that
targets plain `net10.0` and has **no Windows-only code**. Everything that
depends on the operating system goes through an interface `IPlatform`.
The WinForms app keeps working exactly as today.

Behaviour must not change in this phase. All 91 existing tests must keep
passing (some move or get renamed, none is deleted unless a task says so).

---

## C1 — Solution file

**Goal:** one solution with the app and the tests, so later phases can
build everything with `dotnet build LSS.slnx`.

Steps:

1. From the repository root:

   ```bash
   dotnet new sln --name LSS
   ```

   ```bash
   dotnet sln LSS.slnx add LSS.csproj tests/Lss.Tests/Lss.Tests.csproj
   ```

   The .NET 10 SDK creates `LSS.slnx` (the new XML solution format). If
   you get `LSS.sln` instead, the SDK is older than the one used for this
   plan (10.0.401): stop and write it in Notes.
2. Don't add `spike/LssSpike.csproj` to the solution.
3. In `.vscode/tasks.json` leave the paths as they are (they build
   `LSS.csproj`).

Check: `dotnet build LSS.slnx -c Release` and `dotnet test LSS.slnx -c Release`
(91 tests pass).

Commit: `Solution file`

---

## C2 — `IPlatform`, still inside the WinForms project

**Goal:** move every OS-specific call out of `SaveManager` behind an
interface, without moving files between projects yet.

### C2.1 Create `IPlatform.cs` (root folder)

```csharp
namespace Lss;

/// <summary>
/// Tutto quello che dipende dal sistema operativo: Steam, processi, avvio
/// del gioco, istanza unica. Un'implementazione per sistema
/// (WindowsPlatform, poi LinuxPlatform) e una finta per i test.
/// </summary>
public interface IPlatform
{
    /// <summary>
    /// Cartelle di Steam trovate, la principale per prima. Vuoto se Steam
    /// non è installato.
    /// </summary>
    IReadOnlyList<string> FindSteamRoots();

    /// <summary>true se il gioco è aperto (nel dubbio true).</summary>
    bool IsGameRunning();

    /// <summary>Quando è partito il gioco aperto, se si riesce a leggerlo.</summary>
    DateTime? GameStartTime();

    bool IsSteamRunning();

    /// <summary>Apre un indirizzo steam:// (Steam si apre da solo se è chiuso).</summary>
    void OpenSteamUri(string uri);

    /// <summary>
    /// Prende il diritto di essere l'unica istanza dell'app. null se un'altra
    /// istanza è già aperta; altrimenti un oggetto da tenere fino alla chiusura.
    /// </summary>
    IDisposable? AcquireSingleInstance();
}
```

(`IsGameRunning` and `GameStartTime` have no parameters on purpose: phase E
adds the game. Keep this exact interface now.)

### C2.2 Create `Platform.cs` (root folder)

```csharp
namespace Lss;

/// <summary>
/// Implementazione di IPlatform in uso. Program la imposta all'avvio, i test
/// mettono FakePlatform.
/// </summary>
public static class Platform
{
    private static IPlatform? current;

    public static IPlatform Current
    {
        get => current ?? throw new InvalidOperationException("Platform.Current non impostata.");
        set => current = value;
    }
}
```

### C2.3 Create `WindowsPlatform.cs` (root folder)

Class `public sealed class WindowsPlatform : IPlatform`, with
`[System.Runtime.Versioning.SupportedOSPlatform("windows")]` on the class.
Move code **from `SaveManager.cs`** (cut, don't copy):

| From `SaveManager.cs` | To `WindowsPlatform` |
|---|---|
| `FindSteam()` (lines ~315-338) | `FindSteamRoots()`: same logic, but returns `IReadOnlyList<string>`: `[path]` if found, `[]` if not. |
| `IsGameRunning()`, `IsGameLauncherRunning()`, `HasExited()`, `IsProcessRunning()` | same bodies; `IsGameRunning()` becomes the interface method (not static). `IsGameLauncherRunning`, `HasExited`, `IsProcessRunning` become `private static`. |
| `IsGameLauncher(string? file, string windowsFolder)` | `internal static bool IsGameLauncher(...)`, same body. |
| `IsSteamRunning()` | interface method. |

Move **from `SaveManager.Playthroughs.cs`**:

| From | To `WindowsPlatform` |
|---|---|
| body of `GameStartTime()` | `GameStartTime()` (interface method). |
| the `Process.Start(new ProcessStartInfo("steam://rungameid/" + AppId) { UseShellExecute = true })?.Dispose();` line | `OpenSteamUri(string uri)`: `Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true })?.Dispose();` |

Add `AcquireSingleInstance()`:

```csharp
public IDisposable? AcquireSingleInstance()
{
    // Il nome resta quello di Control Save Manager finché i dati sono nella
    // sua cartella: così la vecchia versione e LSS non possono essere aperte
    // insieme.
    var mutex = new Mutex(true, @"Local\ControlSaveManager", out bool first);

    if (first)
        return mutex;

    mutex.Dispose();
    return null;
}
```

Copy the XML comments that were on the moved methods; keep them in Italian.
`WindowsPlatform.cs` needs `using Microsoft.Win32;` and
`using System.Diagnostics;`.

### C2.4 Update `SaveManager`

Keep the public static methods so the forms don't change, but make them
delegate:

```csharp
public static bool IsGameRunning() => Platform.Current.IsGameRunning();

public static bool IsSteamRunning() => Platform.Current.IsSteamRunning();
```

In `SaveManager.Playthroughs.cs`:

```csharp
public static DateTime? GameStartTime() => Platform.Current.GameStartTime();
```

and in `LaunchGame()` replace the `Process.Start(...)` line with
`Platform.Current.OpenSteamUri("steam://rungameid/" + AppId);`.

In `Initialize(...)` replace `string steam = FindSteam();` and the
`if (steam == "")` check with:

```csharp
string steam = Platform.Current.FindSteamRoots().FirstOrDefault() ?? "";
```

(the `if (steam == "")` check that follows stays as it is).

Remove `using Microsoft.Win32;` from `SaveManager.cs`. If
`System.Diagnostics` is no longer used in a file, remove that `using` too.

### C2.5 Update `Program.cs`

1. First line inside `Main()`, before everything else:
   `Platform.Current = new WindowsPlatform();`
2. Replace

   ```csharp
   using Mutex mutex = new(true, @"Local\ControlSaveManager", out bool firstInstance);

   if (!firstInstance)
   ```

   (and the comment above it) with

   ```csharp
   // Due finestre aperte lavorerebbero sugli stessi file.
   using IDisposable? instance = Platform.Current.AcquireSingleInstance();

   if (instance == null)
   ```

### C2.6 Tests

1. Create `tests/Lss.Tests/FakePlatform.cs`:

   ```csharp
   namespace Lss.Tests;

   /// <summary>
   /// IPlatform per i test: niente registro, processi o Steam veri. I campi
   /// si impostano dal test.
   /// </summary>
   public sealed class FakePlatform : IPlatform
   {
       public List<string> SteamRoots { get; } = [];
       public bool GameRunning { get; set; }
       public DateTime? StartTime { get; set; }
       public bool SteamRunning { get; set; }
       public List<string> OpenedUris { get; } = [];

       public IReadOnlyList<string> FindSteamRoots() => SteamRoots;
       public bool IsGameRunning() => GameRunning;
       public DateTime? GameStartTime() => StartTime;
       public bool IsSteamRunning() => SteamRunning;
       public void OpenSteamUri(string uri) => OpenedUris.Add(uri);
       public IDisposable? AcquireSingleInstance() => new MemoryStream();
   }
   ```

2. In `TestFolder.cs`, in class `TestSetup`, add a property and set it as
   first line of `Init()`:

   ```csharp
   /// <summary>Piattaforma finta usata da tutti i test.</summary>
   public static FakePlatform Fake { get; } = new();
   ```

   ```csharp
   Platform.Current = Fake;
   ```

   Tests run in parallel: **never assign `Platform.Current` inside a
   test**. Read or change `TestSetup.Fake` instead, and only for values no
   other test depends on.
3. In `MinorBugTests.cs`, test `GameLauncherIsRecognized`: replace
   `SaveManager.IsGameLauncher` with `WindowsPlatform.IsGameLauncher` (both
   lines). Add at the top of the method body:
   `if (!OperatingSystem.IsWindows()) return;` (the paths are Windows paths).
4. Add `tests/Lss.Tests/PlatformTests.cs` with one test
   `LaunchGameOpensSteamUri`: call `SaveManager.LaunchGame()`, then
   `Assert.True(result.Success)` and
   `Assert.Contains("steam://rungameid/870780", TestSetup.Fake.OpenedUris)`.
   Also `TestSetup` is `internal`: if the compiler complains that
   `FakePlatform` is less accessible, make `FakePlatform` `internal` too.

Check: build, tests (92 tests). Then start the app once
(`dotnet run --project LSS.csproj`) only if Andrix is there to see it;
otherwise skip, it's checked in C7.

Commit: `IPlatform: OS-specific code out of SaveManager`

---

## C3 — Card texts out of the WinForms control

**Goal:** the text helpers tested by `CardTests` must not live in a WinForms
class, so the tests can stop depending on WinForms.

1. Create `CardText.cs` (root folder) with:
   - the record `CardData` moved from `PlaythroughControls.cs` (lines
     ~251-256), unchanged, still `internal sealed record`;
   - `internal static class CardText` with the three methods moved from
     `PlaythroughCard`: `Details(CardData data)`,
     `FormatPlayTime(TimeSpan time)`, `FormatSaved(DateTime saved)`, bodies
     unchanged, with their comments. XML summary of the class:
     `/// Testi delle schede delle partite (dettagli, tempo di gioco, ultimo salvataggio).`
2. In `PlaythroughControls.cs` and `MainForm.cs` replace every
   `PlaythroughCard.Details(`, `PlaythroughCard.FormatPlayTime(`,
   `PlaythroughCard.FormatSaved(` and the unqualified calls inside
   `PlaythroughCard` (`Details(`, `FormatPlayTime(`, `FormatSaved(`) with
   `CardText.Details(` etc. Use Grep to find them all.
3. In `tests/Lss.Tests/CardTests.cs` replace `PlaythroughCard.` with
   `CardText.`.

Check: build, tests.

Commit: `Card texts in CardText`

---

## C4 — Projects `Lss.Core` and `Lss.Platform`

**Goal:** move the files into two libraries.

### C4.1 Create the projects

Create `src/Lss.Core/Lss.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Lss</RootNamespace>
    <AssemblyName>Lss.Core</AssemblyName>
    <NeutralLanguage>en</NeutralLanguage>
  </PropertyGroup>

  <ItemGroup>
    <!-- Genera la classe Strings dai testi, senza bisogno di Visual Studio. -->
    <EmbeddedResource Update="Strings.resx"
                      StronglyTypedLanguage="CSharp"
                      StronglyTypedClassName="Strings"
                      StronglyTypedNamespace="Lss"
                      StronglyTypedFileName="$(IntermediateOutputPath)Strings.Designer.cs" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Lss.Platform" />
    <InternalsVisibleTo Include="LSS" />
    <InternalsVisibleTo Include="Lss.App" />
    <InternalsVisibleTo Include="Lss.Tests" />
  </ItemGroup>

</Project>
```

Create `src/Lss.Platform/Lss.Platform.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Lss</RootNamespace>
    <AssemblyName>Lss.Platform</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Lss.Core\Lss.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Lss.Tests" />
  </ItemGroup>

</Project>
```

### C4.2 Move the files with `git mv`

Into `src/Lss.Core/`: `SaveManager.cs`, `SaveManager.Playthroughs.cs`,
`FileOps.cs`, `IniFile.cs`, `Vdf.cs`, `AppState.cs`, `AppData.cs`, `Log.cs`,
`Result.cs`, `Loc.cs`, `CardText.cs`, `IPlatform.cs`, `Platform.cs`,
`Strings.resx`, `Strings.it.resx`.

Into `src/Lss.Platform/`: `WindowsPlatform.cs`.

Everything else stays at the root (the WinForms app).

### C4.3 Update the WinForms project `LSS.csproj`

1. Add `src/**` to `DefaultItemExcludes` (same line as `tests/**`).
2. Remove the `<EmbeddedResource Update="Strings.resx" ...>` element (the
   texts are now in Core). Keep the `app.ico` resource.
3. Remove `<InternalsVisibleTo Include="Lss.Tests" />` and its `ItemGroup`
   if empty.
4. Add:

   ```xml
   <ItemGroup>
     <ProjectReference Include="src\Lss.Core\Lss.Core.csproj" />
     <ProjectReference Include="src\Lss.Platform\Lss.Platform.csproj" />
   </ItemGroup>
   ```

### C4.4 Update the tests project

In `tests/Lss.Tests/Lss.Tests.csproj` add references to
`..\..\src\Lss.Core\Lss.Core.csproj` and
`..\..\src\Lss.Platform\Lss.Platform.csproj` (keep the reference to
`LSS.csproj` for now).

### C4.5 Solution

```bash
dotnet sln LSS.slnx add src/Lss.Core/Lss.Core.csproj src/Lss.Platform/Lss.Platform.csproj
```

### C4.6 Fix what breaks

- `Strings` and `Loc` are `internal` in Core: `InternalsVisibleTo` covers
  `LSS`, `Lss.Platform` and the tests. If the compiler says a type is
  inaccessible, check the `InternalsVisibleTo` names match the
  `AssemblyName`s exactly.
- Platform warnings `CA1416` must not appear outside `WindowsPlatform`
  (which has `[SupportedOSPlatform("windows")]`). If a `CA1416` warning
  appears in Core, some Windows-only call was left in Core: move it to
  `WindowsPlatform` and stop if it isn't obvious.
- `Program.Version` is used by the forms only: it stays in `Program.cs`.

Check: build with **0 warnings**, tests pass, and:

```bash
grep -rn "Microsoft.Win32\|System.Windows.Forms\|Process\.Start" src/Lss.Core
```

must print nothing.

Commit: `Lss.Core and Lss.Platform libraries`

---

## C5 — Tests on plain `net10.0`

1. In `tests/Lss.Tests/Lss.Tests.csproj`:
   - `<TargetFramework>net10.0</TargetFramework>`;
   - remove `<UseWindowsForms>true</UseWindowsForms>`;
   - remove the `ProjectReference` to `..\..\LSS.csproj`.
2. Build the tests. If something still uses a WinForms type, it belongs to
   the UI: stop and write it in Notes.

Check: tests pass (92).

Commit: `Tests no longer depend on WinForms`

---

## C6 — Tests on Linux in GitHub Actions

Edit `.github/workflows/build.yml`:

1. Add a new job **before** `build`:

   ```yaml
     test:
       strategy:
         fail-fast: false
         matrix:
           os: [windows-latest, ubuntu-latest]
       runs-on: ${{ matrix.os }}
       # Linux: some tests may fail until phase H is done.
       continue-on-error: ${{ matrix.os == 'ubuntu-latest' }}
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with:
             dotnet-version: 10.0.x
         - name: Test
           run: dotnet test tests/Lss.Tests -c Release
   ```

   Workflow comments are in English, like the rest of the file.
2. In job `build` add `needs: test` under `runs-on`.
3. Keep the existing `Test` step in `build` (it produces the `.trx`).

Check: the YAML is valid (`dotnet` can't check it; re-read indentation
carefully, 2 spaces per level as in the rest of the file).

Commit: `CI: tests on Windows and Linux`

---

## C7 — 👤 Check the WinForms app (Andrix)

Start `dotnet run --project LSS.csproj` on the real PC: the window opens,
the playthroughs are there, "Play this" still works with Control. Push the
branch and look at the `test` job on GitHub: note in `PROGRESS.md` which
tests fail on Ubuntu (they are fixed in phase H).
