using UnityEngine;

public class DangerousObstacle : MonoBehaviour
{
    public enum DangersousObstacleType // Changed accessibility to public
    {
        Electrical,
        Falling,
        Solid,
        Spikes, 
        Lightning
    }

    [SerializeField]
    private DangersousObstacleType obstacleType = DangersousObstacleType.Solid;
    [SerializeField]
    private string gameOverReason = "You hit a dangerous obstacle!";

    public DangerousObstacle()
    {
        // Default constructor
        obstacleType = DangersousObstacleType.Solid; // Default to Solid type
    }

    public DangersousObstacleType GetObstacleType()
    {
        return obstacleType;
    }

    public string GetGameOverReason()
    {
        return gameOverReason;
    }
}
