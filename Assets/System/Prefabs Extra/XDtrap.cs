using System;
using System.Collections.Generic;
using UnityEngine;

public class XDtrap : ComponentBehavior
{
    private Dictionary<RObj, float> dmgTimes = new();

    private RObj mon;
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var g = other.GetComponentInParent<ObjHolder>();
        if (g != null && g.obj.it == ItemType.monster)
        {
            Do(g.obj);
        }
    }

    private void Update()
    {
        foreach (var v in MainStates.instance.combats)
        {
            var f = MainStates.instance.GetDistance(v, mon, out float dd);
            if (f < 0.5f || dd < 0.5f)
            {
                Do(v);
            }

        }
    }

    public void Do(RObj v)
    {
         if (!dmgTimes.ContainsKey(v) || dmgTimes[v] < TimeManager.instance.tm - 1)
         {
             if (!dmgTimes.ContainsKey(v)) dmgTimes.Add(v, TimeManager.instance.tm);
             dmgTimes[v] = TimeManager.instance.tm;
             MainStates.instance.DealDamage(v, mon.actSkills[0]);
         }        
    }
}
