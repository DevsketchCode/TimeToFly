using UnityEngine;

public class LoopLevelElement : MonoBehaviour
{
    [SerializeField]
    private float speed = 1f;

    [SerializeField]
    private float width = 6f;

    [SerializeField]
    public bool Pause = false;

    public SpriteRenderer spriteRenderer;

    private Vector2 startSize;


    private void Start()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on the GameObject.");
            return;
        }

        startSize = new Vector2(spriteRenderer.size.x, spriteRenderer.size.y);
    }

    private void Update()
    {
        if (Pause)
        {
            return;
        }
        spriteRenderer.size = new Vector2(spriteRenderer.size.x + speed * Time.deltaTime, spriteRenderer.size.y);

        if (spriteRenderer.size.x > width)
        {
            spriteRenderer.size = startSize;
        }
    }

    public void PauseLevelElementsLoop(bool pause)
    {
        if (pause)
        {
            Pause = true;
        }
        else
        {
            Pause = false;
        }
    }
}
