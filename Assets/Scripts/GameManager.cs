using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField]
    private GameObject gameMenuCanvas;

    [SerializeField]
    private GameObject gameOverCanvas;

    [SerializeField]
    private GameObject winCanvas; // Reference to the Win UI Canvas/GameObject

    [SerializeField]
    public GameObject GameOverReasonCanvas;

    [Header("Screen Delay Settings")]
    [Tooltip("Delay in seconds before showing the Game Over or Win screen.")]
    [SerializeField]
    private float gameOverScreenDisplayDelay = 1.5f; // Unified delay for both screens

    // Private variable to track game over state
    private bool isGameOver = false;

    // Private variable to track win state
    private bool isGameWon = false; // Added to explicitly track win condition

    // Private variable to track pause state
    private bool isGamePausedInternal = false;

    // NEW: Score and Death Count variables
    private int _currentScore = 0;
    private int _deathsCount = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Time.timeScale = 1f;
            // Optional: If you want GameManager to persist across scenes, use DontDestroyOnLoad
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Public methods that can be directly linked to the OnClick() event of the buttons

    public void LoadScene(string sceneName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAllAudio(); // Stop all audio before loading a new scene
        }
        else
        {
            Debug.LogWarning("AudioManager instance is null. Cannot stop audio.");
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName); // Use the fully qualified name to avoid conflict
            Debug.Log($"Loading scene: {sceneName}");
        }
        else
        {
            Debug.LogError("Scene name to load is empty or null.");
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        gameMenuCanvas.SetActive(false);
        gameOverCanvas.SetActive(false);
        if (winCanvas != null) // Ensure winCanvas is hidden when starting a new game
        {
            winCanvas.SetActive(false);
        }
        // Reset game over and win states when starting a new game
        isGameOver = false;
        isGameWon = false;
        isGamePausedInternal = false; // Game is unpaused when starting

        ResetGameStats(); // NEW: Reset score and deaths at game start
    }

    public void GameOver()
    {
        // Only trigger Game Over if not already over or won
        if (isGameOver || isGameWon) return;

        isGameOver = true;
        isGameWon = false; // Ensure win state is false
        isGamePausedInternal = true; // Game is logically paused

        if (GameOverReasonCanvas != null)
        {
            GameOverReasonCanvas.SetActive(true); // Hide the game over reason canvas if it exists
        }

        // NEW: Record replay data on Game Over
        if (ReplayManager.Instance != null)
        {
            GameObject player = GameObject.FindWithTag("Player"); // Assuming player has "Player" tag
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
            ReplayManager.Instance.RecordSafePointReached(playerPos, _currentScore, _deathsCount);
        }
        else
        {
            Debug.LogWarning("ReplayManager not found. Game Over data will not be recorded.");
        }

        // Start the Coroutine to handle the delay and then show the game over screen
        StartCoroutine(ShowEndGameScreenAfterDelay(gameOverCanvas));
    }

    // Method for winning the game
    public void WinGame()
    {
        // Only trigger Win if not already over or won
        if (isGameOver || isGameWon) return;

        isGameOver = true; // Win is also an end state for the game loop
        isGameWon = true;
        isGamePausedInternal = true; // Game is logically paused

        UIManager.instance.StopTimer(); // Stop timer on win

        if (GameOverReasonCanvas != null)
        {
            GameOverReasonCanvas.SetActive(true); // Hide the game over reason canvas if it exists
            GameOverReasonCanvas.GetComponentInChildren<TextMeshProUGUI>().SetText("You made it to safety!"); // Set win message
        }

        // NEW: Record replay data on Win
        if (ReplayManager.Instance != null)
        {
            GameObject player = GameObject.FindWithTag("Player"); // Assuming player has "Player" tag
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
            ReplayManager.Instance.RecordSafePointReached(playerPos, _currentScore, _deathsCount);
        }
        else
        {
            Debug.LogWarning("ReplayManager not found. Win data will not be recorded.");
        }

        // Start the Coroutine to handle the delay and then show the win screen
        StartCoroutine(ShowEndGameScreenAfterDelay(winCanvas));
    }

    // This Coroutine now handles displaying either the Game Over or Win screen
    private IEnumerator ShowEndGameScreenAfterDelay(GameObject screenToShow)
    {
        Debug.Log($"Waiting for {gameOverScreenDisplayDelay} seconds before showing {screenToShow.name}...");
        yield return new WaitForSeconds(gameOverScreenDisplayDelay);

        // ** Reset Time.timeScale back to normal **
        Time.timeScale = 1.0f;
        Debug.Log("Game Over screen appearing. Time.timeScale reset to: " + Time.timeScale);

        Debug.Log($"Displaying {screenToShow.name}.");
        gameMenuCanvas.SetActive(true); // Always show the main game menu canvas if it acts as a container
        gameOverCanvas.SetActive(false); // Hide both initially

        if (winCanvas != null)
        {
            winCanvas.SetActive(false);
        }

        // Now activate the specific screen requested
        if (screenToShow != null)
        {
            screenToShow.SetActive(true);
        }

        Time.timeScale = 0f; // Freeze the game completely after the delay
    }

    public void RestartGame()
    {
        Time.timeScale = 1.0f; // Ensure time is normal for the start of a new game
        gameMenuCanvas.SetActive(false);
        gameOverCanvas.SetActive(false);
        if (winCanvas != null) // Ensure winCanvas is hidden
        {
            winCanvas.SetActive(false);
        }
        // Reset game over and win states before reloading the scene
        isGameOver = false;
        isGameWon = false;
        isGamePausedInternal = false;

        ResetGameStats(); // NEW: Reset score and deaths at game restart

        FlyBehavior.instance.EnablePlayerInput(); // Re-enable player input if applicable

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Pause()
    {
        // Don't allow pausing if the game is already over or won
        if (isGameOver || isGameWon) return;

        isGamePausedInternal = true;
        Time.timeScale = 0f;
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(false); // Ensure game over is hidden
        if (winCanvas != null) // Ensure winCanvas is hidden during a pause
        {
            winCanvas.SetActive(false);
        }
    }

    // Public method to unpause the game
    public void Unpause()
    {
        if (!isGamePausedInternal || isGameOver || isGameWon) return;

        isGamePausedInternal = false;
        Time.timeScale = 1f;
        gameMenuCanvas.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting application...");
        Application.Quit();
    }

    // Public method for other scripts to check if the game is over
    public bool IsGameOver()
    {
        return isGameOver;
    }

    // Public method for other scripts to check if the game has been won
    public bool IsGameWon()
    {
        return isGameWon;
    }

    // Public method for other scripts to check if the game is currently paused
    public bool IsGamePaused()
    {
        return isGamePausedInternal;
    }

    // NEW: Methods for Score and Deaths
    public void AddScore(int amount)
    {
        _currentScore += amount;
        // You might have UIManager.instance.UpdateScoreDisplay(_currentScore); here
        Debug.Log($"Score: {_currentScore}");
    }

    public int GetScore()
    {
        return _currentScore;
    }

    public void PlayerDied()
    {
        _deathsCount++;
        // You might have UIManager.instance.UpdateDeathsDisplay(_deathsCount); here
        Debug.Log($"Deaths: {_deathsCount}");
        // Add logic for player respawn or other death handling here
    }

    public int GetDeaths()
    {
        return _deathsCount;
    }

    // Method to reset score and deaths
    public void ResetGameStats()
    {
        _currentScore = 0;
        _deathsCount = 0;
        // You might also want to reset UI displays here
        Debug.Log("Game stats reset.");
    }
}