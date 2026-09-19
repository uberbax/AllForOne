using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoDismantle : MonoBehaviour
{
    private RObj mon;
    ObjHolder holder;
    public GBind bind;
    private Button btn;

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

        for (int i = 0; i < mon.dbObj.upgradeCost.Count; i++)
        {
            var ss = (i == 0 ? "" : i.ToString());
            bind.GetImage("icon" + ss)?.gameObject.SetActive(true);
            bind.GetText("price"  + ss)?.gameObject.SetActive(true);
            bind.GetImage("icon" + ss).sprite = ResourceHolder.instance.GetIcon(mon.dbObj.upgradeCost[i].Key);
            bind.GetText("price" + ss).text = mon.dbObj.upgradeCost[i].Value.ToString();
        }
        
    }

    public void DoDismantle()
    {
        PopupoManager.instance.ShowRewards(new List<Bon>(), new List<RObj>{mon});
    }


}
