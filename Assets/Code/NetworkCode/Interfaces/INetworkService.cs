using System;
using UnityEngine;
using System.Collections.Generic;

namespace RogueFantasyFixel.Network.Interfaces
{
    public enum NetworkState
    {
        Disconnected,
        Connecating,
        Hosing,
        Connected,
        InGame
    }

    public enum MessageType
    {
        PlayerJoin,
        PlayerLeave,
        PlayerReady,
        GameState,
        PlayerInput,
        PlayerPosition,
        PlayerHealth,
        MissionSync,
        ChatMessage,
        Ping,
        Custom
    }
    public interface INetworkService
    {
        event Action<NetworkState> OnNetworkStateChanged;
        //event Action<ulong, PlayerInfo> OnPlayerConnected;
        //event Action <ulong> OnPlayerDisconnected;
        //event Action<NetworkMessage> OnMessageReceived;

        NetworkState CurrentState { get; }
        bool IsHost { get; }
        bool IsClient { get; }
        ulong LocalPlayerId { get; }
        //IReadOnlyDictionary<ulong, PlayerInfo> ConnectedPlayers { get; }

    }
}
