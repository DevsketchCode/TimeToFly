using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    [SerializeField]
    private float lifeTime = 10f;
    public bool isPaused = false;

    private void Update()
    {
        //Debug.Log($"Object: {gameObject.name}, Paused: {isPaused}, LifeTime: {lifeTime}");
        if (!isPaused)
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    // Public getter for the paused state if needed for debugging
    public bool IsPaused()
    {
        return isPaused;
    }
}