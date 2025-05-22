using UnityEngine;
using System.Collections.Generic; // Required for Dictionary and List
using System.Collections; // Required for IEnumerator

public class AudioManager : MonoBehaviour
{
    // Singleton instance
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("The AudioSource used for playing general background music.")]
    [SerializeField]
    private AudioSource musicAudioSource;

    [Tooltip("The AudioSource used for playing ambient background sounds.")]
    [SerializeField]
    private AudioSource ambientAudioSource;

    [Header("SFX Pooling")]
    [Tooltip("The initial number of SFX AudioSource components to create in the pool.")]
    [SerializeField]
    private int sfxPoolSize = 10; // How many SFX sources to pre-create
    [Tooltip("Parent object for pooled SFX AudioSources to keep hierarchy clean.")]
    [SerializeField]
    private Transform sfxPoolParent;

    private List<AudioSource> sfxPool = new List<AudioSource>();
    private Queue<AudioSource> availableSfxSources = new Queue<AudioSource>();
    private List<AudioSource> activeSfxSources = new List<AudioSource>(); // To manage currently playing ones

    [Header("Global Volume Settings")]

    [Range(0f, 1f)]
    [SerializeField]
    private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField]
    private float musicVolume = 0.12f;

    [Range(0f, 1f)]
    [SerializeField]
    private float ambientVolume = 0.15f;

    [Range(0f, 2f)]
    [SerializeField]
    private float sfxVolume = 1f;

    // A dictionary to store currently playing looping sounds by their clip reference.
    // This allows you to stop specific looping sounds that are *not* main music/ambient.
    private Dictionary<AudioClip, AudioSource> dynamicLoopingSources = new Dictionary<AudioClip, AudioSource>();

    private Coroutine musicFadeCoroutine;
    private Coroutine ambientFadeCoroutine;

    private void Awake()
    {
        // Implement the Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep the AudioManager alive across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
            return; // Exit to prevent further execution for this duplicate
        }

        // Initialize dedicated AudioSources if they are not assigned in the Inspector
        InitializeDedicatedAudioSources();

        // Initialize SFX AudioSource pool
        InitializeSfxPool();

        // Apply initial volume settings
        ApplyVolumeSettings();
    }

    private void InitializeDedicatedAudioSources()
    {
        if (musicAudioSource == null)
        {
            GameObject musicGo = new GameObject("MusicSource");
            musicGo.transform.SetParent(this.transform);
            musicAudioSource = musicGo.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = true; // Music typically loops by default
        }
        if (ambientAudioSource == null)
        {
            GameObject ambientGo = new GameObject("AmbientSource");
            ambientGo.transform.SetParent(this.transform);
            ambientAudioSource = ambientGo.AddComponent<AudioSource>();
            ambientAudioSource.playOnAwake = false;
            ambientAudioSource.loop = true; // Ambient sounds typically loop
        }
    }

    private void InitializeSfxPool()
    {
        if (sfxPoolParent == null)
        {
            GameObject poolParentGo = new GameObject("SFX_Pool");
            poolParentGo.transform.SetParent(this.transform);
            sfxPoolParent = poolParentGo.transform;
        }

        for (int i = 0; i < sfxPoolSize; i++)
        {
            CreateNewSfxSource();
        }
    }

    private AudioSource CreateNewSfxSource()
    {
        GameObject sfxGo = new GameObject("SFX_Source");
        sfxGo.transform.SetParent(sfxPoolParent);
        AudioSource newSource = sfxGo.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.loop = false;
        newSource.spatialBlend = 0f; // 2D sound by default
        sfxPool.Add(newSource);
        availableSfxSources.Enqueue(newSource);
        return newSource;
    }

    private void Update()
    {
        // Clean up finished SFX sources and return them to the pool
        for (int i = activeSfxSources.Count - 1; i >= 0; i--)
        {
            if (!activeSfxSources[i].isPlaying)
            {
                ReturnSfxSourceToPool(activeSfxSources[i]);
                activeSfxSources.RemoveAt(i);
            }
        }
    }

    private void OnValidate()
    {
        ApplyVolumeSettings();
    }

    private void ApplyVolumeSettings()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = masterVolume * musicVolume;
        }
        if (ambientAudioSource != null)
        {
            ambientAudioSource.volume = masterVolume * ambientVolume;
        }

        // Apply to all pooled SFX sources
        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
            {
                source.volume = masterVolume * sfxVolume;
            }
        }
        // Apply to any dynamically created looping sources
        foreach (var entry in dynamicLoopingSources)
        {
            if (entry.Value != null)
            {
                entry.Value.volume = masterVolume * musicVolume; // Or a separate dynamicLoopingVolume
            }
        }
    }

    /// <summary>
    /// Gets an available AudioSource from the SFX pool or creates a new one if needed.
    /// </summary>
    private AudioSource GetAvailableSfxSource()
    {
        if (availableSfxSources.Count > 0)
        {
            return availableSfxSources.Dequeue();
        }
        else
        {
            // If the pool is exhausted, create a new one (expands the pool)
            Debug.LogWarning("SFX AudioSource pool exhausted. Creating a new one.");
            return CreateNewSfxSource();
        }
    }

    /// <summary>
    /// Returns an AudioSource to the SFX pool.
    /// </summary>
    private void ReturnSfxSourceToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null; // Clear the clip
        source.transform.SetParent(sfxPoolParent); // Re-parent for cleanliness
        availableSfxSources.Enqueue(source);
    }

    /// <summary>
    /// Plays an AudioClip once as a sound effect from the SFX pool.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="volumeScale">Optional: Multiplier for the clip's volume (0-1).</param>
    /// <param name="pitch">Optional: Pitch of the sound (default 1).</param>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play null SFX clip.");
            return;
        }

        AudioSource source = GetAvailableSfxSource();
        source.clip = clip;
        source.volume = masterVolume * sfxVolume * volumeScale;
        source.pitch = pitch;
        source.Play();
        activeSfxSources.Add(source); // Track active sources to return them to pool
    }

    /// <summary>
    /// Plays background music. Use FadeInMusic for a gradual start.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="initialVolumeScale">Optional: Multiplier for the clip's starting volume (0-1).</param>
    public void PlayMusic(AudioClip clip, float initialVolumeScale = 0f) // Changed default to 0 for fading
    {
        if (clip == null || musicAudioSource == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play null music clip or Music AudioSource is missing.");
            return;
        }
        if (musicAudioSource.isPlaying && musicAudioSource.clip == clip)
        {
            return;
        }
        musicAudioSource.clip = clip;
        musicAudioSource.volume = masterVolume * musicVolume * initialVolumeScale; // Set initial volume
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    /// <summary>
    /// Plays an ambient background sound. Use FadeInAmbient for a gradual start.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="initialVolumeScale">Optional: Multiplier for the clip's starting volume (0-1).</param>
    public void PlayAmbient(AudioClip clip, float initialVolumeScale = 0f) // Changed default to 0 for fading
    {
        if (clip == null || ambientAudioSource == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play null ambient clip or Ambient AudioSource is missing.");
            return;
        }
        if (ambientAudioSource.isPlaying && ambientAudioSource.clip == clip)
        {
            return;
        }
        ambientAudioSource.clip = clip;
        ambientAudioSource.volume = masterVolume * ambientVolume * initialVolumeScale; // Set initial volume
        ambientAudioSource.loop = true;
        ambientAudioSource.Play();
    }

    /// <summary>
    /// Plays an AudioClip as a dynamically managed looping sound (e.g., a power-up hum, persistent character sound).
    /// Creates a new AudioSource for each unique looping clip.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="volumeScale">Optional: Multiplier for the clip's volume (0-1).</param>
    /// <returns>The AudioSource playing the sound, or null if it couldn't be played.</returns>
    public AudioSource PlayDynamicLoopingSound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play null dynamic looping clip.");
            return null;
        }

        if (dynamicLoopingSources.TryGetValue(clip, out AudioSource existingSource) && existingSource != null && existingSource.isPlaying)
        {
            Debug.LogWarning($"AudioManager: Dynamic looping sound '{clip.name}' is already playing. Skipping new instance.");
            return existingSource;
        }

        GameObject audioObject = new GameObject($"DynamicLoopingAudio_{clip.name}");
        audioObject.transform.SetParent(this.transform); // Keep it organized under AudioManager
        AudioSource newSource = audioObject.AddComponent<AudioSource>();
        newSource.clip = clip;
        newSource.loop = true;
        newSource.volume = masterVolume * musicVolume * volumeScale; // Use music volume for these
        newSource.Play();
        dynamicLoopingSources[clip] = newSource; // Add/update in dictionary
        return newSource;
    }

    /// <summary>
    /// Fades in the background music over a specified duration.
    /// </summary>
    /// <param name="clip">The AudioClip to play. If null, continues fading the current music.</param>
    /// <param name="duration">The duration of the fade in seconds.</param>
    /// <param name="targetVolumeScale">The target volume (0-1) for this specific clip, relative to musicVolume.</param>
    public void FadeInMusic(AudioClip clip, float duration, float targetVolumeScale = 1f)
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine); // Stop any existing fade
        }

        if (clip != null)
        {
            // Start playing the clip immediately at 0 volume
            PlayMusic(clip, 0f); // Use the modified PlayMusic to start at 0
        }
        else if (musicAudioSource == null || !musicAudioSource.isPlaying)
        {
            Debug.LogWarning("AudioManager: No music clip provided and no music is currently playing to fade in.");
            return;
        }

        musicFadeCoroutine = StartCoroutine(FadeAudioSource(musicAudioSource, duration, targetVolumeScale, musicVolume));
    }

    /// <summary>
    /// Fades out the background music over a specified duration.
    /// </summary>
    /// <param name="duration">The duration of the fade in seconds.</param>
    public void FadeOutMusic(float duration)
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
        if (musicAudioSource == null || !musicAudioSource.isPlaying)
        {
            return; // No music to fade out
        }
        musicFadeCoroutine = StartCoroutine(FadeAudioSource(musicAudioSource, duration, 0f, musicVolume, true));
    }


    /// <summary>
    /// Fades in the ambient background sound over a specified duration.
    /// </summary>
    /// <param name="clip">The AudioClip to play. If null, continues fading the current ambient sound.</param>
    /// <param name="duration">The duration of the fade in seconds.</param>
    /// <param name="targetVolumeScale">The target volume (0-1) for this specific clip, relative to ambientVolume.</param>
    public void FadeInAmbient(AudioClip clip, float duration, float targetVolumeScale = 1f)
    {
        if (ambientFadeCoroutine != null)
        {
            StopCoroutine(ambientFadeCoroutine); // Stop any existing fade
        }

        if (clip != null)
        {
            // Start playing the clip immediately at 0 volume
            PlayAmbient(clip, 0f); // Use the modified PlayAmbient to start at 0
        }
        else if (ambientAudioSource == null || !ambientAudioSource.isPlaying)
        {
            Debug.LogWarning("AudioManager: No ambient clip provided and no ambient sound is currently playing to fade in.");
            return;
        }

        ambientFadeCoroutine = StartCoroutine(FadeAudioSource(ambientAudioSource, duration, targetVolumeScale, ambientVolume));
    }

    /// <summary>
    /// Fades out the ambient background sound over a specified duration.
    /// </summary>
    /// <param name="duration">The duration of the fade in seconds.</param>
    public void FadeOutAmbient(float duration)
    {
        if (ambientFadeCoroutine != null)
        {
            StopCoroutine(ambientFadeCoroutine);
        }
        if (ambientAudioSource == null || !ambientAudioSource.isPlaying)
        {
            return; // No ambient to fade out
        }
        ambientFadeCoroutine = StartCoroutine(FadeAudioSource(ambientAudioSource, duration, 0f, ambientVolume, true));
    }

    // Generic Coroutine for fading any AudioSource
    private IEnumerator FadeAudioSource(AudioSource source, float duration, float targetVolumeScale, float categoryVolume, bool stopOnFadeOut = false)
    {
        if (source == null) yield break;

        float startVolume = source.volume;
        float finalVolume = masterVolume * categoryVolume * targetVolumeScale;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, finalVolume, timer / duration);
            yield return null; // Wait for the next frame
        }

        source.volume = finalVolume; // Ensure it hits the exact target volume

        if (stopOnFadeOut && finalVolume <= 0.01f) // If fading to near zero
        {
            source.Stop();
            source.clip = null; // Clear the clip after stopping
        }
    }

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    public void StopMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
            musicAudioSource.clip = null;
        }
    }

    /// <summary>
    /// Stops the currently playing ambient sound.
    /// </summary>
    public void StopAmbient()
    {
        if (ambientAudioSource != null && ambientAudioSource.isPlaying)
        {
            ambientAudioSource.Stop();
            ambientAudioSource.clip = null;
        }
    }

    /// <summary>
    /// Stops a specific dynamic looping audio clip if it's currently playing.
    /// </summary>
    /// <param name="clip">The AudioClip to stop.</param>
    public void StopDynamicLoopingSound(AudioClip clip)
    {
        if (clip == null) return;

        if (dynamicLoopingSources.TryGetValue(clip, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source.gameObject); // Destroy the temporary AudioSource GameObject
            }
            dynamicLoopingSources.Remove(clip);
        }
    }

    /// <summary>
    /// Stops all currently playing SFX (from the pool).
    /// </summary>
    public void StopAllSFX()
    {
        foreach (AudioSource source in activeSfxSources)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop(); // Stop without returning immediately, Update will handle it
            }
        }
        // activeSfxSources will be cleared in Update as they finish
    }

    /// <summary>
    /// Stops all currently playing audio (music, ambient, all SFX, and dynamic looping sounds).
    /// </summary>
    public void StopAllAudio()
    {
        StopMusic();
        StopAmbient();
        StopAllSFX(); // This will clear activeSfxSources over time

        // Stop all dynamic looping sources
        foreach (var entry in new List<AudioSource>(dynamicLoopingSources.Values)) // Create a copy to modify collection during iteration
        {
            if (entry != null)
            {
                entry.Stop();
                Destroy(entry.gameObject);
            }
        }
        dynamicLoopingSources.Clear();
    }

    // --- Volume Control Methods --- (Same as before, but with Ambient volume)

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    public float GetSfxVolume()
    {
        return sfxVolume;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    public float GetAmbientVolume()
    {
        return ambientVolume;
    }
}