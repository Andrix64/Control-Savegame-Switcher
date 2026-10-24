# Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/),
certificate by [SignPath Foundation](https://signpath.org/).

## What is signed

Only `LSS.exe` from the release zips published on the
[Releases page](https://github.com/Andrix64/Local-Save-Switcher/releases)
of this repository. The executable is built by GitHub Actions
([`.github/workflows/build.yml`](.github/workflows/build.yml)) directly from
the source code in this repository, from a version tag (`v*`). Nothing built
elsewhere, on a personal computer or from a fork, is signed.

## Team roles

- Committers and reviewers: [Andrix64](https://github.com/Andrix64)
- Approvers: [Andrix64](https://github.com/Andrix64)

Every signing request is approved manually on SignPath before the release is
published.

## Privacy policy

This program will not transfer any information to other networked systems
unless specifically requested by the user or the person installing or
operating it.

LSS works only on local files: the save folders of the games in Steam's
`userdata` and its own data in `%LOCALAPPDATA%\LSS` (on Linux
`~/.local/share/LSS`). To start a game it asks the locally installed Steam
client to run it (`steam://rungameid/<AppID>`); it does not connect to the
internet by itself.
