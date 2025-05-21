using UnityEngine;

public class Audio_DangerousObjects : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("The AudioClip to play when this object collides with another.")]
    [SerializeField]
    private AudioClip collisionSound;

    [Tooltip("Volume scale for this specific collision sound. 1.0 is full SFX volume.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float localVolumeScale = 1.0f; // Allows individual volume adjustment

    private AudioSource audioSource;

    private void Awake()
    {
        // Get the AudioSource component attached to this GameObject.
        // If it doesn't exist, add one.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // Ensure it doesn't play automatically
            audioSource.loop = false;       // Ensure it's a one-shot sound
            audioSource.spatialBlend = 0f;  // Default to 2D sound (can be changed in Inspector)
        }
    }

    private void OnEnable()
    {
        // Update volume when enabled, in case AudioManager's volume changed.
        ApplyVolume();
    }

    private void OnValidate()
    {
        // Called in editor when script is loaded or value changed in Inspector
        // Good for instantly seeing volume changes in editor.
        ApplyVolume();
    }

    /// <summary>
    /// Applies the volume settings, combining global SFX volume with local scale.
    /// </summary>
    private void ApplyVolume()
    {
        if (audioSource != null && AudioManager.Instance != null)
        {
            audioSource.volume = AudioManager.Instance.GetSfxVolume() * localVolumeScale;
        }
        else if (audioSource != null)
        {
            // Fallback if AudioManager isn't ready/present, use only local scale
            audioSource.volume = localVolumeScale;
        }
    }

    // You can choose between OnCollisionEnter2D (for physics collisions)
    // or OnTriggerEnter2D (for trigger-based collisions).
    // Use the one that matches how your dangerous objects are set up.

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only play sound if this object is considered "dangerous"
        // (assuming this script is only on dangerous objects)
        PlayCollisionSound();
    }

    // Uncomment this if your dangerous objects use Triggers instead of Colliders
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only play sound if this object is considered "dangerous"
        PlayCollisionSound();
    }

    /// <summary>
    /// Plays the assigned collision sound.
    /// </summary>
    public void PlayCollisionSound()
    {
        if (collisionSound == null)
        {
            Debug.LogWarning($"Audio_DangerousObjects on {gameObject.name}: No collision sound assigned.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError($"Audio_DangerousObjects on {gameObject.name}: AudioSource component is missing!");
            return;
        }

        // Apply the latest volume settings before playing
        ApplyVolume();

        // Play the clip. PlayOneShot allows multiple collision sounds to overlap if needed.
        audioSource.PlayOneShot(collisionSound);
    }
}