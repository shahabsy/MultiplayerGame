using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerConnectionManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _maxPlayers = 2;

    // Events for game logic
    public event Action OnHostStarted;
    public event Action OnClientJoined;
    public event Action<string> OnJoinCodeReceived;

    private string _playerId;
    private Lobby _currentLobby;
    private Allocation _hostAllocation;
    private bool _isHosting = false;
    private CancellationTokenSource _cts;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        DontDestroyOnLoad(gameObject);

        _cts = new CancellationTokenSource();
        await InitializeServices();
        SetupNetworkCallbacks();
        UpdateStatus("Ready. Host a game or enter join code.");
    }

    private void UpdateStatus(string message)
    {
        Debug.Log(message);
    }

    private async Task InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _playerId = AuthenticationService.Instance.PlayerId;

            Debug.Log($"Signed in as: {_playerId}");
            UpdateStatus($"Online. Player ID: {_playerId.Substring(0, 8)}...");
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            UpdateStatus($"Error: {e.Message}");
        }
    }

    private void SetupNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) { return; }

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
    }

    #region Host Game
    public async void HostGame(string lobbyName = null)
    {
        string name = string.IsNullOrEmpty(lobbyName) ? "Game Lobby" : lobbyName.Trim();
        UpdateStatus($"Creating relay allocation...");

        try
        {
            // step-1: create relay allocation
            _hostAllocation = await RelayService.Instance.CreateAllocationAsync(_maxPlayers);

            // step-2: get join code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(_hostAllocation.AllocationId);

            // step3: configure unitytransfort with relay (updated API for unity)
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(_hostAllocation, "dtls"));

            // step-4: start host
            NetworkManager.Singleton.StartHost();
            _isHosting = true;

            OnJoinCodeReceived?.Invoke(joinCode);
            await CreateLobbyInService(lobbyName, joinCode);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to host: {e.Message}");
            UpdateStatus($"Error: {e.Message}");
        }
    }

    private async Task CreateLobbyInService(string lobbyName, string joinCode)
    {
        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            lobbyName = $"Lobby_{_playerId.Substring(0, 8)}";
        }
        lobbyName = lobbyName.Trim();

        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, _maxPlayers, options);

            _ = HeartbeatLoop(_cts.Token);
            _ = UpdateLobbyPlayersLoop(_cts.Token);

            Debug.Log($"Lobby created: {_currentLobby.Id}");
            OnHostStarted?.Invoke();
        } 
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");
        }
    }
    #endregion

    #region Join Game
    public async void JoinFirstAvailableLobby()
    {
        UpdateStatus("Searching for lobbies...");

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            if (response.Results.Count == 0)
            {
                UpdateStatus("No available lobbies found.");
                return;
            }

            Lobby lobby = response.Results[0];
            string joinCode = lobby.Data != null && lobby.Data.ContainsKey("joinCode")
                ? lobby.Data["joinCode"].Value
                : null;

            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogWarning($"Lobby '{lobby.Name}' has no join code, cannot join.");
                UpdateStatus($"Error: Lobby '{lobby.Name}' has no join code.");
                return;
            }
            await JoinWithCodeAsync(joinCode);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join: {e.Message}");
            UpdateStatus($"Error: Invalid join code or host not available");
            //_joinButton.interactable = true;
        }
    }
    private async Task JoinWithCodeAsync(string joinCode)
    {
        UpdateStatus($"Joining lobby with code: {joinCode}...");

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

            NetworkManager.Singleton.StartClient();
            UpdateStatus("Connected to host!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join: {e.Message}");
            UpdateStatus($"Error: {e.Message}");
        }
    }
    #endregion

    #region Lobby Maintenance
    private async Task HeartbeatLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _isHosting && _currentLobby != null)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
                Debug.Log("Lobby heartbeat sent");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Heartbeat failed: {e.Message}");
            }
            await Task.Delay(15000, token);
        }
    }

    private async Task UpdateLobbyPlayersLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _isHosting && _currentLobby != null)
        {
            try
            {
                _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update lobby: {e.Message}");
            }
            await Task.Delay(5000, token);
        }
    }
    #endregion

    #region Network callbacks

    private void OnServerStarted()
    {
        UpdateStatus("Server running! Players can join from anywhere.");
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            UpdateStatus($"Player {clientId} joined! Total: {NetworkManager.Singleton.ConnectedClients.Count}/{_maxPlayers}");
        }
        else
        {
            UpdateStatus("Connected to host!"); 
            OnClientJoined?.Invoke();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            UpdateStatus($"Player left. Players: {NetworkManager.Singleton.ConnectedClients.Count - 1}");
        }
        else
        {
            UpdateStatus("Disconnected from host.");
        }
    }

    private void OnServerStopped(bool obj)
    {
        _isHosting = false;
        UpdateStatus("Server stopped");
    }
    #endregion

    private void OnDestroy()
    {
        // Cancel background tasks
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        if (_currentLobby != null && _isHosting)
        {
            try
            {
                LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete lobby: {e.Message}");
            }
        }

        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
