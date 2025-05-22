using UnityEngine;

public class DestroyOffscreen : MonoBehaviour
{
    [Tooltip("The X position (world coordinates) at which this object will be destroyed if it moves to the left of it.")]
    [SerializeField] private float destroyXPosition = -20f; // Adjust in Inspector based on your camera's left edge

    void Update()
    {
        // If the object's X position is less than the destroyXPosition, destroy it.
        if (transform.position.x < destroyXPosition)
        {
            Destroy(gameObject);
        }
    }
}