using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.Netcode;

public class MissionControlMenuScene : MonoBehaviour
{
    [Header("Buttons")]
    public Button CreateGameBtn;
    public Button JoinGameBtn;

    [Header("Multiplayer")]
    public MultiplayerConnectionManager MultiplayerManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (MultiplayerManager == null)
        {
            MultiplayerManager = FindAnyObjectByType<MultiplayerConnectionManager>();
        }

        if (MultiplayerManager == null)
        {
            Debug.LogError("MultiplayerConnectionManager not found in the scene.");
            return;
        }

        MultiplayerManager.OnHostStarted += OnHostStarted;
        MultiplayerManager.OnClientJoined += OnClientJoined;
        MultiplayerManager.OnJoinCodeReceived += OnJoinCodeReceived;

        if (CreateGameBtn != null) CreateGameBtn.onClick.AddListener(OnCreateGameClicked);
        if(JoinGameBtn != null) JoinGameBtn.onClick.AddListener(OnJoinGameClicked);
    }

    private void OnCreateGameClicked()
    {
        if (MultiplayerManager != null)
        {
            MultiplayerManager.HostGame();
        }
        else
        {
            Debug.LogError("MultiplayerConnectionManager is not assigned.");
        }
        //_multiplayerConnectionManager.HostGame();
        //SceneManager.LoadScene("Lobby");
    }

    private void OnJoinGameClicked()
    {
        MultiplayerManager.JoinFirstAvailableLobby();
        //SceneManager.LoadScene("Lobby");
    }

    private void OnHostStarted()
    {
        //SceneManager.LoadScene("Lobby");
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }
    private void OnClientJoined()
    {
        //SceneManager.LoadScene("Lobby");
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    private void OnJoinCodeReceived(string joinCode)
    {
        Debug.Log($"Join code received: {joinCode}");
        // Optionally display the join code to the host player or copy it to clipboard
    }

    private void OnDestroy()
    {
        if (MultiplayerManager != null)
        {
            MultiplayerManager.OnHostStarted -= OnHostStarted;
            MultiplayerManager.OnClientJoined -= OnClientJoined;
            MultiplayerManager.OnJoinCodeReceived -= OnJoinCodeReceived;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
