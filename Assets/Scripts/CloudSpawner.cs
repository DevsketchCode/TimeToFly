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

    [Tooltip("Multiplier for cloud speed when other game objects are unpaused (e.g., 1.5 for 50% faster).")]
    [SerializeField] private float fastCloudSpeedMultiplier = 1.15f; // How much faster they go

    // This property holds the speed that CloudMovement scripts will actually use.
    public float CurrentCloudSpeed { get; private set; }

    // NEW: Add a public Pause property to control the spawner's own forward movement.
    [HideInInspector] // Hide in inspector as it's controlled by other scripts
    public bool Pause { get; set; } = false; // Default to not paused

    [SerializeField] private LevelManager levelManager;
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

        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
            if (levelManager == null)
            {
                Debug.LogError("LevelManager not found in the scene! CloudSpawner cannot function properly.");
                enabled = false;
                return;
            }
        }
        else
        {
            // Initialize current speed to the base speed when the game starts.
            CurrentCloudSpeed = levelManager.baseCloudMoveSpeed;
        }

        // Start the spawning routine. Its internal logic will *not* be affected by this script's 'Pause' flag,
        // ensuring clouds always spawn if the game isn't over.
        spawnRoutine = StartCoroutine(SpawnCloudsRoutine());
    }

    void FixedUpdate()
    {
        // Only move the spawner's position if not paused and game is not over.
        if (!Pause && gameManager != null && !gameManager.IsGameOver())
        {
            // The CloudSpawner should follow the player's forward speed.
            // We'll get the player's forwardSpeed from FlyBehavior.
            if (FlyBehavior.instance != null)
            {
                transform.Translate(Vector2.right * FlyBehavior.instance.forwardSpeed * Time.fixedDeltaTime);
            }
            else
            {
                Debug.LogWarning("FlyBehavior instance not found! CloudSpawner cannot follow player forward speed.");
            }
        }
    }

    private IEnumerator SpawnCloudsRoutine()
    {
        while (true)
        {
            float currentDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(currentDelay);

            // The spawning of clouds should ONLY stop if the game is over.
            // It should NOT be affected by the 'Pause' flag of the spawner itself,
            // as clouds should always continue to appear.
            if (gameManager != null && gameManager.IsGameOver())
            {
                yield break; // Stop the coroutine if game is over
            }

            GameObject cloudToSpawn = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

            // Spawn position is relative to the spawner's current position.
            Vector3 spawnPosition = new Vector3(
                transform.position.x + spawnXOffset,
                transform.position.y + Random.Range(minYOffset, maxYOffset),
                0
            );

            Instantiate(cloudToSpawn, spawnPosition, Quaternion.identity);
        }
    }

    /// <summary>
    /// Sets the cloud speed to either the base speed or the boosted speed.
    /// This directly modifies CurrentCloudSpeed, which CloudMovement scripts should read.
    /// </summary>
    /// <param name="boosted">True for faster speed, false for normal speed.</param>
    public void SetCloudSpeedBoost(bool boosted)
    {
        if (boosted)
        {
            CurrentCloudSpeed = levelManager.baseCloudMoveSpeed * fastCloudSpeedMultiplier;
            // Debug.Log("Clouds are now moving faster! CurrentCloudSpeed: " + CurrentCloudSpeed);
        }
        else
        {
            CurrentCloudSpeed = levelManager.baseCloudMoveSpeed;
            // Debug.Log("Clouds returned to normal speed. CurrentCloudSpeed: " + CurrentCloudSpeed);
        }
    }

    // You can keep these if you still need external control, but 'Pause' handles positional stopping.
    // The SpawnCloudsRoutine itself now only cares about GameManager.IsGameOver() for stopping.
    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null; // Set to null so StartSpawning can re-start it
        }
    }

    public void StartSpawning()
    {
        if (spawnRoutine == null) // Only start if not already running
        {
            spawnRoutine = StartCoroutine(SpawnCloudsRoutine());
        }
    }
}