using UnityEngine;
using UnityEngine.InputSystem;

public class FlyBehavior : MonoBehaviour
{
    [SerializeField]
    private float jumpForce = 5f; // Adjust this value to control jump height

    [SerializeField]
    private float pausedJumpForce = 2f; // Upward force applied when jumping while paused

    [SerializeField]
    private float rotationSpeed = 10f;

    private Rigidbody2D rb;

    public string playerTag = "Player"; // Assign the tag of your player GameObject in the Inspector

    public InputActionReference jumpActionReference;
    private InputAction jumpAction;
    public bool isPaused = false;
    public bool isGrounded = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpAction = jumpActionReference.action;
        jumpAction.Enable();
    }

    private void OnDestroy()
    {
        jumpAction.Disable();
    }

    private void Update()
    {
        //Handles input and applies jump force.
        if (!isPaused && jumpAction.WasPerformedThisFrame())
        {
            // If the game is NOT paused and the Jump action was performed this TestTeframe:
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse); // Apply an immediate upward force (impulse) for a consistent jump.
        }
        else if (isPaused && jumpAction.WasPerformedThisFrame())
        {
            rb.totalForce.Set(0,0); // Reset the velocity to zero to prevent any unwanted movement while paused.
            // If the game IS paused and the Jump action was performed this frame:
            rb.AddForce(Vector2.up * pausedJumpForce, ForceMode2D.Impulse); // Apply a smaller upward impulse for incremental movement while paused.
        }
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, rb.linearVelocity.y * rotationSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision of " + collision.gameObject.name + ": " + collision.gameObject.tag + ".");
        if (collision.gameObject.CompareTag("DangerousObstacle"))
        {
            GameManager.instance.GameOver();
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (collision.gameObject.name == "Ground")
            {
                isGrounded = true;
            }
            PauseLevelElements();
        } else if (collision.gameObject.CompareTag("Proceed"))
        {
            Debug.Log("Proceeding Forward");
            UnpauseLevelElements();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (collision.gameObject.name == "Ground")
            {
                isGrounded = false;
            }
            UnpauseLevelElements();
        }
    }

    public void PauseLevelElements()
    {
        isPaused = true;
        // Find all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        // Iterate through all GameObjects and pause relevant scripts
        foreach (GameObject obj in allObjects)
        {
            // Skip the player GameObject
            if (obj.CompareTag(playerTag))
            {
                continue;
            }

            // Pause LoopLevelElement
            if (obj.TryGetComponent<LoopLevelElement>(out var loop))
            {
                loop.Pause = true;
            }

            // Pause ObjectSpawner and cancel pending destroys
            if (obj.TryGetComponent<ObjectSpawner>(out var spawner))
            {
                spawner.Pause = true;
                spawner.CancelDelayedDestroy();
            }

            // Pause MoveObject
            if (obj.TryGetComponent<MoveObject>(out var move))
            {
                move.Pause = true;
            }

            // PAUSE SELDESTRUCT COMPONENT
            if (obj.TryGetComponent<SelfDestruct>(out var selfDestruct))
            {
                selfDestruct.SetPaused(true);
            }
        }
    }

    // Public method to unpause the game and restart object destroy timers
    public void UnpauseLevelElements()
    {
        isPaused = false; // FlyBehavior's general pause state
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            // Skip the player GameObject
            if (obj.CompareTag(playerTag))
            {
                continue;
            }

            if (obj.TryGetComponent<LoopLevelElement>(out var loop)) loop.Pause = false;
            if (obj.TryGetComponent<ObjectSpawner>(out var spawner)) spawner.Pause = false;
            if (obj.TryGetComponent<MoveObject>(out var move)) move.Pause = false;

            // ***** ADD THIS SECTION TO UNPAUSE SELFDESTRUCT COMPONENTS *****
            if (obj.TryGetComponent<SelfDestruct>(out var selfDestruct))
            {
                selfDestruct.SetPaused(false);
                // Or, if you prefer using the method: selfDestruct.SetPaused(false);
            }
            // ***************************************************************
        }
    }
}