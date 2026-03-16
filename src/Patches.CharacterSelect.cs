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
    private const string HeavenFireTimerName = "__HeavenFireTimer";
    private const string HeavenEmberTimerName = "__HeavenEmberTimer";
    private const string HeavenEmberContainerName = "__HeavenEmberContainer";
    private const string HeavenFireActiveMeta = "__heaven_fire_active";

    private static readonly StringName HParam = new("h");
    private static readonly StringName VParam = new("v");
    private static readonly StringName FontOutlineTheme = "font_outline_color";
    private static readonly Color HeavenOutlineColor = new("160818");
    private static readonly Color HeavenIconTint = new(0.88f, 0.84f, 0.98f, 1f);
    private static readonly Color HeavenEmberBright = new("960aef");
    private static readonly Color HeavenEmberDark = new("8f25d3");

    private static ImageTexture? _emberTexture;

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

    private static readonly AccessTools.FieldRef<NAscensionPanel, ShaderMaterial> IconHsvRef =
        AccessTools.FieldRefAccess<NAscensionPanel, ShaderMaterial>("_iconHsv");

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
                SyncMultiplayerHeavenSelection(__instance);
                Log.Info("[HeavenMode] Cleared Heaven selection because official ascension is now above 0");
            }

            ClampHeavenSelectionToUnlocks(__instance);

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

            int maxSelectableHeaven = GetMaxSelectableHeaven(__instance);
            if (HeavenState.SelectedOption >= maxSelectableHeaven)
                return false;

            HeavenState.SelectedOption += 1;
            PersistPreferredHeaven(__instance, HeavenState.SelectedOption);
            RefreshHeavenUi(__instance);
            SyncMultiplayerHeavenSelection(__instance);
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
            SyncMultiplayerHeavenSelection(__instance);
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
            bool arrowsVisible = ArrowsVisibleRef(__instance);
            NButton leftArrow = LeftArrowRef(__instance);
            NButton rightArrow = RightArrowRef(__instance);

            if (__instance.Ascension != 0)
            {
                bool canUseLeftArrow = arrowsVisible && __instance.Ascension != 0;
                bool canUseRightArrow = arrowsVisible && __instance.Ascension != MaxAscensionRef(__instance);

                if (canUseLeftArrow)
                    leftArrow.Enable();
                else
                    leftArrow.Disable();

                if (canUseRightArrow)
                    rightArrow.Enable();
                else
                    rightArrow.Disable();

                return;
            }

            if (!arrowsVisible)
            {
                leftArrow.Disable();
                rightArrow.Disable();
                return;
            }

            int maxAscension = MaxAscensionRef(__instance);
            int maxSelectableHeaven = GetMaxSelectableHeaven(__instance);
            bool hasHeavenEntry = maxSelectableHeaven > 0 || maxAscension >= 1;
            bool canMoveLeft = HeavenState.SelectedOption < maxSelectableHeaven;
            bool canMoveRight = HeavenState.SelectedOption > 0 || maxAscension > 0;

            bool showLeftArrow = hasHeavenEntry && HeavenState.SelectedOption < HeavenState.MaxLevel;
            ((Godot.CanvasItem)leftArrow).Visible = showLeftArrow;
            ((Godot.CanvasItem)rightArrow).Visible = canMoveRight;

            if (canMoveLeft)
                leftArrow.Enable();
            else
                leftArrow.Disable();

            if (canMoveRight)
                rightArrow.Enable();
            else
                rightArrow.Disable();
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

            int maxSelectableHeaven = HeavenUnlockProgress.GetMaxSelectableHeaven(characterId, __instance.MaxAscension);
            HeavenState.SelectedOption = Math.Clamp(restoredLevel, 0, maxSelectableHeaven);
            if (HeavenState.SelectedOption != restoredLevel)
                HeavenPersistence.SavePreferredSelection(characterId, HeavenState.SelectedOption);
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

    internal static void ApplySyncedHeavenLevel(NCharacterSelectScreen screen, int level)
    {
        try
        {
            NAscensionPanel ascensionPanel = ScreenAscensionPanelRef(screen);
            HeavenState.SelectedOption = ascensionPanel.Ascension == 0
                ? Math.Clamp(level, 0, HeavenState.MaxLevel)
                : 0;
            RefreshHeavenUi(ascensionPanel);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplySyncedHeavenLevel failed: {ex}");
        }
    }

    private static void RefreshHeavenUi(NAscensionPanel ascensionPanel)
    {
        ApplyHeavenFireVisuals(ascensionPanel);
        ascensionPanel.CallDeferred(NAscensionPanel.MethodName.RefreshAscensionText);
        ascensionPanel.CallDeferred(NAscensionPanel.MethodName.RefreshArrowVisibility);
    }

    private static void SyncMultiplayerHeavenSelection(NAscensionPanel ascensionPanel)
    {
        NCharacterSelectScreen? screen = FindCharacterSelectScreen(ascensionPanel);
        if (screen == null)
            return;

        try
        {
            HeavenMultiplayerSync.BroadcastCurrentLevel(screen);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] SyncMultiplayerHeavenSelection failed: {ex}");
        }
    }

    private static void PersistPreferredHeaven(NAscensionPanel ascensionPanel, int heavenLevel)
    {
        try
        {
            if (!TryGetCurrentSingleplayerCharacterId(ascensionPanel, out ModelId characterId))
                return;

            int maxSelectableHeaven = GetMaxSelectableHeaven(ascensionPanel, characterId);
            HeavenPersistence.SavePreferredSelection(characterId, Math.Clamp(heavenLevel, 0, maxSelectableHeaven));
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

    private static void ClampHeavenSelectionToUnlocks(NAscensionPanel ascensionPanel)
    {
        if (ascensionPanel.Ascension != 0 || HeavenState.SelectedOption <= 0)
            return;

        int maxSelectableHeaven = GetMaxSelectableHeaven(ascensionPanel);
        if (HeavenState.SelectedOption <= maxSelectableHeaven)
            return;

        HeavenState.SelectedOption = maxSelectableHeaven;
        PersistPreferredHeaven(ascensionPanel, HeavenState.SelectedOption);
        Log.Info($"[HeavenMode] Clamped Heaven selection to unlocked cap {maxSelectableHeaven}");
    }

    private static int GetMaxSelectableHeaven(NAscensionPanel ascensionPanel)
    {
        if (TryGetCurrentSingleplayerCharacterId(ascensionPanel, out ModelId characterId))
            return GetMaxSelectableHeaven(ascensionPanel, characterId);

        return HeavenConfig.UnlockAll
            ? HeavenState.MaxLevel
            : MaxAscensionRef(ascensionPanel) >= 10 ? 1 : 0;
    }

    private static int GetMaxSelectableHeaven(NAscensionPanel ascensionPanel, ModelId characterId) =>
        HeavenUnlockProgress.GetMaxSelectableHeaven(characterId, MaxAscensionRef(ascensionPanel));

    private static void ApplyHeavenFireVisuals(NAscensionPanel ascensionPanel)
    {
        try
        {
            if (ascensionPanel.Ascension == 0 && HeavenState.SelectedOption > 0)
            {
                EnsureHeavenFireTimer(ascensionPanel);
                EnsureHeavenEmberTimer(ascensionPanel);
                AnimateHeavenFire(ascensionPanel);
                return;
            }

            RestoreOfficialFireVisuals(ascensionPanel);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ApplyHeavenFireVisuals failed: {ex}");
        }
    }

    private static void EnsureHeavenFireTimer(NAscensionPanel ascensionPanel)
    {
        Timer? timer = ascensionPanel.GetNodeOrNull<Timer>(HeavenFireTimerName);
        if (timer == null)
        {
            timer = new Timer
            {
                Name = HeavenFireTimerName,
                WaitTime = 0.16,
                OneShot = false,
                Autostart = false,
            };
            timer.Timeout += () => AnimateHeavenFire(ascensionPanel);
            ascensionPanel.AddChild(timer);
        }

        if (timer.IsStopped())
            timer.Start();

        ascensionPanel.SetMeta(HeavenFireActiveMeta, true);
    }

    private static void EnsureHeavenEmberTimer(NAscensionPanel ascensionPanel)
    {
        Timer? timer = ascensionPanel.GetNodeOrNull<Timer>(HeavenEmberTimerName);
        if (timer == null)
        {
            timer = new Timer
            {
                Name = HeavenEmberTimerName,
                WaitTime = 0.2,
                OneShot = false,
                Autostart = false,
            };
            timer.Timeout += () => SpawnHeavenEmber(ascensionPanel);
            ascensionPanel.AddChild(timer);
        }

        if (timer.IsStopped())
            timer.Start();
    }

    private static void AnimateHeavenFire(NAscensionPanel ascensionPanel)
    {
        if (!ascensionPanel.IsInsideTree() || ascensionPanel.Ascension != 0 || HeavenState.SelectedOption <= 0)
        {
            RestoreOfficialFireVisuals(ascensionPanel);
            return;
        }

        ShaderMaterial shader = IconHsvRef(ascensionPanel);
        Control? icon = ascensionPanel.GetNodeOrNull<Control>("%AscensionIcon");
        double t = Time.GetTicksMsec() / 1000.0;
        float pulse = 0.5f + 0.5f * Mathf.Sin((float)(t * 1.35));
        float flicker = 0.5f + 0.5f * Mathf.Sin((float)(t * 2.4 + HeavenState.SelectedOption * 0.6f));

        float hue = Mathf.Lerp(0.80f, 0.84f, pulse);
        float value = Mathf.Lerp(0.19f, 0.3f, flicker);
        float scale = Mathf.Lerp(0.99f, 1.03f, pulse);

        shader.SetShaderParameter(HParam, hue);
        shader.SetShaderParameter(VParam, value);
        AscensionLevelRef(ascensionPanel).AddThemeColorOverride(FontOutlineTheme, HeavenOutlineColor);

        if (icon != null)
        {
            icon.Scale = new Vector2(scale, scale);
            icon.Modulate = HeavenIconTint;
        }
    }

    private static void SpawnHeavenEmber(NAscensionPanel ascensionPanel)
    {
        if (!ascensionPanel.IsInsideTree() || ascensionPanel.Ascension != 0 || HeavenState.SelectedOption <= 0)
            return;

        Control? icon = ascensionPanel.GetNodeOrNull<Control>("%AscensionIcon");
        if (icon == null)
            return;

        Node2D container = EnsureHeavenEmberContainer(icon);
        Sprite2D ember = new()
        {
            Texture = GetOrCreateEmberTexture(),
            Centered = true,
            Position = new Vector2(
                (float)GD.RandRange(-18.0, 18.0),
                (float)GD.RandRange(12.0, 24.0)),
            Scale = Vector2.One * (float)GD.RandRange(0.3, 0.65),
            Modulate = HeavenEmberBright.Lerp(HeavenEmberDark, (float)GD.RandRange(0.0, 0.45)),
        };
        ember.Modulate = ember.Modulate with { A = (float)GD.RandRange(0.35, 0.7) };
        container.AddChild(ember);

        Vector2 targetPosition = ember.Position + new Vector2(
            (float)GD.RandRange(-7.0, 7.0),
            (float)GD.RandRange(-24.0, -34.0));
        Vector2 targetScale = Vector2.One * (float)GD.RandRange(0.08, 0.18);
        double duration = GD.RandRange(0.55, 0.9);

        Tween tween = ember.CreateTween().SetParallel(true);
        tween.TweenProperty(ember, "position", targetPosition, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ember, "scale", targetScale, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ember, "modulate:a", 0.0f, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Finished += ember.QueueFree;
    }

    private static void RestoreOfficialFireVisuals(NAscensionPanel ascensionPanel)
    {
        Timer? timer = ascensionPanel.GetNodeOrNull<Timer>(HeavenFireTimerName);
        if (timer != null && !timer.IsStopped())
            timer.Stop();

        Timer? emberTimer = ascensionPanel.GetNodeOrNull<Timer>(HeavenEmberTimerName);
        if (emberTimer != null && !emberTimer.IsStopped())
            emberTimer.Stop();

        if (!ascensionPanel.HasMeta(HeavenFireActiveMeta))
            return;

        ascensionPanel.RemoveMeta(HeavenFireActiveMeta);
        ascensionPanel.Call(NAscensionPanel.MethodName.SetFireRed);

        Control? icon = ascensionPanel.GetNodeOrNull<Control>("%AscensionIcon");
        if (icon != null)
        {
            icon.Scale = Vector2.One;
            icon.Modulate = Colors.White;

            Node? emberContainer = icon.GetNodeOrNull(HeavenEmberContainerName);
            emberContainer?.QueueFree();
        }
    }

    private static Node2D EnsureHeavenEmberContainer(Control icon)
    {
        Node2D? container = icon.GetNodeOrNull<Node2D>(HeavenEmberContainerName);
        if (container != null)
            return container;

        container = new Node2D
        {
            Name = HeavenEmberContainerName,
            Position = icon.Size * 0.5f,
            ZIndex = 2,
        };
        icon.AddChild(container);
        return container;
    }

    private static Texture2D GetOrCreateEmberTexture()
    {
        if (_emberTexture != null)
            return _emberTexture;

        Image image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        Vector2 center = new(7.5f, 7.5f);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distance = center.DistanceTo(new Vector2(x, y));
                float alpha = Mathf.Clamp(1f - distance / 7.5f, 0f, 1f);
                alpha *= alpha;
                image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        _emberTexture = ImageTexture.CreateFromImage(image);
        return _emberTexture;
    }

    private static string GetHeavenTitle(int level) => HeavenState.GetFeatureTitle(level);

    private static string GetHeavenDescription(int level) => HeavenState.GetDescription(level);
}
