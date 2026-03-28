using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;
public class CustomLevelManager : MonoBehaviour
{

    public static CustomLevelManager Instance;

    private string savePath;
    public CustomLevelList levelList = new CustomLevelList();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "custom_levels.json");
        LoadLevels();
    }

    public void AddLevel(CustomLevelInfo info)
    {
        levelList.levels.Add(info);
        SaveLevels();
        // Here you can also immediately register it with your LevelHolder
        // e.g. LevelHolder.Instance.RegisterCustomLevel(info);
    }

    void SaveLevels()
    {
        string json = JsonUtility.ToJson(levelList, true);
        File.WriteAllText(savePath, json);  // persists between runs[web:6]
        Debug.Log("Saved custom levels to: " + savePath + "\n" + json);
    }

    void LoadLevels()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            levelList = JsonUtility.FromJson<CustomLevelList>(json);
        }
        else
        {
            levelList = new CustomLevelList();
        }
    }
}
[Serializable]
public class CustomLevelInfo
{
    public string levelName;     // Whatever you want to show in UI
    public string songFilePath;  // Absolute path to song (or folder)
    public string imageFilePath; // Path to cover image
    public string noteFilePath; // Example: your note data, etc.
    public string heldFilePath; // Held note data
}

[Serializable]
public class CustomLevelList
{
    public List<CustomLevelInfo> levels = new List<CustomLevelInfo>();
}