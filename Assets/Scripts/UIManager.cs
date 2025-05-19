using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [SerializeField]
    private TextMeshProUGUI currentScoreText;

    [SerializeField]
    private TextMeshProUGUI highScoreText;

    [SerializeField]
    private TextMeshProUGUI timeText; // Reference to the Time TextMeshPro object

    private int score;
    private float startTime;
    private float endTime;
    private bool hasStartedMoving = false;
    private bool hasStopped = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        currentScoreText.text = score.ToString();
        highScoreText.text = PlayerPrefs.GetInt("HighScore", 0).ToString();
        UpdateHighScore();
        timeText.text = "00:00.000"; // Initialize time text in the desired format
    }

    private void Update()
    {
        if (hasStartedMoving && !hasStopped)
        {
            float currentTime = Time.time - startTime;
            UpdateTimeDisplay(currentTime);
        }
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

    // Call this function when the player first starts moving
    public void StartTimer()
    {
        if (!hasStartedMoving)
        {
            hasStartedMoving = true;
            startTime = Time.time;
            UpdateTimeDisplay(0f); // Initial display of 0 time
        }
    }

    // Call this function when the game is over
    public void StopTimer()
    {
        if (!hasStopped)
        {
            hasStopped = true;
            endTime = Time.time;
            float totalTime = endTime - startTime;
            UpdateTimeDisplay(totalTime);
        }
    }

    private void UpdateTimeDisplay(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 1000) % 1000); // Get milliseconds

        timeText.text = $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }
}