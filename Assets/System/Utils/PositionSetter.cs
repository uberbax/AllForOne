using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class PositionSetter : MonoBehaviour
{
    public GameObject frame;
    public GameObject frameBlack;
    
    public enum CoordMode
    {
        XY,
        XZ
    }

    public TileMode tileMode = TileMode.Manhattan;
    public enum TileMode
    {
        Manhattan = 0,
        Hex = 1,
        Isometric = 2
    }
    
    public static List<(int, int)> tupleDltFull = new List<(int, int)>
    {
        (1, 0),
        (-1, 0),

        (0, 1),
        (0, -1),

        (1, 1),
        (1, -1),
        (-1, 1),
        (-1, -1)
    };
    
    public static List<(int, int)> tupleDltFullISO_even = new List<(int, int)>
    {
        (-1, 0),
        (-1, -1),
        
        (1, 0),
        (1, -1), 
        
        (0, 1),
        (0, -1),

        (-2, 0),
        (2, 0),
    };
    
    public static List<(int, int)> tupleDltFullISO_odd = new List<(int, int)>
    {
        (-1, 0),
        (-1, 1),
        
        (1, 0),
        (1, 1), 
        
        (0, 1),
        (0, -1),

        (-2, 0),
        (2, 0),
    };
    
    public static List<(int, int)> tupleDltFullHex_even = new List<(int, int)>
    {
        (-1, 0),
        (-1, -1),
        
        (1, 0),
        (1, -1), 
        
        (0, 1),
        (0, -1),
        
    };
    
    public static List<(int, int)> tupleDltFullHex_odd = new List<(int, int)>
    {
        (-1, 0),
        (-1, 1),
        
        (1, 0),
        (1, 1), 
        
        (0, 1),
        (0, -1),
        
    };
    
    
    public Transform lo;
    public Transform high;

    public int n = 5;
    public int m = 5;

    public bool useEnblDisablOnDrag = false;
    public bool main = false;

    public CoordMode coordMode = CoordMode.XY;
    public bool useClosestOnNavmesh = false;

    public float width;
    public float height;

    public Transform wallRoot;
    public Transform wallRootOther;
    public Transform fogRoot;
    public UnoDir[,] floors = new UnoDir[100,100];
    public Transform[,] walls = new Transform[100,100];
    public Transform[,] fog = new Transform[100,100];

    public void ClearWalls()
    {
        if (wallRootOther != null)
        {
            for (int i = wallRootOther.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(wallRootOther.GetChild(i).gameObject);
            }
        }

        Pathfinding2D.instance.Reinit();
    }
    
    [ContextMenu("Recreate Points")]
    public void RecreatePoints()
    {
        float width = (high.position.x - lo.position.x) / m;
        float height = (high.position.y - lo.position.y) / n;
        
        for (int i = 0; i < n; i++)
        for (int j = 0; j < m; j++)
        {
            int num = i * m + j;
            if (transform.childCount >= num)
            {
                var go = new GameObject();
                go.transform.SetParent(transform);
                go.transform.SetAsLastSibling();
            }
            
            var tt = transform.GetChild(num);
            tt.name = "floor" + (i + 1) + "x" + (j + 1);

            tt.position = lo.position + new Vector3(width / 2 + j * width, height / 2 + i * height, lo.position.z);
        }
    }

    [ContextMenu("Recreate FROM TILE")]
    public void RecreatePByTile()
    {
        RecreatePointsByTile();
    }
    
    [ContextMenu("Recreate FROM TILE --- FOG")]
    public void RecreatePByTileFog()
    {
        RecreatePointsByTile(fogRoot, frame, true, frameBlack);
    }

    [ContextMenu("<<< Set Order >>>")]
    public void SetOrder()
    {
        for (int i = 0; i < wallRoot.childCount; i++)
        {
            var g = wallRoot.GetChild(i);
            UtilsControl.CalculateLayer(g.gameObject, lo, high);
        }
    }

    public Transform loDrag1;
    public Transform hiDrag1;
    public GameObject dragTile;
    public Transform dragRoot;
    
    [ContextMenu("Recreate Drags")]
    public void RecreateDragsField()
    {
        //clear at first ?
        for (int i = dragRoot.childCount - 1; i >= 0; i--) DestroyImmediate(dragRoot.GetChild(i).gameObject);
        RecreatePointsByTile(dragRoot, null, false, null, loDrag1, hiDrag1, dragTile, 10, 3, 0);
    }
    
    
    public void RecreatePointsByTile(Transform root = null, GameObject tl = null, bool noFog = true, GameObject tl2 = null,
        
        Transform lo1 = null, Transform high1 = null, GameObject frame1 = null, int n1 = -1, int m1 = -1, float shft = 1
        )
    {
        if (lo1 == null) lo1 = lo;
        if (high1 == null) high1 = high;
        if (frame1 == null) frame1 = frame;
        
        
        if (root == null) root = transform;
        if (tl == null) tl = frame1;
        if (tl2 == null) tl2 = frame1;
        
        
        float width = frame1.GetComponent<SpriteRenderer>().bounds.size.x;
        float height = frame1.GetComponent<SpriteRenderer>().bounds.size.y;

        if (n1 > 0 && m1 > 0)
        {
            width = (high1.position.x - lo1.position.x) / (m1-1);
            height = (high1.position.y - lo1.position.y) / (n1-1);
        }
        
        Debug.Log(width + " " + height);
        
        float lox = lo1.position.x;
        float loy = 0;
            
        loy = lo1.position.y;
        if (coordMode == CoordMode.XZ)
            loy = lo1.position.z;

        n = 0;
        int cur = 0;
        
        float MaxY = high1.position.y;
        float MaxX = high1.position.x + (1-shft) * width ;
        if (coordMode == CoordMode.XZ)
            MaxY = high1.position.z;
        
        
        //while (true)
        for (int o = 0; o < 2000; o++)
        {
            if (loy + height > MaxY) break;
            
            if (lox + width > MaxX)
            {
                m = cur;
                lox = lo1.position.x;
                
                if (tileMode == TileMode.Manhattan)
                    loy += height;
                else if (tileMode == TileMode.Isometric)
                    loy += height/2;
                else if (tileMode == TileMode.Hex)
                    loy += height * 0.75f;
                
                n++;
                cur = 0;
                continue;
            }
            
            var go = Instantiate(tl2);
            go.transform.SetParent(root);
            go.transform.SetAsLastSibling();

            if (coordMode == CoordMode.XY)
            {
                if (n % 2 == 1)
                {
                    if (tileMode == TileMode.Isometric ||  tileMode == TileMode.Hex)
                        go.transform.position = new Vector3(lox + width/2 + width, loy, lo1.position.z);
                    else
                        go.transform.position = new Vector3(lox + width * shft, loy, lo1.position.z);
                }
                else 
                    go.transform.position = new Vector3(lox + width * shft, loy, lo1.position.z);
            }
            else
            {
                if (n % 2 == 1)
                {
                    if (tileMode == TileMode.Isometric ||  tileMode == TileMode.Hex)
                        go.transform.position = new Vector3(lox + width/2 + width, loy, lo1.position.z);
                    else
                        go.transform.position = new Vector3(lox + width, loy, lo1.position.z);
                }
                else 
                    go.transform.position = new Vector3(lox + width, lo1.position.y, loy);
            }

            go.name = "floor" + (n + 1) + "x" + (cur + 1);
            floors[n, cur] = go.transform.GetComponent<UnoDir>();
            
            lox += width;
            cur++;

        }
        //recreate fog ? 
        //if (!noFog) RecreatePointsByTile(fogRoot, frameBlack, false);
    }
    
    public Transform b00, b01, b10;

    [ContextMenu("ClearAll")]
    public void ClearAll()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    public Vector3 lowXZ = Vector2.zero;
    public Vector3 highXZ = Vector2.zero;

    [ContextMenu("Find Lowest")]
    public void FindLowest()
    {
        lowXZ = new Vector2(1e+10f, 1e+10f);
        highXZ = new Vector2(-1e+10f, -1e+10f);

        for (int i = 0; i < transform.childCount; i++)
        {
            var gg = transform.GetChild(i);
            if (gg.position.x < lowXZ.x) lowXZ.x = gg.position.x;
            if (gg.position.z < lowXZ.z) lowXZ.z = gg.position.z;
            if (gg.position.x > highXZ.x) highXZ.x = gg.position.x;
            if (gg.position.z > highXZ.z) highXZ.z = gg.position.z;
            
        }


    }
    
    [ContextMenu("Recreate Points HEX HORIZ")]
    public void RecreatePointsHexHoriz()
    {
        var a0 = transform.Find("floor1x1");
        var a1 = transform.Find("floor1x2");
        var a2 = transform.Find("floor2x1");
        
        
        //clear others
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i) == a0 || transform.GetChild(i) == a1 || transform.GetChild(i) == a2) continue;
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        width = 0;
        height = 0;

        float py = 1;
        float pz = 0;

        if (coordMode == CoordMode.XZ)
        {
            py = 0;
            pz = 1;
        }

        if (b01 != null)
        {
            width = b01.position.x - b00.position.x;
            height = b10.position.y - b00.position.y;
            if (coordMode == CoordMode.XZ)
                height = b10.position.z - b00.position.z;
        }
        else
        {
            width = (a1.position.x - a0.position.x);
            height = (a2.position.y - a0.position.y);            
        }

        
        for (int i = 1; i < n+1; i++)
        for (int j = 1; j < m + 1; j++)
        {
            var t0 = transform.Find("floor" + i.ToString() + "x" + j.ToString());
            if (t0 == null)
            {
                var bb = Instantiate(transform.GetChild(0).gameObject, transform);
                bb.name = "floor" + i.ToString() + "x" + j.ToString();
                t0 = bb.transform;
            }
            
            if (i == 1 && j == 1) continue;
            if (i % 2 == 1)
            {
                //we keep z
                float kz = t0.position.z;
                if (coordMode == CoordMode.XZ)
                    kz = t0.position.y;
                    
                t0.position = a0.position + new Vector3((j - 1) * width, py * (i - 1) * height, pz * (i - 1) * height);
                
                if (coordMode == CoordMode.XZ)
                    t0.position = new Vector3(t0.position.x, kz, t0.position.z);
                else
                    t0.position = new Vector3(t0.position.x, t0.position.y, kz);
            }
            else
            {
                float kz = t0.position.z;
                if (coordMode == CoordMode.XZ)
                    kz = t0.position.y;
                
                t0.position = a2.position + new Vector3((j - 1) * width, py * (i - 2) * height, pz * (i - 2) * height);
                
                if (coordMode == CoordMode.XZ)
                    t0.position = new Vector3(t0.position.x, kz, t0.position.z);
                else
                    t0.position = new Vector3(t0.position.x, t0.position.y, kz);
            }
            //
            if (useClosestOnNavmesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(t0.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    var result = hit.position;
                    t0.position = result;
                }
            }
        }
        
        
    }

    [ContextMenu("Recreate From One")]
    public void RecreateFromOne()
    {
        var a0 = transform.Find("floor1x1");

        //clear others
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i) == a0) continue;
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        width = 0;
        height = 0;

        float py = 1;
        float pz = 0;

        if (coordMode == CoordMode.XZ)
        {
            py = 0;
            pz = 1;
        }

        var sx = a0.GetComponent<SpriteRenderer>().bounds.size;
        width = sx.x;
        height = sx.y;
        
        //
        for (int i = 1; i < n+1; i++)
        for (int j = 1; j < m + 1; j++)
        {
            var t0 = transform.Find("floor" + i.ToString() + "x" + j.ToString());
            if (t0 == null)
            {
                var bb = Instantiate(transform.GetChild(0).gameObject, transform);
                bb.name = "floor" + i.ToString() + "x" + j.ToString();
                t0 = bb.transform;
            }
            
            if (i == 1 && j == 1) continue;
            
            {
                //we keep z
                float kz = t0.position.z;
                if (coordMode == CoordMode.XZ)
                    kz = t0.position.y;
                    
                t0.position = a0.position + new Vector3((j - 1) * width, py * (i - 1) * height, pz * (i - 1) * height);
                
                if (coordMode == CoordMode.XZ)
                    t0.position = new Vector3(t0.position.x, kz, t0.position.z);
                else
                    t0.position = new Vector3(t0.position.x, t0.position.y, kz);
            }

            //
            if (useClosestOnNavmesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(t0.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    var result = hit.position;
                    t0.position = result;
                }
            }

            if (high.position.x < t0.position.x)
            {
                var sv = high.position;
                high.position = new Vector3(t0.position.x, sv.y, sv.z);
            }
            if (high.position.y < t0.position.y)
            {
                var sv = high.position;
                high.position = new Vector3(sv.x, t0.position.y, sv.z);
            }
        }
        
    }
    
    [ContextMenu("Do Verify")]
    public void DoVerifyDelete()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var df = transform.GetChild(i).name.Substring(5).Split("x");

            int y = int.Parse(df[0]) - 1;
            int x = int.Parse(df[1]) - 1;

            if (y > n || x > m)
                Destroy(transform.GetChild(i).gameObject);
        }
    }


    public static PositionSetter instance;

    private void Awake()
    {
        if (main)
            instance = this;
        
        //
        n = 0;
        m = 0;
        
        for (int i = 0; i < transform.childCount; i++)
        {
            var s = transform.GetChild(i).name;
            var s1 = s.Substring(5);
            var s2 = s1.Split("x");

            int x = int.Parse(s2[0]) - 1;
            int y = int.Parse(s2[1]) - 1;

            if (x > n) n = x;
            if (y > m) m = y;
            
            floors[x, y] = transform.GetChild(i).GetComponent<UnoDir>();
        }
        //we assign dirs
        for (int i = 0; i <= n; i++)
        for (int j = 0; j <= m; j++)
        {
            if (floors[i, j] == null) continue;
            floors[i, j].x = i;
            floors[i, j].y = j;
            
            
            if (true)
            {
                var what = tupleDltFull;
                if (tileMode == TileMode.Isometric)
                {
                    if (i % 2 == 0)
                        what = tupleDltFullISO_even;
                    else
                        what = tupleDltFullISO_odd;
                }
                else if (tileMode == TileMode.Hex)
                {
                    if (i % 2 == 0)
                        what = tupleDltFullHex_even;
                    else
                        what = tupleDltFullHex_odd;
                }

                for (int k = 0; k < what.Count; k++)
                {
                    int x = i +  what[k].Item1;
                    int y = j +  what[k].Item2;
                    if (x > n || y > m || x < 0 || y < 0) floors[i,j].Dirs.Add(null);
                    else floors[i,j].Dirs.Add(floors[x,y]);
                }
            }
        }
        //
        ParseWalls();
        ParseFog();
    }

    public void ParseFog()
    {
        if (fogRoot == null || !fogRoot.gameObject.activeInHierarchy) return;
        
        for (int i = 0; i < fogRoot.childCount; i++)
        {
            var s = fogRoot.GetChild(i).name;
            var s1 = s.Substring(5);
            var s2 = s1.Split("x");

            int x = int.Parse(s2[0]) - 1;
            int y = int.Parse(s2[1]) - 1;
            
            fog[x, y] = fogRoot.GetChild(i);
        }
    }
    
    public bool wallsParsed = false;
    
    public void ParseWalls()
    {
        if (!ConfigLoader.parseEnded)
        {
            Invoke("ParseWalls", 0.01f);
            return;
        }
        
        wallsParsed = true;
        
        if (!wallRoot) return;
        
        for (int i = 0; i < wallRoot.childCount; i++)
        {
            var s = wallRoot.GetChild(i);
            var hh = GetClosestPos(s.position);
            walls[hh.Item1, hh.Item2] = s;
        }
    }

    public void RemoveWall(int x, int y)
    {
        walls[x, y] = null;
    }

    List<(int, int)> lastOpen = new List<(int, int)>();
    public void OpenFog(Vector3 pos)
    {
        if (!fogRoot.gameObject.activeInHierarchy) return;
        var sz = ConfigLoader.GetMetaParamValue("fog_open");
        var ll = GetAllFog(pos, (int)sz);
        foreach (var v in lastOpen)
        {
            var bb = fog[v.Item1, v.Item2].GetComponent<SpriteRenderer>();
            if (bb) bb.color = new Color(0, 0, 0, 0.5f);
            else
            {
                fog[v.Item1, v.Item2].GetComponent<Renderer>().material.color = new Color(0, 0, 0, 0.5f);
            }
        }

        lastOpen.Clear();
        foreach (var v in ll)
        {
            var bb = fog[v.Item1, v.Item2].GetComponent<SpriteRenderer>();
            if (bb) bb.color = Color.clear;
            else
            {
                fog[v.Item1, v.Item2].GetComponent<Renderer>().material.color = Color.clear;
            }
            
            lastOpen.Add((v.Item1, v.Item2));
        }
    }
    
    public bool IsEmpty(int x, int y, out GameObject result)
    {
        if (walls[x, y] != null && walls[x,y].gameObject.activeSelf)
        {
            result = walls[x, y].gameObject;
            return false;
        }
        foreach (var v in MainStates.instance.combats)
        {
            if (v.ref_pos_x == x && v.ref_pos_y == y)
            {
                result = v.main;
                return false;
            }
        }

        result = null;
        return true;
    }

    public Vector3 GetRandomFreeInRange(Vector3 center, float range)
    {
        var g0 = GetClosestPos(center);
        //iterate over free fields ? should we ?
        float al = Random.Range(0, MathF.PI);
        var r1 = Random.Range(0, range);
        
        Vector3 point = Vector3.zero;
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {
           point = center + new Vector3(r1 * MathF.Cos(al), r1 * MathF.Sin(al), 0);
        }
        else
        {
            point = center + new Vector3(r1 * MathF.Cos(al), 0, r1 * MathF.Sin(al));
        }

        return point;
    }
    
    public List<(int, int, int)> GetPath(Vector3 from, Vector3 to, int szx, int szy, RObj mon, float dstCheck = 1)
    {
        List<(int, int, int)> path = new List<(int, int, int)>();
        var g0 = GetClosestPos(from);
        var g1 = GetClosestPos(to);
        List<(int, int, int, int)> que = new List<(int, int, int, int)>();
        que.Add((g0.Item1, g0.Item2, -1, 0));
        int yk1 = 0;
        int yk2 = -1;
        bool find = false;
        
        int closestH = -1;
        float iniD = MainStates.instance.GetDistance(g0.Item1, g0.Item2, g1.Item1, g1.Item2);
        HashSet<(int,int)> visited = new HashSet<(int,int)>();
        visited.Add((g0.Item1, g0.Item2));
        
        while (yk1 > yk2)
        {
            yk2++;
            var cur = floors[que[yk2].Item1, que[yk2].Item2];

            
            if (tileMode == TileMode.Manhattan || tileMode == TileMode.Isometric || tileMode == TileMode.Hex)
            {
                foreach (var b in cur.Dirs)
                {
                    if (b == null) continue;
                    //check if already in array ?
                    //optimization
                    if (visited.Contains( (b.x, b.y) )) continue;

                    //well we need to check all sizes i suppose

                    bool chc = true;
                    for (int i1 = 0; i1 < szx; i1++)
                        for (int j1 = 0; j1 < szy; j1++)
                        {
                            if (walls[b.x + i1, b.y + j1] != null && walls[b.x + i1, b.y + j1].gameObject.activeSelf) chc = false;
                            var ee = MainStates.instance.combats.Find(x => x.ref_pos_x == b.x + i1 && x.ref_pos_y == b.y + j1);
                            if (ee == mon) continue;
                            if (ee != null && ee.GetPar("passable") < 1)
                            {
                                chc = false;
                            }
                        }
                    
                    if (!chc) continue;

                    float newD = MainStates.instance.GetDistance(b.x, b.y, g1.Item1, g1.Item2);
                    if (newD < iniD)
                    {
                        iniD = newD;
                        closestH = yk1 + 1;
                    }
                    
                    visited.Add((b.x, b.y));
                    que.Add((b.x, b.y, yk2, que[yk1].Item4 + 1));
                    yk1++;
                    if ((b.x == g1.Item1 && b.y == g1.Item2) || newD <= dstCheck)
                    {
                        find = true;
                        break;
                    }
                }
            }

            if (find)
                break;
        }

        if (find) closestH = yk1;
        
        if (!find && closestH >= 0)
        {
            find = true;
        }
        
        if (find)
        {
            var k = que[closestH].Item3;
            path.Add((que[closestH].Item1, que[closestH].Item2, 0));
            while (k >= 0)
            {
                var g = que[k];
                if (g.Item3 < 0)
                {
                    //starting point
                    break;
                }
                path.Add((g.Item1, g.Item2, 0));
                k = g.Item3;
            }
        }
        
        path.Reverse();  
        
        if (szx > 1 && szy > 1)
        {
            int p = 0;
            //PathChecker.instance.ShowPath(path, 2);
        }
        
        return path;
    }

    public List<(int, int, int, int, int)> GetAllFog(Vector3 from, int size)
    {
        List<(int, int, int)> path = new List<(int, int, int)>();
        var g0 = GetClosestPos(from);
        List<(int, int, int, int, int)> que = new List<(int, int, int, int, int)>();
        que.Add((g0.Item1, g0.Item2, -1, 0, 0));
        int yk1 = 0;
        int yk2 = -1;
        bool find = false;
        
        while (yk1 > yk2)
        {
            yk2++;
            if (que[yk2].Item4 < 0) continue;
            if (que[yk2].Item5 >= size) continue;
            var cur = floors[que[yk2].Item1, que[yk2].Item2];
            
            if (tileMode == TileMode.Manhattan)
            {
                foreach (var b in cur.Dirs)
                {
                    if (b == null) continue;
                    //check if already in array ?
                    //optimization
                    var hh = que.Find(c => c.Item1 == b.x && c.Item2 == b.y);
                    if (hh != default) continue;
                    int r = 0;
                    if (walls[b.x, b.y] != null && walls[b.x,b.y].gameObject.activeSelf)
                    {
                        r = -1;
                    }
                    
                    que.Add((b.x, b.y, yk2, r, que[yk2].Item5 + 1));
                    yk1++;

                }
            }

            if (find)
                break;
        }

        return que;
    }
    
    public List<(int, int, int, int, int)> GetAllFreeSquares(Vector3 from, int size, bool includeIni = false)
    {
        List<(int, int, int)> path = new List<(int, int, int)>();
        var g0 = GetClosestPos(from);
        List<(int, int, int, int, int)> que = new List<(int, int, int, int, int)>();
        que.Add((g0.Item1, g0.Item2, -1, 0, 0));
        int yk1 = 0;
        int yk2 = -1;
        bool find = false;
        
        while (yk1 > yk2)
        {
            yk2++;
            if (que[yk2].Item4 < 0) continue;
            if (que[yk2].Item5 >= size) continue;
            var cur = floors[que[yk2].Item1, que[yk2].Item2];
            
            if (tileMode == TileMode.Manhattan)
            {
                foreach (var b in cur.Dirs)
                {
                    if (b == null) continue;
                    //check if already in array ?
                    //optimization
                    var hh = que.Find(c => c.Item1 == b.x && c.Item2 == b.y);
                    if (hh != default) continue;
                    int r = 0;
                    if (walls[b.x, b.y] != null && walls[b.x,b.y].gameObject.activeSelf)
                    {
                        continue;
                    }
                    var ee = MainStates.instance.combats.Find(x => x.ref_pos_x == b.x && x.ref_pos_y == b.y);
                    if (ee != null && ee.GetPar("passable") < 1)
                    {
                        continue;
                    }
                    
                    que.Add((b.x, b.y, yk2, r, que[yk2].Item5 + 1));
                    yk1++;

                }
            }

            if (find)
                break;
        }

        if (!includeIni)
            que=que.GetRange(1, que.Count - 1);
        
        return que;
    }

    public Vector3 GetFarthestPos(Vector3 from, List<(int, int, int, int, int)> poses, out float de)
    {
        float dst = 0;
        de = 0;
        Vector3 farthestPos = default;
        foreach (var pos in poses)
        {
            var ff = floors[pos.Item1, pos.Item2].transform.position - from;
            if (ff.magnitude > dst)
            {
                de = dst;
                dst = ff.magnitude;
                farthestPos = floors[pos.Item1, pos.Item2].transform.position;
            }
        }
        
        return farthestPos;
    }
    
    public Vector3 GetFarthestPosDot(Vector3 from, Vector3 who, List<(int, int, int, int, int)> poses, out float de)
    {
        float dst = 0;
        de = 0;
        Vector3 farthestPos = default;
        foreach (var pos in poses)
        {
            var ff = floors[pos.Item1, pos.Item2].transform.position - who;
            if (Vector3.Dot(from, ff) > dst)
            {
                dst = Vector3.Dot(from, ff);
                de = ff.magnitude;
                
                farthestPos = floors[pos.Item1, pos.Item2].transform.position;
            }
        }
        
        return farthestPos;
    }
    
    
    public (int, int, Vector3) GetClosestPos(Vector3 pos)
    {
        float ratX = pos.x - floors[0, 0].transform.position.x;
        ratX /= (floors[n,m].transform.position.x - floors[0, 0].transform.position.x);

        float ratY = 0;
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {
            ratY = pos.y - floors[0, 0].transform.position.y;
            ratY /= (floors[n, m].transform.position.y - floors[0, 0].transform.position.y);
        }
        else
        {
            ratY = pos.z - floors[0, 0].transform.position.z;
            ratY /= (floors[n, m].transform.position.z - floors[0, 0].transform.position.z);
        }

        int m1 = (int)(ratX * m);
        int n1 = (int)(ratY * n);

        float dst = 1e+10f;
        
        (int, int, Vector3) closest = (0,0,Vector3.zero);
        
        for (int k = 0; k < 3; k++)
        {
            
            for (int i = n1-k; i<=n1+k; i++ )
            for (int j = m1 - k; j <= m1 + k; j++)
            {
                if (i < 0 || i > n) continue;
                if (j < 0 || j > m) continue;

                var vec = floors[i, j].transform.position - pos;
                if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0) vec.z = 0;
                else vec.y = 0;

                if (vec.magnitude < dst)
                {
                    dst =  vec.magnitude;
                    closest = (i, j, floors[i,j].transform.position);
                }
                    
            }
            
        }
        return closest;

    }
    
    private void OnDestroy()
    {
        instance = null;
    }
    
    public void UnitDragged()
    {
        if (useEnblDisablOnDrag)
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).GetComponent<SpriteRenderer>().enabled = true;
        }
    }

    public void DisableField()
    {
        if (useEnblDisablOnDrag)
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }
    }
}