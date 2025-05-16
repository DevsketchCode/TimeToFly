using UnityEngine;
using Random = UnityEngine.Random;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField]
    private float maxTime = 2f;

    [SerializeField]
    private float heightRange = 0.45f;

    [SerializeField]
    private float minHorizontalDistance = 1f; // Minimum horizontal distance between spawns
    [SerializeField]
    private float maxHorizontalDistance = 1.5f; // Maximum horizontal distance between spawns

    [Tooltip("Array of prefabs to spawn randomly.")]
    [SerializeField]
    private GameObject[] prefabsToSpawn;

    [SerializeField]
    public bool Pause = false;

    private float timer;
    public GameObject lastSpawnObject = null;
    public Vector3 lastSpawnPosition = new Vector3(0.5f,0); // Added to store the last spawn position
    private GameObject spawnedObjectInstance; // Declared at class level

    private void Start()
    {
        SpawnObject();
    }

    private void Update()
    {
        if (!Pause)
        {
            timer += Time.deltaTime;
            if (timer > maxTime)
            {
                SpawnObject();
                timer = 0;
            }
        }
    }

    private void SpawnObject()
    {
        Debug.Log("Spawning object...");
        if (prefabsToSpawn != null && prefabsToSpawn.Length > 0)
        {
            int randomIndex = Random.Range(0, prefabsToSpawn.Length);
            GameObject prefabToInstantiate = prefabsToSpawn[randomIndex];
            Debug.Log("Prefab to Instantiate: " + prefabToInstantiate.name);
            Vector3 spawnPosition;
            
            bool validPosition = false;
            int attempts = 0;
            int maxAttempts = 20;

            do
            {
                float randomHorizontalOffset = Random.Range(minHorizontalDistance, maxHorizontalDistance);
                float randomVerticalOffset = Random.Range(-heightRange, heightRange);
                
                spawnPosition = new Vector3(lastSpawnPosition.x,0) + new Vector3(randomHorizontalOffset, randomVerticalOffset, 0f);
                Debug.Log($"Spawn Position: {spawnPosition}");
                if (lastSpawnObject != null)
                {
                    if (Mathf.Abs(spawnPosition.x - lastSpawnPosition.x) >= minHorizontalDistance)
                    {
                        validPosition = true;
                    }
                }
                else
                {
                    validPosition = true;
                }

                attempts++;
                if (attempts > maxAttempts)
                {
                    Debug.LogWarning("Could not find a valid spawn position after " + maxAttempts + " attempts on " + gameObject.name + ". Consider adjusting distance ranges.");
                    validPosition = true;
                }

            } while (!validPosition);

            spawnedObjectInstance = Instantiate(prefabToInstantiate, spawnPosition, Quaternion.identity); // Assigned to the class-level variable
            lastSpawnObject = spawnedObjectInstance;
            Debug.Log("Last Spawned Object: " + lastSpawnObject.name);
            lastSpawnPosition = lastSpawnObject != null ? lastSpawnObject.transform.position : Vector3.zero;

            SelfDestruct selfDestruct = spawnedObjectInstance.AddComponent<SelfDestruct>();
            selfDestruct.isPaused = Pause;
        }
        else
        {
            Debug.LogWarning("No prefabs assigned to the ObjectSpawner on " + gameObject.name + ".");
        }
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