using UnityEngine;
using System.Collections; // Make sure this is included for Coroutines

public class Audio_Player : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip collisionSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip flappingSound;

    // Private flag to prevent playing flapping sound too rapidly
    private bool isFlappingSoundPlaying = false;
    [Tooltip("Minimum time (in seconds) between consecutive flapping sounds.")]
    [SerializeField] private float minFlapDelay = 0.15f; // Adjust this value as needed

    // --- NEW: Flapping Sound Volume ---
    [Tooltip("Volume scale for the flapping sound. 1.0 is full SFX volume, 0.0 is silent.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float flappingVolumeScale = 0.7f; // Adjust this value in the Inspector to make it softer
    // ------------------------------------

    private void Start()
    {
        if (backgroundMusic != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(backgroundMusic, 0.7f);
        }
    }

    public void PlayJumpSound()
    {
        if (jumpSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(jumpSound);
        }
    }

    public void PlayCollisionSound()
    {
        if (collisionSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(collisionSound, 0.8f);
        }
    }

    public void PlayGameOverSound()
    {
        if (gameOverSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllAudio();
            AudioManager.Instance.PlaySFX(gameOverSound);
        }
    }

    /// <summary>
    /// Plays the flapping sound as a one-shot SFX, with a minimum delay between plays.
    /// The pitch can be adjusted for variations.
    /// </summary>
    /// <param name="pitch">The pitch of the flapping sound.</param>
    public void PlayFlappingSound(float pitch = 1.0f)
    {
        if (flappingSound != null && AudioManager.Instance != null && !isFlappingSoundPlaying)
        {
            // Pass the new flappingVolumeScale to PlaySFX
            AudioManager.Instance.PlaySFX(flappingSound, flappingVolumeScale, pitch);
            StartCoroutine(FlappingSoundDelay());
        }
    }

    private System.Collections.IEnumerator FlappingSoundDelay()
    {
        isFlappingSoundPlaying = true;
        yield return new WaitForSeconds(minFlapDelay);
        isFlappingSoundPlaying = false;
    }

    public void StopBackgroundMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }
    }
}