using UnityEngine;
using System.Collections; // Required for Coroutines

public class BackgroundScroller : MonoBehaviour
{
    private float scrollSpeed = 0f; // Will get from LevelManager
    [SerializeField] public bool Pause = false;

    public Transform[] backgrounds;
    public float spriteWidth;

    private bool isScrollingRightTemporarily = false;
    private float currentScrollSpeed; // To store the original scroll speed
    private LevelManager levelManager;

    void Start()
    {
        // Get both background child objects
        backgrounds = new Transform[2];
        if (transform.childCount < 2)
        {
            Debug.LogError("BackgroundScroller requires exactly 2 child background objects.");
            enabled = false;
            return;
        }

        backgrounds[0] = transform.GetChild(0);
        backgrounds[1] = transform.GetChild(1);

        SpriteRenderer sr = backgrounds[0].GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("Child objects must have SpriteRenderer components.");
            enabled = false;
            return;
        }

        spriteWidth = sr.size.x;

        // Find the LevelManager instance
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found in the scene!");
            enabled = false;
            return;
        }
        scrollSpeed = levelManager.backgroundSpeed; // Get background speed from LevelManager
        currentScrollSpeed = scrollSpeed; // Store the initial speed
    }

    void Update()
    {
        if (Pause && !isScrollingRightTemporarily) return;

        float moveDirection = isScrollingRightTemporarily ? 1 : -1; // 1 for right, -1 for left
        float currentSpeed = isScrollingRightTemporarily ? currentScrollSpeed : scrollSpeed;

        foreach (Transform bg in backgrounds)
        {
            bg.Translate(Vector3.right * moveDirection * currentSpeed * Time.deltaTime);

            if (bg.position.x < -spriteWidth && !isScrollingRightTemporarily)
            {
                float otherX = GetOtherBackground(bg).position.x;
                bg.position = new Vector3(otherX + spriteWidth - 0.1f, bg.position.y, bg.position.z);
            }
            else if (bg.position.x > spriteWidth * 2 && isScrollingRightTemporarily)
            {
                float otherX = GetOtherBackground(bg).position.x;
                bg.position = new Vector3(otherX - spriteWidth + 0.1f, bg.position.y, bg.position.z);
            }
        }
    }

    private Transform GetOtherBackground(Transform current)
    {
        return current == backgrounds[0] ? backgrounds[1] : backgrounds[0];
    }

    public void PauseScrolling(bool value)
    {
        Pause = value;
    }

    public void ScrollRightTemporarily(float duration)
    {
        StartCoroutine(ScrollRightRoutine(duration));
    }

    private IEnumerator ScrollRightRoutine(float duration)
    {
        isScrollingRightTemporarily = true;
        yield return new WaitForSeconds(duration);
        isScrollingRightTemporarily = false;
    }
}