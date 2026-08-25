using UnityEngine;
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random

// Add this serializable class here, outside the ObjectSpawner class definition
// This allows you to pair a GameObject prefab with a specific vertical offset
[System.Serializable]
public class SpawnableObject
{
    public GameObject prefab;
    [Tooltip("Adjusts the vertical position of this specific prefab relative to the spawner's calculated Y. Use this to visually align objects.")]
    public float verticalOffset = 0f; // Default to no offset
    [Tooltip("Adjusts the height randomized variation of this specific prefab relative to the spawner's calculated Y.")]
    public float heightRange = 0.45f; // Range for random vertical offset when spawning (this is the RANDOM range)
}

public class ObjectSpawner : MonoBehaviour
{
    [Tooltip("Array of prefabs to spawn randomly, each with its own vertical offset setting.")]
    [SerializeField]
    private SpawnableObject[] prefabsToSpawn; // CHANGED: Now an array of SpawnableObject

    [Header("Spawn Timer Settings")]
    [SerializeField]
    public float minSpawnInterval; // Set to your desired minimum
    [SerializeField]
    public float maxSpawnInterval;

    [Header("Object Spawn Settings")]
    [SerializeField]
    private float selfDestructTime = 15f; // Time before the spawned object self-destructs

    // --- NEW: Safe Object Settings ---
    [Header("Safe Object Settings")]
    [Tooltip("The prefab for the Safe Object that will spawn uniquely.")]
    [SerializeField]
    private GameObject safeObjectPrefab;
    [Tooltip("The Y position offset for the Safe Object relative to the spawner's Y.")]
    [SerializeField]
    private float safeObjectYOffset = 0f; // Adjust this if your safe object isn't centered vertically
    [Tooltip("Minimum distance in X from the right edge of the last regular obstacle to the left edge of the Safe Object.")]
    [SerializeField]
    private float minSafeObjectSpawnDistance = 3.0f; // Adjust this value in the Inspector!

    private bool safeObjectSpawned = false; // Flag to ensure it only spawns once
    // ---------------------------------

    [Header("Object Spawner Details")]
    [SerializeField]
    public bool Pause = false;

    private float spawnTimer;
    private GameObject spawnedObjectInstance; // Reference to the last spawned random object (used for its bounds calculation)
    private LevelManager levelManager; // Still needed for overall game state/rules
    private ProgressTracker progressTracker; // Reference to the new ProgressTracker
    private WeatherManager weatherManager; // NEW: Reference to WeatherManager

