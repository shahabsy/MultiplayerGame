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
        StartCoroutine(InitializeLobbyUI());
    }
    private IEnumerator InitializeLobbyUI()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient);

        yield return new WaitUntil(() => NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(NetworkManager.Singleton.LocalClientId) != null);

        Debug.Log("LobbyUIManager: Network ready, subscriting to events.");
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        RefreshAllThumbnails();
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"LobbyUIManager: OnClientConnected Called");
        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (playerObject != null)
        {
            var lobbyPlayer = playerObject.GetComponent<LobbyPlayer>();
            if ( lobbyPlayer != null)
            {
                CreateThumbnailForPlayer(clientId, lobbyPlayer);
            }
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
        if (NetworkManager.Singleton == null)
            return;

        foreach (var kvp in _thumbnails)
            Destroy(kvp.Value.gameObject);
        _thumbnails.Clear();

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObject != null)
            {
                Debug.Log("RefreshAllThumbnails: Refreshing player thumbnails for all connected clients.");
                var lobbyPlayer = playerObject.GetComponent<LobbyPlayer>();
                if (lobbyPlayer != null)
                {
                    CreateThumbnailForPlayer(clientId, lobbyPlayer);
                }
            }
        }
    }

    private void CreateThumbnailForPlayer(ulong clientId, LobbyPlayer lobbyPlayer)
    {
        Debug.Log($"LobbyUIManager: Creating thumbnail for client {clientId}.");
        if (lobbyPlayer == null) return;
        if (_thumbnails.ContainsKey(clientId)) return;

        GameObject go = Instantiate(playerThumbnailPrefab, playerListContainer);
        var thumbnail = go.GetComponent<PlayerThumbnail>();
        thumbnail.Init(lobbyPlayer, clientId);
        _thumbnails[clientId] = thumbnail;
    }

    public void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
