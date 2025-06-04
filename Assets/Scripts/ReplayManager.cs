// ReplayManager.cs
using UnityEngine;
using System.Collections.Generic; // Required for List

// THIS IS WHERE ALL YOUR 'USING' STATEMENTS SHOULD BE!
#if UNITY_EDITOR
using UnityEditor; // <<<< MOVE THIS LINE HERE!
#endif

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance { get; private set; }

    [Header("Replay Data Settings")]
    [Tooltip("Assign the ScriptableObject that will store the replay data.")]
    [SerializeField] private ReplayData currentReplayData;

    [Tooltip("A unique identifier for the current level. Set this in the Inspector.")]
    [SerializeField] private string currentLevelIdentifier = "Level_01";


    private float _levelStartTime; // Time when the level started

    // --- Debugging / Editor Tools ---
    [Header("Debugging Tools")]
    [Tooltip("Click to log all current replay data to the console.")]
    [SerializeField] public bool logDataButton; // A dummy field to make a button in editor
    [Tooltip("Click to clear all replay data in the ScriptableObject. USE WITH CAUTION!")]
    [SerializeField] public bool clearDataButton; // A dummy field to make a button in editor
    [Tooltip("Click to manually save the current replay data to file.")]
    [SerializeField] public bool saveButton; // A dummy field to make a button in editor
    [Tooltip("Click to manually load replay data from file.")]
    [SerializeField] public bool loadButton; // A dummy field to make a button in editor


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if ReplayManager should persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }

        if (currentReplayData == null)
        {
            Debug.LogError("ReplayManager: No ReplayData ScriptableObject assigned! Please assign one in the Inspector.");
            enabled = false; // Disable component if no data object
            return;
        }

        // Set the level identifier for the current replay data
        currentReplayData.levelIdentifier = currentLevelIdentifier;
        currentReplayData.LoadData(); // Attempt to load existing data for this level on Awake
        currentReplayData.ClearData(); // Always clear data at the start of a NEW game session
    }

    private void OnEnable()
    {
        // This is important: Clear data when the manager is enabled (e.g., when scene loads)
        // This ensures each play session starts fresh for recording.
        // Unless you specifically want to append to previous session.
        currentReplayData.ClearData();
        _levelStartTime = Time.time; // Initialize level start time

        // Added null check for player, as GameObject.FindWithTag might return null
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            RecordPlayerStart(player.transform.position); // Record player's initial position
        }
        else
        {
            Debug.LogWarning("ReplayManager: Player GameObject with tag 'Player' not found in scene at OnEnable. Player start position not recorded.");
        }
    }

    // This makes the dummy boolean fields act as buttons in the Inspector
    private void OnValidate()
    {
        if (logDataButton)
        {
            logDataButton = false; // Reset button state
            if (currentReplayData != null) currentReplayData.LogData();
            else Debug.LogWarning("No ReplayData assigned to log.");
        }
        if (clearDataButton)
        {
            clearDataButton = false; // Reset button state
            if (currentReplayData != null) currentReplayData.ClearData();
            else Debug.LogWarning("No ReplayData assigned to clear.");
        }
        if (saveButton)
        {
            saveButton = false;
            if (currentReplayData != null) currentReplayData.SaveData();
            else Debug.LogWarning("No ReplayData assigned to save.");
        }
        if (loadButton)
        {
            loadButton = false;
            if (currentReplayData != null) currentReplayData.LoadData();
            else Debug.LogWarning("No ReplayData assigned to load.");
        }
    }

    // --- Public Methods for Recording Data ---

    public float GetTimeInLevel()
    {
        return Time.time - _levelStartTime;
    }

    public void RecordPlayerStart(Vector3 position)
    {
        if (currentReplayData == null) return;
        // Clear any previous LevelStart entry to ensure only one
        currentReplayData.playerPositions.RemoveAll(p => p.triggerName == "LevelStart");
        currentReplayData.playerPositions.Add(new ReplayPlayerPositionData(position, "LevelStart", GetTimeInLevel()));
        Debug.Log($"Recorded Player Start Position: {position}");
    }

    // This method is called when an object is spawned by ObjectSpawner
    public void RecordSpawnedObject(string objectName, Vector3 initialPosition)
    {
        if (currentReplayData == null) return;
        currentReplayData.spawnedObjects.Add(new ReplaySpawnedObjectData(objectName, initialPosition));
        // Debug.Log($"Recorded Spawned Object: {objectName} at {initialPosition}"); // Uncomment for verbose logging
    }

    // This method should be called when the player passes a specific point (e.g., scoring collider)
    public void RecordPlayerPosition(string triggerName, Vector3 position)
    {
        if (currentReplayData == null) return;
        currentReplayData.playerPositions.Add(new ReplayPlayerPositionData(position, triggerName, GetTimeInLevel()));
        // Debug.Log($"Recorded Player Position at {triggerName}: {position}"); // Uncomment for verbose logging
    }

    // This method is called when the player reaches the SafePoint
    public void RecordSafePointReached(Vector3 playerPosition, int finalScore, int deathsCount)
    {
        if (currentReplayData == null) return;

        currentReplayData.scoreAtSafePoint = finalScore;
        currentReplayData.deathsAtSafePoint = deathsCount;
        currentReplayData.timeAtSafePoint = GetTimeInLevel();

        // Record player position at the safe point
        // Clear any previous SafePoint entry to ensure only one
        currentReplayData.playerPositions.RemoveAll(p => p.triggerName == "SafePoint");
        currentReplayData.playerPositions.Add(new ReplayPlayerPositionData(playerPosition, "SafePoint", currentReplayData.timeAtSafePoint));

        Debug.Log($"--- Safe Point Reached! ---");
        Debug.Log($"Final Score: {finalScore}");
        Debug.Log($"Deaths: {deathsCount}");
        Debug.Log($"Total Time: {currentReplayData.timeAtSafePoint:F2}s");
        Debug.Log($"Player Position at SafePoint: {playerPosition}");

        currentReplayData.SaveData(); // Automatically save data when safe point is reached
    }
}

// Custom Editor to make boolean fields appear as buttons (Optional, but very useful)
#if UNITY_EDITOR
// The 'using UnityEditor;' statement is now at the very top of the file!
[CustomEditor(typeof(ReplayManager))]
public class ReplayManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw the default inspector

        ReplayManager myScript = (ReplayManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Replay Data Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Log Current Replay Data"))
        {
            myScript.logDataButton = true; // Trigger the OnValidate method
        }
        if (GUILayout.Button("Clear Replay Data (CAUTION!)"))
        {
            myScript.clearDataButton = true; // Trigger the OnValidate method
        }
        if (GUILayout.Button("Save Replay Data Now"))
        {
            myScript.saveButton = true; // Trigger the OnValidate method
        }
        if (GUILayout.Button("Load Replay Data Now"))
        {
            myScript.loadButton = true; // Trigger the OnValidate method
        }
    }
}
#endif