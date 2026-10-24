# Game compatibility

Games tested with LSS. A game works when it keeps its saves in Steam's
`userdata` folder (see the [README](../README.md#which-games-work)). The
columns say where the game was tested; *not tested* means nobody has checked
it yet, not that it doesn't work.

| Game | AppID | Windows | Linux | Notes |
|---|---|---|---|---|
| Control | 870780 | Works (tested by hand) | Not tested yet | Shared file: `remote/preferences_data`, kept the same in every playthrough by default. Steam Cloud must be off. |
| Terraria | 105600 | Not tested yet | Not tested yet | Only the cloud characters and worlds are in `userdata`: the other saves are elsewhere, so LSS doesn't switch them. |

## How to report a game

Tell us if a game works, partly works or doesn't work: open the
[game report form](https://github.com/Andrix64/Local-Save-Switcher/issues/new/choose)
on GitHub. Say which system you used and whether Steam Cloud was off. The
name and AppID are in the list of games of LSS, or on the game's Steam page
(the number in its address, `store.steampowered.com/app/<AppID>`).
