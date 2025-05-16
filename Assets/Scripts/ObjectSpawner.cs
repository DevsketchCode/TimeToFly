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
    private float heightRange = 0.45f; // Range for random vertical offset when spawning
    [SerializeField]
    private float selfDestructTime = 15f; // Time before the spawned object self-destructs

    [Header("Object Spawner Details")]
    [SerializeField]
    public bool Pause = false;

    private float spawnTimer;
    private GameObject spawnedObjectInstance;

    private void Start()
    {
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

            // Calculate a random vertical offset
            float randomVerticalOffset = Random.Range(-heightRange, heightRange);

            // Spawn at the spawner's position with the random vertical offset
            Vector3 spawnPosition = new Vector3(this.transform.position.x,0f,0f) + new Vector3(0f, randomVerticalOffset, 0f);
            spawnedObjectInstance = Instantiate(prefabToInstantiate, spawnPosition, Quaternion.identity);

            Debug.Log("Spawned: " + prefabToInstantiate.name + " at " + spawnPosition + " at time: " + Time.time);

            // Get the SelfDestruct component and it will handle its own lifetime
            SelfDestruct selfDestruct = spawnedObjectInstance.GetComponent<SelfDestruct>();
            // The SelfDestruct script's Update() will automatically start the countdown

            // If you need to override the default lifetime from the prefab, you can do it here:
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

    public void CancelDelayedDestroy() { }
    public void RestartDestroyTimer() { }
}