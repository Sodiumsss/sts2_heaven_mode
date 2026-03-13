using System;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace HeavenMode;

[HarmonyPatch(typeof(NAscensionPanel))]
internal static class Patches_CharacterSelect
{
    private const int MaxHeavenLevel = 2;

    private static readonly AccessTools.FieldRef<NAscensionPanel, int> MaxAscensionRef =
        AccessTools.FieldRefAccess<NAscensionPanel, int>("_maxAscension");

    private static readonly AccessTools.FieldRef<NAscensionPanel, bool> ArrowsVisibleRef =
        AccessTools.FieldRefAccess<NAscensionPanel, bool>("_arrowsVisible");

    private static readonly AccessTools.FieldRef<NAscensionPanel, NButton> LeftArrowRef =
        AccessTools.FieldRefAccess<NAscensionPanel, NButton>("_leftArrow");

    private static readonly AccessTools.FieldRef<NAscensionPanel, NButton> RightArrowRef =
        AccessTools.FieldRefAccess<NAscensionPanel, NButton>("_rightArrow");

    private static readonly AccessTools.FieldRef<NAscensionPanel, MegaLabel> AscensionLevelRef =
        AccessTools.FieldRefAccess<NAscensionPanel, MegaLabel>("_ascensionLevel");

    private static readonly AccessTools.FieldRef<NAscensionPanel, MegaRichTextLabel> InfoRef =
        AccessTools.FieldRefAccess<NAscensionPanel, MegaRichTextLabel>("_info");

    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, StartRunLobby> LobbyRef =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, StartRunLobby>("_lobby");

