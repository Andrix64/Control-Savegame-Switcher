# Phase I — Packages and GitHub Actions

Goal: every tagged build produces, in a draft GitHub release:

| File | For |
|---|---|
| `LSS-<v>-win-x64.zip` (+ `.sha256`) | Windows, exe signed by SignPath |
| `LSS-<v>-linux-x64.tar.gz` (+ `.sha256`) | any Linux |
| `lss_<v>_amd64.deb` (+ `.sha256`) | Ubuntu, Debian, Mint, Pop!_OS |
| `lss-<v>-1.x86_64.rpm` (+ `.sha256`) | Fedora (openSUSE may need `--nodeps`, see I4) |
| `LSS-<v>-x86_64.AppImage` (+ `.sha256`) | any Linux, Steam Deck |

App ID used by the Linux desktop files: **`io.github.andrix64.LocalSaveSwitcher`**.

---

## I1 — `--version`

Packages are checked in CI by running the installed program with
`--version`. In `src/Lss.App/Program.cs`, at the start of `Main`:

```csharp
// Per i controlli dei pacchetti: stampa la versione ed esce, senza finestre.
if (args.Contains("--version"))
{
    Console.WriteLine("LSS " + Version);
    return 0;
}
```

Commit: `--version option`

---

## I2 — Linux desktop files and icons

1. Icons: on Windows, with PowerShell, extract the PNG sizes from
   `src/Lss.App/Assets/app.ico` (it contains 16, 24, 32, 48, 64, 256):

   ```powershell
   Add-Type -AssemblyName System.Drawing
   foreach ($s in 16, 24, 32, 48, 64, 256) {
       New-Item -ItemType Directory -Force "packaging/linux/icons/${s}x${s}" | Out-Null
       $icon = New-Object System.Drawing.Icon("src/Lss.App/Assets/app.ico", $s, $s)
       $icon.ToBitmap().Save("packaging/linux/icons/${s}x${s}/io.github.andrix64.LocalSaveSwitcher.png", [System.Drawing.Imaging.ImageFormat]::Png)
       $icon.Dispose()
   }
   ```

   Check each PNG has the right size (open one with the Read tool: it
   shows images).
2. `packaging/linux/io.github.andrix64.LocalSaveSwitcher.desktop`:

   ```ini
   [Desktop Entry]
   Type=Application
   Name=Local Save Switcher
   GenericName=Save switcher
   Comment=Multiple playthroughs for Steam games
   Comment[it]=Più partite per i giochi Steam
   Exec=lss
   Icon=io.github.andrix64.LocalSaveSwitcher
   Terminal=false
   Categories=Game;Utility;
   Keywords=steam;save;savegame;playthrough;
   ```

3. `packaging/linux/io.github.andrix64.LocalSaveSwitcher.metainfo.xml`
   (AppStream, used by GNOME Software / KDE Discover and Flathub):

   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <component type="desktop-application">
     <id>io.github.andrix64.LocalSaveSwitcher</id>
     <name>Local Save Switcher</name>
     <summary>Multiple playthroughs for Steam games</summary>
     <metadata_license>CC0-1.0</metadata_license>
     <project_license>MIT</project_license>
     <developer id="io.github.andrix64">
       <name>Andrix64</name>
     </developer>
     <description>
       <p>Keep several playthroughs of the same Steam game on one account: pick
       the one you want, LSS loads its saves and starts the game. Works with
       games that keep their saves in Steam's userdata folder.</p>
       <p>Steam is a trademark of Valve Corporation. LSS is not affiliated with
       or endorsed by Valve.</p>
     </description>
     <launchable type="desktop-id">io.github.andrix64.LocalSaveSwitcher.desktop</launchable>
     <url type="homepage">https://github.com/Andrix64/Local-Save-Switcher</url>
     <url type="bugtracker">https://github.com/Andrix64/Local-Save-Switcher/issues</url>
     <content_rating type="oars-1.1" />
     <releases>
       <release version="1.0.0" date="2026-12-01" />
     </releases>
   </component>
   ```

   The release date is a placeholder: J4 updates it.
4. `packaging/linux/lss.sh` (launcher installed as `/usr/bin/lss` by the
   packages; the real program is in `/usr/lib/lss/`):

   ```sh
   #!/bin/sh
   exec /usr/lib/lss/LSS "$@"
   ```

Commit: `Linux desktop entry, metainfo and icons`

---

## I3 — `scripts/release.sh`

A bash script, the Linux equivalent of `scripts/release.ps1` (read it and
mirror its structure and messages, in Italian like that one). Starts with
`#!/usr/bin/env bash` and `set -euo pipefail`. Options: `--skip-tests`,
`--allow-dirty` (same meaning as in `release.ps1`).

