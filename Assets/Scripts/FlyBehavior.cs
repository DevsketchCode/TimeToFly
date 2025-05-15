using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Required for HashSet

public class FlyBehavior : MonoBehaviour
{
    [SerializeField]
    private float jumpForce = 5f;
    [SerializeField]
    private float pausedJumpForce = 2f; // Upward force applied when jumping while paused
    [SerializeField]
    private float rotationSpeed = 10f;

    private Rigidbody2D rb;
    public string playerTag = "Player"; // Assign the tag of your player GameObject

    public InputActionReference jumpActionReference;
    private InputAction jumpAction;
    public bool isPaused = false; // Master pause state for this script's controlled elements
    public bool isGrounded = false;

    // Keep track of all "Obstacle" tagged objects the player is currently touching
    private HashSet<GameObject> currentCollidingObstacles = new HashSet<GameObject>();
    private GameObject currentGroundObject = null; // Optional: to specifically identify the ground object

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
        if (jumpAction.WasPerformedThisFrame())
        {
            if (!isPaused) // Game is NOT paused, normal jump
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
            else // Game IS paused, special jump logic
            {
                bool shouldUnpauseThisJump = false;
                if (isGrounded) // Trying to jump off the ground while paused
                {
                    bool collidingWithNonGroundObstacle = false;
                    foreach (GameObject obs in currentCollidingObstacles)
                    {
                        // Check if this obstacle is something other than the ground we are on
                        if (obs != currentGroundObject && obs.CompareTag("Obstacle")) // Ensure it's an obstacle and not the ground itself
                        {
                            collidingWithNonGroundObstacle = true;
                            Debug.Log($"FlyBehavior: Attempted jump from ground, but also colliding with: {obs.name}. NOT unpausing via jump.");
                            break;
                        }
                    }

                    if (!collidingWithNonGroundObstacle)
                    {
                        shouldUnpauseThisJump = true;
                    }
                }
                // Else: jumping mid-air while paused (e.g., against a wall).
                // This jump itself doesn't unpause based on the "jump from ground AND clear" rule.
                // Unpausing from wall contact is handled by OnCollisionExit2D if all obstacles are cleared.

                if (shouldUnpauseThisJump)
                {
                    Debug.Log("FlyBehavior: Jumped from ground while paused & clear of other obstacles. Unpausing and applying normal jump.");
                    UnpauseLevelElements(); // This will set isPaused to false
                    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse); // Normal jump force
                }
                else
                {
                    Debug.Log("FlyBehavior: Jumped while paused (either on ground but blocked, or mid-air against obstacle). Applying pausedJumpForce.");
                    rb.linearVelocity = Vector2.zero; // Reset velocity for a more controlled "nudge"
                    rb.AddForce(Vector2.up * pausedJumpForce, ForceMode2D.Impulse); // Paused jump force
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.y != 0) // Only rotate if there's vertical movement
        {
            transform.rotation = Quaternion.Euler(0, 0, rb.linearVelocity.y * rotationSpeed);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"FlyBehavior: OnCollisionEnter2D with {collision.gameObject.name}, tag: {collision.gameObject.tag}");

        if (collision.gameObject.CompareTag("DangerousObstacle"))
        {
            GameManager.instance.GameOver();
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            currentCollidingObstacles.Add(collision.gameObject);
            bool wasAlreadyPaused = isPaused;

            if (collision.gameObject.name == "Ground") // Assuming "Ground" is a unique name for your ground objects
            {
                isGrounded = true;
                currentGroundObject = collision.gameObject; // Store reference to current ground
                Debug.Log("FlyBehavior: Landed on Ground. isGrounded = true.");
            }
            else
            {
                Debug.Log($"FlyBehavior: Collided with Obstacle: {collision.gameObject.name}.");
            }

            if (!wasAlreadyPaused) // Only pause if not already paused (e.g. by another simultaneous collision)
            {
                PauseLevelElements();
            }
        }
        else if (collision.gameObject.CompareTag("Proceed"))
        {
            Debug.Log("FlyBehavior: Collided with 'Proceed' trigger. Unpausing.");
            if (isPaused) // Only unpause if actually paused
            {
                UnpauseLevelElements();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log($"FlyBehavior: OnCollisionExit2D from {collision.gameObject.name}, tag: {collision.gameObject.tag}");
        bool exitedAnObstacle = false;

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            exitedAnObstacle = true;
            currentCollidingObstacles.Remove(collision.gameObject);

            if (collision.gameObject == currentGroundObject) // Check if we left the specific ground object we were on
            {
                isGrounded = false;
                currentGroundObject = null; // Clear ground reference
                Debug.Log("FlyBehavior: Left Ground. isGrounded = false.");
                // DO NOT UNPAUSE HERE simply for leaving the ground
            }
            else
            {
                Debug.Log($"FlyBehavior: No longer colliding with Obstacle: {collision.gameObject.name}.");
            }
        }

        // If player was paused, is now airborne, and no longer touching ANY obstacles, unpause.
        if (exitedAnObstacle && isPaused && !isGrounded && currentCollidingObstacles.Count == 0)
        {
            Debug.Log("FlyBehavior: Became airborne and clear of all obstacles while paused. Unpausing.");
            UnpauseLevelElements();
        }
    }

    public void PauseLevelElements()
    {
        if (isPaused) return; // Already paused, do nothing.
        Debug.Log("<color=red>FlyBehavior: PauseLevelElements() - Pausing Game Elements</color>");
        isPaused = true;

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag(playerTag)) continue;
            if (obj.TryGetComponent<LoopLevelElement>(out var loop)) loop.Pause = true;
            if (obj.TryGetComponent<ObjectSpawner>(out var spawner)) spawner.Pause = true;
            if (obj.TryGetComponent<MoveObject>(out var move)) move.Pause = true;
            if (obj.TryGetComponent<SelfDestruct>(out var selfDestruct)) selfDestruct.isPaused = true;
        }
    }

    public void UnpauseLevelElements()
    {
        if (!isPaused) return; // Already unpaused, do nothing.
        Debug.Log("<color=green>FlyBehavior: UnpauseLevelElements() - Unpausing Game Elements</color>");
        isPaused = false;

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag(playerTag)) continue;
            if (obj.TryGetComponent<LoopLevelElement>(out var loop)) loop.Pause = false;
            if (obj.TryGetComponent<ObjectSpawner>(out var spawner)) spawner.Pause = false;
            if (obj.TryGetComponent<MoveObject>(out var move)) move.Pause = false;
            if (obj.TryGetComponent<SelfDestruct>(out var selfDestruct)) selfDestruct.isPaused = false;
        }
    }
}