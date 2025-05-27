using UnityEngine;

public class IncreaseScore : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            UIManager.instance.UpdateScore();

            if (ProgressTracker.Instance != null)
            {
                ProgressTracker.Instance.IncrementObjectsPassed();
            }
            else
            {
                Debug.LogWarning("IncreaseScore: ProgressTracker.Instance is null! Cannot track passed objects.");
            }

            Destroy(gameObject);
        }
    }
}
