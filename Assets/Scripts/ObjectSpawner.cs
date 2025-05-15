using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField]
    private float maxTime = 1.5f;

    [SerializeField]
    private float heightRange = 0.45f;

    [SerializeField]
    private float distanceRange = 0.45f;

    [SerializeField]
    private GameObject _gameObject; // This should be your PipePrefab

    [SerializeField]
    public bool Pause = false;

    private float timer;

    private GameObject spawnedObjectInstance; // Keep track of the instantiated GameObject

    private void Start()
    {
        SpawnObject();
    }

    private void Update()
    {
        // Only update the timer and potentially spawn if not paused
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
        Vector3 spawnPosition = transform.position + new Vector3(Random.Range(0, distanceRange), Random.Range(-heightRange, heightRange));
        spawnedObjectInstance = Instantiate(_gameObject, spawnPosition, Quaternion.identity);

        // Add the SelfDestruct script to the spawned object
        SelfDestruct selfDestruct = spawnedObjectInstance.AddComponent<SelfDestruct>();
        selfDestruct.isPaused = Pause; // Initialize the paused state based on the spawner's pause state
    }

    // Public method to pause the spawner
    public void PauseSpawner(bool pause)
    {
        Pause = pause;
        // Find the SelfDestruct component on the currently spawned object and update its paused state
        if (spawnedObjectInstance != null)
        {
            SelfDestruct selfDestruct = spawnedObjectInstance.GetComponent<SelfDestruct>();
            if (selfDestruct != null)
            {
                selfDestruct.SetPaused(pause);
            }
        }
    }

    // Public method to cancel any immediate destroy (no longer directly managing destroy here)
    public void CancelDelayedDestroy()
    {
        // Not directly used in this approach.
    }

    // Public method to restart the destroy timer (not directly managing destroy here)
    public void RestartDestroyTimer()
    {
        // Not directly used in this approach.
    }
}