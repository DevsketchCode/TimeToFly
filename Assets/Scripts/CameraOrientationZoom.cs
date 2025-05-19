using UnityEngine;

public class CameraOrientationZoom : MonoBehaviour
{
    [Tooltip("Assign your main camera here. If left empty, the script will try to find Camera.main")]
    public Camera mainCamera;

    [Tooltip("The orthographic size for the camera when the device is in Landscape orientation.")]
    public float landscapeOrthographicSize = 1.17f; // Set your desired landscape size here

    [Tooltip("The orthographic size for the camera when the device is in Portrait orientation.")]
    public float portraitOrthographicSize = 2.0f; // Set your desired portrait zoom-out size here (should be > landscape size)

    // Enum for editor-only simulation mode
    public enum SimulatedOrientationMode
    {
        UseDeviceOrientation, // Use actual Screen.orientation (works in builds)
        ForcePortrait,        // Force portrait in editor for testing
        ForceLandscape        // Force landscape in editor for testing
    }

    [Tooltip("Editor Only: Controls how orientation is determined for testing in the editor.")]
    public SimulatedOrientationMode simulatedOrientationMode = SimulatedOrientationMode.UseDeviceOrientation;

    private ScreenOrientation lastAppliedOrientation; // Renamed for clarity

    void Awake()
    {
        // If no camera is assigned in the inspector, try to find the main camera by tag
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Check if a camera was found or assigned
        if (mainCamera == null)
        {
            Debug.LogError("CameraOrientationZoom: No main camera found! Please assign a camera in the inspector or ensure your camera is tagged 'MainCamera'.");
            enabled = false; // Disable the script if no camera is found to prevent errors
            return;
        }

        // Set the initial orientation based on editor simulation or actual device
        ScreenOrientation initialOrientation = GetCurrentEffectiveOrientation();
        lastAppliedOrientation = initialOrientation;
        Debug.Log("CameraOrientationZoom: Initializing. Effective orientation: " + initialOrientation);
        ApplyZoomBasedOnOrientation(initialOrientation);
    }

    void Update()
    {
        // Get the current effective orientation (simulated in editor, real in build)
        ScreenOrientation currentEffectiveOrientation = GetCurrentEffectiveOrientation();

        // Check if the effective orientation has changed since we last applied zoom
        if (currentEffectiveOrientation != lastAppliedOrientation)
        {
            lastAppliedOrientation = currentEffectiveOrientation;
            Debug.Log("CameraOrientationZoom: Effective orientation changed to: " + currentEffectiveOrientation);
            ApplyZoomBasedOnOrientation(currentEffectiveOrientation);
        }
    }

    // Determines the current orientation based on editor settings or device
    private ScreenOrientation GetCurrentEffectiveOrientation()
    {
#if UNITY_EDITOR
        // In the editor, use the simulated mode
        switch (simulatedOrientationMode)
        {
            case SimulatedOrientationMode.ForcePortrait:
                return ScreenOrientation.Portrait;
            case SimulatedOrientationMode.ForceLandscape:
                return ScreenOrientation.LandscapeLeft; // Using LandscapeLeft as a standard landscape value
            case SimulatedOrientationMode.UseDeviceOrientation:
                // In editor, Screen.orientation might not update correctly,
                // but we'll still read it if this mode is selected.
                return Screen.orientation;
        }
        // Fallback in editor
        return ScreenOrientation.Unknown;

#else
        // In a build, always use the actual device orientation
        return Screen.orientation;
#endif
    }


    void ApplyZoomBasedOnOrientation(ScreenOrientation orientation)
    {
        // Apply the appropriate orthographic size based on the given orientation
        if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
        {
            // Zoom out for portrait mode (larger orthographic size)
            mainCamera.orthographicSize = portraitOrthographicSize;
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, -0.33f, -10f); // Shift camera y position up
            Debug.Log("CameraOrientationZoom: Applying Portrait Zoom. Setting orthographicSize to: " + mainCamera.orthographicSize);
        }
        else if (orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight)
        {
            // Set to landscape zoom (original or desired landscape size)
            mainCamera.orthographicSize = landscapeOrthographicSize;
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, 0f, -10f); // Reset camera y position to zero
            Debug.Log("CameraOrientationZoom: Applying Landscape Zoom. Setting orthographicSize to: " + mainCamera.orthographicSize);
        }
        // Handle other potential orientations if necessary
        else
        {
            // This might catch ScreenOrientation.Unknown during transitions or other states
            Debug.Log("CameraOrientationZoom: Effective orientation is " + orientation + ". Maintaining current zoom.");
        }
    }

    // Optional: Call this public method from other scripts if you need to manually trigger
    // an orientation check and zoom update at a specific time (e.g., after loading a scene)
    public void ForceOrientationCheckAndZoomUpdate()
    {
        Debug.Log("CameraOrientationZoom: Forcing orientation check and zoom update.");
        ApplyZoomBasedOnOrientation(GetCurrentEffectiveOrientation());
    }
}