using UnityEngine;
using System.Collections; // Required for Coroutines for audio fades
using System.Collections.Generic; // Required for List

public class LevelManager : MonoBehaviour
{
    // Make LevelManager a Singleton so it's easily accessible throughout the game
    public static LevelManager instance;

    [Header("Game Speeds")]
    [Tooltip("The forward speed of the player (which also drives the camera, object spawner, and effective speed of obstacles).")]
    [SerializeField] public float playerForwardSpeed = 0.65f;

    // If you intend for some objects to have *vertical* movement controlled centrally,
    // you would introduce something like:
    // [Tooltip("The base speed for vertically moving obstacles.")]
    // [SerializeField] public float obstacleVerticalSpeed = 1.0f; // New variable if needed by a separate script

    [Tooltip("The base speed at which clouds move (before any boosts).")]
    [SerializeField] public float baseCloudMoveSpeed = 0.75f; // This value will still be present, but its assignment line will be removed.

    [Header("Background Growth Settings")]
    [Tooltip("Configure each background layer's specific growth multiplier here.")]
    [SerializeField] private List<BackgroundLayerSettings> backgroundLayers = new List<BackgroundLayerSettings>();

    [Header("Object Destruction")]
    // Used currently on Clouds only. Destroyes at specified distance from player.
    // Negative to the left of the player, Postive to the right of the player. 
    [Tooltip("Cloud Management")]
    [SerializeField]
    public float DestroyObjectsXPosition = -8f; 

    // --- Audio Clips and Fade Durations ---
    [Header("Level Audio")] // Organize audio-related fields in the Inspector
    [SerializeField] private AudioClip levelBackgroundMusic;
    [SerializeField] private AudioClip levelAmbientSound;
    [SerializeField] private float musicFadeDuration = 2.0f; // Duration for music fade-in
    [SerializeField] private float ambientFadeDuration = 3.0f; // Duration for ambient fade-in
    // ------------------------------------------

    // Private references to the components whose speeds we want to control
    private FlyBehavior flyBehavior;
    private CloudSpawner cloudSpawner;

    // --- NEW: Custom class to hold settings for each background layer ---
    [System.Serializable]
    private class BackgroundLayerSettings
    {
        [Tooltip("Drag the GameObject with the BackgroundScroller component for this layer here.")]
        public BackgroundScroller scroller;
        [Tooltip("The growth multiplier for this specific background layer. Adjust this value in the Inspector.")]
        public float growthMultiplier = 1.0f;
    }
    // -------------------------------------------------------------------

    private void Awake()
    {
        // Implement the Singleton pattern for LevelManager
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if LevelManager should persist across scenes
        }
        else
        {
            // If another instance already exists, destroy this one to ensure only one LevelManager
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // --- Audio Initialization ---
        if (AudioManager.Instance != null)
        {
            if (levelBackgroundMusic != null) AudioManager.Instance.FadeInMusic(levelBackgroundMusic, musicFadeDuration, 1f);
            if (levelAmbientSound != null) AudioManager.Instance.FadeInAmbient(levelAmbientSound, ambientFadeDuration, 1f);
        }
        else
        {
            Debug.LogError("AudioManager not found! Make sure it's in the scene and set up correctly.");
        }

        // --- Get References to Game Components ---
        flyBehavior = FlyBehavior.instance;
        cloudSpawner = CloudSpawner.instance;

        // --- Apply Speeds to Game Components ---
        ApplySpeeds();
    }

    /// <summary>
    /// Applies the speeds defined in LevelManager to the respective game components.
    /// This can be called at Start, or later if speeds need to change dynamically.
    /// </summary>
    public void ApplySpeeds()
    {
        // Set Player's Forward Speed
        if (flyBehavior != null)
        {
            flyBehavior.forwardSpeed = playerForwardSpeed;
        }
        else
        {
            Debug.LogWarning("FlyBehavior instance not found! Player forward speed cannot be set by LevelManager.");
        }

        // Set Cloud Base Speed (only setting current, not baseCloudMoveSpeed from here)
        if (cloudSpawner != null)
        {
            cloudSpawner.SetCloudSpeedBoost(false); // Ensure clouds start at normal speed, current speed will be set by FlyBehavior/CloudMovement
        }
        else
        {
            Debug.LogWarning("CloudSpawner instance not found! Cloud speed cannot be set by LevelManager.");
        }

        // Set Background Growth Speeds for all configured layers
        foreach (var layerSettings in backgroundLayers)
        {
            if (layerSettings.scroller != null)
            {
                layerSettings.scroller.growthSpeedMultiplier = layerSettings.growthMultiplier;
            }
            else
            {
                Debug.LogWarning("A BackgroundScroller reference is missing in one of the 'Background Layers' entries in LevelManager.");
            }
        }
    }

    // Example of a Game Over method where you might fade out music
    public void GameOver()
    {
        Debug.Log("Game Over!");
        // Stop or pause level elements as needed via their respective methods or Game Manager's central pause.

        if (AudioManager.Instance != null)
        {
            // Example: Fade out music and ambient sound when the game ends
            AudioManager.Instance.FadeOutMusic(1.5f); // Fade out over 1.5 seconds
            AudioManager.Instance.FadeOutAmbient(2.0f); // Fade out over 2.0 seconds
        }
    }

    // You can add other methods here as your game progresses, e.g., to change speeds dynamically
    // public void IncreaseGameSpeed(float amount) { ... }
}