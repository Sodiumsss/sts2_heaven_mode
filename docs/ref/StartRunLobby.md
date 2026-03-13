# StartRunLobby

Namespace: `MegaCrit.Sts2.Core.Multiplayer.Game.Lobby`

## Feature: restore Heaven preference in character select

Relevant vanilla methods:

- `StartRunLobby.SetLocalCharacter(CharacterModel character)`
- `StartRunLobby.SetSingleplayerAscensionAfterCharacterChanged(ModelId characterId)`
- `StartRunLobby.UpdatePreferredAscension()`

## Why this method matters

Vanilla singleplayer character select restores the official preferred ascension inside
`SetSingleplayerAscensionAfterCharacterChanged(ModelId characterId)`.

That method:

- reads `SaveManager.Instance.Progress.GetOrCreateCharacterStats(characterId)`
- applies `PreferredAscension`
- logs `"{characterId} ascension set to preferred: {this.Ascension}"`

## HeavenMode usage

Heaven preference is restored with a postfix on:

- `StartRunLobby.SetSingleplayerAscensionAfterCharacterChanged(ModelId characterId)`

Implementation pattern in this repo:

- if restored official ascension is `0`, load the saved Heaven preference for that character
- otherwise force Heaven selection back to `0`
- after updating `HeavenState.SelectedOption`, refresh the existing `NAscensionPanel`

## Persistence note

Official preferred ascension still uses vanilla progress data.

Heaven preference is stored separately in a profile-scoped sidecar file:

- `saves/heaven_mode_preferences.json`

Key shape:

- `ModelId.ToString()` -> Heaven level

