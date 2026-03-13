# NTopBarPortraitTip

Namespace: `MegaCrit.sts2.Core.Nodes.TopBar`

## Feature: append Heaven difficulty title to the in-run portrait hover tip

Relevant vanilla method:

- `NTopBarPortraitTip.Initialize(IRunState runState)`

## Vanilla behavior

During run initialization, the left-top portrait hover tip is created from:

- `AscensionHelper.GetHoverTip(LocalContext.GetMe(runState).Character, runState.AscensionLevel)`

So the portrait hover tip only knows about the final ascension level that the run started with.

## Implementation note in this repo

Use a Harmony Postfix on `NTopBarPortraitTip.Initialize(...)`.

- Read the private `_hoverTip` field after vanilla initialization.
- If Heaven is enabled, unbox the `HoverTip` value and rewrite its `Title` to:
  - `CharacterName - Heaven1`
  - `CharacterName - Heaven2`
- Append active Heaven titles to the end of the vanilla description list:
  - Heaven 1 adds `+Human World`
  - Heaven 2 adds `+Human World` and `+Hell of Tongue Pulling`
- Prefix appended Heaven lines with a leading space so they align visually with vanilla ascension rows.
- Write the modified `HoverTip` back into `_hoverTip`.

This changes only the in-run left-top portrait tooltip, without affecting other places that also reuse `AscensionHelper.GetHoverTip(...)`.
