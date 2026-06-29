using System;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public string winTag = "wave";
    public string loseTag = "my_side";
    public bool startDo = false;
    public static string reqTag = "";
    
    public static BattleController instance;
    
    private void Awake()
    {
        instance = this;
        EventManager.SUB("battle_start", (x) =>
        {
            startDo = true;
        });
    }

    public void Clean()
    {
        for (int i = MainStates.instance.trashRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(MainStates.instance.trashRoot.transform.GetChild(i).gameObject);
        }
    }
    
    private void Update()
    {
        if (!startDo) return;

        int cntMy = 0;
        int cntEnemy = 0;

        foreach (var v in MainStates.instance.combats)
        {
            if (reqTag != "" && !v.META_TAGS.Contains(reqTag)) continue; 
            if (v.META_TAGS.Contains(winTag) && v.GetPar("health") >= 0) cntEnemy++;
            if (v.META_TAGS.Contains(loseTag) && v.GetPar("health") >= 0) cntMy++;
        }

        int d = 0;
        for (int i = 0; i < MainStates.instance.curSp; i++)
        {
            if (!MainStates.instance.spawners[i].IsDone) d++;
        }
        //
        //check and do
        if (cntEnemy + d == 0)
        {
            startDo = false;
            ModelStatistics.instance.IncreaseStatValue("finish_" + MainStates.instance.lastBattle, 1);
            MainStates.instance.lastBattleResult = 0;
            FunctionTimer.Create(() => { EventManager.INV("battle_ended", new ArgPass { num = 0 }); }, 1);
        }
        else if (cntMy == 0)
        {
            startDo = false;
            MainStates.instance.lastBattleResult = 1;
            FunctionTimer.Create(() => { EventManager.INV("battle_ended", new ArgPass { num = 1 }); }, 1);
        }
        
    }
}
