# NPauseMenu

Namespace: `MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu`

## Feature: Heaven 11 turns `Save and Quit` into save destruction

Relevant vanilla methods:

- `NPauseMenu.OnSaveAndQuitButtonPressed(NButton _)`
- `NPauseMenu.CloseToMenu()`

## Vanilla behavior

- The pause-menu `Save and Quit` button calls `OnSaveAndQuitButtonPressed(...)`.
- That routes into `CloseToMenu()`, which disables the pause menu, resets the action queue, stops run music, and then calls `NGame.ReturnToMainMenu()`.
- The existing current-run save is therefore preserved and can be continued from the main menu.

## Implementation note in this repo

- Heaven 11 prefixes `NPauseMenu.OnSaveAndQuitButtonPressed(...)`.
- When `HeavenState.SelectedOption >= 11`, the mod skips the vanilla `CloseToMenu()` path.
- Before deleting any save, it opens the vanilla generic confirmation popup.
- Instead it:
  - creates `MegaCrit.Sts2.Core.Nodes.Multiplayer.NGenericPopup`
  - uses its internal `NVerticalPopup` to show a custom title/body plus vanilla confirm/cancel buttons
  - waits for any in-flight run save to finish
  - deletes the current singleplayer or multiplayer run save through `SaveManager`
  - clears the Heaven current-run sidecar metadata
  - returns to the main menu

## Result

- At Heaven 11, clicking `Save and Quit` no longer preserves the run.
- The save file is removed before the main menu loads, so the run cannot be continued afterward.
