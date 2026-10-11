using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class XDrand_move : ComponentBehavior
{
    // Start is called before the first frame update
    public RObj mon;

    private float t = 1;
    
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        t = Random.Range(2, 5f);
    }

    private void Update()
    {
        t -= Time.deltaTime;
        if (t <= 0)
        {
            t = Random.Range(2, 5f);
            var g = Random.Range(0, PositionSetter.tupleDltFull.Count);


            if (ConfigLoader.GetMetaParamValue("mode_hex") > 0)
            {
                 var jj = PositionSetter.instance.floors[(int)mon.ref_pos_x, (int)mon.ref_pos_y];
                 var ee = MainStates.RandomPermutation(jj.Dirs.Count);
                 for (int i = 0; i < ee.Length; i++)
                 {
                     var c = jj.Dirs[ee[i]];
                     if (c == null) continue;
                     var hh = UtilsControl.IsPointOnNavMesh(c.transform.position);
                     if (hh)
                     {
                         mon.ref_pos_x = (int)c.x;
                         mon.ref_pos_y = (int)c.y;
                         mon.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("walk");
                         UtilsControl.Instance.MoveTo(mon.main.transform, 1, c.transform.position,
                             () => { mon.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("idle"); }, null,
                             useRight: false);
                     }
                 }
            }
            else
            {
                var pnt = transform.position + new Vector3(PositionSetter.tupleDltFull[g].Item1,
                    PositionSetter.tupleDltFull[g].Item2, 0);
                var hh = UtilsControl.IsPointOnNavMesh(pnt);
                if (hh)
                {
                    mon.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("walk");
                    UtilsControl.Instance.MoveTo(mon.main.transform, 1, pnt,
                        () => { mon.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("idle"); }, null,
                        useRight: false);
                }

            }


        }
    }
}
