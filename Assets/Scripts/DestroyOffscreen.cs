using UnityEngine;

public class DestroyOffscreen : MonoBehaviour
{
    [Tooltip("The X position (world coordinates) at which this object will be destroyed if it moves to the left of it.")]
    [HideInInspector] // Hide in Inspector as this is set in the WeatherManager
    public float destroyXPosition = -10f; // Adjust in Inspector based on your camera's left edge

    private void Awake()
    {
        LevelManager levelManager = LevelManager.instance ?? FindAnyObjectByType<LevelManager>();
        if (levelManager != null )
        {
            destroyXPosition = levelManager.DestroyObjectsXPosition;
        }
    }
    void Update()
    {
        // If the object's X position is less than the destroyXPosition, destroy it.
        if (transform.position.x < destroyXPosition)
        {
            Destroy(gameObject);
        }
    }
}