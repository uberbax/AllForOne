using System;
using System.Collections.Generic;
using Flexalon;
using UnityEngine;
using UnityEngine.UI;

public class GoUse: MonoBehaviour
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
        btn.onClick.AddListener(DoUse);
    }
    private void OnEnable()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        bind = GetComponent<GBind>();
        btn = GetComponent<Button>();
        
        /*
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
        */

        if (mon.dbObj.useSkill == "")
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

    public void DoUse()
    {
        SkillExecutor.instance.ExecuteSkill(MainStates.instance.mainPlayer, mon.dbObj.useSkill, null);
        
        if (parentObj != null)
            parentObj.SetActive(false);
        //gameObject.SetActive(false);
        MainStates.instance.DelItems(new List<Bon>{new Bon{Key = mon.dbObj.ID, Value = 1}});
        
        UIfiller.GlobalRefresh();
    }


}
