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

    // NEW: Private variable to track game over state
    private bool isGameOver = false;

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
        // NEW: Reset game over state when starting a new game
        isGameOver = false;
    }

    public void GameOver()
    {
        // NEW: Set game over state to true
        isGameOver = true;

        UIManager.instance.StopTimer(); // Call StopTimer on game over
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(true);
        Time.timeScale = 0f;
    }

    // Method for winning the game
    public void WinGame()
    {
        // NEW: Set game over state to true (win is also a kind of "game over" for ongoing play)
        isGameOver = true;

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
        // NEW: Reset game over state before reloading the scene
        isGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(false);
        // Note: You might want to distinguish pause from game over more clearly
        // For now, isGameOver remains false during a standard pause.
    }

    public void QuitGame()
    {
        Debug.Log("Quitting application...");
        Application.Quit();
    }

    // NEW: Public method for other scripts to check if the game is over
    public bool IsGameOver()
    {
        return isGameOver;
    }
}