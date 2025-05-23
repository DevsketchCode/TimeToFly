using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Required for List

public class WeatherManager : MonoBehaviour
{
    // NEW: Singleton instance for easy access
    public static WeatherManager instance;

    [Header("Initial Lightning Strike")]
    [Tooltip("Time to wait before the first lightning strike occurs at the level start.")]
    [SerializeField]
    private float initialStrikeDelay = 5f;

    [Tooltip("The GameObject representing the initial lightning flash in the general scene. This is the one used at the beginning and in the safe zone.")]
    [SerializeField]
    private GameObject initialAndSafeZoneLightningObject; // Renamed for clarity

    [Tooltip("Total duration for which the initial lightning effect is active, including all flickers.")]
    [SerializeField]
    private float initialLightningDuration = 0.5f; // Increased for flicker effect


    [Header("End-of-Level Countdown")]
    [Tooltip("Minimum time for the countdown before the final lightning check.")]
    [SerializeField]
    private float minCountdownTime = 10f;
    [Tooltip("Maximum time for the countdown before the final lightning check.")]
    [SerializeField]
    private float maxCountdownTime = 20f;
    private float initialCountdownDuration; // Store the initial random duration
    private float currentCountdownTime;
    private bool countdownStarted = false;
    private bool safeObjectSpawnTriggered = false; // Track if the safe object spawn has been triggered

    // NEW: Flag to indicate if the player is currently within the safe zone trigger
    private bool playerIsCurrentlyInSafeZone = false; // This will be set by FlyBehavior

    // NEW: Public property for ObjectSpawner to check if it should stop regular spawns
    public bool ShouldStopRegularSpawns { get; private set; } = false; // Initially false

    [Header("Safe Object Spawning")]
    [Tooltip("The ObjectSpawner in the scene.")]
    [SerializeField]
    private ObjectSpawner objectSpawner; // Reference to your ObjectSpawner

    [Tooltip("Time before the main countdown ends that the Safe Object should spawn. This is the 'window' for the player to reach it.")]
    [SerializeField]
    private float safeObjectSpawnBufferTime = 5f; // e.g., 5 seconds before total countdown runs out

    [Header("Player Settings")]
    [Tooltip("The Player's GameObject, used to get the Animator and FlyBehavior.")]
    [SerializeField]
    private GameObject playerGameObject;

    [Header("Lightning Bolt Containers")]
    [Tooltip("The main parent GameObject that holds all lightning visual effects.")]
    [SerializeField]
    private GameObject lightningContainerParent; // Your "Lightning" parent object

    [Tooltip("A list of lightning bolt GameObjects that should randomly strike the player on Game Over. These are children of the Lightning Container Parent.")]
    [SerializeField]
    private List<GameObject> playerStrikeLightningBolts; // Your "LightningBolt" objects for player strike

    [Header("Lightning Positioning")]
    [Tooltip("Vertical offset for the lightning container relative to the player's Y position.")]
    [SerializeField]
    private float lightningYOffset = 0f; // You can set a default here, like 0.5f or 1.0f

    [Header("Lightning Flicker Settings")]
    [Tooltip("Minimum number of times the lightning bolt will flicker.")]
    [SerializeField]
    private int minFlickerCount = 2;
    [Tooltip("Maximum number of times the lightning bolt will flicker.")]
    [SerializeField]
    private int maxFlickerCount = 4;
    [Tooltip("Duration each individual lightning flash stays ON during a flicker.")]
    [SerializeField]
    private float flickerOnDuration = 0.08f;
    [Tooltip("Minimum time the lightning stays OFF between flickers.")]
    [SerializeField]
    private float minFlickerOffTime = 0.05f;
    [Tooltip("Maximum time the lightning stays OFF between flickers.")]
    [SerializeField]
    private float maxFlickerOffTime = 0.15f;

    [Tooltip("Total duration for which the player lightning effect is active, including all flickers.")]
    [SerializeField]
    private float playerLightningDuration = 0.5f;

    [Tooltip("Total duration for which the safe zone lightning effect is active, including all flickers.")]
    [SerializeField]
    private float safeZoneLightningDuration = 0.5f;


    [Header("Audio")]
    [Tooltip("The AudioClip for the thunder sound.")]
    [SerializeField]
    private AudioClip thunderSound;

