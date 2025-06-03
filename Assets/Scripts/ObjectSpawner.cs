using UnityEngine;
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random

public class ObjectSpawner : MonoBehaviour
{
    [Tooltip("Array of prefabs to spawn randomly.")]
    [SerializeField]
    private GameObject[] prefabsToSpawn;

    [Header("Spawn Timer Settings")]
    [SerializeField]
    public float minSpawnInterval; // Set to your desired minimum
    [SerializeField]
    public float maxSpawnInterval;

    [Header("Object Spawn Settings")]
    // REMOVED: public GameObject objectSpawner; // This is now THIS GameObject's transform

    [SerializeField]
    private float heightRange = 0.45f; // Range for random vertical offset when spawning
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

    // NEW: Variable to track the rightmost X position of the last spawned regular object
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
            GameObject prefabToInstantiate = prefabsToSpawn[randomIndex];

            float randomVerticalOffset = Random.Range(-heightRange, heightRange);
            // Spawn position is now relative to this spawner's current X and a random Y
            Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y, 0f) + new Vector3(0f, randomVerticalOffset, 0f);

            spawnedObjectInstance = Instantiate(prefabToInstantiate, spawnPosition, Quaternion.identity);

            // Calculate and store the rightmost X position of this newly spawned regular object
            // This assumes the object has a Collider2D and its pivot is roughly centered.
            // If the pivot is not centered, you might need to adjust bounds.extents.x or add a specific offset.
            Collider2D spawnedCollider = spawnedObjectInstance.GetComponent<Collider2D>();
            if (spawnedCollider != null)
            {
                lastSpawnedRegularObjectRightX = spawnedCollider.bounds.max.x;
            }
            else
            {
                // Fallback if no collider, or if you know the typical width of your obstacles
                Debug.LogWarning("Spawned object " + prefabToInstantiate.name + " has no Collider2D. Cannot accurately determine its right edge for safe zone spacing. Using approximate.");
                // A very rough estimate based on the prefab's local position + a standard width.
                // You might need to adjust '0.5f' based on your object's typical size.
                lastSpawnedRegularObjectRightX = spawnedObjectInstance.transform.position.x + 0.5f;
            }

            if (progressTracker != null)
            {
                progressTracker.IncrementTotalObjectsSpawned();
            }

            Debug.Log("Spawned: " + prefabToInstantiate.name + " at " + spawnPosition + " at time: " + Time.time);

            // REMOVED: No longer setting speed on MoveObject, as objects are stationary.
            // MoveObject moveObject = spawnedObjectInstance.GetComponent<MoveObject>();
            // if (moveObject != null && levelManager != null)
            // {
            //     moveObject.SetSpeed(levelManager.objectSpeed);
            // }
            // else if (moveObject == null)
            // {
            //     Debug.LogWarning("Spawned object " + prefabToInstantiate.name + " does not have a MoveObject script (expected for stationary objects).");
            // }
            // else if (levelManager == null)
            // {
            //     Debug.LogError("LevelManager is null in ObjectSpawner, cannot set object speed.");
            // }

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
        // REMOVED: No longer pausing individual spawned objects via MoveObject, as they are stationary.
        // if (spawnedObjectInstance != null)
        // {
        //     MoveObject moveObject = spawnedObjectInstance.GetComponent<MoveObject>();
        //     if (moveObject != null)
        //     {
        //         moveObject.PauseMovement(pause);
        //     }

        //     SelfDestruct selfDestruct = spawnedObjectInstance.GetComponent<SelfDestruct>();
        //     if (selfDestruct != null)
        //     {
        //         selfDestruct.SetPaused(pause);
        //     }
        // }
    }

    public void AddBounceDelay(float delay)
    {
        // This method still adds a delay to the next spawn.
        // The previous logic affecting spawned object movement is removed.
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
        // It should be 'minSafeObjectSpawnDistance' away from the right edge of the last regular obstacle.
        float safeObjectSpawnX = lastSpawnedRegularObjectRightX + minSafeObjectSpawnDistance;

        // Get the width of the safe object to properly align its left edge.
        // Assuming the safeObjectPrefab has a Collider2D and its pivot is centered.
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

        // Adjust spawnX to be the center of the safe object
        safeObjectSpawnX += safeObjectHalfWidth;

        // Spawn at the calculated X, and the spawner's Y (plus offset)
        Vector3 spawnPosition = new Vector3(safeObjectSpawnX, transform.position.y + safeObjectYOffset, 0f);
        GameObject safeInstance = Instantiate(safeObjectPrefab, spawnPosition, Quaternion.identity);
        safeObjectSpawned = true;

        Debug.Log("Safe Object Spawned: " + safeObjectPrefab.name + " at " + spawnPosition + " (last obstacle right X: " + lastSpawnedRegularObjectRightX + ") at time: " + Time.time);

        // REMOVED: No longer setting speed on MoveObject for the Safe Object.
        // MoveObject moveObject = safeInstance.GetComponent<MoveObject>();
        // if (moveObject != null && levelManager != null)
        // {
        //     moveObject.SetSpeed(levelManager.objectSpeed);
        // }
        // else if (moveObject == null)
        // {
        //     Debug.LogWarning("Spawned Safe Object " + safeObjectPrefab.name + " does not have a MoveObject script (expected for stationary objects).");
        // }
    }

    public void ResetSpawner()
    {
        safeObjectSpawned = false;
        spawnTimer = 0f;
        Pause = false;
        lastSpawnedRegularObjectRightX = transform.position.x; // Reset to current spawner X on restart
    }
}