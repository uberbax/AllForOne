using System.Collections.Generic;
using UnityEngine;

public class PathfindingMovement : MonoBehaviour
{
    public Transform target;
    public Pathfinding2D pathfinding;
    private float speed = 1f;
    
    public List<Vector3> currentPath = new List<Vector3>();
    public int currentWaypointIndex = 0;
    private Vector3 targetSavedPos = Vector3.zero;
    
    void Start()
    {
        if (pathfinding == null)
            pathfinding = Pathfinding2D.instance;
        
        //FindNewPath();
    }

    public Transform GetTarget()
    {
        return target;
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        targetSavedPos = target.position;
        FindNewPath();
    }

    public void SetTarget(Vector3 targeto)
    {
        target = null;
        targetSavedPos = targeto;
    }

    public float CheckDistance()
    {
        return Vector3.Distance(target.position, targetSavedPos);
    }
    
    void Update1()
    {
        if (currentPath == null || currentWaypointIndex >= currentPath.Count)
            return;
        
        // Move towards current waypoint
        Vector3 targetPosition = currentPath[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        
        // Check if reached waypoint
        if (transform.position == targetPosition)
        {
            currentWaypointIndex++;
        }
        
        // Recalculate path if target moved far enough
        /*
        if (target != null && Vector2.Distance(transform.position, target.position) > 0.5f)
        {
            FindNewPath();
        }
        */
    }
    
    [ContextMenu("FindNewPath")]
    public void FindNewPath()
    {
        if (target == null || targetSavedPos == Vector3.zero) return;

        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {
            currentPath = pathfinding.FindPath2(new Vector2(transform.position.x, transform.position.y), target == null ? targetSavedPos : new Vector2(target.position.x, target.position.y), transform.position.z);
        }
        else
        {
            currentPath = pathfinding.FindPath3(transform.position, target == null ? targetSavedPos : target.position, Vector3.zero);
        }

        if (currentPath != null && currentPath.Count > 0) currentWaypointIndex = 0;
        
    }
    
    void OnDrawGizmos()
    {
        if (currentPath != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
        }
    }
}