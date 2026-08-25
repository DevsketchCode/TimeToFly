using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro; // Required for Coroutines (still needed for HandleObstacleCollision)

public class FlyBehavior : MonoBehaviour
{
    // Make FlyBehavior a Singleton for easy access from other scripts if needed
    public static FlyBehavior instance; // NEW: Singleton instance

    [Header("Movement Settings")]
    [SerializeField]
    private float jumpForce = 5f;

    [SerializeField]
    public float forwardSpeed = 0.1f; // NEW: Player's forward movement speed

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
    public bool isPaused = false; // This flag indicates a general game pause affecting player input/logic

    [Header("Weather Manager Integration")] // New Header for clarity
    public bool hasReachedSafeZone = false; // NEW: Set to true when player reaches the safe zone.

    [Header("Game Over Slow Motion")] // New Header for clarity
    [SerializeField]
    private float slowMotionTimeScale = 0.2f; // How much to slow down (e.g., 0.2f for 5x slower)

    // NEW: Add a flag to control player input
    public bool canReceiveInput = true; // Control if player can input

    private InputAction jumpAction;
    private Rigidbody2D rb;
    private Animator animator;
    private Audio_Player playerAudio; // Reference to the PlayerAudio script (now Audio_Player)

    private bool isCollidingWithObstacle = false; // Track if currently colliding with a non-ground obstacle
    private bool isBouncing = false; // Flag to indicate if the bounce coroutine is running
    private float flyingTimer; // Timer for the coyote time

    private LevelManager levelManager;
    private ObjectSpawner objectSpawner; // NEW: Reference to ObjectSpawner

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