Steps:

1. `root` = repository root (`$(cd "$(dirname "$0")/.." && pwd)`), then
   `cd "$root"`: nfpm reads the paths in `nfpm.yaml` relative to the
   current folder.
2. Refuse uncommitted changes unless `--allow-dirty`.
3. `version=$(dotnet msbuild "$root/src/Lss.App/Lss.App.csproj" -getProperty:Version)`.
4. Tests unless `--skip-tests`.
5. `dotnet publish "$root/src/Lss.App/Lss.App.csproj" -p:PublishProfile=linux-x64 --nologo`
   → `bin/publish/linux-x64/LSS`.
6. `stage=dist/LSS-$version-linux-x64`: copy `LSS`, `README.md`,
   `CHANGELOG.md`, `LICENSE`, the desktop file, the metainfo, the icons
   folder, and an `install.sh` (step 7). `chmod +x`.
   `tar -C dist -czf dist/LSS-$version-linux-x64.tar.gz LSS-$version-linux-x64`.
7. `install.sh` (written by the script into the stage folder) installs for
   the current user only, no root: `~/.local/lib/lss/LSS`,
   `~/.local/bin/lss` (symlink), desktop file in
   `~/.local/share/applications/`, icons in
   `~/.local/share/icons/hicolor/<size>/apps/`. With `--uninstall` it
   removes them. It prints where it installed things and reminds to add
   `~/.local/bin` to `PATH` if it isn't.
8. `.deb` and `.rpm` with nfpm (I4): `nfpm package --config packaging/linux/nfpm.yaml --packager deb --target dist/`
   and the same with `--packager rpm`, with environment variable
   `VERSION=$version` (nfpm reads `${VERSION}` in the config).
9. AppImage (I5).
10. `sha256sum` of each file into `<file>.sha256`, from inside `dist`
    (so the file contains only the file name, like on Windows).

Commit: `Linux release script`

---

## I4 — `packaging/linux/nfpm.yaml`

```yaml
# deb and rpm packages, built by scripts/release.sh with nfpm.
name: lss
arch: amd64
platform: linux
version: ${VERSION}
version_schema: semver
maintainer: Andrix64 <MAINTAINER_EMAIL>
description: Multiple playthroughs for Steam games.
homepage: https://github.com/Andrix64/Local-Save-Switcher
license: MIT

contents:
  - src: ./bin/publish/linux-x64/LSS
    dst: /usr/lib/lss/LSS
    file_info:
      mode: 0755
  - src: ./packaging/linux/lss.sh
    dst: /usr/bin/lss
    file_info:
      mode: 0755
  - src: ./packaging/linux/io.github.andrix64.LocalSaveSwitcher.desktop
    dst: /usr/share/applications/io.github.andrix64.LocalSaveSwitcher.desktop
  - src: ./packaging/linux/io.github.andrix64.LocalSaveSwitcher.metainfo.xml
    dst: /usr/share/metainfo/io.github.andrix64.LocalSaveSwitcher.metainfo.xml
  - src: ./packaging/linux/icons/256x256/io.github.andrix64.LocalSaveSwitcher.png
    dst: /usr/share/icons/hicolor/256x256/apps/io.github.andrix64.LocalSaveSwitcher.png
  # ... one entry per icon size (16, 24, 32, 48, 64)
  - src: ./LICENSE
    dst: /usr/share/doc/lss/LICENSE

overrides:
  deb:
    depends:
      - libx11-6
      - libice6
      - libsm6
      - libfontconfig1
      - libicu76 | libicu74 | libicu72 | libicu70
  rpm:
    depends:
      - libX11
      - libICE
      - libSM
      - fontconfig
      - libicu
```

