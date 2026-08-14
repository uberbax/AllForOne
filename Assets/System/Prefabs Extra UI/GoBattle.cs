using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoBattle : MonoBehaviour
{
    private RObj mon;
    ObjHolder holder;
    private void OnEnable()
    {
        holder = GetComponentInParent<ObjHolder>();
        mon = holder.obj;
        var b = GetComponent<Button>();
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() =>
            {
                MainStates.instance.inBattle = true;
                //we do battle
                
                Transitioner.instance.DoFade(1, 1, () =>
                {
                    EventManager.INV("battle_press", new ArgPass{what = "battle9"});
                    //MainStates.instance.CreateLevelAtPos(2, 30, "LEVEL_1");
                    var ee = WaveSpawner.instance.DoSpawnAnyPos(new List<Bon>{new Bon{Key = mon.dbObj.ID, Value = 1, Val3 = (int)mon.GetPar("level")}},"enemy", false, applyExtra:true, overridesViz:MainStates.overridesViz);
                    if (mon.dbObj.pars["is_boss"] > 0)
                    {
                        MainStates.instance.curObjs["last_boss"] = ee[0];
                    }
                    
                    MainStates.instance.lastBattleTrigger = mon.main;
                    ModelStatistics.instance.SetStatValue("battle",2); 
                    holder.transform.parent.gameObject.SetActive(false);
                }, null);
            }
        );
    }


}
