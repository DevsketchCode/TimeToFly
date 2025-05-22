using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField]
    private GameObject gameMenuCanvas;

    [SerializeField]
    private GameObject gameOverCanvas;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    // Public methods that can be directly linked to the OnClick() event of your buttons

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
    }

    public void GameOver()
    {
        UIManager.instance.StopTimer(); // Call StopTimer on game over
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        gameMenuCanvas.SetActive(false);
        gameOverCanvas.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting application...");
        Application.Quit();
    }
}
