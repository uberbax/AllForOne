using System;
using System.Collections.Generic;
using Flexalon;
using UnityEngine;
using UnityEngine.UI;

public class GoDismantle : MonoBehaviour
{
    private RObj mon;
    ObjHolder holder;
    public GBind bind;
    private Button btn;

    public GameObject parentObj;

    //
    List<Bon> savedRes = new List<Bon>();
    
    void Start()
    {
        btn.onClick.AddListener(DoDismantle);
    }
    private void OnEnable()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        bind = GetComponent<GBind>();
        btn = GetComponent<Button>();
        
        for (int i = 0; i < 3; i++)
        {
            var ss = (i == 0 ? "" : i.ToString());
            bind.GetImage("icon" + ss)?.gameObject.SetActive(false);
            bind.GetText("price"  + ss)?.gameObject.SetActive(false);
        }

        var lvl = mon.GetPar("level");
        savedRes = new List<Bon>();
        for (int i = 0; i < mon.dbObj.upgradeCost.Count; i++)
        {
            var ss = (i == 0 ? "" : i.ToString());
            bind.GetImage("icon" + ss)?.gameObject.SetActive(true);
            bind.GetText("price"  + ss)?.gameObject.SetActive(true);
            bind.GetImage("icon" + ss).sprite = ResourceHolder.instance.GetIcon(mon.dbObj.upgradeCost[i].Key);
            //level multiply
            bind.GetText("price" + ss).text = (mon.dbObj.upgradeCost[i].Value * lvl).ToString();
            
            savedRes.Add(new Bon{Key = mon.dbObj.upgradeCost[i].Key, Value = (int)(mon.dbObj.upgradeCost[i].Value * lvl)});
        }

        if (mon.dbObj.pars["subtype"] == 100 || mon.dbObj.pars["subtype"] == MainStates.subtypes["adorn"] ||
            mon.dbObj.pars["subtype"] == MainStates.subtypes["pet"])
        {
            //transform.localScale = Vector3.zero;
            var a = transform.GetComponent<FlexalonObject>();
            a.Scale = Vector3.zero;
        }
        else
        {
            //transform.localScale = Vector3.one;
            var a = transform.GetComponent<FlexalonObject>();
            a.Scale = Vector3.one;
        }
    }

    public void DoDismantle()
    {
        PopupoManager.instance.ShowRewards(new List<Bon>(), new List<RObj>{mon}, 
            "<color=red>WARNING</color>", "Dismantle this item ?", () =>
            {
                mon.owner.inventory.Remove(mon);
                MainStates.instance.AddItems(savedRes);
                
                if (parentObj != null)
                    parentObj.SetActive(false);
                //gameObject.SetActive(false);
                UIfiller.GlobalRefresh();
            });
    }


}
