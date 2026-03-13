# NGame

Namespace: `MegaCrit.Sts2.Core.Nodes`

## Feature: Heaven levels inherit official Ascension 10

Relevant vanilla methods:

- `NGame.StartNewSingleplayerRun(...)`
- `NGame.StartNewMultiplayerRun(...)`
- `RunState.CreateForNewRun(...)`

## Implementation note in this repo

The character-select UI keeps Heaven layered on top of official ascension `0`, but the actual run must still inherit official Ascension `10`.

Use Harmony Prefix on both run-start methods, and also on `RunState.CreateForNewRun(...)` as the final safety net, to rewrite the outgoing `ascensionLevel` argument:

- If official ascension is `0` and `HeavenState.SelectedOption >= 1`, replace the ascension passed into run creation with `10`.
- Otherwise keep the original official ascension value.

This makes:

- Heaven `1` = official Ascension `10` + Heaven `1` custom rule
- Heaven `2` = official Ascension `10` + Heaven `1` + Heaven `2`

The effective remapping must survive all vanilla start-run paths, so the final guaranteed interception point is `RunState.CreateForNewRun(...)`.
