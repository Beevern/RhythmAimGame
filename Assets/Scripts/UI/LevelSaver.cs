using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.UI;
using TMPro;
public class LevelSaver : MonoBehaviour
{
    public string folderName; // The name of the folder to create

    public ArrayList dataList = new ArrayList(); // Your ArrayList to save
    public string fileName = "times.json"; // The name of the JSON file
    public string folderPath;

    // Method to save a file to a new folder
    public void SaveFiles(string[] filePaths)
    {
        // Define the path to the new folder
        folderName = FileSelection.theName;
        folderPath = Path.Combine(Application.persistentDataPath, folderName);

        // Create the folder if it does not exist
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log("Created folder: " + folderPath);
        }

        // Copy each selected file to the new folder
        foreach (string filePath in filePaths)
        {
            string fileName = Path.GetFileName(filePath);
            string destinationPath = Path.Combine(folderPath, fileName);
            
            // Copy the file to the new location
            File.Copy(filePath, destinationPath, true);
        }
    }
    public void SaveArrayListData()
    {
        //dataList = LevelMaker.times;
        // Convert the ArrayList to a JSON string
        foreach (GameObject marker in dataList)
        {
            Debug.Log(marker.GetComponent<Marker>().myTime);
        }
        string json = JsonUtility.ToJson(new SerializableArrayList(dataList));
        
        // Define the file path
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        // Save the JSON string to the file
        File.WriteAllText(filePath, json);
    }

    // Method to load the ArrayList data from JSON
    public void LoadArrayListData()
    {
        // Define the file path
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        // Check if the file exists
        if (File.Exists(filePath))
        {
            // Read the JSON string from the file
            string json = File.ReadAllText(filePath);

            // Convert the JSON string back into an ArrayList
            SerializableArrayList loadedData = JsonUtility.FromJson<SerializableArrayList>(json);
            dataList = loadedData.ToArrayList();

            Debug.Log("ArrayList data loaded: " + json);
        }
        else
        {
            Debug.LogError("Data file not found: " + filePath);
        }
    }
    
}

// A helper class to serialize an ArrayList
[System.Serializable]
public class SerializableArrayList
{
    public ArrayList list = new ArrayList();

    public SerializableArrayList(ArrayList arrayList)
    {
        list = arrayList;
    }

    // Converts the SerializableArrayList back into a regular ArrayList
    public ArrayList ToArrayList()
    {
        return new ArrayList(list);
    }
}