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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HeavenMode;

internal static class Patches_Heaven7
{
    private const int DamagePerKill = 2;

    internal static Task AfterKill(Task __result, IReadOnlyCollection<Creature> creatures, bool force)
    {
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
            foreach (Player player in livingPlayers)
            {
                Creature playerCreature = player.Creature;
                DamageResult result = playerCreature.LoseHpInternal(totalDamage, ValueProp.Unblockable | ValueProp.Unpowered);
                if (result.UnblockedDamage <= 0)
                    continue;

                totalHpLoss += result.UnblockedDamage;
                PlayKillPunishFeedback(playerCreature, result);

                if (result.WasTargetKilled && playerCreature.IsDead)
                    await CreatureCmd.Kill(playerCreature, true);
            }

            if (totalHpLoss > 0)
            {
                Log.Info(
                    $"[HeavenMode] Applied Heaven {HeavenState.SelectedOption} kill punishment for {killedMonsterCount} kill(s): " +
                    $"hp loss {totalDamage} to each living player ({livingPlayers.Count} player(s))");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyKillPunishment failed: {ex}");
        }
    }

    private static void PlayKillPunishFeedback(Creature creature, DamageResult result)
    {
        try
        {
            Node? vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
            NDamageNumVfx? damageNum = NDamageNumVfx.Create(creature, result);
            if (damageNum != null)
            {
                if (vfxContainer != null)
                    vfxContainer.AddChildSafely(damageNum);
                else
                    ((Node?)NRun.Instance?.GlobalUi)?.AddChildSafely(damageNum);
            }

            if (vfxContainer != null)
                vfxContainer.AddChildSafely(NHitSparkVfx.Create(creature));

            _ = CreatureCmd.TriggerAnim(creature, "Hit", 0.0f);
            if (LocalContext.IsMe(creature))
                PlayerHurtVignetteHelper.Play();

            NGame.Instance?.ScreenShake(
                result.UnblockedDamage < 6 ? ShakeStrength.Weak : ShakeStrength.Medium,
                ShakeDuration.Short);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] PlayKillPunishFeedback failed: {ex}");
        }
    }
}
