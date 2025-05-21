using UnityEngine;

public class LevelManager : MonoBehaviour
{
    // Make LevelManager a Singleton so it's easily accessible
    public static LevelManager instance;

    [SerializeField]
    public float levelSpeed = 0.65f;

    [SerializeField]
    public float objectSpeed = 0.65f;

    [SerializeField]
    public float backgroundSpeed = 0.15f;

    // --- Audio Clips and Fade Durations ---
    [Header("Level Audio")] // Organize audio-related fields in the Inspector
    [SerializeField] private AudioClip levelBackgroundMusic;
    [SerializeField] private AudioClip levelAmbientSound;
    [SerializeField] private float musicFadeDuration = 2.0f; // Duration for music fade-in
    [SerializeField] private float ambientFadeDuration = 3.0f; // Duration for ambient fade-in
    // ------------------------------------------

    private void Awake()
    {
        // Implement the Singleton pattern for LevelManager
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // If another instance already exists, destroy this one to ensure only one LevelManager
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // This is where you want to initiate the audio fades when the level starts.
        // Ensure AudioManager.Instance is not null before calling its methods.
        if (AudioManager.Instance != null)
        {
            // Fade in background music
            if (levelBackgroundMusic != null)
            {
                // Calls the FadeInMusic method from AudioManager
                AudioManager.Instance.FadeInMusic(levelBackgroundMusic, musicFadeDuration, 1f);
            }

            // Fade in ambient sound
            if (levelAmbientSound != null)
            {
                // Calls the FadeInAmbient method from AudioManager
                AudioManager.Instance.FadeInAmbient(levelAmbientSound, ambientFadeDuration, 1f);
            }
        }
        else
        {
            Debug.LogError("AudioManager not found! Make sure it's in the scene and set up correctly.");
        }
    }

    // You'll likely have other methods here, e.g., for Game Over, pausing, etc.
    // Example of a Game Over method where you might fade out music
    public void GameOver()
    {
        Debug.Log("Game Over!");
        // Stop or pause level elements as needed
        // ...

        if (AudioManager.Instance != null)
        {
            // Example: Fade out music and ambient sound when the game ends
            AudioManager.Instance.FadeOutMusic(1.5f); // Fade out over 1.5 seconds
            AudioManager.Instance.FadeOutAmbient(2.0f); // Fade out over 2.0 seconds

            // You might also play a one-shot game over sound effect here or via Audio_Player
            // Audio_Player.instance.PlayGameOverSound(); // (If Audio_Player is a singleton or accessed differently)
        }
    }

    // ... (any other methods like Update, etc., would go here)
}