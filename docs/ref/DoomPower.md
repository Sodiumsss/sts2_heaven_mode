# DoomPower

- `MegaCrit.Sts2.Core.Models.Powers.DoomPower`
- `MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<DoomPower>(Creature, decimal, Creature?, CardModel?, bool)`

Relevant vanilla behavior:

- `DoomPower` is a `Debuff` counter.
- a creature is considered doomed when `Owner.CurrentHp <= Amount`
- on that side's `BeforeTurnEnd(...)`, the doomed creature(s) are killed by `DoomPower.DoomKill(...)`

Implementation note in this repo:

- Heaven 7 no longer lets its kill-punishment directly kill a player in combat.
- when the punishment would be lethal under `CurrentHp + Block <= pending damage`, the mod applies `3` Doom instead of reducing HP to `0`.
- this avoids the multiplayer desync path caused by "combat death -> combat victory revive -> next room state mismatch".
