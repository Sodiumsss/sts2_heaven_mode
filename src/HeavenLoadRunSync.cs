using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace HeavenMode;

internal static class HeavenLoadRunSync
{
    private sealed class Binding
    {
        public required LoadRunLobby Lobby { get; init; }
        public required NMultiplayerLoadGameScreen Screen { get; set; }
        public required int LoadedLevel { get; set; }
        public required MessageHandlerDelegate<LobbyHeavenLevelChangedMessage> HeavenChangedHandler { get; init; }
        public required MessageHandlerDelegate<LobbyHeavenLevelRequestMessage> HeavenRequestHandler { get; init; }
    }

    private static readonly Dictionary<LoadRunLobby, Binding> Bindings = new();

    public static void RegisterLobby(NMultiplayerLoadGameScreen screen)
    {
        LoadRunLobby lobby = GetLobby(screen);
        if (lobby.NetService.Type == NetGameType.Singleplayer)
            return;

        if (Bindings.TryGetValue(lobby, out Binding? existing))
        {
            existing.Screen = screen;
            return;
        }

        int loadedLevel = 0;
        if (lobby.NetService.Type == NetGameType.Host)
        {
            loadedLevel = HeavenPersistence.LoadSelection(lobby.Run.StartTime);
            if (loadedLevel == 0 && HeavenState.SelectedOption > 0)
            {
                // No persistence file (run predates the feature, or file was lost).
                // The UI panel setup hasn't fired yet at postfix time, so SelectedOption
                // still holds the correct in-memory level from the ongoing session.
                loadedLevel = HeavenState.SelectedOption;
                // Write it now so host restarts and future reconnects also work.
                HeavenPersistence.SaveSelection(lobby.Run.StartTime, loadedLevel);
                Log.Info($"[HeavenMode] No persistence file for startTime={lobby.Run.StartTime}; captured in-memory Heaven level {loadedLevel}");
            }
            else
            {
                Log.Info($"[HeavenMode] Restored Heaven level {loadedLevel} for loaded multiplayer run startTime={lobby.Run.StartTime}");
            }
            HeavenState.SelectedOption = loadedLevel;
        }

        Binding binding = new()
        {
            Lobby = lobby,
            Screen = screen,
            LoadedLevel = loadedLevel,
            HeavenChangedHandler = (message, senderId) => HandleHeavenChanged(lobby, message, senderId),
            HeavenRequestHandler = (_, senderId) => HandleHeavenRequest(lobby, senderId),
        };

        lobby.NetService.RegisterMessageHandler(binding.HeavenChangedHandler);
        lobby.NetService.RegisterMessageHandler(binding.HeavenRequestHandler);
        Bindings[lobby] = binding;
        Log.Info($"[HeavenMode] Registered loaded-run Heaven sync for {lobby.NetService.Type} lobby");
    }

    public static void UnregisterLobby(NMultiplayerLoadGameScreen screen)
    {
        LoadRunLobby? lobby = TryGetLobby(screen);
        if (lobby == null)
            return;

        if (!Bindings.Remove(lobby, out Binding? binding))
            return;

        lobby.NetService.UnregisterMessageHandler(binding.HeavenChangedHandler);
        lobby.NetService.UnregisterMessageHandler(binding.HeavenRequestHandler);
        Log.Info($"[HeavenMode] Unregistered loaded-run Heaven sync for {lobby.NetService.Type} lobby");
    }

    public static void OnLobbyOpened(NMultiplayerLoadGameScreen screen)
    {
        LoadRunLobby? lobby = TryGetLobby(screen);
        if (lobby == null)
            return;

        if (lobby.NetService.Type != NetGameType.Client)
            return;

        ulong hostId = GetHostId(lobby);
        lobby.NetService.SendMessage(new LobbyHeavenLevelRequestMessage(), hostId);
        Log.Info($"[HeavenMode] Requested Heaven level for loaded multiplayer run from host {hostId}");
    }

    public static void OnPlayerConnected(NMultiplayerLoadGameScreen screen, ulong playerId)
    {
        LoadRunLobby? lobby = TryGetLobby(screen);
        if (lobby == null)
            return;

        if (lobby.NetService.Type != NetGameType.Host || playerId == lobby.NetService.NetId)
            return;

        SendCurrentLevel(lobby, playerId);
    }

    private static void HandleHeavenChanged(
        LoadRunLobby lobby,
        LobbyHeavenLevelChangedMessage message,
        ulong senderId)
    {
        int level = Math.Clamp(message.HeavenLevel, 0, HeavenState.MaxLevel);
        HeavenState.SelectedOption = level;
        if (Bindings.TryGetValue(lobby, out Binding? binding))
            binding.LoadedLevel = level;
        Log.Info($"[HeavenMode] Received loaded-run Heaven level {level} from player {senderId}");
    }

    private static void HandleHeavenRequest(LoadRunLobby lobby, ulong senderId)
    {
        if (lobby.NetService.Type != NetGameType.Host)
            return;

        SendCurrentLevel(lobby, senderId);
    }

    private static void SendCurrentLevel(LoadRunLobby lobby, ulong? targetPlayerId)
    {
        // Use the level stored at registration time, not HeavenState.SelectedOption, which may have
        // been cleared to 0 by AfterSetAscensionLevel firing during lobby UI updates.
        int level = Bindings.TryGetValue(lobby, out Binding? binding)
            ? binding.LoadedLevel
            : HeavenState.SelectedOption;

        LobbyHeavenLevelChangedMessage message = new()
        {
            HeavenLevel = Math.Clamp(level, 0, HeavenState.MaxLevel),
        };

        if (targetPlayerId.HasValue)
        {
            lobby.NetService.SendMessage(message, targetPlayerId.Value);
            Log.Info($"[HeavenMode] Sent loaded-run Heaven level {message.HeavenLevel} to player {targetPlayerId.Value}");
            return;
        }

        lobby.NetService.SendMessage(message);
        Log.Info($"[HeavenMode] Broadcast loaded-run Heaven level {message.HeavenLevel}");
    }

    private static ulong GetHostId(LoadRunLobby lobby) => lobby.Run.Players.Count > 0
        ? lobby.Run.Players[0].NetId
        : lobby.NetService.NetId;

    private static LoadRunLobby GetLobby(NMultiplayerLoadGameScreen screen) =>
        AccessTools.Field(typeof(NMultiplayerLoadGameScreen), "_runLobby")?.GetValue(screen) as LoadRunLobby
        ?? throw new InvalidOperationException("Load game screen lobby is not initialized.");

    private static LoadRunLobby? TryGetLobby(NMultiplayerLoadGameScreen screen) =>
        AccessTools.Field(typeof(NMultiplayerLoadGameScreen), "_runLobby")?.GetValue(screen) as LoadRunLobby;
}
