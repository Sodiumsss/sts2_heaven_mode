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

## Feature: multiplayer Heaven selection sync

Relevant vanilla classes and methods:

- `StartRunLobby.NetService.RegisterMessageHandler<T>(...)`
- `StartRunLobby.NetService.SendMessage<T>(...)`
- `NCharacterSelectScreen.InitializeMultiplayerAsHost(...)`
- `NCharacterSelectScreen.InitializeMultiplayerAsClient(...)`
- `NCharacterSelectScreen.PlayerConnected(LobbyPlayer player)`
- `NCharacterSelectScreen.OnSubmenuOpened()`

## Why this matters

Vanilla lobby sync only carries official ascension via `LobbyAscensionChangedMessage`.
Our Heaven level is a separate overlay on top of ascension `0`, so it is invisible to joined
clients unless we send it ourselves.

## HeavenMode usage

This repo adds two custom reliable lobby messages:

- `LobbyHeavenLevelChangedMessage`
- `LobbyHeavenLevelRequestMessage`

Implementation pattern:

- host registers both handlers when the multiplayer character-select screen initializes
- client also registers the same handlers when its multiplayer character-select screen initializes
- when a client opens the submenu, it requests the current Heaven level from the host
- when the host changes Heaven level, it broadcasts the current level
- when a new player joins, the host sends the current Heaven level to that specific peer

Client-side apply step:

- update `HeavenState.SelectedOption`
- refresh the existing `NAscensionPanel`

This is the key fix that makes joined players:

- see Heaven 1..10 in the lobby UI
- carry the same Heaven runtime state into the run
