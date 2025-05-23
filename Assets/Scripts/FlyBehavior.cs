using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Required for Coroutines

public class FlyBehavior : MonoBehaviour
{
    // Make FlyBehavior a Singleton for easy access from other scripts if needed
    public static FlyBehavior instance; // NEW: Singleton instance

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

    [Header("Player Settings")]
    public string playerTag = "Player";
    public InputActionReference jumpActionReference;
    public bool currentlyFlying = false; // Track if the player is flying
    public bool isOnGround = false;
    public bool hasMoved = false;

    // --- Flapping Sound Settings (These can remain, still useful for animation speed) ---
    [Header("Audio Settings")]
    [Range(0.5f, 2.0f)] // Pitch usually goes from 0.5 to 2.0 (half speed to double speed)
    [SerializeField] private float flappingSoundPitch = 1.0f; // Control the "speed" of the flapping sound

    [Header("Level Settings")]
    [SerializeField]
    private LayerMask obstacleLayer; // Assign the layer of your Obstacle objects in the Inspector
    public bool isPaused = false;

    [Header("Weather Manager Integration")] // New Header for clarity
    public bool hasReachedSafeZone = false; // NEW: Set to true when player reaches the safe zone.

    private InputAction jumpAction;
    private Rigidbody2D rb;
    private Animator animator;
    private Audio_Player playerAudio; // Reference to the PlayerAudio script (now Audio_Player)

    private bool isCollidingWithObstacle = false; // Track if currently colliding with a non-ground obstacle
    private bool isBouncing = false; // Flag to indicate if the bounce coroutine is running
    private float flyingTimer; // Timer for the coyote time

    private LevelManager levelManager;

