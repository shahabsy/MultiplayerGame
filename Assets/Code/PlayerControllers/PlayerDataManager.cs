using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class SerializableDictionary
{
    public List<string> Keys = new List<string>();
    public List<bool> Values = new List<bool>();

    public void FromDictionary(Dictionary<string, bool> dict)
    {
        Keys.Clear();
        Values.Clear();
        foreach (var kvp in dict)
        {
            Keys.Add(kvp.Key);
            Values.Add(kvp.Value);
        }
    }

    public Dictionary<string, bool> ToDictionary()
    {
        var dict = new Dictionary<string, bool>();
        for (int i = 0; i < Keys.Count; i++)
        {
            dict[Keys[i]] = Values[i];
        }
        return dict;
    }
}

[System.Serializable]
public class PlayerSaveWrapper
{
    public string guid;
    public string name;
    public string iconName;
    public bool isReadyToStartGame;
    public SerializableDictionary flags;

    public PlayerSaveWrapper(SerializedPlayerSaveData data)
    {
        guid = data.GUID;
        name = data.Name;
        isReadyToStartGame = data.IsReadyToStartGame;
        flags = new SerializableDictionary();
        flags.FromDictionary(data.Flags);
    }

    public SerializedPlayerSaveData ToSaveData()
    {
        return new SerializedPlayerSaveData
        {
            GUID = guid,
            Name = name,            
            IsReadyToStartGame = isReadyToStartGame,
            Flags = flags.ToDictionary()
        };
    }

}
public class PlayerDataManager
{
    private static string savePath = Path.Combine(Application.persistentDataPath, "playerShipA.json");

    public static void SavePlayer(Player player)
    {
        try
        {
            var data = player.GetSaveData();
            var wrapper = new PlayerSaveWrapper(data);
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Player saved to {savePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving player data: " + ex.Message);
        }
    }

    public static Player LoadPlayer()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                var wrapper = JsonUtility.FromJson<PlayerSaveWrapper>(json);
                var saveData = wrapper.ToSaveData();
                return Player.FromSaveData(saveData);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error loading player data: " + ex.Message);
        }
        return null;
    }
}
