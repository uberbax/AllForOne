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
                if (a.it == ItemType.item && ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.randomize_drop_stats) > 0)
                {
                    MainStates.instance.RandomizeItemStats(a);
                }

                a.AddMeta("drop");
                a.main.transform.position = transform.position;
                a.main.transform.localScale *= ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.drop_scale);
                a.main.name += "LOOT";
                //ok scale ?
                var gb = a.visMain.GetComponent<SpriteRenderer>();
                if (gb != null)
                {
                    float width = gb.bounds.size.x;

                    if (width > 0f)
                    {
                        gb.transform.localScale *= (1f / width) * 0.5f;
                    }
                    /*
                    Vector2 size = gb.bounds.size;
                    if (size.x > 1)
                        a.main.transform.localScale /= size.x;
                        */
                }

                if (MainStates.isCameraFace)
                {
                    a.visMain.transform.forward = -Camera.main.transform.forward;
                }

                if (BattleController.instance.startDo || DungeonController.instance.inDungeon || 
                    MainStates.instance.inBattle)
                {
                    a.main.transform.parent = MainStates.instance.trashRoot;
                }
                

                if (mon.GetPar("drop_pick") > 0)
                {
                    a.AddViz("pick");
                    a.AddViz("shiny");
                    a.visuals["pick"].GetComponent<XDpick>().Pick(a, deathFrom);
                }
                else
                    UtilsControl.Instance.StartRandomDrop(a.main.transform, a, () =>
                    {
                        var beam = LootRarityBeam.Attach(a);
                        a.AddViz("take");
                        a.AddViz("shiny");
                        if (beam)
                        { 
                            UtilsControl.CalculateLayer(beam.gameObject);
                        }
                        if (MainStates.isCameraFace && beam)
                        {
                            beam.transform.forward = Camera.main.transform.forward;
                        }
                    }, mm.main.transform);
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
