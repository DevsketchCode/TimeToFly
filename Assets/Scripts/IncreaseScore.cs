using UnityEngine;

public class IncreaseScore : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            UIManager.instance.UpdateScore();
            Destroy(gameObject);
        }
    }
}
