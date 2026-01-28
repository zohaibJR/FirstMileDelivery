using UnityEngine;
using System.IO;

public static class SaveSystem
{
    static string path = Application.persistentDataPath + "/playerData.json";

    public static bool HasSave()
    {
        return File.Exists(path);
    }

    public static void Save(PlayerData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log("Saved: " + path);
    }

    public static PlayerData Load()
    {
        if (!HasSave()) return null;
        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<PlayerData>(json);
    }

    public static void DeleteSave()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Old save deleted.");
        }
    }
}
