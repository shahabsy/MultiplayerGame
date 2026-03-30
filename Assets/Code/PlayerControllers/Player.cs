using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SerializedPlayerSaveData
{
    public string GUID;
    public string Name;
    public bool IsReadyToStartGame;
    public Dictionary<string, bool> Flags;
}
public class Player
{
    public string GUID;
    public string Name;
    public string IconName;
    public bool IsReadyToStartGame;
    public Dictionary<string, bool> Flags;

    public Player()
    {
        Flags = new Dictionary<string, bool>();
    }

    public void InitNewPlayer()
    {
        GUID = System.Guid.NewGuid().ToString();
        Name = $"Player{Random.Range(1000, 9999)}";
        IsReadyToStartGame = false;
        Flags = new Dictionary<string, bool>();
        Flags["PlayerMission"] = false;

        Save();
    }
    public void SetFlag(string key, bool value)
    {
        Flags[key] = value;
        Save();
    }
    public void Save()
    {
        PlayerDataManager.SavePlayer(this);
    }
    public static Player Load()
    {
        return PlayerDataManager.LoadPlayer();
    }
    public SerializedPlayerSaveData GetSaveData()
    {
        return new SerializedPlayerSaveData
        {
            GUID = GUID,
            Name = Name,
            IsReadyToStartGame = IsReadyToStartGame,
            Flags = Flags
        };
    }
    public static Player FromSaveData(SerializedPlayerSaveData data)
    {
        var player = new Player
        {
            GUID = data.GUID,
            Name = data.Name,
            IsReadyToStartGame = data.IsReadyToStartGame,
            Flags = data.Flags ?? new Dictionary<string, bool>()
        };
        return player;
    }
   



   

    

    

}
