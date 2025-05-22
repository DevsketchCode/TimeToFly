using UnityEngine;
using System.Collections;

public class CloudSpawner : MonoBehaviour
{
    public static CloudSpawner instance; // Singleton instance

    [Header("Cloud Prefabs")]
    [Tooltip("Drag all your different cloud prefabs here. The spawner will pick one randomly.")]
    [SerializeField] private GameObject[] cloudPrefabs;

    [Header("Spawn Timing")]
    [Tooltip("Minimum time between spawning new clouds.")]
    [SerializeField] private float minSpawnDelay = 3f;
    [Tooltip("Maximum time between spawning new clouds.")]
    [SerializeField] private float maxSpawnDelay = 7f;

    [Header("Spawn Relative Position")]
    [Tooltip("The X offset from the spawner's position where clouds will spawn (should be off-screen to the right).")]
    [SerializeField] private float spawnXOffset = 20f;

    [Tooltip("Minimum Y offset from the spawner's Y position for cloud spawning.")]
    [SerializeField] private float minYOffset = -2f;
    [Tooltip("Maximum Y offset from the spawner's Y position for cloud spawning.")]
    [SerializeField] private float maxYOffset = 2f;

    [Header("Cloud Movement Speed")]
    [Tooltip("The normal speed at which clouds move. This is the master base speed for ALL clouds.")]
    [SerializeField] private float baseCloudMoveSpeed = 1.0f; // This is your normal speed

    [Tooltip("Multiplier for cloud speed when other game objects are unpaused (e.g., 1.5 for 50% faster).")]
    [SerializeField] private float fastCloudSpeedMultiplier = 1.5f; // How much faster they go

    // This property holds the speed that CloudMovement scripts will actually use.
    public float CurrentCloudSpeed { get; private set; }

    private GameManager gameManager;
    private Coroutine spawnRoutine;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        // Initialize current speed to the base speed when the game starts.
        CurrentCloudSpeed = baseCloudMoveSpeed;
    }

    void Start()
    {
        gameManager = GameManager.instance;
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager instance not found! CloudSpawner might not pause correctly.");
        }

        if (cloudPrefabs == null || cloudPrefabs.Length == 0)
        {
            Debug.LogError("No cloud prefabs assigned to CloudSpawner. Disabling spawner.");
            enabled = false;
            return;
        }

        spawnRoutine = StartCoroutine(SpawnCloudsRoutine());
    }

    private IEnumerator SpawnCloudsRoutine()
    {
        while (true)
        {
            float currentDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(currentDelay);

            if (gameManager != null && gameManager.IsGameOver())
            {
                yield return null;
                continue;
            }

            GameObject cloudToSpawn = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

            Vector3 spawnPosition = new Vector3(
                transform.position.x + spawnXOffset,
                transform.position.y + Random.Range(minYOffset, maxYOffset),
                0
            );

            GameObject newCloud = Instantiate(cloudToSpawn, spawnPosition, Quaternion.identity);
            // CloudMovement will now fetch speed from CloudSpawner.instance.CurrentCloudSpeed in its Update()
        }
    }

    /// <summary>
    /// Sets the cloud speed to either the base speed or the boosted speed.
    /// </summary>
    /// <param name="boosted">True for faster speed, false for normal speed.</param>
    public void SetCloudSpeedBoost(bool boosted)
    {
        if (boosted)
        {
            CurrentCloudSpeed = baseCloudMoveSpeed * fastCloudSpeedMultiplier;
            Debug.Log("Clouds are now moving faster!");
        }
        else
        {
            CurrentCloudSpeed = baseCloudMoveSpeed;
            Debug.Log("Clouds returned to normal speed.");
        }
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }
    }

    public void StartSpawning()
    {
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnCloudsRoutine());
        }
    }
}