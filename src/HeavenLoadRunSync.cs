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

        if (lobby.NetService.Type == NetGameType.Host)
        {
            int level = HeavenPersistence.LoadSelection(lobby.Run.StartTime);
            HeavenState.SelectedOption = level;
            Log.Info($"[HeavenMode] Restored Heaven level {level} for loaded multiplayer run startTime={lobby.Run.StartTime}");
        }

        Binding binding = new()
        {
            Lobby = lobby,
            Screen = screen,
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

    public static void BroadcastCurrentLevel(NMultiplayerLoadGameScreen screen)
    {
        LoadRunLobby? lobby = TryGetLobby(screen);
        if (lobby == null)
            return;

        BroadcastCurrentLevel(lobby);
    }

    public static void BroadcastCurrentLevel(LoadRunLobby lobby)
    {
        if (lobby.NetService.Type != NetGameType.Host)
            return;

        SendCurrentLevel(lobby, null);
    }

    private static void HandleHeavenChanged(
        LoadRunLobby lobby,
        LobbyHeavenLevelChangedMessage message,
        ulong senderId)
    {
        int level = Math.Clamp(message.HeavenLevel, 0, HeavenState.MaxLevel);
        HeavenState.SelectedOption = level;
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
        LobbyHeavenLevelChangedMessage message = new()
        {
            HeavenLevel = Math.Clamp(HeavenState.SelectedOption, 0, HeavenState.MaxLevel),
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
