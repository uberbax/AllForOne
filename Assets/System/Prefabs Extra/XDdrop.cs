using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XDdrop : ComponentBehavior
{
    RObj mon;

    private bool withHit = false;

    public RObj deathFrom;
    internal void Bind(RObj owner) => mon = owner;

    private bool EnsureBound()
    {
        if (mon != null) return true;
        var holder = GetComponentInParent<ObjHolder>();
        if (holder != null) Bind(holder.obj);
        return mon != null;
    }

    void Start() => EnsureBound();
    public void Activate()
    {
        if (!EnsureBound()) return;
        var mm = mon;
        string dropTable = withHit ? mm.dbObj.dropPerHit : mm.dbObj.drop;
        if (string.IsNullOrEmpty(dropTable)) return;
        //cant drop from own uniyts
        if (mm.tags.Contains("player")) return;
        
        List<Bon> aa = ModelSet.GetMeItemsBon(dropTable);
        
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

    public void Changed(float dltHp)
    {
        if (!(dltHp < 0) || !EnsureBound() || string.IsNullOrEmpty(mon.dbObj.dropPerHit)) return;
        bool previousWithHit = withHit;
        withHit = true;
        deathFrom = mon.lastDmgFrom;
        try
        {
            Activate();
        }
        finally
        {
            withHit = previousWithHit;
        }
    }
}
