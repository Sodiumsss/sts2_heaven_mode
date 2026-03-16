using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace HeavenMode;

internal struct LobbyHeavenLevelChangedMessage : INetMessage, IPacketSerializable
{
    public int HeavenLevel;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.VeryDebug;

    public void Serialize(PacketWriter writer) => writer.WriteInt(HeavenLevel, 4);

    public void Deserialize(PacketReader reader) => HeavenLevel = reader.ReadInt(4);
}

internal struct LobbyHeavenLevelRequestMessage : INetMessage, IPacketSerializable
{
    public bool ShouldBroadcast => false;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.VeryDebug;

    public void Serialize(PacketWriter writer)
    {
    }

    public void Deserialize(PacketReader reader)
    {
    }
}

internal static class HeavenMultiplayerSync
{
    private sealed class Binding
    {
        public required StartRunLobby Lobby { get; init; }
        public required NCharacterSelectScreen Screen { get; set; }
        public required MessageHandlerDelegate<LobbyHeavenLevelChangedMessage> HeavenChangedHandler { get; init; }
        public required MessageHandlerDelegate<LobbyHeavenLevelRequestMessage> HeavenRequestHandler { get; init; }
    }

    private static readonly Dictionary<StartRunLobby, Binding> Bindings = new();

    public static void RegisterLobby(NCharacterSelectScreen screen)
    {
        StartRunLobby lobby = GetLobby(screen);
        if (lobby.NetService.Type == NetGameType.Singleplayer)
            return;

        if (Bindings.TryGetValue(lobby, out Binding? existing))
        {
            existing.Screen = screen;
            return;
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
        Log.Info($"[HeavenMode] Registered multiplayer Heaven sync for {lobby.NetService.Type} lobby");
    }

    public static void UnregisterLobby(NCharacterSelectScreen screen)
    {
        StartRunLobby? lobby = TryGetLobby(screen);
        if (lobby == null)
            return;

        if (!Bindings.Remove(lobby, out Binding? binding))
            return;

        lobby.NetService.UnregisterMessageHandler(binding.HeavenChangedHandler);
        lobby.NetService.UnregisterMessageHandler(binding.HeavenRequestHandler);
        Log.Info($"[HeavenMode] Unregistered multiplayer Heaven sync for {lobby.NetService.Type} lobby");
    }

    public static void OnLobbyOpened(NCharacterSelectScreen screen)
    {
        StartRunLobby? lobby = TryGetLobby(screen);
        if (lobby == null)
            return;

        if (lobby.NetService.Type != NetGameType.Client)
            return;

        ulong hostId = GetHostId(lobby);
        lobby.NetService.SendMessage(new LobbyHeavenLevelRequestMessage(), hostId);
        Log.Info($"[HeavenMode] Requested synced Heaven level from host {hostId}");
    }

    public static void OnPlayerConnected(NCharacterSelectScreen screen, LobbyPlayer player)
    {
        StartRunLobby? lobby = TryGetLobby(screen);
        if (lobby == null)
            return;

        if (lobby.NetService.Type != NetGameType.Host || player.id == lobby.NetService.NetId)
            return;

        SendCurrentLevel(lobby, player.id);
    }

    public static void BroadcastCurrentLevel(NCharacterSelectScreen screen)
    {
        StartRunLobby? lobby = TryGetLobby(screen);
        if (lobby == null)
            return;

        if (lobby.NetService.Type != NetGameType.Host)
            return;

        SendCurrentLevel(lobby, null);
    }

    private static void HandleHeavenChanged(
        StartRunLobby lobby,
        LobbyHeavenLevelChangedMessage message,
        ulong senderId)
    {
        int level = Math.Clamp(message.HeavenLevel, 0, HeavenState.MaxLevel);
        HeavenState.SelectedOption = level;
        Log.Info($"[HeavenMode] Received synced Heaven level {level} from player {senderId}");

        if (!Bindings.TryGetValue(lobby, out Binding? binding))
            return;

        Patches_CharacterSelect.ApplySyncedHeavenLevel(binding.Screen, level);
    }

    private static void HandleHeavenRequest(StartRunLobby lobby, ulong senderId)
    {
        if (lobby.NetService.Type != NetGameType.Host)
            return;

        SendCurrentLevel(lobby, senderId);
    }

    private static void SendCurrentLevel(StartRunLobby lobby, ulong? targetPlayerId)
    {
        LobbyHeavenLevelChangedMessage message = new()
        {
            HeavenLevel = Math.Clamp(HeavenState.SelectedOption, 0, HeavenState.MaxLevel),
        };

        if (targetPlayerId.HasValue)
        {
            lobby.NetService.SendMessage(message, targetPlayerId.Value);
            Log.Info($"[HeavenMode] Sent Heaven level {message.HeavenLevel} to player {targetPlayerId.Value}");
            return;
        }

        lobby.NetService.SendMessage(message);
        Log.Info($"[HeavenMode] Broadcast Heaven level {message.HeavenLevel} to multiplayer lobby");
    }

    private static ulong GetHostId(StartRunLobby lobby)
    {
        foreach (LobbyPlayer player in lobby.Players)
        {
            if (player.slotId == 0)
                return player.id;
        }

        return lobby.Players.Count > 0 ? lobby.Players[0].id : lobby.NetService.NetId;
    }

    private static StartRunLobby GetLobby(NCharacterSelectScreen screen) =>
        AccessTools.Field(typeof(NCharacterSelectScreen), "_lobby")?.GetValue(screen) as StartRunLobby
        ?? throw new InvalidOperationException("Character select screen lobby is not initialized.");

    private static StartRunLobby? TryGetLobby(NCharacterSelectScreen screen) =>
        AccessTools.Field(typeof(NCharacterSelectScreen), "_lobby")?.GetValue(screen) as StartRunLobby;
}
