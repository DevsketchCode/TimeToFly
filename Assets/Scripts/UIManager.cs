using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // Added for scene name check, if needed for mode setting

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [SerializeField]
    private TextMeshProUGUI currentScoreText;

    [SerializeField]
    private TextMeshProUGUI highScoreText;

    [SerializeField]
    private TextMeshProUGUI timeText; // Reference to the Time Value TextMeshPro object

    [Header("Time Display Settings")]
    [Tooltip("The TextMeshPro object for the title above the time display (e.g., 'Time Elapsed', 'Time Left').")]
    [SerializeField]
    private TextMeshProUGUI timeTitleText; // NEW: For the adjustable title

    [Tooltip("The default text to display for the time title.")]
    [SerializeField]
    private string defaultTimeTitle = "TIME"; // NEW: Default text for the title

    public enum TimeDisplayMode { TimePassed, Countdown } // NEW: Enum for display modes

    [Tooltip("Determines how the time is displayed: showing elapsed time or time remaining.")]
    [SerializeField]
    private TimeDisplayMode displayMode = TimeDisplayMode.TimePassed; // NEW: Select mode in Inspector

    private int score;
    private float startTime;
    private float endTime;
    private bool hasStartedMoving = false;
    private bool hasStopped = false;

    // We no longer manage countdown time here; WeatherManager will provide it.
    // private float countdownTimeReference; 


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // Optional: If you want UIManager to persist across scenes, use DontDestroyOnLoad
            // For now, assume it's created per scene, so destroy duplicates.
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentScoreText.text = score.ToString();
        highScoreText.text = PlayerPrefs.GetInt("HighScore", 0).ToString();
        UpdateHighScore();

        // Initialize time display based on mode
        if (displayMode == TimeDisplayMode.TimePassed)
        {
            timeText.text = "00:00.000"; // Time passed starts at 0
        }
        else // Countdown mode
        {
            // For countdown, the initial value will be pushed by WeatherManager's StartCountdown,
            // but we can set a placeholder here.
            timeText.text = "00:00.000";
        }

        // Set the time title text from the Inspector variable
        if (timeTitleText != null)
        {
            timeTitleText.text = defaultTimeTitle;
        }
        else
        {
            Debug.LogWarning("Time Title TextMeshProUGUI is not assigned in UIManager!");
        }
    }

    private void Update()
    {
        // Only calculate and display time if in 'TimePassed' mode
        if (displayMode == TimeDisplayMode.TimePassed && hasStartedMoving && !hasStopped)
        {
            float currentTime = Time.time - startTime;
            UpdateTimeDisplay(currentTime);
        }
        // If displayMode is Countdown, WeatherManager will call UpdateTimeDisplay directly
    }

    private void UpdateHighScore()
    {
        if (score > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt("HighScore", score);
            highScoreText.text = score.ToString();
        }
    }

    public void UpdateScore()
    {
        score++;
        currentScoreText.text = score.ToString();
        UpdateHighScore();
    }

    // Call this function when the player first starts moving (primarily for TimePassed mode)
    public void StartTimer()
    {
        if (displayMode == TimeDisplayMode.TimePassed && !hasStartedMoving)
        {
            hasStartedMoving = true;
            startTime = Time.time;
            UpdateTimeDisplay(0f); // Initial display of 0 time
        }
        // If in Countdown mode, the timer is managed externally (WeatherManager)
    }

    // Call this function when the game is over (primarily for TimePassed mode)
    public void StopTimer()
    {
        if (displayMode == TimeDisplayMode.TimePassed && !hasStopped)
        {
            hasStopped = true;
            endTime = Time.time;
            float totalTime = endTime - startTime;
            UpdateTimeDisplay(totalTime);
        }
    }

    // This method will be called by WeatherManager (for countdown) or UIManager itself (for time passed)
    public void UpdateTimeDisplay(float timeValue) // Renamed parameter for clarity
    {
        // If in countdown mode, and timeValue somehow becomes negative, clamp it to 0
        if (displayMode == TimeDisplayMode.Countdown && timeValue < 0)
        {
            timeValue = 0;
        }

        int minutes = Mathf.FloorToInt(timeValue / 60);
        int seconds = Mathf.FloorToInt(timeValue % 60);
        int milliseconds = Mathf.FloorToInt((timeValue * 1000) % 1000);

        timeText.text = $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }

    // NEW: Public method to manually set the time title text (e.g., from another script)
    public void SetTimeTitle(string newTitle)
    {
        if (timeTitleText != null)
        {
            timeTitleText.text = newTitle;
        }
    }

    // Optional: A public method to set the display mode from another script if needed
    public void SetTimeDisplayMode(TimeDisplayMode mode)
    {
        displayMode = mode;
        // Re-initialize display if mode changes during runtime
        if (displayMode == TimeDisplayMode.TimePassed)
        {
            UpdateTimeDisplay(0f);
        }
        // For Countdown, it will rely on WeatherManager to push the first value
    }
}