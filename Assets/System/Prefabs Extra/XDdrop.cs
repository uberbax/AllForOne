using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XDdrop : ComponentBehavior
{
    RObj mon;
    float prevHp = -1;

    private bool withHit = false;

    public RObj deathFrom;
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }
    public void Activate()
    {
        var mm = GetComponentInParent<ObjHolder>().obj;

        if (mm.dbObj.drop == "") return;
        //cant drop from own uniyts
        if (mm.tags.Contains("player")) return;
        
        List<Bon> aa = new List<Bon>();
        if (withHit) aa = ModelSet.GetMeItemsBon(mm.dbObj.dropPerHit);
        else aa = ModelSet.GetMeItemsBon(mm.dbObj.drop);
        
        //var a = DatabaseAll.instance.CreateItem("apple", 1, true, true);
        //drop here

        foreach (var v in aa)
        {
            if (ResourceHolder.instance.items.ContainsKey(v.Key))
            {
                var a = DatabaseAll.instance.CreateItem(v.Key, v.Value, true, true);
                if (a.it == ItemType.item && ConfigLoader.GetMetaParamValue("randomize_drop_stats") > 0)
                {
                    MainStates.instance.RandomizeItemStats(a);
                }

                a.AddMeta("drop");
                a.main.transform.position = transform.position;
                a.main.transform.localScale *= ConfigLoader.GetMetaParamValue("drop_scale");
                a.main.name += "LOOT";

                if (mon.GetPar("drop_pick") > 0)
                {
                    a.AddViz("pick");
                    a.visuals["pick"].GetComponent<XDpick>().Pick(a, deathFrom);
                }
                else
                    UtilsControl.Instance.StartRandomDrop(a.main.transform, a, () => { a.AddViz("take"); }, mm.main.transform);
            }
            else
            {
                var a = Instantiate(ResourceHolder.instance.skillsWorld[v.Key]);
                a.transform.position = transform.position;
            }

        }
    }

    private void Update()
    {
        var hh = mon.GetPar("health");
        if (prevHp == -1) prevHp = hh;

        if (hh < prevHp && mon.dbObj.dropPerHit != "")
        {
            withHit = true;
            deathFrom = mon.lastDmgFrom;
            Activate();
            withHit = false;
        }
        
        prevHp = hh;
    }
}
