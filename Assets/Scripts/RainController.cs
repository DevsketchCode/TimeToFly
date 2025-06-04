using UnityEngine;

public class RainController : MonoBehaviour
{
    // --- Rain Angling Settings ---
    public ParticleSystem rainParticleSystem; // Assign your RainGenerator's ParticleSystem here
    public float maxRainAngle = 10f; // Max horizontal velocity when moving. Adjust as needed.
    public float defaultRainAngle = 0f; // Horizontal velocity when stopped (straight down)

    // --- Object Following Settings (NEW) ---
    [Header("Following Settings")]
    public Transform targetToFollow; // Assign your Player's transform here (same as CameraFollow's target)
    public float followSmoothSpeed = 0.125f; // How smoothly the RainGenerator follows
    public Vector3 followOffset; // Adjust the RainGenerator's X-offset from the player, relative to its initial Y/Z


    private FlyBehavior flyBehavior;
    private float initialRainGeneratorY; // Store the RainGenerator's initial Y position
    private float initialRainGeneratorZ; // Store the RainGenerator's initial Z position


    void Start()
    {
        // --- Initialize Particle System ---
        if (rainParticleSystem == null)
        {
            rainParticleSystem = GetComponent<ParticleSystem>();
            if (rainParticleSystem == null)
            {
                Debug.LogError("RainController: ParticleSystem not assigned and not found on this GameObject. Disabling.");
                enabled = false;
                return;
            }
        }

        // --- Initialize FlyBehavior Reference ---
        flyBehavior = FlyBehavior.instance;
        if (flyBehavior == null)
        {
            Debug.LogError("RainController: FlyBehavior instance not found! Rain will not angle or follow player movement.");
            // We'll still try to function but log the error
        }

        // --- Store Initial Position for Following ---
        // Store the RainGenerator's initial Y and Z positions from where it was placed in the editor.
        // These values will be maintained for the RainGenerator's Y and Z coordinates during gameplay.
        initialRainGeneratorY = transform.position.y;
        initialRainGeneratorZ = transform.position.z;


        // Set initial rain angle
        SetRainAngle(defaultRainAngle);
    }

    void LateUpdate() // Changed to LateUpdate for smoother following after all other updates
    {
        // --- Essential Null Checks ---
        if (targetToFollow == null)
        {
            Debug.LogWarning("RainController: Target (Player) to follow not assigned.");
            return;
        }
        if (rainParticleSystem == null)
        {
            // Already logged in Start, just return.
            return;
        }
        if (flyBehavior == null)
        {
            Debug.LogError("RainController: FlyBehavior reference is null in LateUpdate. Was it found in Start()?");
            // We might still want to follow even if flyBehavior is null, just not angle.
            // So, no return here, but check flyBehavior before using it for angling.
        }

        // --- Object Following Logic (Similar to CameraFollow) ---
        // Only start following once the player has initiated movement.
        // This keeps the RainGenerator stationary until the game starts.
        if (flyBehavior != null && !flyBehavior.hasMoved)
        {
            SetRainAngle(defaultRainAngle); // Rain straight down when stopped
            return; // Exit LateUpdate, preventing any movement or angling if player hasn't moved
        }

        // Calculate the desired position for the RainGenerator:
        // X: Follow the target's X position, adjusted by your 'followOffset.x'.
        // Y: Remain at the 'initialRainGeneratorY' (fixed Y-axis).
        // Z: Remain at the 'initialRainGeneratorZ' (fixed Z-axis).
        Vector3 desiredPosition = new Vector3(
            targetToFollow.position.x + followOffset.x, // Follow target's X, applying X offset
            initialRainGeneratorY,                       // Keep RainGenerator's Y fixed at its starting Y
            initialRainGeneratorZ                        // Keep RainGenerator's Z fixed at its starting Z
        );

        // Smoothly move the RainGenerator's current position towards the calculated desired position.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSmoothSpeed);

        // Apply the newly calculated smoothed position to the RainGenerator's transform.
        transform.position = smoothedPosition;


        // --- Rain Angle Adjustment Logic ---
        // This part now executes *after* the RainGenerator has moved to its new position.
        if (flyBehavior != null) // Only attempt to angle if FlyBehavior reference is valid
        {
            float currentHorizontalSpeed = flyBehavior.forwardSpeed; // Using the player's forward speed

            // Angle the rain if the player is moving forward
            float targetAngle = (currentHorizontalSpeed > 0) ? -maxRainAngle : defaultRainAngle; // Negative for backward angle when moving right

            SetRainAngle(targetAngle);
        }
        else
        {
            // If flyBehavior is null, set rain to default (straight down)
            SetRainAngle(defaultRainAngle);
        }
    }

    private void SetRainAngle(float angle)
    {
        if (rainParticleSystem == null) return;

        var velocityOverLifetime = rainParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = angle;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
    }
}