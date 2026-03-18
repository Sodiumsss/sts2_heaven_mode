using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace HeavenMode;

internal static class Patches_Heaven7
{
    private const int DamagePerKill = 2;
    private const int DoomAmountOnLethal = 3;

    private static bool _applyingPunishment;

    internal static Task AfterKill(Task __result, IReadOnlyCollection<Creature> creatures, bool force)
    {
        if (_applyingPunishment)
            return __result;

        if (HeavenState.SelectedOption < HeavenState.KillPunishLevel)
            return __result;

        return ApplyKillPunishment(__result, creatures);
    }

    private static async Task ApplyKillPunishment(Task originalTask, IReadOnlyCollection<Creature> creatures)
    {
        await originalTask;

        try
        {
            int killedMonsterCount = creatures.Count(c => c.IsMonster && c.IsDead);
            if (killedMonsterCount <= 0)
                return;

            IRunState? runState = RunManager.Instance?.DebugOnlyGetState();
            if (runState?.CurrentRoom is not CombatRoom)
                return;

            List<Player> livingPlayers = runState.Players
                .Where(p => p.Creature != null && p.Creature.IsAlive)
                .ToList();
            if (livingPlayers.Count == 0)
                return;

            int totalDamage = killedMonsterCount * DamagePerKill;
            int totalHpLoss = 0;
            int doomAppliedCount = 0;

            _applyingPunishment = true;
            try
            {
                foreach (Player player in livingPlayers)
                {
                    Creature playerCreature = player.Creature;
                    int effectiveSurvivability = playerCreature.CurrentHp + playerCreature.Block;
                    if (effectiveSurvivability <= totalDamage)
                    {
                        await PowerCmd.Apply<DoomPower>(playerCreature, DoomAmountOnLethal, null, null);
                        doomAppliedCount++;
                        Log.Info(
                            $"[HeavenMode] Heaven {HeavenState.SelectedOption} kill punishment converted lethal damage " +
                            $"to {DoomAmountOnLethal} Doom for player {player.NetId} (hp={playerCreature.CurrentHp}, block={playerCreature.Block}, damage={totalDamage})");
                        continue;
                    }

                    int newHp = Math.Max(playerCreature.CurrentHp - totalDamage, 0);
                    int actualLoss = playerCreature.CurrentHp - newHp;
                    if (actualLoss <= 0)
                        continue;

                    totalHpLoss += actualLoss;
                    await CreatureCmd.SetCurrentHp(playerCreature, (decimal)newHp);
                    PlayKillPunishFeedback(playerCreature, actualLoss);
                }
            }
            finally
            {
                _applyingPunishment = false;
            }

            if (totalHpLoss > 0)
            {
                Log.Info(
                    $"[HeavenMode] Applied Heaven {HeavenState.SelectedOption} kill punishment for {killedMonsterCount} kill(s): " +
                    $"hp loss {totalDamage} to each living player ({livingPlayers.Count} player(s))");
            }

            if (doomAppliedCount > 0)
            {
                Log.Info(
                    $"[HeavenMode] Applied Heaven {HeavenState.SelectedOption} lethal fallback: " +
                    $"{DoomAmountOnLethal} Doom to {doomAppliedCount} player(s)");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyKillPunishment failed: {ex}");
        }
    }

    private static void PlayKillPunishFeedback(Creature creature, int damage)
    {
        try
        {
            Node? vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;

            if (vfxContainer != null)
                vfxContainer.AddChildSafely(NHitSparkVfx.Create(creature));

            _ = CreatureCmd.TriggerAnim(creature, "Hit", 0.0f);
            if (LocalContext.IsMe(creature))
                PlayerHurtVignetteHelper.Play();

            NGame.Instance?.ScreenShake(
                damage < 6 ? ShakeStrength.Weak : ShakeStrength.Medium,
                ShakeDuration.Short);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] PlayKillPunishFeedback failed: {ex}");
        }
    }
}

