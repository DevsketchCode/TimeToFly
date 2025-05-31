using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal; // Still useful if you might grab Light2D components in the future

public class FlickerLight : MonoBehaviour
{
    [Tooltip("The GameObject whose active state will be flickered. Assign this in the Inspector.")]
    [SerializeField]
    private GameObject targetGameObjectToFlicker;

    public enum FlickerType
    {
        Regular,    // Simple on/off toggle
        Electrical  // More erratic, quick bursts of flickering
    }

    [Tooltip("Choose the type of flickering behavior.")]
    [SerializeField]
    private FlickerType flickerMode = FlickerType.Regular;

    [Tooltip("How fast the light flickers in Regular mode. A lower value means faster flickering.")]
    [SerializeField]
    [Range(0.01f, 1.0f)] // Provides a slider in the Inspector for easy adjustment
    private float regularFlickerSpeed = 0.1f; // Default flicker speed for regular mode

    [Header("Electrical Flicker Settings")]
    [Tooltip("Minimum duration for an electrical flicker burst.")]
    [SerializeField]
    private float minElectricalBurstDuration = 0.1f;
    [Tooltip("Maximum duration for an electrical flicker burst.")]
    [SerializeField]
    private float maxElectricalBurstDuration = 0.3f;
    [Tooltip("How quickly individual flickers happen within an electrical burst.")]
    [SerializeField]
    [Range(0.01f, 0.1f)]
    private float electricalFlickerInterval = 0.05f;
    [Tooltip("Minimum time between electrical flicker bursts (when the light is off).")]
    [SerializeField]
    private float minTimeBetweenElectricalBursts = 0.5f;
    [Tooltip("Maximum time between electrical flicker bursts (when the light is off).")]
    [SerializeField]
    private float maxTimeBetweenElectricalBursts = 1.5f;


    private bool isFlickering = false;
    private Coroutine currentFlickerCoroutine; // To keep track of the running coroutine

    void Start()
    {
        // If the target GameObject is not assigned, log an error and disable.
        // It's crucial that this is assigned manually or found via some other method
        // as we can't assume which GameObject it should be anymore.
        if (targetGameObjectToFlicker == null)
        {
            Debug.LogError("FlickerLight: Target GameObject to flicker is not assigned. Disabling script.");
            enabled = false;
            return;
        }

        // Ensure the target GameObject starts in an inactive state to begin the flicker from off.
        targetGameObjectToFlicker.SetActive(false);

        StartFlicker();
    }

    /// <summary>
    /// Starts the flickering coroutine if it's not already running.
    /// </summary>
    public void StartFlicker()
    {
        if (!isFlickering)
        {
            isFlickering = true;
            // Stop any existing flicker coroutine before starting a new one
            if (currentFlickerCoroutine != null)
            {
                StopCoroutine(currentFlickerCoroutine);
            }

            if (flickerMode == FlickerType.Regular)
            {
                currentFlickerCoroutine = StartCoroutine(DoRegularFlicker());
            }
            else if (flickerMode == FlickerType.Electrical)
            {
                currentFlickerCoroutine = StartCoroutine(DoElectricalFlicker());
            }
        }
    }

    /// <summary>
    /// Stops the flickering coroutine.
    /// </summary>
    public void StopFlicker()
    {
        if (isFlickering)
        {
            isFlickering = false;
            if (currentFlickerCoroutine != null)
            {
                StopCoroutine(currentFlickerCoroutine);
            }
            // Ensure the target GameObject is off when flickering stops
            if (targetGameObjectToFlicker != null)
            {
                targetGameObjectToFlicker.SetActive(false);
            }
        }
    }

    private IEnumerator DoRegularFlicker()
    {
        while (isFlickering)
        {
            // Toggle the target GameObject's active state
            if (targetGameObjectToFlicker != null)
            {
                targetGameObjectToFlicker.SetActive(!targetGameObjectToFlicker.activeSelf);
            }
            else
            {
                Debug.LogWarning("FlickerLight: Target GameObject is null during regular flicker. Stopping flicker.");
                StopFlicker();
                yield break;
            }

            // Wait for a random duration based on regularFlickerSpeed
            float waitTime = regularFlickerSpeed * Random.Range(0.5f, 1.5f);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator DoElectricalFlicker()
    {
        while (isFlickering)
        {
            // Turn off the light for a period
            if (targetGameObjectToFlicker != null)
            {
                targetGameObjectToFlicker.SetActive(false);
            }
            else
            {
                Debug.LogWarning("FlickerLight: Target GameObject is null during electrical flicker. Stopping flicker.");
                StopFlicker();
                yield break;
            }

            float timeBetweenBursts = Random.Range(minTimeBetweenElectricalBursts, maxTimeBetweenElectricalBursts);
            yield return new WaitForSeconds(timeBetweenBursts);

            // Start an electrical burst
            if (targetGameObjectToFlicker != null)
            {
                float burstDuration = Random.Range(minElectricalBurstDuration, maxElectricalBurstDuration);
                float burstTimer = 0f;

                while (burstTimer < burstDuration && isFlickering)
                {
                    targetGameObjectToFlicker.SetActive(!targetGameObjectToFlicker.activeSelf);
                    yield return new WaitForSeconds(electricalFlickerInterval);
                    burstTimer += electricalFlickerInterval;
                }
            }
            else
            {
                Debug.LogWarning("FlickerLight: Target GameObject became null during electrical burst. Stopping flicker.");
                StopFlicker();
                yield break;
            }
            // Ensure the light is off at the end of a burst to prepare for the next off period
            if (targetGameObjectToFlicker != null)
            {
                targetGameObjectToFlicker.SetActive(false);
            }
        }
    }

    // Optional: Call this from an external script to change flicker type dynamically
    public void SetFlickerMode(FlickerType newMode)
    {
        if (flickerMode != newMode)
        {
            flickerMode = newMode;
            // Restart flicker to apply new mode immediately
            if (isFlickering)
            {
                StopFlicker();
                StartFlicker();
            }
        }
    }

    // Optional: Call this from an external script to change flicker speed dynamically
    public void SetRegularFlickerSpeed(float newSpeed)
    {
        regularFlickerSpeed = newSpeed;
    }
}