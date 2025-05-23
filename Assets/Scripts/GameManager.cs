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

    // Private variable to track game over state
    private bool isGameOver = false;

    // NEW: Private variable to track win state
    private bool isGameWon = false; // Added to explicitly track win condition

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
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
    }

    public void GameOver()
    {
        // Only trigger Game Over if not already over or won
        if (isGameOver || isGameWon) return;

        isGameOver = true;
        isGameWon = false; // Ensure win state is false

        UIManager.instance.StopTimer(); // Call StopTimer on game over
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(true);
        if (winCanvas != null) // Ensure winCanvas is hidden
        {
            winCanvas.SetActive(false);
        }
        Time.timeScale = 0f;
    }

    // Method for winning the game
    public void WinGame()
    {
        // Only trigger Win if not already over or won
        if (isGameOver || isGameWon) return;

        isGameOver = true; // Win is also an end state for the game loop
        isGameWon = true;

        UIManager.instance.StopTimer(); // Stop timer on win
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(false); // Ensure game over is hidden
        if (winCanvas != null)
        {
            winCanvas.SetActive(true); // Display win screen
        }
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        gameMenuCanvas.SetActive(false);
        gameOverCanvas.SetActive(false);
        if (winCanvas != null) // Ensure winCanvas is hidden
        {
            winCanvas.SetActive(false);
        }
        // Reset game over and win states before reloading the scene
        isGameOver = false;
        isGameWon = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Pause()
    {
        // Don't allow pausing if the game is already over or won
        if (isGameOver || isGameWon) return;

        Time.timeScale = 0f;
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(false); // Ensure game over is hidden
        if (winCanvas != null) // Ensure winCanvas is hidden during a pause
        {
            winCanvas.SetActive(false);
        }
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

    // NEW: Public method for other scripts to check if the game has been won
    public bool IsGameWon()
    {
        return isGameWon;
    }
}