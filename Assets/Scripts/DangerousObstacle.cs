using UnityEngine;

public class DangerousObstacle : MonoBehaviour
{
    public enum DangersousObstacleType // Changed accessibility to public
    {
        Electrical,
        Falling,
        Solid,
        Spikes
    }

    [SerializeField]
    private DangersousObstacleType obstacleType = DangersousObstacleType.Solid;

    public DangerousObstacle()
    {
        // Default constructor
        obstacleType = DangersousObstacleType.Solid; // Default to Solid type
    }

    public DangersousObstacleType GetObstacleType()
    {
        return obstacleType;
    }
}
