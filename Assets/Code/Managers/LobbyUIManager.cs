using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerThumbnailPrefab;

    private Dictionary<ulong, PlayerThumbnail> _thumbnails = new Dictionary<ulong, PlayerThumbnail>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        Debug.Log("LobbyUIManager: coroutine will subscrit to events.");
        StartCoroutine(InitializeLobbyUI());
    }
    private IEnumerator InitializeLobbyUI()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient);
        
        yield return new WaitForSeconds(0.5f); // Small delay to ensure all clients have spawned their LobbyPlayer

        float startTime = Time.time;
        const float timeout = 5f;
        LobbyPlayer localPlayer = null;

        while (Time.time - startTime < timeout)
        {
            localPlayer = FindLocalLobbyPlayer();
            if (localPlayer != null) break;
            yield return null;
        }
        if (localPlayer == null)
        {
            Debug.LogWarning("LobbyUIManager: Failed to find local LobbyPlayer within timeout.");
            //yield break;
        } else
        {
            Debug.Log("LobbyUIManager: Local LobbyPlayer found: " + localPlayer.PlayerName.Value);
        }

        Debug.Log("LobbyUIManager: InitializeLobbyUI.");
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        LobbyPlayer.OnPlayerSpawned += OnLobbyPlayerSpawned;
        LobbyPlayer.OnPlayerDespawned += OnLobbyPlayerDespawned;

        RefreshAllThumbnails();

        DumpCurrentLobbyPlayers("After InitializedLobbyUI subscription and RefreshAllThumbnails");
    }

    private void DumpCurrentLobbyPlayers(string v)
    {
        if (NetworkManager.Singleton == null) return;

        ulong locaId = NetworkManager.Singleton.LocalClientId;
        var players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

        foreach(var p in players)
        {
            Debug.Log($"LobbyUIManager: {v} - Found LobbyPlayer: ClientId={p.OwnerClientId}, PlayerName={p.PlayerName.Value}, IsOwner={p.IsOwner}, IsReady={p.IsReady.Value}");
        }
    }

    private LobbyPlayer FindLocalLobbyPlayer()
    {
        Debug.Log("LobbyUIManager: FinalLocalLobbyPlayer.");
        var players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        foreach (var p in players)
        {
            if (p.IsOwner) return p;
        }
        return null;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"LobbyUIManager: OnClientConnected Called");
        StartCoroutine(AddThumbnailAfterSpawn(clientId));
    }

    private IEnumerator AddThumbnailAfterSpawn(ulong clientId)
    {
        Debug.Log("LobbyUIManager: addThumbnailAfterSpawn.");
        yield return null;
        
        LobbyPlayer lobbyPlayer = null;
        var players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.OwnerClientId == clientId)
            {
                lobbyPlayer = p;
                break;
            }
        }

        if (lobbyPlayer != null)
        {
            CreateThumbnailForPlayer(clientId, lobbyPlayer);
        }
        else
        {
            Debug.LogWarning($"LobbyUIManager: Could not find LobbyPlayer for client {clientId} after connection.");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"LobbyUIManager: OnClientDisconnected Called");
        if (_thumbnails.TryGetValue(clientId, out var thumbnail))
        {
            Destroy(thumbnail.gameObject);
            _thumbnails.Remove(clientId);
        }
    }

    private void RefreshAllThumbnails()
    {
        Debug.Log("LobbyUIManager: RefreshAllThumbnails.");
        if (NetworkManager.Singleton == null) return;

        foreach (var kvp in _thumbnails)
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);

        _thumbnails.Clear();

        var lobbyPlayers = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

        foreach (var lobbyPlayer in lobbyPlayers)
        {
            var clientId = lobbyPlayer.OwnerClientId;
            if (!_thumbnails.ContainsKey(clientId))
            {
                Debug.Log("Creating thumbnail for clientId: " + clientId);
                CreateThumbnailForPlayer(clientId, lobbyPlayer);   
            }
        }
    }

    private void CreateThumbnailForPlayer(ulong clientId, LobbyPlayer lobbyPlayer)
    {
        Debug.Log("LobbyUIManager: CreateThumbnailForPlayer.");
        if (lobbyPlayer == null || _thumbnails.ContainsKey(clientId)) return;

        GameObject go = Instantiate(playerThumbnailPrefab, playerListContainer);
        var thumbnail = go.GetComponent<PlayerThumbnail>();
        thumbnail.Init(lobbyPlayer, clientId); 
        _thumbnails[clientId] = thumbnail;
    }

    private void OnLobbyPlayerSpawned(LobbyPlayer lobbyPlayer)
    {
        Debug.Log("LobbyUIManager: OnLobbyPlayerSpawned.");
        if (lobbyPlayer == null) return;
        var clientId = lobbyPlayer.OwnerClientId;
        if (!_thumbnails.ContainsKey(clientId))
        {
            Debug.Log("OnLobbyPlayerSpawned: Creating thumbnail for clientId: " + clientId);
            CreateThumbnailForPlayer(clientId, lobbyPlayer);
        }
    }

    private void OnLobbyPlayerDespawned(ulong clientId)
    {
        Debug.Log("LobbyUIManager: OnLobbyPlayerDespawned.");
        if (_thumbnails.TryGetValue(clientId, out var thumbnail))
        {
            Destroy(thumbnail.gameObject);
            _thumbnails.Remove(clientId);
        }
    }
    public void OnDestroy()
    {
        Debug.Log("LobbyUIManager: OnDestroy.");
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        LobbyPlayer.OnPlayerSpawned -= OnLobbyPlayerSpawned;
        LobbyPlayer.OnPlayerDespawned -= OnLobbyPlayerDespawned;
    }
}