using System;
using System.Collections;
using System.Collections.Generic;
using SpriteFracture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDdeath : ComponentBehavior
{
    public static bool autoAddExp = true;
    // Start is called before the first frame update
    public RObj mon;

    // Update is called once per frame
    private bool done = false;

    public static float fadeAfter = 0;
    
    public static bool contributeEach = false;
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    public void DoDeath()
    {
        EventManager.INV("unit_died", new ArgPass
        {
            who = mon,
            who2 = mon.lastDmgFrom,
            what = mon.dbObj != null ? mon.dbObj.ID : ""
        });

        mon.RemoveViz("hp");
        mon.RemoveViz("coll");
        mon.RemoveViz("realcol");
        mon.RemoveViz("shadow");
        mon.RemoveViz("combat");
        var g = mon.visMain.transform.Find("shadow");
        if (g != null) Destroy(g.gameObject);
        g = mon.visMain.transform.Find("shadow (1)");
        if (g != null) Destroy(g.gameObject);
        
        //here goes the drop
        if (contributeEach)
        {
            if (!mon.tags.Contains("player"))
                MainStates.instance.HandleMonsterKilled(mon);
        }
        
        if (mon.visuals.ContainsKey("drop"))
        {
            mon.visuals["drop"].GetComponent<XDdrop>().deathFrom = mon.lastDmgFrom;
            mon.visuals["drop"].SendMessage("Activate", SendMessageOptions.DontRequireReceiver);
        }

        if (mon.dbObj.onDeath != "")
        {
            var h0 = DatabaseAll.instance.CreateProjectile(mon, mon.dbObj.onDeath, Vector3.zero, false, false);
            SkillExecutor.instance.ExecuteSkill(mon, h0);
        }

        
            foreach (var v in mon.buffs)
            {
                if (v.dbObj.onDeath == "") continue;
                var h1 = DatabaseAll.instance.CreateProjectile(mon, v.dbObj.onDeath, Vector3.zero, false, false);
                SkillExecutor.instance.ExecuteSkill(mon, h1);
            }
        
        
        //addexp ?
        if (!mon.tags.Contains("player"))
        {
            ModelStatistics.instance.IncreaseStatValue("kill_" + mon.dbObj.ID, 1);
            ModelStatistics.instance.IncreaseStatValue("kill_any", 1);
        }
        else
        {
            ModelStatistics.instance.IncreaseStatValue("lost_any", 1);
        }

        if (mon.decreaseStatOnDeath != "")
            ModelStatistics.instance.IncreaseStatValue(mon.decreaseStatOnDeath, -1);
        
        if (autoAddExp)
            MainStates.instance.AddItem(MainStates.instance.all["main_player"], "exp", 50);

        bool frac = false;
        if (mon.visMain.GetComponent<SpriteFracturer2D>() != null)
        {
            frac = true;
            StartCoroutine(mon.visMain.GetComponent<SpriteFracturer2D>().Fracture());
            MainStates.instance.all.Remove(mon.RID);
            
            FunctionTimer.Create(() =>
            {
                Destroy(mon.main);
                MainStates.instance.all.Remove(mon.RID);
            }, 2);
        }
        else
        {
            
            FunctionTimer.Create(() =>
                {
                    //?
                    var gh = mon.visuals["vis_main"].transform;
                    if (gh.GetComponent<SpriteRenderer>() == null)
                        gh = gh.GetChild(0);
                    
                    UtilsControl.Instance.ApplyCurve(gh,
                        AnimationCurve.Linear(0, 1, 1, 0), UtilsControl.CurveType.Color,
                        () =>
                        {
                            Destroy(mon.main);
                            MainStates.instance.all.Remove(mon.RID);
                        }, 1, 1, 1, 0, Color.clear);
                },
                fadeAfter
            );
        }

        if (ConfigLoader.GetMetaParamValue("blood_death") > 0 && !frac)
        {
            string ss = "blood";
            Vector3 dlt = Vector3.zero;
            if (ConfigLoader.GetMetaParamValue("coord_mode_xy") < 1)
            {
                ss = "blood3D";
                dlt = new Vector3(0, 0.1f, 0);
            }
            var gz = Instantiate(ResourceHolder.instance.XD[ss], MainStates.instance.trashRoot);
            gz.transform.position = mon.main.transform.position - dlt;
        }
        
        //attachables
        foreach (var v in mon.attachables)
        {
            v.transform.position -= new Vector3(0, 0.5f, 0);
            v.parent = null;
            v.GetComponentInChildren<XDpick>().picked = false;
            v.GetComponentInChildren<XDpick>().EnableCollider();
                                            
            var kk = v.GetComponent<ObjHolder>();
            kk.enabled = true;
            kk.obj.Position = v.position;
            kk.obj.AdjustPosition();
            var tt = v.GetComponent<MoveDir>();
            if (tt != null) tt.enabled = true;
        }

    }

    void Update()
    {
        var h = mon.GetPar("health");
        if (!done && h <= 0)
        {
            done = true;
            mon.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("death");
            DoDeath();
        }
    }
}
