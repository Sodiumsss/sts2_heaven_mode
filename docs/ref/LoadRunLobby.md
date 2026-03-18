# LoadRunLobby

- `MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.LoadRunLobby`
- `MegaCrit.Sts2.Core.Runs.RunManager.SetUpSavedMultiPlayer(RunState, LoadRunLobby)`
- `MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NMultiplayerLoadGameScreen`

Relevant vanilla flow:

- host opens a loaded multiplayer lobby with a `SerializableRun`
- clients rejoin that lobby and receive the host's `SerializableRun`
- when all players are ready, `NMultiplayerLoadGameScreen.BeginRun()` leads to `RunManager.SetUpSavedMultiPlayer(...)`
- the actual room load uses `Run.PreFinishedRoom`

Implementation note in this repo:

- Heaven state was originally restored from a local metadata file keyed by `save.StartTime`
- that is not reliable for loaded multiplayer runs, because clients may not have the same local metadata at continue time
- the mod now restores the host's Heaven level from `save.StartTime` when the loaded-run lobby is created, then re-synchronizes that level to clients inside the `LoadRunLobby` / `NMultiplayerLoadGameScreen` flow before the run begins
