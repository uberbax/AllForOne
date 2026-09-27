using System;
using System.Collections.Generic;
using UnityEngine;

public class XDcom_effect : ComponentBehavior
{
    private RObj mon;
    private void Start()
    {
        mon = transform.GetComponentInParent<ObjHolder>().obj;
        var t0 = transform.Find("legs");
        var t1 = mon.visMain.transform.Find("legs");

        if (t0 != null)
        {
            transform.position = t1.position + (transform.position - t0.position);
        }
    }
    
}
