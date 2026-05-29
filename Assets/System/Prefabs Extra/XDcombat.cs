using System;
using System.Collections.Generic;
using UnityEngine;

public class XDcombat : ComponentBehavior
{
    public RObj mon;

    public string curTg = "chill";
    private string state = "idle";

    private string reqViz = "";
    private string reqTag = "";

    private int no = 0;
    public void AfterSet(string par)
    {
        if (pars.ContainsKey("no"))
            no = int.Parse(pars["no"]);
    }
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        EventManager.SUB("battle_start", (x) =>
        {
            if (no == 0) curTg = MainStates.instance.tgBattle;
            mon.RemoveViz("drag");
        });
        EventManager.SUB("battle_ended", (x) =>
        {
            curTg = "chill";
        });
        
        
        MainStates.instance.combats.Add(mon);
        EventManager.INV("added_combat_unit", new ArgPass());

        if (ConfigLoader.GetMetaParamValue("use_stater") > 0)
        {
            if (mon.RID != "main_player")
                mon.AddViz("stater");
        }
    }
    
    // Update is called once per frame
    private void Update()
    {
        Iteration();
    }

    private float lastIterationTm = -1;
    private float iterationTime = 0;
    
    public void Iteration(bool ignoreTag = false, bool ignoreState = false, RObj overTarget = null)
    {
        if (curTg != MainStates.instance.tgBattle && !ignoreTag) return;
        if (mon.GetPar("health") <= 0) return;
        if (pars.ContainsKey("main")) return;
        if (mon.GetPar("do_nothing") > 0) return;  
        
        if (mon.HasVis("stater") && !ignoreState)
        {
            mon.visuals["stater"].GetComponent<XDstater>().Iteration(this);
            return;
        }        
        
        var c = MainStates.instance.GetClosestEnemy(mon, out float d);
        var d1 = MainStates.instance.GetLowestDistanceSkills(mon);

        if (overTarget != null) c = overTarget;
        
        if (c == null)
        {
            return;
        }
        
        bool sucCast = false;
        bool noSight = false;
        //check skills
        for (int i = 0; i < mon.actSkills.Count; i++)
        {
            var skl = mon.actSkills[i];
            //dont execute those who req action ?
            if (skl.GetPar("action_req") > 0 && !mon.tags.Contains("enemy")) continue;
            
            if (skl.GetPar("cd") <= 0)
            {
                //can cast
                var res = SkillExecutor.instance.ExecuteSkill(mon, skl);
                if (res == ExecReso.OK) sucCast = true;
                if (res == ExecReso.NO_SIGHT) noSight = true;
            }
        }
        
        
        if ( (d > d1 || noSight) && !sucCast && mon.GetPar("no_move") < 1)
        {
            var vec = (c.Position - mon.Position).normalized;
            if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0) vec.z = 0;
            
            //get path ?
            MainStates.instance.MovePath(mon, c);
            
        }
    }


    void OnDestroy()
    {
        MainStates.instance.combats.Remove(mon);
    }
    
}
