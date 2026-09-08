using System;
using System.Collections.Generic;
using System.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UnoItem : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public CanvasGroup cg;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI amountText;
    public Image  icon;

    public GameObject bgDisabled;
    public GameObject checkTaken;
    
    public List<GameObject> readyTaken;
    //bg normal ?

    public string curState = "";

    public Bon itm = new Bon();
    //bgfocus3 - RED DAY
    //
    //state: ready, not_available, taken
    public void Fill(int dayNum, int amount, string item, string state, int num)
    {
        dayText.text = "Day " + dayNum.ToString();
        icon.sprite = ResourceHolder.instance.GetIcon(item);
        amountText.text = amount.ToString();
        curState = state;
        this.num = num;
        itm = new Bon{Key = item, Value =  amount};
        
        if (btn == null) btn = GetComponent<Button>();
        
        if (state == "ready")
        {
            foreach (var v in readyTaken) v.SetActive(true);
            btn.interactable = true;
        }
        else if (state == "taken")
        {
            cg.alpha = 0.5f;
            checkTaken.SetActive(true);
            foreach (var v in readyTaken) v.SetActive(false);
            btn.interactable = false;
        }
        else if (state == "not_available")
        {
            btn.interactable = false;
        }
    }

    public bool monthly = false;
    public Transform holder;

    public Button btn;
    private int num = 0;

    private void Start()
    {
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                curState = "taken";
                ModelStatistics.instance.SetStatValue("monthly_taken" + num, 1);
                MainStates.instance.AddItems(new List<Bon>{itm});
                
                var ii = DatabaseAll.instance.CreateItem(itm.Key, itm.Value);
                EventManager.INV("show_item", new ArgPass{who = ii});
                
                var h = transform.parent.GetComponentInParent<UnoItem>();
                h.FillMonthly();
            });
        }
    }

    public void OnEnable()
    {
        if (btn == null)
            btn = gameObject.GetComponent<Button>();
        
        if (monthly)
            FillMonthly();
    }

    public void FillMonthly()
    {
        
        var p = ModelStatistics.instance.GetStatValue("monthly_available");
        p = 5;
        
        List<ElTasko> tasks = new List<ElTasko>();
        
        foreach (var v in DatabaseAll.instance.allTasks.Values)
        {
            if (v.category == ElTasko.Category.monthly)
                tasks.Add(v);
        }

        for (int i = 0; i < holder.childCount; i++)
        {
            var gg = holder.GetChild(i).GetComponent<UnoItem>();
            var f0 = ModelStatistics.instance.GetStatValue("monthly_taken" + i.ToString());
            gg.Fill(i, tasks[i].rewards[0].Value, tasks[i].rewards[0].Key, f0 > 0 ? "taken" : i < p? "ready" : "not_available", i);
        }
        
    }
}
