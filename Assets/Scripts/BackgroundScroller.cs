using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] public bool Pause = false; // Control if the background width should stop growing

    // Use [HideInInspector] to prevent this field from showing in the Inspector for BackgroundScroller components.
    // It remains 'public' so LevelManager can access and set its value.
    [HideInInspector]
    public float growthSpeedMultiplier = 1.0f; // Default value, will be overridden by LevelManager

    private SpriteRenderer backgroundSpriteRenderer; // Reference to the SpriteRenderer
    private BoxCollider2D boxCollider;               // NEW: Reference to the BoxCollider2D
    private FlyBehavior flyBehavior;                 // Reference to the player's FlyBehavior for movement status and position
    private float initialWidth;                      // The initial width of the background.
    private float playerInitialXPosition;            // The player's X position when the game starts.

    private void Start()
    {
        backgroundSpriteRenderer = GetComponent<SpriteRenderer>();

        if (backgroundSpriteRenderer == null)
        {
            Debug.LogError("BackgroundScroller requires a SpriteRenderer component on this GameObject.");
            enabled = false;
            return;
        }

        // Get the BoxCollider2D component if it exists on this GameObject
        boxCollider = GetComponent<BoxCollider2D>();
        // No error is needed if it's null, as not all backgrounds will have a collider.


        // Ensure the SpriteRenderer's Draw Mode is Tiled for width growth to work visually.
        if (backgroundSpriteRenderer.drawMode != SpriteDrawMode.Tiled)
        {
            Debug.LogWarning("BackgroundScroller: SpriteRenderer Draw Mode is not set to Tiled. Setting to Tiled.");
            backgroundSpriteRenderer.drawMode = SpriteDrawMode.Tiled;
        }

        initialWidth = backgroundSpriteRenderer.size.x;

        // Get player's FlyBehavior instance. This is more efficient than FindGameObjectWithTag in Update.
        flyBehavior = FlyBehavior.instance; // Assuming FlyBehavior is a Singleton
        if (flyBehavior != null)
        {
            // Store the player's starting X position.
            // This is crucial for calculating how far the player has moved.
            playerInitialXPosition = flyBehavior.transform.position.x;
        }
        else
        {
            Debug.LogError("FlyBehavior instance not found! Background growth will not work correctly without player reference.");
            enabled = false; // Disable this component if player is not found.
            return;
        }
    }

    private void Update()
    {
        // If paused, or no player, or player hasn't started moving, do nothing.
        if (Pause || flyBehavior == null || !flyBehavior.hasMoved)
        {
            return;
        }

        // Calculate how far the player has moved horizontally since the start.
        // This is the player's current X minus their starting X.
        float playerTravelDistanceX = flyBehavior.transform.position.x - playerInitialXPosition;

        // Calculate the new desired width for the background.
        // It's the initial width plus the distance the player has traveled, scaled by the growth multiplier.
        float newWidth = initialWidth + (playerTravelDistanceX * growthSpeedMultiplier);

        // Ensure the background never shrinks below its initial size.
        newWidth = Mathf.Max(initialWidth, newWidth);

        // Calculate how much the width has actually changed from its initial size.
        float widthChange = newWidth - initialWidth;

        // Apply the new calculated width to the SpriteRenderer.
        backgroundSpriteRenderer.size = new Vector2(newWidth, backgroundSpriteRenderer.size.y);

        // NEW: If a BoxCollider2D exists, update its size and offset
        if (boxCollider != null)
        {
            // Set the collider's new width to match the sprite's new width
            boxCollider.size = new Vector2(newWidth, boxCollider.size.y);

            // When the sprite grows from the Left Center pivot, its visual center shifts right.
            // The BoxCollider2D's offset defines its center relative to the GameObject's pivot.
            // So, to keep the collider aligned with the growing sprite, its center (offset.x)
            // needs to be half of its new width.
            boxCollider.offset = new Vector2(newWidth / 2f, boxCollider.offset.y);
        }
    }

    // This method can be called by other scripts (like FlyBehavior's PauseLevelElements)
    public void PauseScrolling(bool value)
    {
        Pause = value;
    }
}