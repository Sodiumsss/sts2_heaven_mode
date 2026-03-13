# NContinueRunInfo

Namespace: `MegaCrit.Sts2.Core.Nodes.Screens.MainMenu`

## Feature: show Heaven title in main-menu Continue tooltip

Relevant vanilla method:

- `NContinueRunInfo.ShowInfo(SerializableRun save)`

## Vanilla behavior

The Continue tooltip renders its difficulty line directly from `save.Ascension`:

- if `save.Ascension > 0`, show `Ascension X`

This loses Heaven-specific UI intent because Heaven runs are persisted as official Ascension 10.

## Implementation note in this repo

Heaven mode writes a small profile-scoped current-run sidecar file:

- `saves/heaven_mode_current.json`

- New-run setup and save hooks record:
  - `start_time`
  - `heaven_level`
- `NContinueRunInfo.ShowInfo(...)` reads that metadata
- if the save belongs to a Heaven run, overwrite the tooltip difficulty label with:
  - `Heaven 1`
  - `Heaven 2`

This keeps the main-menu Continue tooltip aligned with the Heaven UI naming, even after restarting the game.
