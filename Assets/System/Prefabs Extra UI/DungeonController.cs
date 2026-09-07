using System;
using System.Collections.Generic;
using LayerLab;
using TMPro;
using UnityEngine;

public class DungeonController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static DungeonController instance;
    public bool inDungeon = false;
    public int cur = 0;
    public int last = 3;
    public SampleCharacterMover mover;
    
    private List<List<Bon>> tests = new List<List<Bon>>
    {
        new List<Bon>{ new Bon{ Key = "skeleton", Value = 1, Val2 = "obj_berserk:1"} }, 
        new List<Bon>{ new Bon{ Key = "skeleton_archer", Value = 1} }, 
        new List<Bon>{ new Bon{ Key = "goblin", Value = 1} } 
    };

    public TextMeshProUGUI waveNumber;
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        EventManager.SUB("start_dungeon", B);
        EventManager.SUB("after_battle", C);
        EventManager.SUB("battle_ended", D);
    }

    private void D(ArgPass obj)
    {
        MainStates.instance.awaitUnits["second_main"] = 1;
        BattleController.instance.Clean();
        MainStates.instance.lastTargetSelected = null;
        if (cur >= last - 1)
        {
            inDungeon = false;
            return;
        }
        
        cur++;
    }

    private void C(ArgPass obj)
    {
        if (inDungeon)
        {
            if (MainStates.instance.lastBattleResult == 1)
            {
                inDungeon = false;
                EventManager.INV("after_battle", null);
                return;
            }
            
            MainStates.instance.inBattle = true;
            var mm = CreateField();
            MainStates.instance.dropTables["battle_reward"] = new List<Bon>();
            FunctionTimer.Create(
                () => EventManager.INV("battle_start", null), 0.5f);
            
            DoLittleRun(mm);
        }
    }

    [ContextMenu("TryReset")]
    public void TryReset()
    {
        MainStates.instance.all["second_main"].ResetCDs();
        MainStates.instance.mainPlayer.ResetCDs();
    }

    public List<RObj> CreateField()
    {
            List<Bon> curLevel = new List<Bon>();
            curLevel = tests[cur];
            //curLevel.Add(new Bon{Key = "skeleton_archer", Value = 1});
            
            var ee = WaveSpawner.instance.DoSpawnAnyPos(curLevel,
                "enemy", false, applyExtra:true, overridesViz:MainStates.overridesViz);
            
            MainStates.instance.curObjs["last_mon"] = ee[0];
            MainStates.instance.curObjs["last_leg"] = null;
            MainStates.instance.lastBattleTrigger = ee[0].main; 
            
            //? doesnt work
            MainStates.instance.all["second_main"].ResetCDs();
            MainStates.instance.mainPlayer.ResetCDs();

            return ee;

    }
    private void B(ArgPass obj)
    {
        //we are starting dungeon
        MainStates.instance.inBattle = true;
        //we do battle
                
        Transitioner.instance.DoFade(1, 1, () =>
        {

            inDungeon = true;
            cur = 0;
            ModelStatistics.instance.SetStatValue("battle",2); 
            EventManager.INV("battle_press", new ArgPass{what = "battle9"});
            
            var mm = CreateField();
            
            DoLittleRun(mm);
            //MainStates.instance.ApplyMonsterExtraParams(ee[0],mon);
            
                    
        }, null);
    }

    public void DoLittleRun(List<RObj> enemies)
    {
        //ss
        Vector3 sdvig = new Vector3(5, 0, 0);
        foreach (var e in enemies)
        {
            e.main.transform.position += sdvig;
        }

        mover.speed = 2;
        //
        FunctionTimer.Create(() =>
        {
            var allies = MainStates.instance.GetMines(MainStates.metaCreateLevel);
            foreach (var e in allies)
            {
                e.visuals["animator"].GetComponent<XDanimator>().SetState("walk");
            }            
        }, 0.1f);

        
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            var tmp = i;
            UtilsControl.Instance.MoveTo(enemies[i].main.transform, 2, enemies[i].main.transform.position - sdvig,
                () =>
                {
                    if (tmp == 0)
                    {
                        var allies = MainStates.instance.GetMines(MainStates.metaCreateLevel);
                        foreach (var e in allies)
                        {
                            e.visuals["animator"].GetComponent<XDanimator>().SetState("idle");
                            mover.speed = 0;
                        }   
                    }
                }, null, useRight: false);
        }
    }

    private void Update()
    {
        if (inDungeon)
        {
            waveNumber.text = "Floor " + (cur + 1) + "/" + last;
        }
        else
        {
            waveNumber.text = "";
        }
    }
}
