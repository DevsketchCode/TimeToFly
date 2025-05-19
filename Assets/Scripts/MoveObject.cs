using UnityEngine;
using System.Collections; // Required for Coroutines

public class MoveObject : MonoBehaviour
{
    private float speed = 0f; // Initialize to 0, will get from LevelManager

    [SerializeField]
    public bool Pause = false;

    private LevelManager levelManager;

    private void Start()
    {
        // Find the LevelManager instance in the scene
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found in the scene!");
            enabled = false; // Disable this script if LevelManager is missing
            return;
        }
        speed = levelManager.objectSpeed; // Get the object speed from LevelManager
    }

    private void Update()
    {
        if (!Pause && !isMovingRightTemporarily)
        {
            transform.position += Vector3.left * speed * Time.deltaTime;
        }
        else if (isMovingRightTemporarily)
        {
            transform.position += Vector3.right * speed * Time.deltaTime; // Move right at the same speed
        }
    }

    public void PauseMovement(bool pause)
    {
        Pause = pause;
    }

    private bool isMovingRightTemporarily = false;

    public void MoveRightTemporarily(float duration)
    {
        StartCoroutine(MoveRightRoutine(duration));
    }

    private IEnumerator MoveRightRoutine(float duration)
    {
        isMovingRightTemporarily = true;
        yield return new WaitForSeconds(duration);
        isMovingRightTemporarily = false;
    }

    // Optional: If you still want to set speed externally for specific MoveObjects
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
}