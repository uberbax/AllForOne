using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoCountSell : MonoBehaviour
{
    private RObj mon;
    ObjHolder holder;
    public Button increase;
    public Button decrease;

    public TextMeshProUGUI countSell;
    
    private void OnEnable()
    {
        holder = GetComponentInParent<ObjHolder>();
        mon = holder.obj;
        
        increase.onClick.RemoveAllListeners();
        decrease.onClick.RemoveAllListeners();
        increase.onClick.AddListener(() => DoIncrease());
        decrease.onClick.AddListener(() => DoDecrease());
        
    }

    public void DoIncrease()
    {
        var cnt_sell = mon.GetPar("count_sell");
        var amount = mon.GetPar("amount");
        if (cnt_sell >= amount)
        {
            return;
        }
        mon.ChangePar("count_sell", 1);
    }

    public void DoDecrease()
    {
        var cnt_sell = mon.GetPar("count_sell");
        if (cnt_sell == 1) return;
        mon.ChangePar("count_sell", -1);
    }

    private void Update()
    {
        if (mon.RID != holder.obj.RID)
        {
            mon = holder.obj;
        }
        
        var cnt_sell = mon.GetPar("count_sell");
        if (cnt_sell == 0)
        {
            mon.SetPar("count_sell", 1);
            cnt_sell = 1;
        }
        var amount = mon.GetPar("amount");
        if (cnt_sell > amount)
        {
            mon.SetPar("count_sell", amount);
            cnt_sell = amount;
        }
        
        countSell.text = cnt_sell.ToString();
    }
}
