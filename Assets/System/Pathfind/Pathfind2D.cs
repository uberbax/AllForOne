using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml.ConditionalFormatting;
using UnityEngine;

public class Pathfinding2D : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2 gridSize = new Vector2(100, 100);
    public float nodeRadius = 0.5f;
    public LayerMask obstacleLayer;
    
    private Node[,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;
    private Vector2 gridOrigin;

    public static Pathfinding2D instance;

    public float edgeRad = 0.3f;

    public bool noAlgo1 = false;
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Set obstacle layer to only include "Nopass"
        obstacleLayer = LayerMask.GetMask("Nopass");

        if (!noAlgo1)
        {
            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridSize.y / nodeDiameter);
            CreateGrid();
        }
    }
    
    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        gridOrigin = (Vector2)transform.position - (gridSize / 2);
        
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = gridOrigin + new Vector2(x * nodeDiameter + nodeRadius, y * nodeDiameter + nodeRadius);
                bool walkable = !Physics2D.OverlapCircle(worldPoint, nodeRadius, obstacleLayer);
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }
    
    public List<Vector2> FindPath(Vector2 startPos, Vector2 targetPos)
    {
        Node startNode = GetNodeFromWorldPoint(startPos);
        Node targetNode = GetNodeFromWorldPoint(targetPos);
        
        if (!startNode.walkable || !targetNode.walkable)
        {
            Debug.Log("Start or target position is not walkable!");
            return null;
        }
        
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);
        
        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || 
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }
            
            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;
                
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;
                    
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        
        return null; // Path not found
    }

    //my algo

    private bool inited = false;
    private List<Collider2D> colls = new List<Collider2D>();
    private List<Collider> colls3D = new List<Collider>();
    
    List<Vector3> keyPoints = new List<Vector3>();
    public List<Vector3> FindPath2(Vector2 startPos, Vector2 targetPos, float zPos)
    {
        if (!inited)
        {
            var gg = FindObjectsByType<Collider2D>(FindObjectsSortMode.None).ToList();
            colls = gg.FindAll(x => x.gameObject.layer == LayerMask.NameToLayer("Nopass"));
            inited = true;
        }
        
        keyPoints.Clear();
        keyPoints.Add(startPos);
        keyPoints.Add(targetPos);
        List<float> dsts = new List<float>();
        List<bool> mark = new List<bool>();
        List<Vector3> ans = new List<Vector3>();
        List<int> path = new List<int>();
        
        float MAX = 1e+8f;
        dsts.Add(0);
        dsts.Add(MAX);
        mark.Add(false);
        mark.Add(false);
        path.Add(-1);
        path.Add(-1);
        
        foreach (var v in colls)
        {
            Vector3 vec = Vector3.zero;
            
            vec = v.bounds.center - (v.bounds.size / 2 + new Vector3(edgeRad, edgeRad, 0));
            keyPoints.Add(vec);
            dsts.Add(MAX);
            mark.Add(false);
            path.Add(-1);
            
            vec = v.bounds.center + (v.bounds.size / 2 + new Vector3(edgeRad, edgeRad, 0));
            keyPoints.Add(vec);
            dsts.Add(MAX);
            mark.Add(false);
            path.Add(-1);
            
            vec = v.bounds.center + new Vector3(-v.bounds.size.x / 2 - edgeRad, v.bounds.size.y / 2 + edgeRad, 0);
            keyPoints.Add(vec);
            dsts.Add(MAX);
            mark.Add(false);
            path.Add(-1);
            
            vec = v.bounds.center + new Vector3(v.bounds.size.x / 2 + edgeRad, -v.bounds.size.y / 2 - edgeRad, 0);
            keyPoints.Add(vec);
            dsts.Add(MAX);
            mark.Add(false);
            path.Add(-1);
        }
        //dijkstra   
        bool find = false;
        while (true)
        {
            var min = MAX;
            int fnd = -1;
            for (int i = 0; i < keyPoints.Count; i++)
            {
                if (dsts[i] < min && !mark[i])
                {
                    min = dsts[i];
                    fnd = i;
                }
            }

            if (fnd < 0) break;
            //we find

            if (fnd == 1)
            {
                find = true;
                break;
            }
            mark[fnd] = true;
            

            for (int i = 0; i < keyPoints.Count; i++)
            {
                if (mark[i]) continue;
                var dst = (keyPoints[fnd] - keyPoints[i]).magnitude;
                var res = Physics2D.Raycast(keyPoints[fnd], keyPoints[i] - keyPoints[fnd], dst, 1 << LayerMask.NameToLayer("Nopass"));
                //var res = Physics2D.CircleCast(keyPoints[fnd], 0.15f, keyPoints[i] - keyPoints[fnd], dst, 1 << LayerMask.NameToLayer("Nopass"));
                
                if (res.collider != null)
                {
                    continue;
                }

                if (dsts[fnd] + dst < dsts[i])
                {
                    dsts[i] = dsts[fnd] + dst;
                    path[i] = fnd;
                }
            }
            
        }

        if (find)
        {
            ans.Add(targetPos);
            int r = path[1];
            while (r > 0)
            {
                ans.Add(keyPoints[r]);
                r = path[r];
            }

            ans.Reverse();
        }

        return ans;

    }
    
    public List<Vector3> FindPath3(Vector3 startPos, Vector3 targetPos, Vector3 floorPos)
    {
        startPos += new Vector3(0, 0.3f, 0);
        targetPos += new Vector3(0, 0.3f, 0);
        if (!inited)
        {
            var gg = FindObjectsByType<Collider>(FindObjectsSortMode.None).ToList();
            colls3D = gg.FindAll(x => x.gameObject.layer == LayerMask.NameToLayer("Nopass"));
            inited = true;
        }
        
        keyPoints.Clear();
        keyPoints.Add(startPos);
        keyPoints.Add(targetPos);
        List<float> dsts = new List<float>();
        List<bool> mark = new List<bool>();
        List<Vector3> ans = new List<Vector3>();
        List<int> path = new List<int>();
        
        float MAX = 1e+8f;
        dsts.Add(0);
        dsts.Add(MAX);
        mark.Add(false);
        mark.Add(false);
        path.Add(-1);
        path.Add(-1);
        
        foreach (var v in colls3D)
        {
            Vector3 vec = Vector3.zero;
            
            vec = v.bounds.center - (new Vector3(v.bounds.size.x / 2, 0, v.bounds.size.z / 2) + new Vector3(edgeRad, 0, edgeRad));
            keyPoints.Add(vec);
            dsts.Add(MAX);
            mark.Add(false);
            path.Add(-1);
            
            vec = v.bounds.center + (new Vector3(v.bounds.size.x / 2, 0, v.bounds.size.z / 2) + new Vector3(edgeRad, 0, edgeRad));
            keyPoints.Add(vec);
            dsts.Add(MAX);
            mark.Add(false);
            path.Add(-1);
            
            vec = v.bounds.center + new Vector3(-v.bounds.size.x / 2 - edgeRad, 0, v.bounds.size.z / 2 + edgeRad);
            keyPoints.Add(vec);
            dsts.Add(MAX);
            mark.Add(false);
            path.Add(-1);
            
            vec = v.bounds.center + new Vector3(v.bounds.size.x / 2 + edgeRad, 0, -v.bounds.size.z / 2 - edgeRad);
            keyPoints.Add(vec);
            dsts.Add(MAX);
            mark.Add(false);
            path.Add(-1);
        }
        //dijkstra   
        bool find = false;
        while (true)
        {
            var min = MAX;
            int fnd = -1;
            for (int i = 0; i < keyPoints.Count; i++)
            {
                if (dsts[i] < min && !mark[i])
                {
                    min = dsts[i];
                    fnd = i;
                }
            }

            if (fnd < 0) break;
            //we find

            if (fnd == 1)
            {
                find = true;
                break;
            }
            mark[fnd] = true;
            

            for (int i = 0; i < keyPoints.Count; i++)
            {
                if (mark[i]) continue;
                var dst = (keyPoints[fnd] - keyPoints[i]).magnitude;
                

                RaycastHit res;
                var b = Physics.Raycast(keyPoints[fnd], keyPoints[i] - keyPoints[fnd], dst,
                    1 << LayerMask.NameToLayer("Nopass"));
                
                if (b) continue;

                
                //var res = Physics2D.CircleCast(keyPoints[fnd], 0.15f, keyPoints[i] - keyPoints[fnd], dst, 1 << LayerMask.NameToLayer("Nopass"));
                


                if (dsts[fnd] + dst < dsts[i])
                {
                    dsts[i] = dsts[fnd] + dst;
                    path[i] = fnd;
                }
            }
            
        }

        if (find)
        {
            ans.Add(targetPos);
            int r = path[1];
            while (r > 0)
            {
                ans.Add(keyPoints[r]);
                r = path[r];
            }

            ans.Reverse();
        }

        return ans;

    }
    
    
    
    
    List<Vector2> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Add(startNode);
        path.Reverse();
        
        List<Vector2> waypoints = new List<Vector2>();
        foreach (Node node in path)
        {
            waypoints.Add(node.worldPosition);
        }
        
        return waypoints;
    }
    
    List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        
        return neighbors;
    }
    
    Node GetNodeFromWorldPoint(Vector2 worldPosition)
    {
        float percentX = (worldPosition.x - gridOrigin.x) / gridSize.x;
        float percentY = (worldPosition.y - gridOrigin.y) / gridSize.y;
        
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        
        return grid[x, y];
    }
    
    int GetDistance(Node a, Node b)
    {
        int distX = Mathf.Abs(a.gridX - b.gridX);
        int distY = Mathf.Abs(a.gridY - b.gridY);
        
        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);
        return 14 * distX + 10 * (distY - distX);
    }
    
    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize.x, gridSize.y, 0));
        
        if (grid != null)
        {
            foreach (Node node in grid)
            {
                Gizmos.color = node.walkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }

    public void Reinit()
    {
        inited = false;
    }
}

[System.Serializable]
public class Node
{
    public bool walkable;
    public Vector2 worldPosition;
    public int gridX, gridY;
    
    public int gCost;
    public int hCost;
    public Node parent;
    
    public int fCost
    {
        get { return gCost + hCost; }
    }
    
    public Node(bool _walkable, Vector2 _worldPos, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }
}