using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Assign your Player's transform here
    public float smoothSpeed = 0.125f;
    public Vector3 offset; // Adjust the camera's X-offset from the player. Y and Z components of this offset will NOT be used for camera position.

    private float initialCameraY; // Store the camera's Y position from the scene
    private float initialCameraZ; // Store the camera's Z position from the scene (CRITICAL for 2D perspective)

    private FlyBehavior flyBehavior; // Reference to the player's FlyBehavior script

    void Start()
    {
        // Store the camera's initial Y and Z positions from where it was placed in the editor.
        // These values will be maintained for the camera's Y and Z coordinates during gameplay.
        initialCameraY = transform.position.y;
        initialCameraZ = transform.position.z;

        // Get the reference to the FlyBehavior instance (your player script).
        flyBehavior = FlyBehavior.instance;
        if (flyBehavior == null)
        {
            Debug.LogError("CameraFollow: FlyBehavior instance not found! CameraFollow requires it.");
            enabled = false; // Disable this component if we can't find the player's behavior.
            return;
        }

        // Optional: If you want an immediate snap to the initial offset position
        // without a smooth transition when the player first moves, you could
        // uncomment this, but typically smooth movement is desired.
        // transform.position = new Vector3(target.position.x + offset.x, initialCameraY, initialCameraZ);
    }

    void LateUpdate()
    {
        // Essential null checks to prevent errors if target or FlyBehavior are missing.
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: Target (Player) not assigned or found.");
            return;
        }
        if (flyBehavior == null)
        {
            Debug.LogError("CameraFollow: FlyBehavior reference is null in LateUpdate. Was it found in Start()?");
            return;
        }

        // IMPORTANT: The camera will only start following the player AFTER they have initiated movement.
        // Until 'flyBehavior.hasMoved' is true, the camera will remain exactly where you placed it in the editor.
        if (!flyBehavior.hasMoved)
        {
            return; // Exit LateUpdate, preventing any camera movement
        }

        // Calculate the desired position for the camera:
        // X: Follow the target's X position, adjusted by your 'offset.x'.
        // Y: Remain at the 'initialCameraY' (fixed Y-axis).
        // Z: Remain at the 'initialCameraZ' (fixed Z-axis, crucial for 2D visibility).
        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x, // Follow target's X, applying the X offset
            initialCameraY,               // Keep camera's Y fixed at its starting Y
            initialCameraZ                // Keep camera's Z fixed at its starting Z
        );

        // Smoothly move the camera's current position towards the calculated desired position.
        // This creates a smooth following effect rather than an instant jump.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Apply the newly calculated smoothed position to the camera's transform.
        transform.position = smoothedPosition;
    }
}