using System;
using System.Collections.Generic;
using UnityEngine;

public class DungeonController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static DungeonController instance;
    public bool inDungeon = false;
    public int cur = 0;
    public int last = 3;

    private List<string> tests = new List<string> { "skeleton", "skeleton_archer", "goblin"};
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        EventManager.SUB("start_dungeon", B);
        EventManager.SUB("after_battle", C);
    }

    private void C(ArgPass obj)
    {
        if (cur >= last - 1)
        {
            inDungeon = false;
            return;
        }
        
        cur++;
        CreateField();
    }

    public void CreateField()
    {
            List<Bon> curLevel = new List<Bon>();
            curLevel.Add(new Bon{Key = tests[cur], Value = 1});
            //curLevel.Add(new Bon{Key = "skeleton_archer", Value = 1});
            
            var ee = WaveSpawner.instance.DoSpawnAnyPos(curLevel,
                "enemy", false, applyExtra:true, overridesViz:MainStates.overridesViz);
            
            MainStates.instance.curObjs["last_mon"] = ee[0];
            MainStates.instance.lastBattleTrigger = null;        
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
            
            CreateField();
                    
            //MainStates.instance.ApplyMonsterExtraParams(ee[0],mon);
            
                    
        }, null);
    }
}
