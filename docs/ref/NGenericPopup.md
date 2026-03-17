# NGenericPopup

Namespace: `MegaCrit.Sts2.Core.Nodes.Multiplayer`

## Feature: reusable confirm/cancel popup for Heaven 11 save destruction

Relevant vanilla methods:

- `NGenericPopup.Create()`
- `NGenericPopup.WaitForConfirmation(...)`
- `NVerticalPopup.SetText(string title, string body)`
- `NVerticalPopup.InitYesButton(...)`
- `NVerticalPopup.InitNoButton(...)`

## Vanilla behavior

- `NGenericPopup` is the game's generic confirmation dialog wrapper.
- Vanilla uses it for quit confirmation, reset confirmation, load-save confirmation, and similar yes/no flows.
- Internally it owns an `NVerticalPopup`, which can render either localized text or raw strings.

## Implementation note in this repo

- Heaven 11 reuses the vanilla `NGenericPopup` scene instead of building a new modal.
- After the popup is created and added to `NModalContainer`, the mod reaches its internal `NVerticalPopup`.
- The popup text is then filled with mod-defined Heaven 11 warning text, while the buttons reuse vanilla `GENERIC_POPUP.confirm` and `GENERIC_POPUP.cancel`.

## Result

- The confirmation step keeps the official popup look and behavior.
- Heaven 11 only destroys the save after the player explicitly confirms.