    [Tooltip("Volume for the thunder sound.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float thunderVolume = 1.0f;

    private FlyBehavior playerFlyBehavior;
    private Animator playerAnimator;
    private ProgressTracker progressTracker;

    private GameObject currentActivePlayerLightningBolt;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject); // Ensures only one instance exists
        }
    }

    void Start()
    {
        // Get references
        if (playerGameObject != null)
        {
            playerFlyBehavior = playerGameObject.GetComponent<FlyBehavior>();
            playerAnimator = playerGameObject.GetComponent<Animator>();
            if (playerFlyBehavior == null)
            {
                Debug.LogError("FlyBehavior not found on playerGameObject!");
            }
            if (playerAnimator == null)
            {
                Debug.LogError("Animator not found on playerGameObject!");
            }
        }
        else
        {
            Debug.LogError("Player GameObject not assigned in WeatherManager!");
            enabled = false;
            return;
        }

        if (objectSpawner == null)
        {
            objectSpawner = FindObjectOfType<ObjectSpawner>();
            if (objectSpawner == null)
            {
                Debug.LogError("ObjectSpawner not assigned and not found in scene for WeatherManager!");
                enabled = false;
                return;
            }
        }

        progressTracker = ProgressTracker.Instance;
        if (progressTracker == null)
        {
            Debug.LogError("ProgressTracker instance not found! Please ensure it's in the scene.");
            enabled = false;
            return;
        }

        // --- Deactivate all lightning objects initially ---
        if (lightningContainerParent != null)
        {
            lightningContainerParent.SetActive(false); // Deactivate the main parent
        }
        else
        {
            Debug.LogError("Lightning Container Parent not assigned in WeatherManager!");
            enabled = false;
            return;
        }

        // Ensure all individual lightning visuals are turned off
        if (initialAndSafeZoneLightningObject != null) initialAndSafeZoneLightningObject.SetActive(false);
        foreach (GameObject lb in playerStrikeLightningBolts)
        {
            if (lb != null) lb.SetActive(false);
        }
        // --------------------------------------------------------

        StartCoroutine(InitialLightningSequence());
    }

    public void SetPlayerSafeZoneStatus(bool isSafe)
    {
        playerIsCurrentlyInSafeZone = isSafe;
        Debug.Log($"WeatherManager: PlayerIsCurrentlyInSafeZone set to {isSafe}");

        if (playerIsCurrentlyInSafeZone)
        {
            StopAllCoroutines(); // Stop all running coroutines on this script
            countdownStarted = false; // Ensure countdown is no longer active
            ShouldStopRegularSpawns = true; // NEW: Make sure spawner stops regular items

            // This means the player won by reaching the safe zone BEFORE the timer ran out.
            Debug.Log("Player reached safe zone, stopping all weather events and triggering WinGame!");

            if (GameManager.instance != null)
            {
                GameManager.instance.WinGame();
            }
            else
            {
                Debug.LogWarning("GameManager instance not found. Cannot trigger WinGame.");
            }
            // Optionally, play a "safe zone" specific lightning visual or sound
            StartCoroutine(PerformLightningStrike(initialAndSafeZoneLightningObject, safeZoneLightningDuration));
        }
    }


    void Update()
    {
        if (playerIsCurrentlyInSafeZone || (GameManager.instance != null && GameManager.instance.IsGameWon()))
        {
            if (UIManager.instance != null)
            {
                UIManager.instance.UpdateTimeDisplay(0f);
            }
            return; // Do not process countdown or final strike logic
        }

        if (countdownStarted)
        {
            currentCountdownTime -= Time.deltaTime;

            if (UIManager.instance != null)
            {
                UIManager.instance.UpdateTimeDisplay(currentCountdownTime);
            }

            // --- IMPORTANT CHANGE: Only check progress if the safe object hasn't been triggered yet ---
            if (!safeObjectSpawnTriggered && currentCountdownTime <= safeObjectSpawnBufferTime)
            {
                // This condition makes sure we only spawn the safe object once
                // AND that the player has met the progression requirement.
                if (progressTracker != null && progressTracker.HasMetProgressionRequirement())
                {
                    if (objectSpawner != null)
                    {
                        objectSpawner.SpawnSafeObject();
                        safeObjectSpawnTriggered = true;
                        ShouldStopRegularSpawns = true; // NEW: Tell the spawner to stop regular items
                        Debug.Log($"Safe Object spawn triggered! Time remaining: {currentCountdownTime:F2}. Regular spawns halted.");
                    }
                    else
                    {
                        Debug.LogError("ObjectSpawner is null, cannot spawn Safe Object!");
                    }
                }
                else
                {
                    // This means we're in the time window for the safe object, but progress isn't met.
                    // This is where you might want to consider the "larger gap" or "no other spawns"
                    // logic to prevent unfair deaths if progression isn't met.
                    // If progress is NOT met, we still want to stop regular spawns
                    // to give the player a chance to get the required progress before the end.
                    if (!ShouldStopRegularSpawns) // Prevent redundant setting
                    {
                        ShouldStopRegularSpawns = true; // NEW: Halt regular spawns even if progress isn't met yet
                        Debug.Log($"Safe Object spawn window entered. Regular spawns halted. Progress not yet met: {progressTracker.objectsPassed}/{progressTracker.GetEstimatedTotalObjects()}.");
                    }
                }
            }

            if (currentCountdownTime <= 0)
            {
                countdownStarted = false;
                StartCoroutine(FinalLightningSequence());
            }
        }
    }

    private void StartCountdown()
    {
        initialCountdownDuration = Random.Range(minCountdownTime, maxCountdownTime);
        currentCountdownTime = initialCountdownDuration;
        countdownStarted = true;
        safeObjectSpawnTriggered = false;
        ShouldStopRegularSpawns = false; // NEW: Reset for new countdown

        if (progressTracker != null && objectSpawner != null)
        {
            progressTracker.ResetProgress(initialCountdownDuration, objectSpawner);
        }
        else
        {
            Debug.LogError("ProgressTracker or ObjectSpawner not found for countdown initialization!");
        }

        Debug.Log($"Countdown started for: {currentCountdownTime:F2} seconds. Initial Duration: {initialCountdownDuration:F2}");
    }

    private IEnumerator InitialLightningSequence()
    {
        yield return new WaitForSeconds(initialStrikeDelay);

        if (!playerIsCurrentlyInSafeZone)
        {
            yield return StartCoroutine(PerformLightningStrike(initialAndSafeZoneLightningObject, initialLightningDuration));
        }

        StartCountdown();
    }

    private IEnumerator FinalLightningSequence()
    {
        yield return null; // Wait one frame

        if (playerIsCurrentlyInSafeZone)
        {
            Debug.Log("Player reached safe zone, skipping final lightning strike (already won by entering safe zone).");
            yield break;
        }
        else
        {
            Debug.Log("Player did NOT reach safe zone in time! Game Over scenario.");

            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("isBurnt");
                Debug.Log("Triggering 'isBurnt' animation.");
            }

            GameObject lightningToStrikePlayer = null;
            if (playerStrikeLightningBolts.Count > 0)
            {
                int randomIndex = Random.Range(0, playerStrikeLightningBolts.Count);
                lightningToStrikePlayer = playerStrikeLightningBolts[randomIndex];
                currentActivePlayerLightningBolt = lightningToStrikePlayer;
            }
            else
            {
                Debug.LogWarning("No player strike lightning bolts assigned!");
                lightningToStrikePlayer = initialAndSafeZoneLightningObject;
            }

            if (lightningToStrikePlayer != null)
            {
                yield return StartCoroutine(PerformLightningStrike(lightningToStrikePlayer, playerLightningDuration));
            }
            else
            {
                Debug.LogWarning("No lightning object to strike player with!");
            }

            if (GameManager.instance != null)
            {
                GameManager.instance.GameOver();
            }
            else
            {
                Debug.LogWarning("GameManager instance not found for GameOver call.");
            }
        }
    }

    private IEnumerator PerformLightningStrike(GameObject lightningBoltVisual, float totalDuration)
    {
        if (lightningContainerParent == null || lightningBoltVisual == null)
        {
            Debug.LogWarning("Lightning Container Parent or specific lightning bolt visual not assigned for strike!");
            yield break;
        }

        if (playerGameObject != null)
        {
            Vector3 playerPos = playerGameObject.transform.position;
            lightningContainerParent.transform.position = new Vector3(lightningContainerParent.transform.position.x, playerPos.y + lightningYOffset, lightningContainerParent.transform.position.z);
        }
        else
        {
            Debug.LogWarning("Player GameObject is null, cannot position lightning container!");
            yield break;
        }

        lightningContainerParent.SetActive(true);

        if (thunderSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(thunderSound, thunderVolume);
        }

        yield return new WaitForSeconds(0.05f);

        int actualFlickerCount = Random.Range(minFlickerCount, maxFlickerCount + 1);

        for (int i = 0; i < actualFlickerCount; i++)
        {
            lightningBoltVisual.SetActive(true);
            yield return new WaitForSeconds(flickerOnDuration);

            lightningBoltVisual.SetActive(false);

            if (i < actualFlickerCount - 1)
            {
                float randomOffTime = Random.Range(minFlickerOffTime, maxFlickerOffTime);
                yield return new WaitForSeconds(randomOffTime);
            }
        }

        lightningBoltVisual.SetActive(false);
        lightningContainerParent.SetActive(false);
        currentActivePlayerLightningBolt = null;
    }

    public void ResetWeather()
    {
        StopAllCoroutines();

        countdownStarted = false;
        safeObjectSpawnTriggered = false;
        currentCountdownTime = 0f;
        initialCountdownDuration = 0f;
        playerIsCurrentlyInSafeZone = false;
        ShouldStopRegularSpawns = false; // NEW: Reset this flag too

        if (initialAndSafeZoneLightningObject != null) initialAndSafeZoneLightningObject.SetActive(false);
        foreach (GameObject lb in playerStrikeLightningBolts)
        {
            if (lb != null) lb.SetActive(false);
        }
        if (currentActivePlayerLightningBolt != null) currentActivePlayerLightningBolt.SetActive(false);
        currentActivePlayerLightningBolt = null;

        if (lightningContainerParent != null) lightningContainerParent.SetActive(false);


        if (objectSpawner != null)
        {
            objectSpawner.ResetSpawner();
        }
        else
        {
            Debug.LogWarning("ObjectSpawner is null during WeatherManager ResetWeather. Cannot reset spawner.");
        }

        StartCoroutine(InitialLightningSequence());
    }
}