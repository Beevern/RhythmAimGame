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
        // dataList holds marker times (doubles). JsonUtility can't serialise an ArrayList,
        // so copy them into a typed list first.
        string json = JsonUtility.ToJson(new SerializableArrayList(dataList), true);
        
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

// A helper class to serialize the marker times (JsonUtility only handles typed lists)
[System.Serializable]
public class SerializableArrayList
{
    public System.Collections.Generic.List<double> list = new System.Collections.Generic.List<double>();

    public SerializableArrayList(ArrayList arrayList)
    {
        foreach (object item in arrayList)
        {
            list.Add(System.Convert.ToDouble(item));
        }
        list.Sort();
    }

    // Converts the SerializableArrayList back into a regular ArrayList
    public ArrayList ToArrayList()
    {
        return new ArrayList(list);
    }
}