Write out all six icon entries (don't leave the `# ...` comment).

`MAINTAINER_EMAIL`: the address is public inside every package. **Ask
Andrix** which one to use (for example a GitHub `noreply` address) and
don't guess; until then leave the placeholder and note it in
`PROGRESS.md`.

`libicu`: .NET needs ICU for languages and dates (Italian formatting). The
deb names change with the distribution version; the list covers Debian 12
(72), Debian 13 (76), Ubuntu 22.04 (70), Ubuntu 24.04 (74). On openSUSE
the package is `libicu-suse65_1` or similar: the rpm targets Fedora;
openSUSE users use the AppImage or `rpm -i --nodeps`. Write this in the
README in J2.

nfpm version: install it in CI from its GitHub releases (I6). Check the
latest release tag with `gh release view -R goreleaser/nfpm --json tagName,publishedAt`
and use it only if it was published at least two weeks ago, otherwise the
previous one. Write the version in the workflow, never `latest`.

Commit: `deb and rpm packages`

---

## I5 — AppImage

In `scripts/release.sh`, after the packages:

1. AppDir `dist/LSS.AppDir`:
   - `usr/lib/lss/LSS`
   - `usr/share/applications/io.github.andrix64.LocalSaveSwitcher.desktop`
   - `usr/share/metainfo/io.github.andrix64.LocalSaveSwitcher.metainfo.xml`
   - `usr/share/icons/hicolor/<size>/apps/...png` (all sizes)
   - at the root: a copy of the desktop file, a copy of the 256 icon named
     `io.github.andrix64.LocalSaveSwitcher.png`, and `AppRun`:

     ```sh
     #!/bin/sh
     HERE="$(dirname "$(readlink -f "$0")")"
     exec "$HERE/usr/lib/lss/LSS" "$@"
     ```

     (`chmod +x`).
   - In the root copy of the desktop file, `Exec=lss` is fine (appimagetool
     only checks the file).
2. Tool: `appimagetool-x86_64.AppImage` from
   `https://github.com/AppImage/appimagetool/releases` (a fixed release,
   chosen like nfpm, not `continuous`). Download it to `dist/tools/` if
   missing, `chmod +x`, and run it with `--appimage-extract-and-run` (CI
   runners have no FUSE):

   ```bash
   ARCH=x86_64 "$tool" --appimage-extract-and-run dist/LSS.AppDir "dist/LSS-$version-x86_64.AppImage"
   ```

3. Delete `dist/LSS.AppDir` after building.

Commit: `AppImage`

---

## I6 — Workflow

Rewrite `.github/workflows/build.yml` into four jobs. Keep the header
comment about SignPath and the existing Windows steps (tag check, single
exe, artifact `unsigned-exe`, SignPath, signature check, zip).

1. `test` — as in C6/H7 (matrix Windows + Ubuntu).
2. `windows` — the current `build` job renamed, `needs: test`; it uploads
   the artifact `lss-windows` (`dist/*.zip`, `dist/*.zip.sha256`). Remove
   its "Draft release" step (moved to `release`). Paths use
   `src/Lss.App/Lss.App.csproj` and `-p:PublishProfile=win-x64`.
3. `linux` — `runs-on: ubuntu-22.04` (older glibc = runs on more
   distributions), `needs: test`:
   - checkout, setup-dotnet 10.0.x;
   - install nfpm (`.deb` from its release, `sudo dpkg -i`);
   - `bash scripts/release.sh --skip-tests`;
   - smoke tests:

     ```bash
     docker run --rm -v "$PWD/dist:/dist" ubuntu:24.04 bash -c "apt-get update -qq && apt-get install -y -qq /dist/lss_*_amd64.deb && lss --version"
     ```

     ```bash
     docker run --rm -v "$PWD/dist:/dist" fedora:42 bash -c "dnf install -y -q /dist/lss-*.x86_64.rpm && lss --version"
     ```

     ```bash
     chmod +x dist/*.AppImage && dist/LSS-*-x86_64.AppImage --appimage-extract-and-run --version
     ```

   - upload the artifact `lss-linux` (tar.gz, deb, rpm, AppImage and their
     `.sha256`).
4. `release` — only on `v*` tags, `needs: [windows, linux]`,
   `runs-on: ubuntu-latest`: download both artifacts
   (`actions/download-artifact@v4` with `merge-multiple: true` into
   `dist/`), then `softprops/action-gh-release@v2` with `draft: true`,
   `prerelease: ${{ contains(github.ref_name, '-') }}`, all files, and the
   body of the current workflow (signed / not signed) passed from the
   `windows` job as a job output.

The tag check step stays in `windows` (it reads the version from
`src/Lss.App/Lss.App.csproj`).

Check: read the YAML again for indentation; Andrix runs it with a push and
*Run workflow* (👤 I7).

Commit: `CI: Linux packages and one release with everything`

---

## I7 — 👤 First CI run (Andrix)

Push `lss`, open Actions, check the four jobs (on a branch `release` is
skipped). Download `lss-linux` and try the AppImage on the Steam Deck and
the `.deb` on Ubuntu. Note results in `PROGRESS.md`.
