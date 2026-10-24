using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDtake : ComponentBehavior
{
    public RObj mon;

    private bool done = false;
    public float dstTake = 1;
    //dst
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    public void AfterSet(string par)
    {
        if (pars.ContainsKey("dst"))
            dstTake = float.Parse(pars["dst"], CultureInfo.InvariantCulture);
    }
    
    void Update()
    {
        var a = MainStates.instance.lastAllySelected == null ? MainStates.instance.all["main_player"] :  MainStates.instance.lastAllySelected;
        
        var rr = MainStates.instance.GetDistance(mon, a, out float tt);
        if (!done && rr <= dstTake)
        {
            done = true;
            
            SoundManager.instance.PlayAny("pickup");

            PickupNotificationStack.Show(mon.dbObj.ID,
                ConfigLoader.Instance.GetMeLocale(mon.dbObj.ID), mon.GetPar("amount"),
                ResourceHolder.instance.GetIcon(mon.dbObj.ID));
            //we change pars runtime
            MainStates.instance.AddItem(a, mon);

            if (MainStates.anyPickAdd != null)
            {
                MainStates.instance.AddItem(a, MainStates.anyPickAdd.Key, MainStates.anyPickAdd.Value);
            }
            
            a.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("pickup");

            mon.Destroy();

        }

    }
}
