using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Required for List

public class WeatherManager : MonoBehaviour
{
    [Header("Initial Lightning Strike")]
    [Tooltip("Time to wait before the first lightning strike occurs at the level start.")]
    [SerializeField]
    private float initialStrikeDelay = 5f;

    [Tooltip("The GameObject representing the initial lightning flash in the general scene. This is the one used at the beginning and in the safe zone.")]
    [SerializeField]
    private GameObject initialAndSafeZoneLightningObject; // Renamed for clarity

    // The existing 'initialLightningDuration' will now be the *total* duration for the initial strike's flicker sequence.
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

    [Header("Safe Object Spawning")]
    [Tooltip("The ObjectSpawner in the scene.")]
    [SerializeField]
    private ObjectSpawner objectSpawner; // Reference to your ObjectSpawner

    [Tooltip("Time before the main countdown ends that the Safe Object should spawn.")]
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

    // The 'initialAndSafeZoneLightningObject' (defined above) will be used for initial and safe zone strikes.
    // It should also be a child of 'lightningContainerParent'.

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


    // The existing 'playerLightningDuration' will now be the *total* duration for the player strike's flicker sequence.
    [Tooltip("Total duration for which the player lightning effect is active, including all flickers.")]
    [SerializeField]
    private float playerLightningDuration = 0.5f; // Increased for flicker effect

    // The existing 'safeZoneLightningDuration' will now be the *total* duration for the safe zone strike's flicker sequence.
    [Tooltip("Total duration for which the safe zone lightning effect is active, including all flickers.")]
    [SerializeField]
    private float safeZoneLightningDuration = 0.5f; // Increased for flicker effect


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

    // --- NEW: Track the currently active lightning bolt for player strike ---
    private GameObject currentActivePlayerLightningBolt;
    // ----------------------------------------------------------------------

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

        // --- NEW: Deactivate all lightning objects initially ---
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

    // In WeatherManager.cs

    void Update()
    {
        if (countdownStarted)
        {
            currentCountdownTime -= Time.deltaTime;

            // Pass the current countdown time to the UIManager to display
            if (UIManager.instance != null)
            {
                UIManager.instance.UpdateTimeDisplay(currentCountdownTime);
            }
            // --------------------------

            if (!safeObjectSpawnTriggered && currentCountdownTime <= safeObjectSpawnBufferTime)
            {
                if (progressTracker != null && progressTracker.HasMetProgressionRequirement())
                {
                    if (objectSpawner != null)
                    {
                        objectSpawner.SpawnSafeObject();
                        safeObjectSpawnTriggered = true;
                        Debug.Log($"Safe Object spawn triggered! Time remaining: {currentCountdownTime:F2}");
                    }
                    else
                    {
                        Debug.LogError("ObjectSpawner is null, cannot spawn Safe Object!");
                    }
                }
                else
                {
                    Debug.Log($"Safe Object spawn condition met (time), but progression not yet sufficient. Current progress: {progressTracker.objectsPassed}/{progressTracker.GetEstimatedTotalObjects()} (Passed: {progressTracker.objectsPassed}, Total Spawned: {progressTracker.totalObjectsSpawned})");
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

        // Call the new helper coroutine for the strike effect
        yield return StartCoroutine(PerformLightningStrike(initialAndSafeZoneLightningObject, initialLightningDuration));

        StartCountdown();
    }

    private IEnumerator FinalLightningSequence()
    {
        yield return null; // Wait one frame

        if (playerFlyBehavior != null && playerFlyBehavior.hasReachedSafeZone)
        {
            Debug.Log("Player reached safe zone in time! Win scenario.");
            yield return new WaitForSeconds(0.5f); // Short delay before safe zone lightning

            // Call the new helper coroutine for the strike effect
            yield return StartCoroutine(PerformLightningStrike(initialAndSafeZoneLightningObject, safeZoneLightningDuration));

            if (GameManager.instance != null)
            {
                GameManager.instance.WinGame();
            }
            else
            {
                Debug.LogWarning("GameManager instance not found for WinGame call.");
            }
        }
        else // Player did NOT reach safe zone in time! Game Over scenario.
        {
            Debug.Log("Player did NOT reach safe zone in time! Game Over scenario.");

            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("isBurnt");
                Debug.Log("Triggering 'isBurnt' animation.");
            }

            // Randomly select the player strike lightning bolt BEFORE calling the strike routine
            GameObject lightningToStrikePlayer = null;
            if (playerStrikeLightningBolts.Count > 0)
            {
                int randomIndex = Random.Range(0, playerStrikeLightningBolts.Count);
                lightningToStrikePlayer = playerStrikeLightningBolts[randomIndex];
                currentActivePlayerLightningBolt = lightningToStrikePlayer; // Store the reference
            }
            else
            {
                Debug.LogWarning("No player strike lightning bolts assigned!");
                // Fallback to initialAndSafeZoneLightningObject if no specific player ones are assigned
                lightningToStrikePlayer = initialAndSafeZoneLightningObject;
            }

            // Call the new helper coroutine for the strike effect
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
            yield break; // Exit if references are missing
        }

        // 1. Position the main lightning parent at the player's location, applying the Y-offset
        if (playerGameObject != null)
        {
            Vector3 playerPos = playerGameObject.transform.position;
            // Adjust the Y position with the new offset
            lightningContainerParent.transform.position = new Vector3(lightningContainerParent.transform.position.x, playerPos.y + lightningYOffset, lightningContainerParent.transform.position.z);
        }
        else
        {
            Debug.LogWarning("Player GameObject is null, cannot position lightning container!");
            yield break;
        }

        // 2. Activate the main lightning parent
        lightningContainerParent.SetActive(true);

        // 3. Play thunder sound (only once per strike)
        if (thunderSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(thunderSound, thunderVolume);
        }

        // Small initial delay before the first flicker starts
        yield return new WaitForSeconds(0.05f);

        int actualFlickerCount = Random.Range(minFlickerCount, maxFlickerCount + 1); // +1 because Random.Range for int is exclusive for max

        for (int i = 0; i < actualFlickerCount; i++)
        {
            lightningBoltVisual.SetActive(true);
            yield return new WaitForSeconds(flickerOnDuration);

            lightningBoltVisual.SetActive(false);

            // Don't wait after the last flicker
            if (i < actualFlickerCount - 1)
            {
                float randomOffTime = Random.Range(minFlickerOffTime, maxFlickerOffTime);
                yield return new WaitForSeconds(randomOffTime);
            }
        }

        // Final deactivation
        lightningBoltVisual.SetActive(false); // Ensure it's off
        lightningContainerParent.SetActive(false); // Deactivate the parent
        currentActivePlayerLightningBolt = null; // Clear reference if it was a player strike
    }

    public void ResetWeather()
    {
        countdownStarted = false;
        safeObjectSpawnTriggered = false;
        currentCountdownTime = 0f;
        initialCountdownDuration = 0f;

        // Ensure all lightning visuals are turned off and the container is inactive
        if (initialAndSafeZoneLightningObject != null) initialAndSafeZoneLightningObject.SetActive(false);
        foreach (GameObject lb in playerStrikeLightningBolts)
        {
            if (lb != null) lb.SetActive(false);
        }
        if (currentActivePlayerLightningBolt != null) currentActivePlayerLightningBolt.SetActive(false); // Just in case
        currentActivePlayerLightningBolt = null;

        if (lightningContainerParent != null) lightningContainerParent.SetActive(false);
        StopAllCoroutines();

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