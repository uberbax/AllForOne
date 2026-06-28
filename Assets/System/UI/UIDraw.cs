using System;
using System.Collections.Generic;
using UnityEngine;

public class UIDraw : MonoBehaviour
{
    public string whatDraw = "itm_draw1";
    public UIfiller filler;

    private void OnEnable()
    {
        filler.selfReward = new List<Bon>();
        filler.OnEnable();
    }

    void Start()
    {
        EventManager.SUB(whatDraw, (x) =>
        {
            var res = ModelSet.GetMeItemsBon(x.what, x.num);
            //we can prepare res if its shards
            
            filler.selfReward = res;
            filler.OnEnable();
            MainStates.instance.AddItems(res);
        });
    }


}
