using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDbattle : ComponentBehavior
{
    public RObj mon;

    private bool done = false;
    public float dstTake = 1;
    private string level = "LEVEL_1";
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    public void Set(string par)
    {
        var t = float.Parse(par, CultureInfo.InvariantCulture);
        dstTake = t;
    }

    public void AfterSet(string par)
    {
        if (pars.ContainsKey("level"))
            level = pars["level"];
    }
    
    void Update()
    {
        var a = MainStates.instance.lastAllySelected == null ? MainStates.instance.all["main_player"] :  MainStates.instance.lastAllySelected;
        
        var rr = MainStates.instance.GetDistance(mon, a, out float tt);
        if (rr <= dstTake && !MainStates.instance.inBattle)
        {
            MainStates.instance.inBattle = true;
            //we do battle
            EventManager.INV("battle_press", new ArgPass{what = "battle9"});
            MainStates.instance.CreateLevelAtPos(2, 30, level);
            MainStates.instance.lastBattleTrigger = mon.main;
            
            //mon.Destroy();

        }

    }
}
