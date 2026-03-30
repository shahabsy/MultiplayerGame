using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using Unity.Multiplayer.PlayMode;

public class GameInstanceManager
{
    public Camera MainCamera;
    public GameObject LoadingScreen;

    public Player CurrentPlayer;
    public Team LobbyTeam;
    public Team MissionTeam;
    public BaseGame CurrentGame;

    public List<string> InGamePlayer;
    public int TeamSize;

    public bool Cheat_Invulnerable;

    private static GameInstanceManager m_instance;

    public static GameInstanceManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new GameInstanceManager();
            }
            return m_instance;
        }
    }

    public GameInstanceManager()
    {
        
    }

    public void InitCamera(Camera camera)
    {
        MainCamera = camera;
    }

    public void InitLoadingScreen(GameObject loadingScreen)
    {
        LoadingScreen = loadingScreen;
    }

    public void ShowLoadingScreen()
    {
        if (LoadingScreen != null)
        {
            LoadingScreen.SetActive(true);
        }
    }

    public void HideLoadingScreen()
    {
        if (LoadingScreen != null)
        {
            LoadingScreen.SetActive(false);
        }
    }

    public bool IsLoadingScreenActive()
    {
        return LoadingScreen != null && LoadingScreen.activeSelf;
    }

    public void InitPlayer()
    {
        CurrentPlayer = Player.Load();
        if (CurrentPlayer == null)
        {
            CurrentPlayer = new Player();
            CurrentPlayer.InitNewPlayer(); // This also saves the new player
        }

        InitPlayerSaveGameSettings();
    }

    public void InitPlayerSaveGameSettings()
    {
        
    }

    public void InitLobbyTeam()
    {
        LobbyTeam = new Team();
        LobbyTeam.Players.Add(CurrentPlayer);
    }

    public void ResetMissionTeam()
    {
        if (MissionTeam != null)
        {
            MissionTeam = null;
        }
    }

    public void SetMissionTeam(Team lobbyTeamData, Team missionTeamIds)
    {
        ResetMissionTeam();
        MissionTeam = new Team();
        foreach (var player in lobbyTeamData.Players)
        {
            if (missionTeamIds.Players.Any(p => p.GUID == player.GUID))
            {
                MissionTeam.Players.Add(player);
            }
        }
        TeamSize = MissionTeam.Players.Count;
    }

    public void CreateGame()
    {
        ResetGameInstance();
        CurrentGame = new BaseGame();
    }

    public void SetPlayersInGame(Team missionTeam)
    {
        InGamePlayer = new List<string>();
        foreach (var player in missionTeam.Players)
        {
            InGamePlayer.Add(player.GUID);
        }
    }

    public void RemoveInGamePlayer(string guid)
    {
        InGamePlayer?.Remove(guid);
    }

    public void PauseCurrentGame()
    {
        CurrentGame.PauseUpdateLoop();
    }

    public void UnpauseCurrentGame()
    {
        CurrentGame.UnpauseUpdateLoop();
    }

    public void ResetGameInstance()
    {
        //TheFactory.Instance.ResetFactory();
    }

    // Update is called once per frame
    public void PersistentUpdate()
    {
        
    }
}
