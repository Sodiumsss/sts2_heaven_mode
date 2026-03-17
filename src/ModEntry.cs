using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
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
            HeavenConfig.Initialize();
            ModManager.OnMetricsUpload += HeavenUnlockProgress.HandleMetricsUpload;

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
            Log.Info($"[HeavenMode] Config loaded. unlock={HeavenConfig.UnlockAll}");
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
            AccessTools.Method(typeof(AncientEventModel), "SetInitialEventState"),
            AccessTools.Method(typeof(Patches_Player), "AfterSetInitialEventState"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(CardSelectCmd), nameof(CardSelectCmd.FromDeckForRemoval)),
            AccessTools.Method(typeof(Patches_EventRoom), "AfterFromDeckForRemoval"));

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
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewSinglePlayer)),
            AccessTools.Method(typeof(Patches_Heaven3), "AfterSetUpNewSinglePlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewSinglePlayer)),
            AccessTools.Method(typeof(Patches_Heaven8), "AfterSetUpNewSinglePlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewSinglePlayer)),
            AccessTools.Method(typeof(Patches_Heaven10), "AfterSetUpNewSinglePlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "AfterSetUpNewMultiPlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer)),
            AccessTools.Method(typeof(Patches_Heaven3), "AfterSetUpNewMultiPlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer)),
            AccessTools.Method(typeof(Patches_Heaven8), "AfterSetUpNewMultiPlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewMultiPlayer)),
            AccessTools.Method(typeof(Patches_Heaven10), "AfterSetUpNewMultiPlayer"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics)),
            AccessTools.Method(typeof(Patches_Heaven3), "BeforeFinalizeStartingRelics"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "SetupPlayerTurn"),
            AccessTools.Method(typeof(Patches_Heaven4), "AfterSetupPlayerTurn"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "SetupPlayerTurn"),
            AccessTools.Method(typeof(Patches_Heaven9), "BeforeSetupPlayerTurn"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "SetupPlayerTurn"),
            AccessTools.Method(typeof(Patches_Heaven5), "AfterSetupPlayerTurn"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(SaveManager), nameof(SaveManager.SaveRun)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "AfterSaveRun"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Kill), new[] { typeof(IReadOnlyCollection<Creature>), typeof(bool) }),
            AccessTools.Method(typeof(Patches_Heaven7), "AfterKill"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardPileCmd), "ShuffleIfNecessary"),
            AccessTools.Method(typeof(Patches_Heaven9), "BeforeShuffleIfNecessary"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.CardPileCmd), "ShuffleIfNecessary"),
            AccessTools.Method(typeof(Patches_Heaven9), "AfterShuffleIfNecessary"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Hooks.Hook), "AfterCardDrawn"),
            AccessTools.Method(typeof(Patches_Heaven9), "AfterHookAfterCardDrawn"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Potions.NPotionContainer), "Initialize"),
            AccessTools.Method(typeof(Patches_PotionUi), "AfterInitialize"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Potions.NPotionContainer), "GrowPotionHolders"),
            AccessTools.Method(typeof(Patches_PotionUi), "AfterGrowPotionHolders"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpSavedSinglePlayer)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "BeforeSetUpSavedSinglePlayer"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpSavedSinglePlayer)),
            AccessTools.Method(typeof(Patches_Heaven10), "BeforeSetUpSavedSinglePlayer"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer)),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "BeforeSetUpSavedMultiPlayer"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpSavedMultiPlayer)),
            AccessTools.Method(typeof(Patches_Heaven10), "BeforeSetUpSavedMultiPlayer"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.ProceedFromTerminalRewardsScreen)),
            AccessTools.Method(typeof(Patches_Heaven10), "BeforeProceedFromTerminalRewardsScreen"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), nameof(RunManager.EnterNextAct)),
            AccessTools.Method(typeof(Patches_Heaven10), "BeforeEnterNextAct"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(Creature), nameof(Creature.AfterTurnStart)),
            AccessTools.Method(typeof(Patches_Heaven10), "AfterTurnStart"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(NContinueRunInfo), "ShowInfo"),
            AccessTools.Method(typeof(Patches_SaveAndLoad), "AfterContinueShowInfo"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(StartRunLobby), "SetSingleplayerAscensionAfterCharacterChanged"),
            AccessTools.Method(typeof(Patches_CharacterSelect), "AfterSetSingleplayerAscensionAfterCharacterChanged"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsHost)),
            AccessTools.Method(typeof(Patches_MultiplayerCharacterSelect), "AfterInitializeMultiplayerAsHost"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsClient)),
            AccessTools.Method(typeof(Patches_MultiplayerCharacterSelect), "AfterInitializeMultiplayerAsClient"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened)),
            AccessTools.Method(typeof(Patches_MultiplayerCharacterSelect), "AfterOnSubmenuOpened"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.PlayerConnected)),
            AccessTools.Method(typeof(Patches_MultiplayerCharacterSelect), "AfterPlayerConnected"));

        TryPatch(
            harmony,
            AccessTools.Method(typeof(NCharacterSelectScreen), "CleanUpLobby"),
            AccessTools.Method(typeof(Patches_MultiplayerCharacterSelect), "BeforeCleanUpLobby"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(StartRunLobby), "BeginRun"),
            AccessTools.Method(typeof(Patches_MultiplayerLobby), "BeforeBeginRun"),
            isPrefix: true);

        TryPatch(
            harmony,
            AccessTools.Method(typeof(RunManager), "UpdateRichPresence"),
            AccessTools.Method(typeof(Patches_Platform), "AfterUpdateRichPresence"));

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
