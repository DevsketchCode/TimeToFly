using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    private float moveSpeed;

    // Removed Start(), as speed is now fetched every frame in Update()

    void Update()
    {
        // Continuously get the latest speed from the CloudSpawner.
        // This ensures clouds react immediately to speed changes.
        if (CloudSpawner.instance != null)
        {
            moveSpeed = CloudSpawner.instance.CurrentCloudSpeed;
        }
        else
        {
            Debug.LogWarning("CloudSpawner instance not found! Cloud movement cannot be updated.");
            // Optionally, you could disable this component or fall back to a default speed.
            return;
        }

        // Apply the movement using the current speed.
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
    }
}