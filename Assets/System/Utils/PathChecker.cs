using System;
using System.Collections.Generic;
using UnityEngine;

public class PathChecker : MonoBehaviour
{
    // Update is called once per frame
    public List<GameObject> points = new List<GameObject>();
    private int maxCnt = 50;
    public List<(int, int, int)> lastPath = new();
    private void Awake()
    {
        for (int i= 0; i < transform.childCount; i++)
            points.Add(transform.GetChild(i).gameObject);

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

    void Update()
    {
        //if (Input.GetMouseButtonDown(1))
        //{
            var uu = UtilsControl.GetMousePoint();
            var h = MainStates.instance.lastAllySelected == null ? MainStates.instance.mainPlayer.Position : MainStates.instance.lastAllySelected.Position;
            lastPath = PositionSetter.instance.GetPath(h, uu);
            
            //
            for (int i = 0; i < points.Count; i++)
            {
                points[i].SetActive(false);
            }
            for (int i = 0;  i < lastPath.Count; i++)
            {
                points[i].SetActive(true);
                if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
                {
                    points[i].transform.position = new Vector3(
                        PositionSetter.instance.floors[lastPath[i].Item1, lastPath[i].Item2].transform.position.x,
                        PositionSetter.instance.floors[lastPath[i].Item1, lastPath[i].Item2].transform.position.y,
                        points[i].transform.position.z);
                }
                else
                {
                    points[i].transform.position = new Vector3(
                        PositionSetter.instance.floors[lastPath[i].Item1, lastPath[i].Item2].transform.position.x,
                        points[i].transform.position.y,
                        PositionSetter.instance.floors[lastPath[i].Item1, lastPath[i].Item2].transform.position.z
                        );
                }
            }
            
        //}
    }
    
    
}
