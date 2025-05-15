using UnityEngine;

public class MoveObject : MonoBehaviour
{
    [SerializeField]
    private float speed = 0.65f;

    [SerializeField]
    public bool Pause = false;

    private void Update()
    {
        // Only move if not paused
        if (!Pause)
        {
            transform.position += Vector3.left * speed * Time.deltaTime;
        }
    }

    // Public method to pause/unpause the movement
    public void PauseMovement(bool pause)
    {
        Pause = pause;
    }
}
