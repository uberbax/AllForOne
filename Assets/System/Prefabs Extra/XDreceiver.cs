using System;
using System.Collections.Generic;
using UnityEngine;

public class XDreceiver : ComponentBehavior
{
    private RObj mon;
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //
        var hh = other.GetComponent<XDpick>();
        if (hh != null)
        {
            //MainStates.instance.AddItems(new List<RObj>{hh.mon});
            MainStates.instance.AddItems(new List<Bon>{ new Bon{Key = hh.mon.dbObj.ID, Value = 1}});
            
            Destroy(hh.mon.main);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        //
        var hh = other.GetComponent<XDpick>();
        if (hh != null)
        {
            //MainStates.instance.AddItems(new List<RObj>{hh.mon});
            MainStates.instance.AddItems(new List<Bon>{ new Bon{Key = hh.mon.dbObj.ID, Value = 1}});
            
            Destroy(hh.mon.main);
        }
    }
}
