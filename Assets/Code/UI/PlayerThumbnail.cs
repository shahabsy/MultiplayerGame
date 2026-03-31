using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies;
using System;
using Unity.Collections;

public class PlayerThumbnail : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    public GameObject readyCheckmark;
    private LobbyPlayer _lobbyPlayer;
    private ulong _clientId;

    private void Awake()
    {
        
    }

    public void Init(LobbyPlayer lobbyPlayer, ulong clientId)
    {
        _lobbyPlayer = lobbyPlayer;
        _clientId = clientId;

        UpdateName(_lobbyPlayer.PlayerName.Value);
        UpdateReady(_lobbyPlayer.IsReady.Value);

        _lobbyPlayer.OnDataChanged += OnPlayerDataChanged;
    }

    private void OnPlayerDataChanged(ulong clientId)
    {
        if (clientId == _clientId)
        {
            UpdateName(_lobbyPlayer.PlayerName.Value);
            UpdateReady(_lobbyPlayer.IsReady.Value);
        }
    }

    private void UpdateName(FixedString32Bytes name)
    {
        playerNameText.text = name.ToString();
    }

    private void UpdateReady(bool ready)
    {
        readyCheckmark.SetActive(ready);
    }

    private void OnDestroy()
    {
        if (_lobbyPlayer != null)
        {
            _lobbyPlayer.OnDataChanged -= OnPlayerDataChanged;
        }
    }
}
