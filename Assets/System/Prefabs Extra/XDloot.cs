using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    
    //GLOBAL overrides
    public static bool doMagnet = false;
    
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
        if (Input.GetMouseButtonUp(0))
        {
            var d = UtilsControl.CheckClick();
            if (d != gameObject) return;
            
            Debug.Log("BOM");
            var a = MainStates.instance.lastAllySelected == null
                ? MainStates.instance.all["main_player"]
                : MainStates.instance.lastAllySelected;

            var rr = MainStates.instance.GetDistance(mon, a, out float tt);
            if (rr <= dstTake)
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


    }

    public void Open()
    {
        opened = true;
        //
        if (doMagnet)
        {
            var a = MainStates.instance.lastAllySelected == null
                ? MainStates.instance.all["main_player"]
                : MainStates.instance.lastAllySelected;
            
            var b = mon.main.AddComponent<CameraFollow>();
            b.smoothSpeed = 0.01f;
            b.look = false;
            b.smallify = true;
            b.target = a.main.transform;
            b.act = Open2;

        }
        else
        {
            Open2();
        }
        
    }
    
    public void Open2()
    {
           if (mon.HasVis("animator"))
           {
               mon.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("open");
           }
           
           if (mon.main.name.ToLower().IndexOf("chest") >= 0)
              SoundManager.instance.PlayAny("chest_open");
           else 
               SoundManager.instance.PlayAny("pickup");
           
           if (MainStates.lootTakeShowReward)
           {
               
               var ss = MainStates.instance.GetInventoryBon(mon);
               MainStates.instance.AddItems(ss);
               PopupoManager.instance.ShowRewards(ss);

               var ff = DatabaseAll.instance.CreateItem(ss[0].Key, ss[0].Value);
               EventManager.INV("show_item", new ArgPass{who = ff});
           }
           else
           {
               MainStates.instance.curLoot = mon;
               MainStates.instance.UI_second.SetActive(true);
           }
        
        
           if (MainStates.disappearLootOnTake)
           {
               Destroy(mon.main);
           }
    }
    
}

