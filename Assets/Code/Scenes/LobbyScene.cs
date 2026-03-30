using UnityEngine;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class LobbyScene : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform PlayerListContainer;
    public GameObject PlayerListItemPrefab;

    public Button ReadyButton;
    public Button StartButton;
    public TextMeshProUGUI StatusText;

    private Dictionary<ulong, LobbyPlayer> _players = new Dictionary<ulong, LobbyPlayer>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if(NetworkManager.Singleton.IsHost)
        {
            StartButton.gameObject.SetActive(true);
            StartButton.onClick.AddListener(OnStartGameClicked);
        }
        else
        {
            StartButton.gameObject.SetActive(false);
        }
        ReadyButton.onClick.AddListener(OnReadyClicked);

        RefreshPlayerList();
    }

    private void OnClientConnected(ulong clientId)
    {
        RefreshPlayerList();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        foreach(Transform child in PlayerListContainer)
        {
            Destroy(child.gameObject);
        }

        _players.Clear();

        var lobbyPlayers = FindObjectsOfType<LobbyPlayer>();

        foreach(LobbyPlayer lp in lobbyPlayers)
        {
            _players[lp.OwnerClientId] = lp;

            var item = Instantiate(PlayerListItemPrefab, PlayerListContainer);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{lp.PlayerName.Value} {(lp.IsReady.Value ? "Ready" : "Not Ready")}";
            }
            if (lp.IsOwner)
            {
                item.GetComponent<SpriteRenderer>().color = Color.green;
            }

            lp.OnDataChanged += (clientId) => RefreshPlayerList();
        }
    }

    private void OnReadyClicked()
    {
        var localPlayer = FindObjectsOfType<LobbyPlayer>().FirstOrDefault(lp => lp.IsOwner);
        if (localPlayer != null)
        {
            localPlayer.ToggleReadyServerRpc();
        }
    }

    private void OnStartGameClicked()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (_players.Values.Any(p => !p.IsReady.Value)) 
        {
            StatusText.text = "Not all players are ready!";
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene("Mission", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
