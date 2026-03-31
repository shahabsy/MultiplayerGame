using UnityEngine;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class LobbyScene : MonoBehaviour
{
    [Header("UI References")]
    public Button ReadyButton;
    public Button StartButton;
    public TextMeshProUGUI StatusText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ReadyButton.onClick.AddListener(OnReadyClicked);
        if (NetworkManager.Singleton.IsHost)
        {
            StartButton.gameObject.SetActive(true);
            StartButton.onClick.AddListener(OnStartGameClicked);
        }
        else
        {
            StartButton.gameObject.SetActive(false);
        }
    }

    private LobbyPlayer GetLocalLobbyPlayer()
    {
        var allPlayers = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        return allPlayers.FirstOrDefault(p => p.IsOwner);
    }

    private void OnReadyClicked()
    {
        var localPlayer = GetLocalLobbyPlayer();
        
        if (localPlayer != null)
        {
            localPlayer.ToggleReadyServerRpc();
        }
    }

    private void OnStartGameClicked()
    {
        if (!NetworkManager.Singleton.IsHost) { return; }

        var allPlayers = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        if (allPlayers.Any(p => !p.IsReady.Value)) 
        {
            StatusText.text = "Not all players are ready!";
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene("Mission", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