    // Variable to track the rightmost X position of the last spawned regular object
    private float lastSpawnedRegularObjectRightX = 0f;

    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found in the scene!");
            enabled = false;
            return;
        }

        progressTracker = ProgressTracker.Instance; // Assuming ProgressTracker is a Singleton
        if (progressTracker == null)
        {
            Debug.LogError("ProgressTracker instance not found! Please ensure it's in the scene.");
            enabled = false;
            return;
        }

        weatherManager = WeatherManager.instance; // Assuming WeatherManager is a Singleton
        if (weatherManager == null)
        {
            Debug.LogError("WeatherManager instance not found! ObjectSpawner cannot function correctly without it.");
            enabled = false;
            return;
        }

        // Initially set lastSpawnedRegularObjectRightX to the spawner's current X position
        // This ensures the first object spawns correctly relative to the spawner's start.
        lastSpawnedRegularObjectRightX = transform.position.x;

        SpawnObject(); // Spawn an object immediately at the start
        ResetSpawnTimer(); // Set a random initial spawn delay
    }

    private void Update()
    {
        // The spawner's X position is now updated by FlyBehavior, so no movement logic here.

        if (Pause) // If spawner is paused, do nothing
        {
            return;
        }

        // Only spawn regular objects if WeatherManager allows it
        if (weatherManager.ShouldStopRegularSpawns)
        {
            return; // Don't spawn any regular objects
        }

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnObject();
            ResetSpawnTimer();
        }
    }

    private void SpawnObject()
    {
        if (weatherManager.ShouldStopRegularSpawns)
        {
            Debug.Log("ObjectSpawner: Attempted to spawn regular object but WeatherManager halted spawns.");
            return;
        }

        if (prefabsToSpawn != null && prefabsToSpawn.Length > 0)
        {
            int randomIndex = Random.Range(0, prefabsToSpawn.Length);
            // Get the selected SpawnableObject entry (which contains both the prefab and its offset)
            SpawnableObject selectedSpawnable = prefabsToSpawn[randomIndex];
            GameObject prefabToInstantiate = selectedSpawnable.prefab; // Extract the actual GameObject prefab

            if (prefabToInstantiate == null)
            {
                Debug.LogWarning($"Prefab at index {randomIndex} in 'Prefabs To Spawn' array is null. Skipping this spawn.");
                return;
            }

            float randomVerticalOffset = Random.Range(-selectedSpawnable.heightRange, selectedSpawnable.heightRange);
            // Calculate spawn position: base Y + random range + specific prefab's custom vertical offset
            Vector3 spawnPosition = new Vector3(
                transform.position.x,
                transform.position.y + randomVerticalOffset + selectedSpawnable.verticalOffset, // ADDED: selectedSpawnable.verticalOffset
                0f
            );

            spawnedObjectInstance = Instantiate(prefabToInstantiate, spawnPosition, Quaternion.identity);

            // Calculate and store the rightmost X position of this newly spawned regular object
            Collider2D spawnedCollider = spawnedObjectInstance.GetComponent<Collider2D>();
            if (spawnedCollider != null)
            {
                lastSpawnedRegularObjectRightX = spawnedCollider.bounds.max.x;
            }
            else
            {
                // Fallback if no collider, or if you know the typical width of your obstacles
                Debug.LogWarning("Spawned object " + prefabToInstantiate.name + " has no Collider2D. Cannot accurately determine its right edge for safe zone spacing. Using approximate.");
                lastSpawnedRegularObjectRightX = spawnedObjectInstance.transform.position.x + 0.5f; // A very rough estimate
            }

            if (progressTracker != null)
            {
                progressTracker.IncrementTotalObjectsSpawned();
            }

            Debug.Log("Spawned: " + prefabToInstantiate.name + " at " + spawnPosition + " at time: " + Time.time);

            // Record the spawned object's initial data for replay
            if (ReplayManager.Instance != null)
            {
                ReplayManager.Instance.RecordSpawnedObject(prefabToInstantiate.name, spawnPosition);
            }
            else
            {
                Debug.LogWarning("ReplayManager not found. Object spawning will not be recorded.");
            }

            SelfDestruct selfDestruct = spawnedObjectInstance.GetComponent<SelfDestruct>();
            if (selfDestruct != null && selfDestructTime > 0)
            {
                selfDestruct.SetLifeTime(selfDestructTime);
            }
            else if (selfDestruct == null)
            {
                Debug.LogWarning("Spawned object " + prefabToInstantiate.name + " does not have a SelfDestruct script.");
            }
        }
        else
        {
            Debug.LogWarning("No prefabs assigned to the ObjectSpawner on " + gameObject.name + ".");
        }
    }

    private void ResetSpawnTimer()
    {
        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    public void PauseSpawner(bool pause)
    {
        Pause = pause;
    }

    public void AddBounceDelay(float delay)
    {
        // This method still adds a delay to the next spawn.
        spawnTimer += delay;
        if (spawnTimer < 0f)
        {
            spawnTimer = 0f;
        }
    }

    public void SpawnSafeObject()
    {
        if (safeObjectPrefab == null)
        {
            Debug.LogError("Safe Object Prefab is not assigned in ObjectSpawner!");
            return;
        }
        if (safeObjectSpawned)
        {
            Debug.LogWarning("Attempted to spawn Safe Object multiple times. Ignoring.");
            return;
        }

        // Calculate the target X position for the Safe Object
        float safeObjectSpawnX = lastSpawnedRegularObjectRightX + minSafeObjectSpawnDistance;

        // Get the width of the safe object to properly align its left edge.
        float safeObjectHalfWidth = 0f;
        Collider2D safePrefabCollider = safeObjectPrefab.GetComponent<Collider2D>();
        if (safePrefabCollider != null)
        {
            safeObjectHalfWidth = safePrefabCollider.bounds.extents.x;
        }
        else
        {
            Debug.LogWarning("Safe Object Prefab has no Collider2D. Assuming half-width of 0.5 for spawning calculation.");
            safeObjectHalfWidth = 0.5f; // Fallback estimate
        }

        safeObjectSpawnX += safeObjectHalfWidth;

        // Spawn at the calculated X, and the spawner's Y (plus its own offset)
        Vector3 spawnPosition = new Vector3(safeObjectSpawnX, transform.position.y + safeObjectYOffset, 0f);
        GameObject safeInstance = Instantiate(safeObjectPrefab, spawnPosition, Quaternion.identity);
        safeObjectSpawned = true;

        Debug.Log("Safe Object Spawned: " + safeObjectPrefab.name + " at " + spawnPosition + " (last obstacle right X: " + lastSpawnedRegularObjectRightX + ") at time: " + Time.time);

        SelfDestruct safeSelfDestruct = safeInstance.GetComponent<SelfDestruct>();
        if (safeSelfDestruct != null && selfDestructTime > 0)
        {
            safeSelfDestruct.SetLifeTime(selfDestructTime);
        }
    }

    public void ResetSpawner()
    {
        safeObjectSpawned = false;
        spawnTimer = 0f;
        Pause = false;
        lastSpawnedRegularObjectRightX = transform.position.x; // Reset to current spawner X on restart
    }
}