    // NEW: Singleton Awake method
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

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

        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found in the scene!");
            enabled = false;
        }

        flyingTimer = 0f;

        playerAudio = GetComponent<Audio_Player>();
        if (playerAudio == null)
        {
            Debug.LogWarning("Audio_Player component not found on player object!");
        }

        // --- IMPORTANT: Initial state for clouds ---
        // When the game starts, your level elements are typically unpaused.
        // Therefore, the clouds should start at the faster speed.
        if (CloudSpawner.instance != null)
        {
            CloudSpawner.instance.SetCloudSpeedBoost(true);
        }
        // --- End Initial State ---

        // NEW: Inform WeatherManager that player is NOT in the safe zone at game start
        if (WeatherManager.instance != null)
        {
            WeatherManager.instance.SetPlayerSafeZoneStatus(false);
        }
        else
        {
            Debug.LogWarning("WeatherManager instance not found. Cannot set initial safe zone status.");
        }
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
            currentlyFlying = true;
            flyingTimer = flyingCoyoteTime;

            if (playerAudio != null)
            {
                playerAudio.PlayFlappingSound(flappingSoundPitch);
            }
        }
        else if (isPaused && jumpAction.WasPerformedThisFrame())
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * pausedJumpForce, ForceMode2D.Impulse);
            if (!hasMoved)
            {
                animator.SetBool("isShocked", true);
                currentlyFlying = true;
                flyingTimer = flyingCoyoteTime;
                hasMoved = true;
                UIManager.instance.StartTimer();

                if (playerAudio != null)
                {
                    playerAudio.PlayFlappingSound(flappingSoundPitch);
                }
            }
            else
            {
                if (playerAudio != null)
                {
                    playerAudio.PlayFlappingSound(flappingSoundPitch);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, rb.linearVelocity.y * rotationSpeed);
        HandleAnimation();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger entered by: " + other.gameObject.name + " Tag: " + other.gameObject.tag);
        if (other.gameObject.CompareTag("DangerousObstacle"))
        {
            isCollidingWithObstacle = true;
            PauseLevelElements(); // This calls the pause logic

            if (playerAudio != null)
            {
                playerAudio.PlayCollisionSound();
            }

            animator.SetBool("isBurnt", true);
            // Removed redundant HandleAnimation() call here, it's called in FixedUpdate
            GameManager.instance.GameOver();
        }
        else if (other.gameObject.CompareTag("Obstacle") || other.gameObject.CompareTag("ObstacleNoPause"))
        {
            if (other.gameObject.name == "Ground")
            {
                isOnGround = true;
                currentlyFlying = false;
                flyingTimer = 0f;

                if (!isBouncing)
                {
                    PauseLevelElements(); // This calls the pause logic
                }
                return;
            }

            if (other.gameObject.layer == LayerMask.NameToLayer("Obstacles") && other.gameObject.CompareTag("LeftBounceCollider"))
            {
                Debug.Log("Bouncing off LeftBounceCollider (Trigger): Tag:" + other.gameObject.tag);
                isCollidingWithObstacle = true;
                StartCoroutine(HandleObstacleCollision()); // This handles pause/unpause internally
                return;
            }

            if (!other.gameObject.CompareTag("ObstacleNoPause"))
            {
                isCollidingWithObstacle = true;
                PauseLevelElements(); // This calls the pause logic
            }
        }
        else if (other.gameObject.CompareTag("Proceed"))
        {
            Debug.Log("Proceeding Forward (Trigger)");
            UnpauseLevelElements(); // This calls the unpause logic
        }
        else if (other.gameObject.CompareTag("SafeZone"))
        {
            hasReachedSafeZone = true;
            PauseLevelElements(); // This calls the pause logic
            Debug.Log("Player reached safe zone!");

            // NEW: Inform WeatherManager that the player IS in the safe zone
            if (WeatherManager.instance != null)
            {
                WeatherManager.instance.SetPlayerSafeZoneStatus(true);
            }
            else
            {
                Debug.LogWarning("WeatherManager instance not found. Cannot inform it about safe zone status.");
            }
        }
        else if (other.gameObject.CompareTag("SelfDestruct"))
        {
            isCollidingWithObstacle = true;
            PauseLevelElements(); // This calls the pause logic
            animator.SetBool("isBurnt", true);
            // Removed redundant HandleAnimation() call here
            GameManager.instance.GameOver();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision with: " + collision.gameObject.name + " Tag: " + collision.gameObject.tag);

        if (collision.gameObject.CompareTag("DangerousObstacle"))
        {
            isCollidingWithObstacle = true;
            PauseLevelElements(); // This calls the pause logic

            if (playerAudio != null)
            {
                playerAudio.PlayCollisionSound();
            }

            animator.SetBool("isBurnt", true);
            // Removed redundant HandleAnimation() call here
            GameManager.instance.GameOver();
        }
        else if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("ObstacleNoPause"))
        {
            if (collision.gameObject.name == "Ground")
            {
                isOnGround = true;
                currentlyFlying = false;
                flyingTimer = 0f;

                if (!isBouncing)
                {
                    PauseLevelElements(); // This calls the pause logic
                }
                return;
            }
            if (!collision.gameObject.CompareTag("LeftBounceCollider"))
            {
                isCollidingWithObstacle = true;
                PauseLevelElements(); // This calls the pause logic
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
                currentlyFlying = true; // Player is now in air (jumping/falling)
                flyingTimer = flyingCoyoteTime;

                if (isPaused && !isBouncing && !isCollidingWithObstacle)
                {
                    UnpauseLevelElements(); // This calls the unpause logic
                }
                return;
            }
            isCollidingWithObstacle = false;
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
                    UnpauseLevelElements(); // This calls the unpause logic
                }
            }
        }
    }

    private IEnumerator HandleObstacleCollision()
    {
        isBouncing = true;
        PauseLevelElements(); // This calls the pause logic and will also set clouds to normal speed

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
                    move.MoveRightTemporarily(bounceDuration);
                }
                if (obj.TryGetComponent<ObjectSpawner>(out var spawner))
                {
                    spawner.AddBounceDelay(bounceDuration);
                }
            }
        }

        yield return new WaitForSeconds(bounceDuration);

        UnpauseLevelElements(); // This calls the unpause logic and will set clouds to faster speed
        isBouncing = false;
    }

    private void HandleAnimation()
    {
        // Add null check for animator
        if (animator == null) return;

        // NEW: If "isBurnt" is true, stop all other animations
        if (animator.GetBool("isBurnt"))
        {
            animator.SetBool("isFlying", false);
            animator.SetBool("isOnGround", false);
            animator.SetBool("isShocked", false); // Ensure shocked is false if burnt
            animator.SetFloat("FlappingSpeed", 1.0f); // Set to default or 0 if preferred
            return; // Don't process other animation states if burnt
        }

        bool isMovingVertically = Mathf.Abs(rb.linearVelocity.y) > flyingVelocityThreshold;

        // If 'isShocked' is true, set general animation booleans to false
        // and let the animator controller handle the 'isShocked' animation itself.
        if (!animator.GetBool("isShocked"))
        {
            animator.SetBool("isFlying", false);
            animator.SetBool("isOnGround", false);
            // You might want to set flapping speed to a neutral or 0 if shocked anim doesn't flap.
            // For now, let's just make sure it's not overriding current anim.
            animator.SetFloat("FlappingSpeed", 1.0f);
        }
        else // Only update flying/ground states if NOT shocked
        {
            if (isOnGround)
            {
                currentlyFlying = false;
                flyingTimer = 0f;
            }
            else // Player is in the air
            {
                if (isMovingVertically)
                {
                    currentlyFlying = true;
                    flyingTimer = flyingCoyoteTime;
                }
                else // Not moving vertically, but in the air (at the peak)
                {
                    if (flyingTimer > 0)
                    {
                        flyingTimer -= Time.fixedDeltaTime;
                        currentlyFlying = true;
                    }
                    else
                    {
                        currentlyFlying = false;
                    }
                }
            }

            animator.SetBool("isFlying", currentlyFlying);
            animator.SetBool("isOnGround", isOnGround);

            // Flapping speed is usually tied to flying
            if (currentlyFlying)
            {
                animator.SetFloat("FlappingSpeed", flappingAnimationSpeed);
            }
            else
            {
                animator.SetFloat("FlappingSpeed", 1.0f);
            }
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
        // --- ADDED: Set Cloud speed to normal when other elements are paused ---
        if (CloudSpawner.instance != null)
        {
            CloudSpawner.instance.SetCloudSpeedBoost(false); // Set clouds to normal speed
        }
        // ------------------------------------------------------------------
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
        // --- ADDED: Set Cloud speed to boosted when other elements are unpaused ---
        if (CloudSpawner.instance != null)
        {
            CloudSpawner.instance.SetCloudSpeedBoost(true); // Set clouds to faster speed
        }
        // -------------------------------------------------------------------
    }
}