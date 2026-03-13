using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace HeavenMode;

internal static class Patches_RunStart
{
    [HarmonyPatch(typeof(NGame), nameof(NGame.StartNewSingleplayerRun))]
    [HarmonyPrefix]
    internal static void BeforeStartNewSingleplayerRun(ref int ascensionLevel)
    {
        try
        {
            int effectiveAscension = HeavenState.GetEffectiveAscension(ascensionLevel);
            if (effectiveAscension == ascensionLevel)
                return;

            ascensionLevel = effectiveAscension;
            Log.Info($"[HeavenMode] Raised singleplayer ascension to {ascensionLevel} for Heaven={HeavenState.SelectedOption}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeStartNewSingleplayerRun failed: {ex}");
        }
    }

    [HarmonyPatch(typeof(NGame), nameof(NGame.StartNewMultiplayerRun))]
    [HarmonyPrefix]
    internal static void BeforeStartNewMultiplayerRun(StartRunLobby lobby, ref int ascensionLevel)
    {
        try
        {
            int effectiveAscension = HeavenState.GetEffectiveAscension(ascensionLevel);
            if (effectiveAscension == ascensionLevel)
                return;

            ascensionLevel = effectiveAscension;
            Log.Info($"[HeavenMode] Raised multiplayer ascension to {ascensionLevel} for Heaven={HeavenState.SelectedOption}, lobby={lobby != null}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeStartNewMultiplayerRun failed: {ex}");
        }
    }

    [HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
    [HarmonyPrefix]
    internal static void BeforeCreateForNewRun(ref int ascensionLevel)
    {
        try
        {
            int effectiveAscension = HeavenState.GetEffectiveAscension(ascensionLevel);
            if (effectiveAscension == ascensionLevel)
                return;

            ascensionLevel = effectiveAscension;
            Log.Info($"[HeavenMode] Raised run creation ascension to {ascensionLevel} for Heaven={HeavenState.SelectedOption}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeCreateForNewRun failed: {ex}");
        }
    }
}