    private static readonly AccessTools.FieldRef<NCharacterSelectScreen, NAscensionPanel> ScreenAscensionPanelRef =
        AccessTools.FieldRefAccess<NCharacterSelectScreen, NAscensionPanel>("_ascensionPanel");

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NAscensionPanel.SetAscensionLevel))]
    private static void AfterSetAscensionLevel(NAscensionPanel __instance)
    {
        try
        {
            if (__instance.Ascension > 0 && HeavenState.SelectedOption != 0)
            {
                HeavenState.SelectedOption = 0;
                PersistPreferredHeaven(__instance, 0);
                Log.Info("[HeavenMode] Cleared Heaven selection because official ascension is now above 0");
            }

            RefreshHeavenUi(__instance);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterSetAscensionLevel failed: {ex}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("DecrementAscension")]
    private static bool BeforeDecrementAscension(NAscensionPanel __instance)
    {
        try
        {
            if (__instance.Ascension != 0)
                return true;

            if (HeavenState.SelectedOption >= MaxHeavenLevel)
                return false;

            HeavenState.SelectedOption += 1;
            PersistPreferredHeaven(__instance, HeavenState.SelectedOption);
            RefreshHeavenUi(__instance);
            Log.Info($"[HeavenMode] Heaven option {HeavenState.SelectedOption} selected via ascension left arrow");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeDecrementAscension failed: {ex}");
            return true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("IncrementAscension")]
    private static bool BeforeIncrementAscension(NAscensionPanel __instance)
    {
        try
        {
            if (__instance.Ascension != 0 || HeavenState.SelectedOption <= 0)
                return true;

            HeavenState.SelectedOption -= 1;
            PersistPreferredHeaven(__instance, HeavenState.SelectedOption);
            RefreshHeavenUi(__instance);
            Log.Info($"[HeavenMode] Heaven option {HeavenState.SelectedOption} selected via ascension right arrow");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeIncrementAscension failed: {ex}");
            return true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("RefreshArrowVisibility")]
    private static void AfterRefreshArrowVisibility(NAscensionPanel __instance)
    {
        try
        {
            if (__instance.Ascension != 0)
                return;

            bool arrowsVisible = ArrowsVisibleRef(__instance);
            if (!arrowsVisible)
                return;

            int maxAscension = MaxAscensionRef(__instance);
            bool hasHeavenStepLeft = HeavenState.SelectedOption < MaxHeavenLevel;
            bool canMoveRight = HeavenState.SelectedOption > 0 || maxAscension > 0;

            ((Godot.CanvasItem)LeftArrowRef(__instance)).Visible = hasHeavenStepLeft;
            ((Godot.CanvasItem)RightArrowRef(__instance)).Visible = canMoveRight;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterRefreshArrowVisibility failed: {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("RefreshAscensionText")]
    private static void AfterRefreshAscensionText(NAscensionPanel __instance)
    {
        try
        {
            if (__instance.Ascension != 0 || HeavenState.SelectedOption <= 0)
                return;

            AscensionLevelRef(__instance).SetTextAutoSize(HeavenState.SelectedOption.ToString());
            InfoRef(__instance).Text =
                $"[b][gold]{GetHeavenTitle(HeavenState.SelectedOption)}[/gold][/b]\n{GetHeavenDescription(HeavenState.SelectedOption)}";
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterRefreshAscensionText failed: {ex}");
        }
    }

    internal static void AfterSetSingleplayerAscensionAfterCharacterChanged(
        StartRunLobby __instance,
        ModelId characterId)
    {
        try
        {
            if (__instance.NetService.Type != NetGameType.Singleplayer)
                return;

            int restoredLevel = __instance.Ascension == 0
                ? HeavenPersistence.LoadPreferredSelection(characterId)
                : 0;

            HeavenState.SelectedOption = Math.Clamp(restoredLevel, 0, MaxHeavenLevel);
            Log.Info($"[HeavenMode] Restored preferred Heaven for {characterId}: level={HeavenState.SelectedOption}");

            if (__instance.LobbyListener is not NCharacterSelectScreen screen)
                return;

            RefreshHeavenUi(ScreenAscensionPanelRef(screen));
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterSetSingleplayerAscensionAfterCharacterChanged failed: {ex}");
        }
    }

    private static void RefreshHeavenUi(NAscensionPanel ascensionPanel)
    {
        ascensionPanel.CallDeferred(NAscensionPanel.MethodName.RefreshAscensionText);
        ascensionPanel.CallDeferred(NAscensionPanel.MethodName.RefreshArrowVisibility);
    }

    private static void PersistPreferredHeaven(NAscensionPanel ascensionPanel, int heavenLevel)
    {
        try
        {
            if (!TryGetCurrentSingleplayerCharacterId(ascensionPanel, out ModelId characterId))
                return;

            HeavenPersistence.SavePreferredSelection(characterId, heavenLevel);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] PersistPreferredHeaven failed: {ex}");
        }
    }

    private static bool TryGetCurrentSingleplayerCharacterId(NAscensionPanel ascensionPanel, out ModelId characterId)
    {
        characterId = ModelId.none;

        NCharacterSelectScreen? screen = FindCharacterSelectScreen(ascensionPanel);
        if (screen == null)
            return false;

        StartRunLobby lobby = LobbyRef(screen);
        if (lobby == null || lobby.NetService.Type != NetGameType.Singleplayer || lobby.LocalPlayer.character == null)
            return false;

        characterId = lobby.LocalPlayer.character.Id;
        return true;
    }

    private static NCharacterSelectScreen? FindCharacterSelectScreen(Node node)
    {
        for (Node? current = node; current != null; current = current.GetParent())
        {
            if (current is NCharacterSelectScreen screen)
                return screen;
        }

        return null;
    }

    private static string GetHeavenTitle(int level) => level switch
    {
        1 => Loc.Get("HEAVEN_TITLE_1", "Human World"),
        2 => Loc.Get("HEAVEN_TITLE_2", "Hell of Tongue Pulling"),
        _ => string.Empty,
    };

    private static string GetHeavenDescription(int level) => level switch
    {
        1 => Loc.Get("HEAVEN_DESC_1", "Heaven 1: when Neow starts, your current HP is set to 10."),
        2 => Loc.Get("HEAVEN_DESC_2", "Heaven 2 includes Heaven 1 effects."),
        _ => string.Empty,
    };
}
