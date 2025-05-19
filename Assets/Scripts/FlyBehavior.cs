using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Required for Coroutines

public class FlyBehavior : MonoBehaviour
{
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

    [SerializeField]
    private LayerMask obstacleLayer; // Assign the layer of your Obstacle objects in the Inspector

    private Rigidbody2D rb;

    public string playerTag = "Player";
    public InputActionReference jumpActionReference;
    private InputAction jumpAction;
    private bool hasMoved = false;
    public bool isPaused = false;
    public bool isGrounded = false;
    private bool isCollidingWithObstacle = false; // Track if currently colliding with a non-ground obstacle
    private bool isBouncing = false; // Flag to indicate if the bounce coroutine is running
    private LevelManager levelManager;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpAction = jumpActionReference.action;
        jumpAction.Enable();

        // Find the LevelManager instance
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found in the scene!");
            enabled = false;
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
            if (!hasMoved)
            {
                hasMoved = true;
                UIManager.instance.StartTimer(); // Call StartTimer when first jump occurs
            }
        }
        else if (isPaused && jumpAction.WasPerformedThisFrame())
        {
            rb.linearVelocity = Vector2.zero; // Directly set velocity to zero
            rb.AddForce(Vector2.up * pausedJumpForce, ForceMode2D.Impulse);
        }
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, rb.linearVelocity.y * rotationSpeed);
    }

    private void OnTriggerEnter2D(Collider2D other) // Changed from OnCollisionEnter2D to OnTriggerEnter2D and parameter type
    {
        Debug.Log("Trigger entered by: " + other.gameObject.name + " Tag: " + other.gameObject.tag);
        if (other.gameObject.CompareTag("DangerousObstacle"))
        {
            PauseLevelElements();
            GameManager.instance.GameOver();
        }
        else if (other.gameObject.CompareTag("Obstacle") || other.gameObject.CompareTag("ObstacleNoPause"))
        {
            if (other.gameObject.name == "Ground")
            {
                isGrounded = true;
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
                StartCoroutine(HandleObstacleCollision());
                return;
            }

            if (!other.gameObject.CompareTag("ObstacleNoPause"))
            {
                // Consider if non-bounce obstacle triggers should pause.
                // If so, keep this. If not, remove it.
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
            PauseLevelElements();
            GameManager.instance.GameOver();
        }
        else if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("ObstacleNoPause"))
        {
            if (collision.gameObject.name == "Ground")
            {
                isGrounded = true;
                if (!isBouncing)
                {
                    PauseLevelElements();
                }
                return;
            }
            if (!collision.gameObject.CompareTag("LeftBounceCollider")) // Only pause if it's a regular obstacle, not a bounce trigger
            {
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
                isGrounded = false;
                // Only unpause if the game was paused due to ground contact and not bouncing
                if (isPaused && !isBouncing)
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