using UnityEngine;

public class ProgressTracker : MonoBehaviour
{
    public static ProgressTracker Instance { get; private set; }

    [Header("Progression Settings")]
    [Tooltip("The minimum percentage of ESTIMATED objects the player must pass for the Safe Object to appear.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float requiredPassPercentage = 0.7f;

    [Header("Debug Info (Read Only)")]
    [SerializeField] private int _totalObjectsSpawned = 0; // Changed to private backing field
    [SerializeField] private int _objectsPassed = 0;       // Changed to private backing field
    [SerializeField] private int estimatedTotalObjects = 0;
    [SerializeField] private bool progressionRequirementMet = false;

    // Public properties to allow other scripts to read these values for logging/display
    public int totalObjectsSpawned => _totalObjectsSpawned; // Public getter for totalObjectsSpawned
    public int objectsPassed => _objectsPassed;           // Public getter for objectsPassed

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void IncrementTotalObjectsSpawned()
    {
        _totalObjectsSpawned++; // Access backing field
        UpdateProgressionStatus();
    }

    public void IncrementObjectsPassed()
    {
        _objectsPassed++; // Access backing field
        UpdateProgressionStatus();
    }

    private void UpdateProgressionStatus()
    {
        if (estimatedTotalObjects <= 0)
        {
            progressionRequirementMet = false;
            return;
        }

        float currentPercentage = (float)_objectsPassed / estimatedTotalObjects; // Use backing field
        progressionRequirementMet = currentPercentage >= requiredPassPercentage;
    }

    public bool HasMetProgressionRequirement()
    {
        return progressionRequirementMet;
    }

    public void ResetProgress(float initialCountdownDuration, ObjectSpawner spawner)
    {
        _totalObjectsSpawned = 0; // Reset backing field
        _objectsPassed = 0;       // Reset backing field
        progressionRequirementMet = false;

        if (spawner != null && initialCountdownDuration > 0)
        {
            float avgSpawnInterval = (spawner.minSpawnInterval + spawner.maxSpawnInterval) / 2f;
            if (avgSpawnInterval > 0)
            {
                estimatedTotalObjects = Mathf.Max(1, Mathf.CeilToInt(initialCountdownDuration / avgSpawnInterval));
            }
            else
            {
                estimatedTotalObjects = 1;
            }
        }
        else
        {
            estimatedTotalObjects = 0;
        }

        Debug.Log($"ProgressionTracker: Reset. Estimated Total Objects: {estimatedTotalObjects}");
    }

    public int GetEstimatedTotalObjects()
    {
        return estimatedTotalObjects;
    }
}