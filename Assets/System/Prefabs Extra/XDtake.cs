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
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    public void Set(string par)
    {
        var t = float.Parse(par, CultureInfo.InvariantCulture);
        dstTake = t;
    }
    void Update()
    {
        var a = MainStates.instance.lastAllySelected == null ? MainStates.instance.all["main_player"] :  MainStates.instance.lastAllySelected;
        
        var rr = MainStates.instance.GetDistance(mon, a, out float tt);
        if (rr <= dstTake)
        {

            UtilsControl.Instance.FlyTextUI(mon.main.transform, ConfigLoader.Instance.GetMeLocale(mon.dbObj.ID) + " x" + mon.GetPar("amount"), Color.white,
                null, doCam: true);          
            //we change pars runtime
            MainStates.instance.AddItem(a, mon);

            mon.Destroy();

        }

    }
}
