# CreatureCmd

## Feature: command-layer heal execution

Location: `sts2_decompile/sts2/MegaCrit/sts2/Core/Commands/CreatureCmd.cs`

Key method: `public static async Task Heal(Creature creature, Decimal amount, bool playAnim = true)`

Relevant logic:

```csharp
amount = Hook.ModifyHealAmount(..., creature, amount);
Decimal num = Math.Min(amount, (Decimal) (creature.MaxHp - creature.CurrentHp));
creature.HealInternal(amount);

if (playAnim)
{
  ...
}

if (amount > 0M)
  await Hook.AfterCurrentHpChanged(..., creature, amount);
```

Notes:

- `CreatureCmd.Heal(...)` is the command-layer entry point for healing.
- Hook-based heal modifiers run here before `creature.HealInternal(amount)`.
- VFX, history tracking, and `AfterCurrentHpChanged` also depend on the same `amount`.
- If a mod wants Neow's opening heal to end at `10` HP, patching `CreatureCmd.Heal(...)` before execution keeps the whole downstream flow aligned with the capped value.

## Heaven 7 hook

Relevant method:

- `public static async Task Kill(IReadOnlyCollection<Creature> creatures, bool force = false)`

Why this hook works for Heaven 7:

- normal combat damage collects killed creatures and then routes them through `CreatureCmd.Kill(...)`
- a postfix here can count how many monsters actually died in that batch
- this supports single-target kills and multi-kills with the same logic

Current mod strategy in this repo:

- patch `CreatureCmd.Kill(IReadOnlyCollection<Creature>, bool)` with a postfix
- after the original kill flow finishes, count `creatures` where `IsMonster && IsDead`
- apply `2` HP loss per killed monster to every living player

Multiplayer note:

- do not target `LocalContext.GetMe(...)` here
- in multiplayer, each peer has a different local player, which causes checksum divergence
- applying the same punishment to the same set of players on every peer keeps combat state deterministic
