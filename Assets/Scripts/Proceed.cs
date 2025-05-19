using UnityEngine;

public class Proceed : MonoBehaviour
{
    FlyBehavior flyBehavior;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Or however you locate your player
        if (playerObject != null)
        {
            flyBehavior = playerObject.GetComponent<FlyBehavior>();
        }
        else
        {
            Debug.LogError("Player GameObject not found with tag 'Player' for Proceed script.");
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            FlyBehavior flyBehavior = collision.GetComponent<FlyBehavior>();
            if (flyBehavior != null)
            {
                flyBehavior.UnpauseLevelElements();
            }
            else
            {
                Debug.LogError("FlyBehavior script not found on the Player GameObject.");
            }
        }
    }
}
