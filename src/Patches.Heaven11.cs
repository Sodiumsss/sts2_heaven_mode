using System;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HeavenMode;

[HarmonyPatch(typeof(NPauseMenu))]
internal static class Patches_Heaven11
{
    private static bool _saveQuitConfirmationOpen;

    private static readonly AccessTools.FieldRef<NGenericPopup, NVerticalPopup> VerticalPopupRef =
        AccessTools.FieldRefAccess<NGenericPopup, NVerticalPopup>("_verticalPopup");

    [HarmonyPrefix]
    [HarmonyPatch("OnSaveAndQuitButtonPressed")]
    private static bool BeforeOnSaveAndQuitButtonPressed(NPauseMenu __instance)
    {
        try
        {
            if (!HeavenState.ShouldDestroySaveOnQuit)
                return true;

            if (_saveQuitConfirmationOpen)
                return false;

            TaskHelper.RunSafely(ShowDestroySaveConfirmation(__instance));
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] BeforeOnSaveAndQuitButtonPressed failed: {ex}");
            return true;
        }
    }

    private static async Task ShowDestroySaveConfirmation(NPauseMenu pauseMenu)
    {
        _saveQuitConfirmationOpen = true;

        try
        {
            NGenericPopup? popup = NGenericPopup.Create();
            if (popup == null)
            {
                Log.Warn("[HeavenMode] Failed to create NGenericPopup for Heaven 11 save-and-quit confirmation");
                return;
            }

            if (NModalContainer.Instance == null)
            {
                Log.Warn("[HeavenMode] NModalContainer unavailable for Heaven 11 save-and-quit confirmation");
                return;
            }

            NModalContainer.Instance.Add((Node)popup);

            if (!popup.IsNodeReady())
                await popup.ToSignal(popup, Node.SignalName.Ready);

            TaskCompletionSource<bool> confirmation = new();
            NVerticalPopup? verticalPopup = VerticalPopupRef(popup);
            if (verticalPopup == null)
            {
                Log.Warn("[HeavenMode] NGenericPopup vertical popup was unavailable for Heaven 11 confirmation");
                return;
            }

            verticalPopup.SetText(
                Loc.Get("HEAVEN11_SAVE_QUIT_CONFIRM_TITLE", "No Turning Back"),
                Loc.Get(
                    "HEAVEN11_SAVE_QUIT_CONFIRM_BODY",
                    "There is no retreat in the Spire.\nSave and Quit will destroy this run."));
            verticalPopup.InitYesButton(
                new LocString("main_menu_ui", "GENERIC_POPUP.confirm"),
                _ => confirmation.TrySetResult(true));
            verticalPopup.InitNoButton(
                new LocString("main_menu_ui", "GENERIC_POPUP.cancel"),
                _ => confirmation.TrySetResult(false));

            bool confirmed = await confirmation.Task;
            if (!confirmed)
            {
                Log.Info("[HeavenMode] Heaven 11 save-and-quit destruction cancelled");
                return;
            }

            await DestroySaveAndReturnToMenu(pauseMenu);
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] ShowDestroySaveConfirmation failed: {ex}");
        }
        finally
        {
            _saveQuitConfirmationOpen = false;
        }
    }

    private static async Task DestroySaveAndReturnToMenu(NPauseMenu pauseMenu)
    {
        try
        {
            DisablePauseMenu(pauseMenu);

            RunManager.Instance.ActionQueueSet.Reset();
            NRunMusicController.Instance?.StopMusic();

            if (SaveManager.Instance.CurrentRunSaveTask != null)
            {
                try
                {
                    await SaveManager.Instance.CurrentRunSaveTask;
                }
                catch (Exception ex)
                {
                    Log.Warn($"[HeavenMode] Save task failed before Heaven 11 cleanup: {ex.Message}");
                }
            }

            switch (RunManager.Instance.NetService.Type)
            {
                case NetGameType.Singleplayer:
                    SaveManager.Instance.DeleteCurrentRun();
                    break;
                case NetGameType.Host:
                    SaveManager.Instance.DeleteCurrentMultiplayerRun();
                    break;
            }

            HeavenPersistence.ClearCurrentRunSelection();
            Log.Info("[HeavenMode] Heaven 11 intercepted Save and Quit; deleted current run save instead");

            if (NGame.Instance != null)
                await NGame.Instance.ReturnToMainMenu();
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] DestroySaveAndReturnToMenu failed: {ex}");
        }
    }

    private static void DisablePauseMenu(NPauseMenu pauseMenu)
    {
        try
        {
            pauseMenu.GetNode("%BackButton").Call("Disable");

            string[] buttonNodeNames =
            {
                "%ButtonContainer/Resume",
                "%ButtonContainer/Settings",
                "%ButtonContainer/Compendium",
                "%ButtonContainer/GiveUp",
                "%ButtonContainer/Disconnect",
                "%ButtonContainer/SaveAndQuit",
            };

            foreach (string nodeName in buttonNodeNames)
                pauseMenu.GetNode(nodeName).Call("Disable");
        }
        catch (Exception ex)
        {
            Log.Warn($"[HeavenMode] DisablePauseMenu failed: {ex.Message}");
        }
    }
}
