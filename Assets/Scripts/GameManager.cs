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
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(false);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        gameMenuCanvas.SetActive(false);
        gameOverCanvas.SetActive(false);
    }

    public void GameOver()
    {
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        gameOverCanvas.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        gameMenuCanvas.SetActive(true);
        gameOverCanvas.SetActive(false);
    }
}
