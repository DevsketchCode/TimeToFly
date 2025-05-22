using UnityEngine;
using System.Collections; // Required for Coroutines

public class SelfDestruct : MonoBehaviour
{
    [SerializeField] private bool setSelfDestruct = true;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private bool startTimerOnAwake = true;

    private float currentLifeTime;
    private bool isPaused = false;

    private void Awake()
    {
        if (setSelfDestruct)
        {
            currentLifeTime = lifeTime;
            if (startTimerOnAwake)
            {
                StartCoroutine(StartSelfDestructTimer());
            }
        }
    }

    private IEnumerator StartSelfDestructTimer()
    {
        while (currentLifeTime > 0)
        {
            if (!isPaused)
            {
                currentLifeTime -= Time.deltaTime;
            }
            yield return null; // Wait for the next frame
        }

        // Object has "passed" the player and is now off-screen
        // --- Ensure ProgressTracker.Instance exists before calling ---
        if (ProgressTracker.Instance != null)
        {
            ProgressTracker.Instance.IncrementObjectsPassed();
        }
        else
        {
            Debug.LogWarning("SelfDestruct: ProgressTracker.Instance is null! Cannot track passed objects.");
        }
        // ------------------------------------------------------------------

        Destroy(gameObject);
    }

    public void SetLifeTime(float newLifeTime)
    {
        lifeTime = newLifeTime;
        currentLifeTime = lifeTime; // Reset current time if already running
        if (!startTimerOnAwake) // Ensure timer starts if it wasn't on awake
        {
            StartCoroutine(StartSelfDestructTimer());
        }
    }

    public void SetPaused(bool pause)
    {
        isPaused = pause;
    }
}