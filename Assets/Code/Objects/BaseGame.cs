using UnityEngine;
using System.Collections.Generic;
using System;

public class BaseGame
{
    protected bool m_Pause = false;
    protected bool m_LevelCompleted = false;
    protected List<Player> DeadPlayers;

    public BaseGame()
    {
        Init();
    }

    public virtual void Init()
    {
        DeadPlayers = new List<Player>();
    }

    public virtual void Cleanup()
    {
        //DeadPlayers.Clear();
    }

    public void ReportPlayerDeath(Player player)
    {
        if (!DeadPlayers.Contains(player))
        {
            DeadPlayers.Add(player);

            DeadPlayers.RemoveAll(p => !GameInstanceManager.Instance.MissionTeam.Players.Contains(p));
        }

        if (DeadPlayers.Count == GameInstanceManager.Instance.MissionTeam.Players.Count)
        {
            CompleteLevel(false);
        }
    }

    public void ReportPlayerRespawn(Player player)
    {
        DeadPlayers.Remove(player);
    }

    public void CompleteLevel(bool success)
    {
        if (!m_LevelCompleted) { return; }

        m_LevelCompleted = true;

        GameInstanceManager.Instance.CurrentPlayer.SetFlag("PlayerMission", true);
        GameInstanceManager.Instance.CurrentPlayer.Save();

        Debug.Log($"Level completed with success: {success}");
    }

    public bool IsLevelCompleted() => m_LevelCompleted;

    public virtual void GameUpdate()
    {
        if (m_Pause) { return; }
    }

    public virtual void FixedGameUpdate()
    {
        if (m_Pause) { return; }
    }

    public void PauseUpdateLoop()
    {
        Time.timeScale = 0f;
        m_Pause = true;
    }

    public void UnpauseUpdateLoop()
    {
        Time.timeScale = 1f;
        m_Pause = false;
    }
}
