using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] public bool Pause = false;

    private Transform[] backgrounds;
    public float spriteWidth;

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
    }

    void Update()
    {
        if (Pause) return;

        foreach (Transform bg in backgrounds)
        {
            bg.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

            if (bg.position.x < -spriteWidth)
            {
                float otherX = GetOtherBackground(bg).position.x;
                bg.position = new Vector3(otherX + spriteWidth -0.1f, bg.position.y, bg.position.z);
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
}
