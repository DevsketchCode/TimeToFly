// ReplayData.cs
using UnityEngine;
using System.Collections.Generic;
using System.IO; // Required for file operations

[CreateAssetMenu(fileName = "NewReplayData", menuName = "Replay/Replay Data")]
public class ReplayData : ScriptableObject
{
    public string levelIdentifier = "DefaultLevel"; // A unique name for this replay data set
    public List<ReplaySpawnedObjectData> spawnedObjects = new List<ReplaySpawnedObjectData>();
    public List<ReplayPlayerPositionData> playerPositions = new List<ReplayPlayerPositionData>();

    // NEW: Variables to store game outcome data
    public int scoreAtSafePoint;
    public int deathsAtSafePoint;
    public float timeAtSafePoint;

    // --- Debugging / Utility Methods ---

    public void ClearData()
    {
        spawnedObjects.Clear();
        playerPositions.Clear();
        // NEW: Also clear outcome data
        scoreAtSafePoint = 0;
        deathsAtSafePoint = 0;
        timeAtSafePoint = 0f;
        Debug.Log($"Replay data for '{levelIdentifier}' cleared.");
    }

    public void LogData()
    {
        Debug.Log($"--- Replay Data for Level: {levelIdentifier} ---");

        // NEW: Log outcome data
        Debug.Log($"Outcome Data: Score = {scoreAtSafePoint}, Deaths = {deathsAtSafePoint}, Time = {timeAtSafePoint:F2}s");

        // Existing player start position log (ensure it's not null)
        ReplayPlayerPositionData playerStart = playerPositions.Find(p => p.triggerName == "LevelStart");
        if (playerStart != null)
        {
            Debug.Log($"Player Start Position: {playerStart.position}");
        }
        else
        {
            Debug.Log("Player Start Position: Not recorded or found.");
        }


        Debug.Log("--- Player Positions ---");
        if (playerPositions.Count == 0)
        {
            Debug.Log("  No player positions recorded.");
        }
        else
        {
            foreach (var pos in playerPositions)
            {
                Debug.Log($"  [{pos.timeInLevel:F2}s] {pos.triggerName}: {pos.position}");
            }
        }

        Debug.Log("--- Spawned Objects (Initial Positions) ---");
        if (spawnedObjects.Count == 0)
        {
            Debug.Log("  No spawned objects recorded.");
        }
        else
        {
            foreach (var obj in spawnedObjects)
            {
                Debug.Log($"  {obj.objectName}: {obj.initialPosition}");
            }
        }
        Debug.Log("-------------------------------------------");
    }

    // --- Persistence Methods (Save/Load to JSON) ---

    // Get the file path for saving/loading
    private string GetFilePath()
    {
        // Using Application.persistentDataPath for cross-platform persistence
        return Path.Combine(Application.persistentDataPath, $"replayData_{levelIdentifier}.json");
    }

    public void SaveData()
    {
        // Use a serializable container for the data, otherwise ScriptableObject itself isn't directly serializable by JsonUtility for file save/load.
        // JsonUtility.ToJson(this, true) directly serializes ScriptableObject public fields.
        // It's generally better practice to create a separate serializable class for the data structure you save/load from file,
        // and then copy the data from the ScriptableObject into that class for saving, and back from it for loading.
        // However, for simple cases like this, JsonUtility.ToJson(this) can work.

        // Create a serializable data structure to hold all necessary data for JSON serialization.
        // This is a more robust approach than relying on JsonUtility.ToJson(this) for complex ScriptableObjects,
        // as it gives you full control over what gets serialized.
        ReplayDataSerializableContainer serializableContainer = new ReplayDataSerializableContainer
        {
            levelIdentifier = this.levelIdentifier,
            spawnedObjects = new List<ReplaySpawnedObjectData>(this.spawnedObjects), // Create new list to avoid reference issues
            playerPositions = new List<ReplayPlayerPositionData>(this.playerPositions), // Create new list
            scoreAtSafePoint = this.scoreAtSafePoint,
            deathsAtSafePoint = this.deathsAtSafePoint,
            timeAtSafePoint = this.timeAtSafePoint
        };


        string json = JsonUtility.ToJson(serializableContainer, true); // true for pretty printing
        string filePath = GetFilePath();

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"Replay data for '{levelIdentifier}' saved to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save replay data to {filePath}: {e.Message}");
        }
    }

    public void LoadData()
    {
        string filePath = GetFilePath();

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                ReplayDataSerializableContainer loadedContainer = JsonUtility.FromJson<ReplayDataSerializableContainer>(json);

                // Copy loaded data back into the ScriptableObject
                this.levelIdentifier = loadedContainer.levelIdentifier;
                this.spawnedObjects = loadedContainer.spawnedObjects ?? new List<ReplaySpawnedObjectData>(); // Handle potential null if data was empty
                this.playerPositions = loadedContainer.playerPositions ?? new List<ReplayPlayerPositionData>(); // Handle potential null
                this.scoreAtSafePoint = loadedContainer.scoreAtSafePoint;
                this.deathsAtSafePoint = loadedContainer.deathsAtSafePoint;
                this.timeAtSafePoint = loadedContainer.timeAtSafePoint;

                Debug.Log($"Replay data for '{levelIdentifier}' loaded from: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load replay data from {filePath}: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"No saved replay data found for '{levelIdentifier}' at: {filePath}");
            ClearData(); // Clear the current object if no saved data exists
        }
    }
}


// NEW: Serializable container class for JSON persistence
// This helps JsonUtility serialize lists correctly when saving/loading ScriptableObjects.
[System.Serializable]
public class ReplayDataSerializableContainer
{
    public string levelIdentifier;
    public List<ReplaySpawnedObjectData> spawnedObjects;
    public List<ReplayPlayerPositionData> playerPositions;
    public int scoreAtSafePoint;
    public int deathsAtSafePoint;
    public float timeAtSafePoint;
}

// Ensure these structs are also defined in a file accessible to ReplayData.cs
// They might be in a separate file (e.g., ReplayDataStructures.cs) or at the bottom of ReplayData.cs
// If they are in a separate file, ensure that file also has the [System.Serializable] attribute
// for each struct and that it's correctly named and in the Assets folder.