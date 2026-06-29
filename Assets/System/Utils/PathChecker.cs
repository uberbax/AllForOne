using System;
using System.Collections.Generic;
using UnityEngine;

public class PathChecker : MonoBehaviour
{
    // Update is called once per frame
    public List<GameObject> points = new List<GameObject>();
    
    public List<GameObject> points2 = new List<GameObject>();
    public Transform root2;
    
    private int maxCnt = 50;
    public List<(int, int, int)> lastPath = new();
    
    public static PathChecker instance;
    private void Awake()
    {
        instance = this;
        
        for (int i= 0; i < transform.childCount; i++)
            points.Add(transform.GetChild(i).gameObject);
        
        for (int i= 0; i < root2.childCount; i++)
            points2.Add(root2.GetChild(i).gameObject);

        var vv = transform.childCount;
        for (int i = 0; i < maxCnt - vv; i++)
        {
            var g = Instantiate(points[0], transform);
            points.Add(g);
        }
    }

    private void OnEnable()
    {
        lastPath.Clear();
    }

    public void ShowPath(List<(int, int, int)> lastPath, int val = 1)
    {
        List<GameObject> pnts = new List<GameObject>();
        if (val == 1) pnts = points;
        else if (val == 2) pnts = points2;
        
            for (int i = 0; i < pnts.Count; i++)
            {
                pnts[i].SetActive(false);
            }
            for (int i = 0;  i < lastPath.Count; i++)
            {
                pnts[i].SetActive(true);
                if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
                {
                    pnts[i].transform.position = new Vector3(
                        PositionSetter.instance.floors[lastPath[i].Item1, lastPath[i].Item2].transform.position.x,
                        PositionSetter.instance.floors[lastPath[i].Item1, lastPath[i].Item2].transform.position.y,
                        pnts[i].transform.position.z);
                }
                else
                {
                    pnts[i].transform.position = new Vector3(
                        PositionSetter.instance.floors[lastPath[i].Item1, lastPath[i].Item2].transform.position.x,
                        pnts[i].transform.position.y,
                        PositionSetter.instance.floors[lastPath[i].Item1, lastPath[i].Item2].transform.position.z
                        );
                }
            }        
    }

    void Update()
    {
        //if (Input.GetMouseButtonDown(1))
        //{
            var uu = UtilsControl.GetMousePoint();
            var lastMon = MainStates.instance.lastAllySelected == null ? MainStates.instance.mainPlayer : MainStates.instance.lastAllySelected;
            var h = lastMon.Position;
            lastPath = PositionSetter.instance.GetPath(h, uu, lastMon.dbObj.sizeX, lastMon.dbObj.sizeY, lastMon, 0);
            
            ShowPath(lastPath, 1);
            //

        
        //}
    }
    
    
}
