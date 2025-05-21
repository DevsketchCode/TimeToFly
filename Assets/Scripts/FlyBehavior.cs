using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Required for Coroutines

public class FlyBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float jumpForce = 5f;

    [SerializeField]
    private float pausedJumpForce = 2f;

    [SerializeField]
    private float rotationSpeed = 10f;

    [SerializeField]
    private float bounceDuration = 0.25f; // Set this in the Inspector

    [SerializeField]
    private float forwardCheckDistance = 0.5f; // Adjust as needed to check in front

    [Header("Animation Settings")]
    [SerializeField]
    private float flyingVelocityThreshold = 0.1f; // The minimum vertical velocity to consider the player "flying"

    [Range(0.25f, 5.0f)]
    [SerializeField]
    private float flappingAnimationSpeed = 1.0f; // Default speed for the flapping animation

    [Tooltip("Time to keep 'isFlying' true after velocity is zero at peak. Set to 0.05f for a short delay.")]
    [SerializeField]
    private float flyingCoyoteTime = 0.05f; // Time to keep 'isFlying' true after velocity is zero at peak

    [Header("Level Settings")]
    [SerializeField]
    private LayerMask obstacleLayer; // Assign the layer of your Obstacle objects in the Inspector
    public bool isPaused = false;

    [Header("Player Settings")]
    public string playerTag = "Player";
    public InputActionReference jumpActionReference;
    public bool currentlyFlying = false; // Track if the player is flying
    public bool isOnGround = false;
    public bool hasMoved = false;

    private InputAction jumpAction;
    private Rigidbody2D rb;
    private Animator animator;
    
    private bool isCollidingWithObstacle = false; // Track if currently colliding with a non-ground obstacle
    private bool isBouncing = false; // Flag to indicate if the bounce coroutine is running
    private float flyingTimer; // Timer for the coyote time

    private LevelManager levelManager;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpAction = jumpActionReference.action;
        jumpAction.Enable();
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found in the player object!");
        }

        // Find the LevelManager instance
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found in the scene!");
            enabled = false;
        }

        flyingTimer = 0f; // Initialize the timer
    }

    private void OnDestroy()
    {
        jumpAction.Disable();
    }

    private void Update()
    {
        if (!isPaused && jumpAction.WasPerformedThisFrame())
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            // When jumping, immediately set flying to true and reset timer
            currentlyFlying = true;
            flyingTimer = flyingCoyoteTime;
        }
        else if (isPaused && jumpAction.WasPerformedThisFrame())
        {
            rb.linearVelocity = Vector2.zero; // Directly set velocity to zero
            rb.AddForce(Vector2.up * pausedJumpForce, ForceMode2D.Impulse);
            if (!hasMoved)
            {
                // When jumping, immediately set flying to true and reset timer
                animator.SetBool("isShocked", true);
                // When leaving ground, immediately set flying to true (since you're jumping/falling)
                currentlyFlying = true;
                flyingTimer = flyingCoyoteTime;

                hasMoved = true;
                UIManager.instance.StartTimer(); // Call StartTimer when first jump occurs
            }
        }
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, rb.linearVelocity.y * rotationSpeed);
        HandleAnimation(); // Call HandleAnimation in FixedUpdate for physics-based consistency
    }

    private void OnTriggerEnter2D(Collider2D other) // Changed from OnCollisionEnter2D to OnTriggerEnter2D and parameter type
    {
        Debug.Log("Trigger entered by: " + other.gameObject.name + " Tag: " + other.gameObject.tag);
        if (other.gameObject.CompareTag("DangerousObstacle"))
        {
            isCollidingWithObstacle = true;
            PauseLevelElements();
            GameManager.instance.GameOver();
        }
        else if (other.gameObject.CompareTag("Obstacle") || other.gameObject.CompareTag("ObstacleNoPause"))
        {
            if (other.gameObject.name == "Ground")
            {
                isOnGround = true;
                currentlyFlying = false; // Ensure flying is false on ground
                flyingTimer = 0f; // Reset timer on ground

                if (!isBouncing)
                {
                    PauseLevelElements();
                }
                return;
            }

            // Handle bounce trigger
            if (other.gameObject.layer == LayerMask.NameToLayer("Obstacles") && other.gameObject.CompareTag("LeftBounceCollider"))
            {
                Debug.Log("Bouncing off LeftBounceCollider (Trigger): Tag:" + other.gameObject.tag);
                isCollidingWithObstacle = true;
                StartCoroutine(HandleObstacleCollision());
                return;
            }

            if (!other.gameObject.CompareTag("ObstacleNoPause"))
            {
                // Consider if non-bounce obstacle triggers should pause.
                // If so, keep this. If not, remove it.
                isCollidingWithObstacle = true;
                PauseLevelElements();
            }
        }
        else if (other.gameObject.CompareTag("Proceed"))
        {
            Debug.Log("Proceeding Forward (Trigger)");
            UnpauseLevelElements();
        }
    }

    // You might still need OnCollisionEnter2D for the ground and potentially other non-trigger obstacles
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision with: " + collision.gameObject.name + " Tag: " + collision.gameObject.tag);

        if (collision.gameObject.CompareTag("DangerousObstacle"))
        {
            isCollidingWithObstacle = true;
            PauseLevelElements();
            GameManager.instance.GameOver();
        }
        else if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("ObstacleNoPause"))
        {
            if (collision.gameObject.name == "Ground")
            {
                isOnGround = true;
                currentlyFlying = false; // Ensure flying is false on ground
                flyingTimer = 0f; // Reset timer on ground

                if (!isBouncing)
                {
                    PauseLevelElements();
                }
                return;
            }
            if (!collision.gameObject.CompareTag("LeftBounceCollider")) // Only pause if it's a regular obstacle, not a bounce trigger
            {
                isCollidingWithObstacle = true;
                PauseLevelElements();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (collision.gameObject.name == "Ground")
            {
                isOnGround = false;
                // When leaving ground, immediately set flying to true (since you're jumping/falling)
                currentlyFlying = true;
                flyingTimer = flyingCoyoteTime;

                // Only unpause if the game was paused due to ground contact and not bouncing
                if (isPaused && !isBouncing && !isCollidingWithObstacle)
                {
                    UnpauseLevelElements();
                }
                return;
            }
            isCollidingWithObstacle = false;
            // Check for leaving top/bottom of other obstacles
            float playerBottom = GetComponent<Collider2D>().bounds.min.y;
            float playerTop = GetComponent<Collider2D>().bounds.max.y;
            float obstacleBottom = collision.collider.bounds.min.y;
            float obstacleTop = collision.collider.bounds.max.y;

            bool leavingTopOrBottom = (playerBottom > obstacleTop + 0.05f || playerTop < obstacleBottom - 0.05f);

            if (leavingTopOrBottom && isPaused)
            {
                Vector2 forwardDirection = Vector2.right;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, forwardDirection, forwardCheckDistance, obstacleLayer);

                if (hit.collider == null)
                {
                    UnpauseLevelElements();
                }
            }
        }
    }

    private IEnumerator HandleObstacleCollision()
    {
        isBouncing = true;
        PauseLevelElements();

        // Find all relevant moving objects and tell them to move right
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (!obj.CompareTag(playerTag))
            {
                if (obj.TryGetComponent<BackgroundScroller>(out var loop))
                {
                    loop.ScrollRightTemporarily(bounceDuration);
                }
                if (obj.TryGetComponent<MoveObject>(out var move))
                {
                    // No need to pass speed here, MoveObject will handle its own speed
                    move.MoveRightTemporarily(bounceDuration);
                }
                if (obj.TryGetComponent<ObjectSpawner>(out var spawner))
                {
                    spawner.AddBounceDelay(bounceDuration);
                }
            }
        }

        yield return new WaitForSeconds(bounceDuration);

        UnpauseLevelElements();
        isBouncing = false;
    }

    private void HandleAnimation()
    {
        // Primary condition for flying: not on ground AND moving significantly
        bool isMovingVertically = Mathf.Abs(rb.linearVelocity.y) > flyingVelocityThreshold;

        if (isOnGround)
        {
            currentlyFlying = false; // Definitely not flying if on the ground
            flyingTimer = 0f; // Reset timer
        }
        else // Player is in the air
        {
            if (isMovingVertically)
            {
                currentlyFlying = true; // Actively moving, so flying
                flyingTimer = flyingCoyoteTime; // Reset coyote timer
            }
            else // Not moving vertically, but in the air (at the peak)
            {
                // Decrement coyote time
                if (flyingTimer > 0)
                {
                    flyingTimer -= Time.fixedDeltaTime; // Use fixedDeltaTime since HandleAnimation is in FixedUpdate
                    currentlyFlying = true; // Keep flying true due to coyote time
                }
                else
                {
                    currentlyFlying = false; // Coyote time ran out, no significant vertical movement
                }
            }
        }

        animator.SetBool("isFlying", currentlyFlying);
        animator.SetBool("isOnGround", isOnGround);

        // Set the animation speed based on whether the player is flying
        if (currentlyFlying)
        {
            animator.SetFloat("FlappingSpeed", flappingAnimationSpeed);
        }
        else
        {
            // You might want to reset the speed to 1.0f when not flying,
            // or to a specific "idle" or "grounded" animation speed.
            // For now, let's assume default speed when not flying.
            animator.SetFloat("FlappingSpeed", 1.0f); // Or another appropriate default speed
        }
    }

    public void PauseLevelElements()
    {
        isPaused = true;
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (!obj.CompareTag(playerTag))
            {
                if (obj.TryGetComponent<BackgroundScroller>(out var loop)) loop.Pause = true;
                if (obj.TryGetComponent<ObjectSpawner>(out var spawner)) spawner.Pause = true;
                if (obj.TryGetComponent<MoveObject>(out var move)) move.Pause = true;
                if (obj.TryGetComponent<SelfDestruct>(out var selfDestruct)) selfDestruct.SetPaused(true);
            }
        }
    }

    public void UnpauseLevelElements()
    {
        isPaused = false;
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (!obj.CompareTag(playerTag))
            {
                if (obj.TryGetComponent<BackgroundScroller>(out var loop)) loop.Pause = false;
                if (obj.TryGetComponent<ObjectSpawner>(out var spawner)) spawner.Pause = false;
                if (obj.TryGetComponent<MoveObject>(out var move)) move.Pause = false;
                if (obj.TryGetComponent<SelfDestruct>(out var selfDestruct)) selfDestruct.SetPaused(false);
            }
        }
    }
}