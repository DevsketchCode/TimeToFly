using UnityEngine;
using Random = UnityEngine.Random;

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
    [SerializeField]
    public GameObject objectSpawner; // This is the GameObject that defines the spawn X position

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
    private bool safeObjectSpawned = false; // Flag to ensure it only spawns once
    // ---------------------------------

    [Header("Object Spawner Details")]
    [SerializeField]
    public bool Pause = false;

    private float spawnTimer;
    private GameObject spawnedObjectInstance; // Reference to the last spawned random object
    private LevelManager levelManager;
    private ProgressTracker progressTracker; // Reference to the new ProgressTracker

    private void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found in the scene!");
            enabled = false;
            return;
        }

        // --- Get ProgressTracker instance ---
        progressTracker = ProgressTracker.Instance; // Get the singleton instance
        if (progressTracker == null)
        {
            Debug.LogError("ProgressTracker instance not found! Please ensure it's in the scene.");
            enabled = false;
            return;
        }
        // No need to call ResetProgress here, WeatherManager will do it
        // ------------------------------------------

        SpawnObject(); // Spawn an object immediately at the start
        ResetSpawnTimer(); // Set a random initial spawn delay
    }

    private void Update()
    {
        if (!Pause)
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f)
            {
                SpawnObject();
                ResetSpawnTimer();
            }
        }
    }

    private void SpawnObject()
    {
        if (prefabsToSpawn != null && prefabsToSpawn.Length > 0)
        {
            int randomIndex = Random.Range(0, prefabsToSpawn.Length);
            GameObject prefabToInstantiate = prefabsToSpawn[randomIndex];

            float randomVerticalOffset = Random.Range(-heightRange, heightRange);
            Vector3 spawnPosition = new Vector3(objectSpawner.transform.position.x, objectSpawner.transform.position.y, 0f) + new Vector3(0f, randomVerticalOffset, 0f);
            spawnedObjectInstance = Instantiate(prefabToInstantiate, spawnPosition, Quaternion.identity);

            // --- Inform the ProgressTracker about the new spawned object ---
            if (progressTracker != null)
            {
                progressTracker.IncrementTotalObjectsSpawned();
            }
            // -------------------------------------------------------------------

            Debug.Log("Spawned: " + prefabToInstantiate.name + " at " + spawnPosition + " at time: " + Time.time);

            MoveObject moveObject = spawnedObjectInstance.GetComponent<MoveObject>();
            if (moveObject != null && levelManager != null)
            {
                moveObject.SetSpeed(levelManager.objectSpeed);
            }
            else if (moveObject == null)
            {
                Debug.LogWarning("Spawned object " + prefabToInstantiate.name + " does not have a MoveObject script.");
            }
            else if (levelManager == null)
            {
                Debug.LogError("LevelManager is null in ObjectSpawner, cannot set object speed.");
            }

            SelfDestruct selfDestruct = spawnedObjectInstance.GetComponent<SelfDestruct>();
            if (selfDestruct != null && selfDestructTime > 0)
            {
                selfDestruct.SetLifeTime(selfDestructTime);
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
        if (spawnedObjectInstance != null)
        {
            SelfDestruct selfDestruct = spawnedObjectInstance.GetComponent<SelfDestruct>();
            if (selfDestruct != null)
            {
                selfDestruct.SetPaused(pause);
            }
        }
    }

    public void AddBounceDelay(float delay)
    {
        spawnTimer += delay;
        if (spawnTimer < 0f)
        {
            spawnTimer = 0f;
        }
    }

    // --- Public method to spawn the Safe Object ---
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

        Vector3 spawnPosition = new Vector3(objectSpawner.transform.position.x, objectSpawner.transform.position.y + safeObjectYOffset, 0f);
        GameObject safeInstance = Instantiate(safeObjectPrefab, spawnPosition, Quaternion.identity);
        safeObjectSpawned = true;

        Debug.Log("Safe Object Spawned: " + safeObjectPrefab.name + " at " + spawnPosition + " at time: " + Time.time);

        // Ensure the Safe Object also moves
        MoveObject moveObject = safeInstance.GetComponent<MoveObject>();
        if (moveObject != null && levelManager != null)
        {
            moveObject.SetSpeed(levelManager.objectSpeed);
        }
        else if (moveObject == null)
        {
            Debug.LogWarning("Spawned Safe Object " + safeObjectPrefab.name + " does not have a MoveObject script.");
        }
    }

    // --- Public method to reset for game restarts ---
    public void ResetSpawner()
    {
        safeObjectSpawned = false;
        spawnTimer = 0f; // Will trigger a new spawn on next update
        Pause = false;
    }
}