        objectSpawner = FindObjectOfType<ObjectSpawner>(); // NEW: Get reference to ObjectSpawner
        if (objectSpawner == null)
        {
            Debug.LogWarning("ObjectSpawner not found in the scene! Player won't move spawner.");
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
            CloudSpawner.instance.Pause = false; // Ensure spawner is NOT paused at start
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
        // Only allow input if canReceiveInput is true AND the game is NOT in a full game over/win state (checked by GameManager)
        // If GameManager.instance.IsGameOver() returns true, this player input will be ignored.
        if (!isPaused && canReceiveInput && jumpAction.WasPerformedThisFrame())
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            currentlyFlying = true;
            flyingTimer = flyingCoyoteTime;

            if (playerAudio != null)
            {
                playerAudio.PlayFlappingSound(flappingSoundPitch);
            }
        }
        else if (isPaused && canReceiveInput && jumpAction.WasPerformedThisFrame())
        {
            // This block is for when the player is paused (e.g., hit a non-dangerous obstacle)
            // but still has input enabled (like in the "isShocked" state).
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
        // Apply rotation based on vertical velocity
        transform.rotation = Quaternion.Euler(0, 0, rb.linearVelocity.y * rotationSpeed);

        // Handle player animation states
        HandleAnimation();

        // Player's continuous forward movement logic
        // This block only executes if the game is not paused, input is enabled,
        // GameManager instance exists, and the game is not in a Game Over state.
        if (!isPaused && canReceiveInput && GameManager.instance != null && !GameManager.instance.IsGameOver())
        {
            // If the player hasn't "moved" yet, mark them as having moved now.
            // This flag signals the start of the game's actual progression.
            if (!hasMoved)
            {
                hasMoved = true;
                // Optionally start the UI timer here, aligning it with the player's first forward movement.
                if (UIManager.instance != null)
                {
                    UIManager.instance.StartTimer();
                }
            }

            // Move the player character continuously forward
            transform.Translate(Vector2.right * forwardSpeed * Time.fixedDeltaTime);

            // Synchronize the ObjectSpawner's X position with the player's forward movement.
            // This creates the effect of the level "scrolling" with the player.
            if (objectSpawner != null)
            {
                objectSpawner.transform.position = new Vector3(
                    objectSpawner.transform.position.x + (forwardSpeed * Time.fixedDeltaTime),
                    objectSpawner.transform.position.y, // Keep Y position as is
                    objectSpawner.transform.position.z
                );
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        Debug.Log("Trigger entered by: " + collider.gameObject.name + " Tag: " + collider.gameObject.tag);
        // Ensure that "DangerousObstacle" and "SelfDestruct" (which seems to be an instant kill)
        // trigger the immediate stop and Game Over.
        if (collider.gameObject.CompareTag("DangerousObstacle") || collider.gameObject.CompareTag("SelfDestruct"))
        {
            HandleDeathCondition(collider.gameObject); // Call the dedicated death handler
        }
        else if (collider.gameObject.CompareTag("Obstacle") || collider.gameObject.CompareTag("ObstacleNoPause"))
        {
            if (collider.gameObject.name == "Ground")
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

            if (collider.gameObject.layer == LayerMask.NameToLayer("Obstacles") && collider.gameObject.CompareTag("LeftBounceCollider"))
            {
                Debug.Log("Bouncing off LeftBounceCollider (Trigger): Tag:" + collider.gameObject.tag);
                isCollidingWithObstacle = true;
                StartCoroutine(HandleObstacleCollision()); // This handles pause/unpause internally
                return;
            }

            if (!collider.gameObject.CompareTag("ObstacleNoPause"))
            {
                isCollidingWithObstacle = true;
                PauseLevelElements(); // This calls the pause logic
            }
        }
        else if (collider.gameObject.CompareTag("Proceed"))
        {
            Debug.Log("Proceeding Forward (Trigger)");
            UnpauseLevelElements(); // This calls the unpause logic
        }
        else if (collider.gameObject.CompareTag("SafeZone"))
        {
            hasReachedSafeZone = true;

            // Immediately disable player input and pause level elements
            DisablePlayerInput();
            PauseLevelElements();
            rb.linearVelocity = Vector2.zero; // Stop player movement immediately on win

            Debug.Log("Player reached safe zone!");

            if (UIManager.instance != null)
            {
                UIManager.instance.StopTimer(); // Call StopTimer on game over
            }
            else
            {
                Debug.LogWarning("UIManager instance not found! Cannot stop timer on game over.");
            }

            if (WeatherManager.instance != null)
            {
                WeatherManager.instance.SetPlayerSafeZoneStatus(true);
            }
            else
            {
                Debug.LogWarning("WeatherManager instance not found. Cannot inform it about safe zone status.");
            }

            if (GameManager.instance != null)
            {
                GameManager.instance.WinGame();
            }
            else
            {
                Debug.LogError("GameManager instance not found! Cannot call Win Game.");
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collider)
    {
        Debug.Log("Collision with: " + collider.gameObject.name + " Tag: " + collider.gameObject.tag);

        if (collider.gameObject.CompareTag("DangerousObstacle"))
        {
            HandleDeathCondition(collider.gameObject); // Call the dedicated death handler
        }
        else if (collider.gameObject.CompareTag("Obstacle") || collider.gameObject.CompareTag("ObstacleNoPause"))
        {
            if (collider.gameObject.name == "Ground")
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
            if (!collider.gameObject.CompareTag("LeftBounceCollider"))
            {
                isCollidingWithObstacle = true;
                PauseLevelElements(); // This calls the pause logic
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collider)
    {
        if (collider.gameObject.CompareTag("Obstacle"))
        {
            if (collider.gameObject.name == "Ground")
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
            float obstacleBottom = collider.collider.bounds.min.y;
            float obstacleTop = collider.collider.bounds.max.y;

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

    // New helper method to centralize death handling logic
    private void HandleDeathCondition(GameObject obstacleObject)
    {
        // Only proceed if the game isn't already in a game over state
        // This prevents multiple Game Over calls if multiple hazardous objects are hit rapidly
        if (GameManager.instance != null && GameManager.instance.IsGameOver())
        {
            return;
        }

        DisablePlayerInput(); // Stop player input
        PauseLevelElements(); // Pause all level elements (BackgroundScroller, MoveObject, ObjectSpawner)
        rb.linearVelocity = Vector2.zero; // Immediately stop player's Rigidbody movement

        isCollidingWithObstacle = true; // Mark as colliding with obstacle

        if (UIManager.instance != null)
        {
            UIManager.instance.StopTimer(); // Call StopTimer on game over
            UIManager.instance.hasStopped = true; // Ensure the timer stops updating
            Debug.Log("Stopping timer on game over.");
        }
        else
        {
            Debug.LogWarning("UIManager instance not found! Cannot stop timer on game over.");
        }

        // ** Activate Slow Motion **
        Time.timeScale = slowMotionTimeScale;
        Debug.Log("Player hit Dangerous Obstacle! Initiating slow motion. Time.timeScale = " + Time.timeScale);


        if (playerAudio != null)
        {
            playerAudio.PlayCollisionSound();
        }
        animator.SetBool("isFlying", false); // Ensure flying animation is off

        if (obstacleObject.GetComponent<DangerousObstacle>().GetObstacleType() == DangerousObstacle.DangersousObstacleType.Electrical || obstacleObject.GetComponent<DangerousObstacle>().GetObstacleType() == DangerousObstacle.DangersousObstacleType.Lightning)
        {
            animator.SetBool("isElectricuted", true); // Trigger shock animation
        }
        else
        {
            animator.SetBool("hitDangerousObstacle", true); // Trigger death animation
        }

        Debug.Log("Start custom end screen message");
        if (GameManager.instance != null)
        {
            GameManager.instance.GameOver(); // Call Game Over (GameManager handles the delay for the screen)
            Debug.Log("Start custom end screen message: GameManager found.");

            if (obstacleObject != null)
            {
                Debug.Log("Start custom end screen message: Obstacle found. " + obstacleObject.name);
                GameManager.instance.GameOverReasonCanvas.GetComponentInChildren<TextMeshProUGUI>().SetText(obstacleObject.GetComponent<DangerousObstacle>().GetGameOverReason()); // Set the reason text from the obstacle
                Debug.Log("obstacle: " + obstacleObject.name + "obstacleObject.GetComponent<DangerousObstacle>().GetGameOverReason() = " + obstacleObject.GetComponent<DangerousObstacle>().GetGameOverReason());
            }
            else
            {
                GameManager.instance.GameOverReasonCanvas.GetComponentInChildren<TextMeshProUGUI>().SetText("You didn't make it!"); // Default reason text
            }
        }
        else
        {
            Debug.LogError("GameManager instance not found! Cannot call Game Over.");
        }
    }

    private IEnumerator HandleObstacleCollision()
    {
        isBouncing = true;
        PauseLevelElements(); // This calls the pause logic and will also set clouds to normal speed

        // For this new movement style, the objects themselves won't move right temporarily.
        // Instead, the player and spawner pause their forward movement.
        // So, the 'ScrollRightTemporarily' and 'MoveRightTemporarily' calls are no longer relevant
        // if objects are stationary and the camera follows the player.
        // We only need the pause effect here.

        yield return new WaitForSeconds(bounceDuration);

        UnpauseLevelElements(); // This calls the unpause logic and will set clouds to faster speed
        isBouncing = false;
    }

    private void HandleAnimation()
    {
        // Add null check for animator
        if (animator == null) return;

        // NEW: If hit a dangerous object, stop all other animations
        if (animator.GetBool("isBurnt") || animator.GetBool("isElectricuted") || animator.GetBool("hitDangerousObstacle"))
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
        // Use FindObjectsOfType to get all relevant objects and pause them.
        // This is a more robust way to ensure everything stops.

        // Pause BackgroundScrollers
        BackgroundScroller[] backgroundScrollers = FindObjectsOfType<BackgroundScroller>();
        foreach (BackgroundScroller bs in backgroundScrollers)
        {
            bs.Pause = true;
        }

        // Pause ObjectSpawners
        ObjectSpawner[] objectSpawners = FindObjectsOfType<ObjectSpawner>();
        foreach (ObjectSpawner os in objectSpawners)
        {
            os.Pause = true;
        }

        // MoveObjects (obstacles, etc.) should not be paused if they are now stationary.
        // If they *were* moving before, they should simply stop moving.
        // We remove the iteration over MoveObjects because they are now stationary.

        // Pause SelfDestruct components (if they should stop their countdowns)
        SelfDestruct[] selfDestructs = FindObjectsOfType<SelfDestruct>();
        foreach (SelfDestruct sd in selfDestructs)
        {
            sd.SetPaused(true);
        }

        // --- Set Cloud speed to normal when other elements are paused ---
        if (CloudSpawner.instance != null)
        {
            CloudSpawner.instance.SetCloudSpeedBoost(false); // Set clouds to normal speed
            CloudSpawner.instance.Pause = true; // NEW: Pause CloudSpawner
        }
    }

    public void UnpauseLevelElements()
    {
        isPaused = false;
        // Unpause BackgroundScrollers
        BackgroundScroller[] backgroundScrollers = FindObjectsOfType<BackgroundScroller>();
        foreach (BackgroundScroller bs in backgroundScrollers)
        {
            bs.Pause = false;
        }

        // Unpause ObjectSpawners
        ObjectSpawner[] objectSpawners = FindObjectsOfType<ObjectSpawner>();
        foreach (ObjectSpawner os in objectSpawners)
        {
            os.Pause = false;
        }

        // MoveObjects (obstacles, etc.) should not be unpaused if they are now stationary.
        // We remove the iteration over MoveObjects here.

        // Unpause SelfDestruct components
        SelfDestruct[] selfDestructs = FindObjectsOfType<SelfDestruct>();
        foreach (SelfDestruct sd in selfDestructs)
        {
            sd.SetPaused(false);
        }

        // --- Set Cloud speed to boosted when other elements are unpaused ---
        if (CloudSpawner.instance != null)
        {
            CloudSpawner.instance.SetCloudSpeedBoost(true); // Set clouds to faster speed
            CloudSpawner.instance.Pause = false; // NEW: Unpause CloudSpawner
        }
    }

    /// <summary>
    /// Disables player input by setting canReceiveInput to false.
    /// </summary>
    public void DisablePlayerInput()
    {
        canReceiveInput = false;
        // It's generally good practice to also disable the input action itself
        jumpAction.Disable();
    }

    /// <summary>
    /// Enables player input by setting canReceiveInput to true.
    /// </summary>
    public void EnablePlayerInput()
    {
        canReceiveInput = true;
        jumpAction.Enable();
    }
}