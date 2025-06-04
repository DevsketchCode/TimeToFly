// ReplayDataStructures.cs
using UnityEngine;
using System.Collections.Generic;

// Serializable class to store data about a spawned object
[System.Serializable]
public class ReplaySpawnedObjectData
{
    public string objectName; // Name of the prefab or unique identifier
    public Vector3 initialPosition; // World position when spawned
    // Add other relevant data if needed (e.g., initial rotation, scale, custom properties)

    public ReplaySpawnedObjectData(string name, Vector3 position)
    {
        objectName = name;
        initialPosition = position;
    }
}

// Serializable class to store player position at key moments
[System.Serializable]
public class ReplayPlayerPositionData
{
    public Vector3 position;       // Player's world position
    public string triggerName;     // Name of the trigger (e.g., "LevelStart", "SafePoint1", "ScoreColliderPass")
    public float timeInLevel;      // Time elapsed since the level started when this position was recorded

    public ReplayPlayerPositionData(Vector3 pos, string trigger, float time)
    {
        position = pos;
        triggerName = trigger;
        timeInLevel = time;
    }
}