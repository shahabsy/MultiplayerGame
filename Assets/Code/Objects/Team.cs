using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Team
{
    public List<Player> Players;
    public Team()
    {
        Players = new List<Player>();
    }

    public void DestroyTeam()
    {
        
    }

    public void RemovePlayer(Player player)
    {
        Players.Remove(player);
    }

    public int GetTeamSize() => Players.Count;

    public Player GetPlayerWithId(string guid)
    {
        return Players.FirstOrDefault(p => p.GUID == guid);
    }

    public void UnreadyAll()
    {
        foreach (Player player in Players)
        {
            player.IsReadyToStartGame = false;
        }
    }


}
