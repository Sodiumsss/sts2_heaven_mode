# NAscensionPanel

Namespace: `MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect`

## Feature: Heaven level piggybacks on the official ascension panel

Relevant vanilla methods:

- `NAscensionPanel.DecrementAscension()`
- `NAscensionPanel.IncrementAscension()`
- `NAscensionPanel.RefreshArrowVisibility()`
- `NAscensionPanel.RefreshAscensionText()`
- `NAscensionPanel.SetAscensionLevel(int ascension)`

## Implementation note in this repo

Use Harmony to hijack the official ascension panel instead of injecting a custom Heaven selector.

- When official ascension is `0`, patch `DecrementAscension()` so the left arrow enters Heaven levels.
- Heaven levels are stored in `HeavenState.SelectedOption`:
  - `0` = Off
  - `1` = Heaven 1
  - `2` = Heaven 2
- Patch `IncrementAscension()` so Heaven `2 -> 1 -> 0` uses the official right arrow.
- Patch `RefreshArrowVisibility()` so the left arrow remains visible at official ascension `0`.
- Patch `RefreshAscensionText()` to overwrite the official description box with Heaven title + description.
- Patch `SetAscensionLevel(int)` to clear Heaven selection when the player moves back into official ascension `> 0`.

## Result

The official ascension panel becomes the only difficulty UI:

- Official ascension `0` + left arrow => Heaven `1`
- Heaven `1` + left arrow => Heaven `2`
- Heaven `1/2` + right arrow => move back toward official ascension `0`
- The official description box shows the current Heaven title and description while Heaven is selected

## Preference persistence

Heaven preference is not part of vanilla `PreferredAscension`.

Current repo behavior:

- save the selected Heaven level per character when the player changes Heaven in `NAscensionPanel`
- clear the saved Heaven preference when the player moves back to official ascension `> 0`
- restore the saved Heaven level when `StartRunLobby.SetSingleplayerAscensionAfterCharacterChanged(ModelId)` reapplies that character's preferred difficulty
