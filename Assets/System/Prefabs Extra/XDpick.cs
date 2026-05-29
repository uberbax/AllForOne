using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDpick : ComponentBehavior
{
    public RObj mon;
    public RObj owner;

    private bool done = false;

    private float dstTake = 1;

    public bool picked = false;
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    public void Pick(RObj mon, RObj a)
    {
         picked = true;

         var t0 = GetComponent<Collider2D>();
         var t1 = GetComponent<Collider>();
         if (t0) t0.enabled = false;
         if (t1) t1.enabled = false;
         
         
         a.attachables.Add(mon.main.transform);
         mon.owner = a;
         UtilsControl.Instance.MoveTo(mon.main.transform, 10, a.Position + new Vector3(0, 0.5f, 0), ()=>
         {
             mon.main.transform.parent = a.main.transform;
         }, null);        
    }
    
    void OnMouseUp()
    {
        Debug.Log("BOM");
        var a = MainStates.instance.lastAllySelected == null ? MainStates.instance.all["main_player"] :  MainStates.instance.lastAllySelected;

        var rr = MainStates.instance.GetDistance(mon, a, out float dd);

            if (rr <= 1 && !picked)
            {
                Pick(mon, a);
            }
            
    }

    public void EnableCollider()
    {
        var t0 = GetComponent<Collider2D>();
        var t1 = GetComponent<Collider>();
        if (t0) t0.enabled = true;
        if (t1) t1.enabled = true;
    }
    private void Update()
    {
        if (owner == null) return;
        if (owner.GetPar("health") <= 0)
        {
            owner = null;
            mon.main.transform.parent = null;
            UtilsControl.Instance.MoveTo(mon.main.transform, 10, mon.Position - new Vector3(0, 0.5f, 0), null, null);
        }
    }
}
