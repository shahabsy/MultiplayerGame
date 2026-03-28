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
    [Header("UI References")]
    [SerializeField] private TMP_InputField _lobbyNameInput;
    [SerializeField] private TMP_InputField _joinCodeInput;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _refreshButton;
    [SerializeField] private TMP_Text _joinCodeDisplayText;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private TMP_Text _playerCountText;
    [SerializeField] private Transform _lobbyListContainer;
    [SerializeField] private Transform _lobbyButtonPrefab;

    [Header("Settings")]
    [SerializeField] private int _maxPlayers = 2;

    private string _playerId;
    private Lobby _currentLobby;
    private Allocation _hostAllocation;
    private bool _isHosting = false;
    private List<GameObject> _lobbyButtons = new List<GameObject>();
    private CancellationTokenSource _cts;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        _cts = new CancellationTokenSource();
        await InitializeServices();
        SetupUI();
        SetupNetworkCallbacks();
        UpdateStatus("Ready. Host a game or enter join code.");
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

    private void SetupUI()
    {
        if (_hostButton != null) { _hostButton.onClick.AddListener(HostGame); }
        if (_joinButton != null) { _joinButton.onClick.AddListener(JoinWithCode); }
        if (_refreshButton != null) { _refreshButton.onClick.AddListener(async () => await RefreshLobbies()); }
    }

    private void SetupNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) { return; }

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
    }

    private async Task RefreshLobbies()
    {
        UpdateStatus("Searching for lobbies...");
        ClearLobbyList();

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 20,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            foreach (Lobby lobby in response.Results)
            {
                AddLobbyToUI(lobby);
            }

            UpdateStatus($"Found {response.Results.Count} lobbies");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to query lobbies: {e.Message}");
            UpdateStatus($"Error: {e.Message}");
        }
    }

    #region Host Game
    public async void HostGame()
    {
        string lobbyName = _lobbyNameInput != null &&
            !string.IsNullOrEmpty(_lobbyNameInput.text)
            ? _lobbyNameInput.text : "Game Lobby";

        UpdateStatus($"Creating relay allocation...");
        _hostButton.interactable = false;

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

            // step-5: display join code
            if (_joinCodeDisplayText != null)
            {
                _joinCodeDisplayText.text = $"Join code: {joinCode}";
            }

            await CreateLobbyInService(lobbyName, joinCode);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to host: {e.Message}");
            UpdateStatus($"Error: {e.Message}");
            _hostButton.interactable = true;
        }
    }

    private async Task CreateLobbyInService(string lobbyName, string joinCode)
    {
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
        } 
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");
        }
    }
    #endregion

    #region Join Game
    public async void JoinWithCode()
    {
        string joinCode = _joinCodeInput != null ? _joinCodeInput.text : "";

        if (string.IsNullOrEmpty(joinCode))
        {
            UpdateStatus($"Please enter a join code!");
            return;
        }

        UpdateStatus($"Joining with code: {joinCode}...");
        _joinButton.interactable = false;

        try
        {
            // step-1: join Relay using join code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // step-2: configure unity transport with relay 
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

            // step-3: start client
            NetworkManager.Singleton.StartClient();
            UpdateStatus("Connected to host!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join: {e.Message}");
            UpdateStatus($"Error: Invalid join code or host not available");
            _joinButton.interactable = true;
        }
    }
    #endregion

    #region Lobby Browser

    private void AddLobbyToUI(Lobby lobby)
    {
        if (_lobbyButtonPrefab == null || _lobbyListContainer == null) return;

        GameObject buttonObj = Instantiate(_lobbyButtonPrefab, _lobbyListContainer).gameObject;

        Button button = buttonObj.GetComponent<Button>();
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonText == null) buttonText = buttonObj.GetComponent<TMP_Text>();

        string joinCode = lobby.Data != null && lobby.Data.ContainsKey("joinCode")
            ? lobby.Data["joinCode"].Value
            : null;
        if(string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning($"Lobby '{lobby.Name}' has no join code, cannot join.");
            buttonText.text = $"{lobby.Name} ({lobby.Players.Count}/{lobby.MaxPlayers}) - [NO CODE]";
            button.interactable = false;
            return;
        }
        button.onClick.AddListener(() => JoinWithCodeFromLobby(joinCode));
        _lobbyButtons.Add(buttonObj);
    }

    private async void JoinWithCodeFromLobby(string joinCode)
    {
        _joinCodeInput.text = joinCode;
        await JoinWithCodeAsync(joinCode);
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

    private void ClearLobbyList()
    {
        foreach (var button in _lobbyButtons)
        {
            if (button != null) { Destroy(button); }
        }
        _lobbyButtons.Clear();
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
                UpdatePlayerCount();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to update lobby: {e.Message}");
            }
            await Task.Delay(5000, token);
        }
    }

    private void UpdatePlayerCount()
    {
        if (_playerCountText != null && _currentLobby != null)
        {
            _playerCountText.text = $"Players: {_currentLobby.Players.Count}/{_currentLobby.MaxPlayers}";
        }
        else if (_playerCountText != null && NetworkManager.Singleton != null)
        {
            _playerCountText.text = $"Players: {NetworkManager.Singleton.ConnectedClients.Count}/{_maxPlayers}";
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
            UpdateStatus($"Player joined! Total: {NetworkManager.Singleton.ConnectedClients.Count}/{_maxPlayers}");
            UpdatePlayerCount();
        }
        else
        {
            UpdateStatus("Connected to host!");
            _joinButton.interactable = true;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            UpdateStatus($"Player left. Players: {NetworkManager.Singleton.ConnectedClients.Count - 1}");
            UpdatePlayerCount();
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
        _hostButton.interactable = true;
        _joinButton.interactable = true;

        if (_refreshButton != null)
            _refreshButton.interactable = true;
    }
    #endregion

    private void UpdateStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }
        Debug.Log($"[Multiplayer] {message}");
    }

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
