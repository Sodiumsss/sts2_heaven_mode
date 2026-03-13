using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HeavenMode;

[ModInitializer("Initialize")]
public static class ModEntry
{
    public static void Initialize()
    {
        try
        {
            Assembly assembly = typeof(ModEntry).Assembly;
            int patchTypeCount = assembly.GetTypes().Count(t =>
                t.GetCustomAttributes(inherit: false).Any(a =>
                    a.GetType().Name is "HarmonyPatch" or "HarmonyPatchAttribute") ||
                t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Any(m => m.GetCustomAttributes(inherit: false).Any(a =>
                        a.GetType().Name is "HarmonyPatch" or "HarmonyPatchAttribute")));

            Harmony harmony = new("com.heavenmode");
            harmony.PatchAll(assembly);
            ApplyManualPatches(harmony);
            Log.Info($"[HeavenMode] PatchAll applied for {assembly.GetName().Name}, patch types discovered: {patchTypeCount}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Initialize PatchAll failed: {ex}");
        }

        Task.Delay(3000).ContinueWith(_ => PrintInit());
    }

    private static void PrintInit()
    {
        try
        {
            Log.Info(Loc.Get("MOD_INIT", "Heaven Mode initialized."));
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] PrintInit failed: {ex}");
        }
    }

    private static void ApplyManualPatches(Harmony harmony)
    {
        TryPatch(
            harmony,
            AccessTools.Method(typeof(Creature), nameof(Creature.SetCurrentHpInternal)),
            AccessTools.Method(typeof(Patches_Player), "AfterSetCurrentHp"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(AncientEventModel), "BeforeEventStarted"),
            AccessTools.Method(typeof(Patches_Player), "BeforeAncientEventStarted"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunState), nameof(RunState.CreateForNewRun)),
            AccessTools.Method(typeof(Patches_RunStart), "BeforeCreateForNewRun"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewSinglePlayer)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "AfterSetUpNewSinglePlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "AfterSetUpNewMultiPlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(SaveManager), nameof(SaveManager.SaveRun)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "AfterSaveRun"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpSavedSinglePlayer)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "BeforeSetUpSavedSinglePlayer"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "BeforeSetUpSavedMultiPlayer"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(NContinueRunInfo), "ShowInfo"),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "AfterContinueShowInfo"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(StartRunLobby), "SetSingleplayerAscensionAfterCharacterChanged"),
            AccessTools.Method(typeof(Patches_CharacterSelect), "AfterSetSingleplayerAscensionAfterCharacterChanged"));
    }

    private static void TryPatch(Harmony harmony, MethodInfo? original, MethodInfo? patchMethod, bool isPrefix = false)
    {
        if (original == null || patchMethod == null)
        {
            Log.Error($"[HeavenMode] Manual patch skipped. original={original != null}, patch={patchMethod != null}");
            return;
        }

        try
        {
            if (isPrefix)
            {
                harmony.Patch(original, prefix: new HarmonyMethod(patchMethod));
                Log.Info($"[HeavenMode] Manual prefix applied: {original.DeclaringType?.FullName}.{original.Name} -> {patchMethod.Name}");
            }
            else
            {
                harmony.Patch(original, postfix: new HarmonyMethod(patchMethod));
                Log.Info($"[HeavenMode] Manual postfix applied: {original.DeclaringType?.FullName}.{original.Name} -> {patchMethod.Name}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] Manual patch failed for {original.DeclaringType?.FullName}.{original.Name}: {ex}");
        }
    }
}
