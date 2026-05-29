using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDloot : ComponentBehavior
{
    public RObj mon;

    private bool done = false;

    private float dstTake = 1;
    
    public List<Bon> price;

    private bool opened = false;
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    void OnMouseUp()
    {
        Debug.Log("BOM");
        var a = MainStates.instance.lastAllySelected == null ? MainStates.instance.all["main_player"] :  MainStates.instance.lastAllySelected;

        var rr = MainStates.instance.GetDistance(mon, a, out float tt);
        if (rr <= 1)
        {
            if (!opened && price.Count > 0)
            {
                var bb = MainStates.instance.UI_dynamikPrice.GetComponent<Buyable>();
                bb.SetParams(true, price, "chest", "open_chest", () => Open(), true);
                bb.gameObject.SetActive(true);
            }
            else
            {
                Open();
            }

        }
        

    }

    public void Open()
    {
        opened = true;
         if (mon.HasVis("animator"))
         {
             mon.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("open");
         }
         MainStates.instance.curLoot = mon;
         MainStates.instance.UI_second.SetActive(true);
         SoundManager.instance.PlayAny("chest_open");
    }
}
