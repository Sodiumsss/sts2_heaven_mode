using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace HeavenMode;

[HarmonyPatch(typeof(NMultiplayerLoadGameScreen))]
internal static class Patches_MultiplayerLoadGame
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMultiplayerLoadGameScreen.InitializeAsHost))]
    private static void AfterInitializeAsHost(NMultiplayerLoadGameScreen __instance)
    {
        TryRegister(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMultiplayerLoadGameScreen.InitializeAsClient))]
    private static void AfterInitializeAsClient(NMultiplayerLoadGameScreen __instance)
    {
        TryRegister(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMultiplayerLoadGameScreen.OnSubmenuOpened))]
    private static void AfterOnSubmenuOpened(NMultiplayerLoadGameScreen __instance)
    {
        try
        {
            HeavenLoadRunSync.OnLobbyOpened(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Load-game AfterOnSubmenuOpened failed: {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMultiplayerLoadGameScreen.PlayerConnected))]
    private static void AfterPlayerConnected(NMultiplayerLoadGameScreen __instance, ulong playerId)
    {
        try
        {
            HeavenLoadRunSync.OnPlayerConnected(__instance, playerId);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Load-game AfterPlayerConnected failed: {ex}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NMultiplayerLoadGameScreen.BeginRun))]
    private static void BeforeBeginRun(NMultiplayerLoadGameScreen __instance)
    {
        try
        {
            HeavenLoadRunSync.BroadcastCurrentLevel(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Load-game BeforeBeginRun failed: {ex}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("CleanUpLobby")]
    private static void BeforeCleanUpLobby(NMultiplayerLoadGameScreen __instance)
    {
        try
        {
            HeavenLoadRunSync.UnregisterLobby(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Load-game BeforeCleanUpLobby failed: {ex}");
        }
    }

    private static void TryRegister(NMultiplayerLoadGameScreen screen)
    {
        try
        {
            HeavenLoadRunSync.RegisterLobby(screen);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Load-game Heaven sync registration failed: {ex}");
        }
    }
}
