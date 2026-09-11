using System.Collections.Generic;
using UnityEngine;

public class MonthlyGift : MonoBehaviour
{
    public GameObject monthlyGift;

    public static MonthlyGift instance;
    
    public List<GameObject> cant = new List<GameObject>();

    void Awake()
    {
        instance = this;
        EventManager.SUB("first_day", NewDay);
        EventManager.SUB("new_day", NewDay);
    }

    private void FirstDay(ArgPass obj)
    {

    }

    private void NewDay(ArgPass obj)
    {
        ModelStatistics.instance.SetStatValue("monthly_gift_daily_open", 0);
        ModelStatistics.instance.IncreaseStatValue("monthly_available", 1); 
        //daily quest add
        
        var a = MainStates.instance.playerData.playerTasks.FindAll(x => x.id.IndexOf("daily_") >= 0);
        var b= a[Random.Range(0, a.Count)];
        b.started = 1;
        
    }

    public void MonthlyLogic()
    {
        //not in battle, not level_up ?, level >= 5, and new monthly is available ?
        //region monthly

        bool check = true;
        foreach (var v in cant)
        {
            if (v.activeSelf) check = false;
        }
        
        if (!check) return;
        
        var monthlyAvail = ModelStatistics.instance.GetStatValue("monthly_available");
        if (monthlyAvail < 1) return;
        
        if (monthlyAvail < 1)
        {
            monthlyAvail = 1;
            ModelStatistics.instance.IncreaseStatValue("monthly_available", 1);
        }
        
        //was it opened today ?
        var dailyOpen = ModelStatistics.instance.GetStatValue("monthly_gift_daily_open");        
        if (dailyOpen >= 1) return;
        
        var batlStat = ModelStatistics.instance.GetStatValue("battle");
        if (batlStat == 2) return;

        var lvlStat = MainStates.instance.mainPlayer.GetPar("level");
        if (lvlStat < 5) return;

        var has = AnyMonthlyNotTaken();
        if (!has) return;
        
        monthlyGift.SetActive(true);

    }

    public void DailyLogic()
    {
        
    }
    
    public bool AnyMonthlyNotTaken()
    {
          var monthlyAvail = ModelStatistics.instance.GetStatValue("monthly_available"); 
          //check if any is not taken
          bool res = false;
          for (int i = 0; i < monthlyAvail; i++)
          {
              var f0 = ModelStatistics.instance.GetStatValue("monthly_taken" + i.ToString());
              if (f0 < 1) res = true;
          }

          return res;
    }
    
    // Update is called once per frame
    void Update()
    {
        DailyLogic();
        MonthlyLogic();
    }
}
