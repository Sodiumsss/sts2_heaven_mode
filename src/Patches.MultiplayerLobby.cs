using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace HeavenMode;

[HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
internal static class Patches_MultiplayerLobby
{
    [HarmonyPrefix]
    private static void BeforeBeginRun(StartRunLobby __instance)
    {
        try
        {
            HeavenMultiplayerSync.BroadcastCurrentLevel(__instance);
            Log.Info($"[HeavenMode] Re-broadcast Heaven level {HeavenState.SelectedOption} immediately before multiplayer BeginRun");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeBeginRun failed: {ex}");
        }
    }
}
