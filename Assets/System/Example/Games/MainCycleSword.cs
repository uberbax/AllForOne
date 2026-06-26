using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MainCycleSword : MonoBehaviour
{
    public Button oneIteration;
    
    public bool AlwaysMove = true;
    public Button moveSkill;
    private RObj main;
    private RObj secondMain;

    private bool inBattle = false;
    
    private void Awake()
    {
        EventManager.SUB("PARSE_ENDED", B);
        
        //give one building at start
        
        EventManager.SUB("game_start", (x) => StartGame());
        
        oneIteration.onClick.AddListener(() => StartCoroutine(MainStates.instance.OneIteration(true)));
        
        EventManager.SUB("main_move", (x) =>
        {
            PositionSetter.instance.OpenFog(x.pos);
        });
        
        EventManager.SUB("battle_start", (x) =>
        {
            inBattle = true;
        });
        EventManager.SUB("battle_press", BattleClicked);
        
        EventManager.SUB("after_battle", (x) =>
        {
            ModelStatistics.instance.SetStatValueForce("battle",1);
            Camera.main.GetComponent<CameraFollow>().target = main.main.transform;
            secondMain.Destroy();
            MainStates.instance.UI_skills.SetActive(true);
            if (MainStates.instance.lastBattleResult == 0)
            {
                Destroy(MainStates.instance.lastBattleTrigger);
                MainStates.instance.inBattle = false;
                inBattle = false;
            }
            else
            {
                MainStates.instance.inBattle = false;
                inBattle = false;
            }
        });
        
        EventManager.SUB("go_home", (x) =>
        {
            Camera.main.GetComponent<CameraFollow>().target = basePos;
        });
        
        EventManager.SUB("go_map", (x) =>
        {
            Camera.main.GetComponent<CameraFollow>().target = main.main.transform;
        });
        
    }

    public Transform battlePoint;
    public Transform playerPos;
    public Transform basePos;
    
    
    private void BattleClicked(ArgPass obj)
    {
        MainStates.instance.UI_battleSelect.SetActive(false);
        WaveSpawner.ClearExcept("sword");

        MainStates.instance.lastBattle = obj.what;
        //WaveSpawner.instance.DoSpawnAll(MainStates.instance.lastBattle);
        
        MainStates.instance.UI_squadList.SetActive(false);
        MainStates.instance.UI_skills.SetActive(false);
        
        Camera.main.GetComponent<CameraFollow>().target = battlePoint;
        MainStates.instance.mainPlayer.ResetCDs();

        //we need to create player clone basically, but with available skills
        
        secondMain = new RObj("hero", 1, 1, true, Vector3.zero,true, ItemType.monster, "second_main");
        MainStates.instance.ApplyPlayerConfigParams(secondMain);
        secondMain.AddViz("shadow");
        secondMain.AddViz("combat#no:1");
        secondMain.AddViz("hp");
        secondMain.AddViz("coll#scale:0.5");
        secondMain.AddViz("animator#pr:1");
        secondMain.AddViz("drag");
        
        secondMain.AdjustPosition();
        secondMain.AddMeta("my_side");
        secondMain.AddMeta("sword");
        
        secondMain.main.transform.position = playerPos.position;
        secondMain.Position = playerPos.position;
        secondMain.AdjustPosition();
        
        secondMain.actSkills.Clear();
        MainStates.instance.AcquireSkill(secondMain, "basic_melee");
        
        secondMain.SetScale(true);
        //MainStates.instance.mainPlayer.main.transform.position = playerPos.position;
        //MainStates.instance.mainPlayer.Position = playerPos.position;
        //MainStates.instance.mainPlayer.AdjustPosition();

    }

    public void StartGame()
    {

        
    }
    

    private void Start()
    {
        
    }

    private void B(ArgPass obj)
    {
        Debug.Log("haha");
        //
        main = new RObj("hero", 1, 1, true, Vector3.zero,true, ItemType.monster, "main_player");
        MainStates.instance.ApplyPlayerConfigParams(main);
        main.AddViz("shadow");
        main.AddViz("combat#no:1");
        main.AddViz("hp");
        main.AddViz("coll#scale:0.5");
        main.AddViz("select");
        main.AddViz("animator#pr:1");
        
        main.AdjustPosition();
        main.AddMeta("my_side");
        
            
        /*
        //alyy low hp
        //check same place ? hp is lowering ???
        var ally = new RObj("militia", 1, 1, true, new Vector3(-1,0,0),true, ItemType.monster);
        ally.AddViz("shadow");
        ally.AddViz("combat");
        ally.AddViz("hp");
        ally.AddViz("coll#val:0.5");
        ally.AddViz("animator#pr:1");
        
        ally.AdjustPosition();
        ally.AddMeta("my_side");
        
        ally.SetPar("registered_damage", 50);
        //
        
        ModelStatistics.instance.UpdateAllTasks();
     
        
        var ll = WaveSpawner.instance.DoSpawnAny(
            new List<Bon>{ 
                new Bon{Key = "insect", Value = 5},
                 new Bon{Key = "archer", Value = 5} }, "enemy", 
            MainStates.instance.minT, MainStates.instance.maxT, false, Vector3.zero, Vector3.zero );

        foreach (var v in ll)
        {
            v.AdjustPosition();
        }
        */
        
        
        MainStates.instance.UI_skills.SetActive(true);
        foreach (var v in  main.actSkills)
        {
            v.SetPar("action_req", 1);
        }
        
        FunctionTimer.Create(() =>
        {
            PositionSetter.instance.OpenFog(main.Position);
        }, 0, () => PositionSetter.instance.wallsParsed);

        //params
        MainStates.lootTakeShowReward = true;
        MainStates.disappearLootOnTake = true;
        MainStates.allowAutoIterate = false;
        MainStates.metaCreateLevel = "sword";
        BattleController.reqTag = "sword";
    }

    public void HandleAutomove()
    {
        if (AlwaysMove)
        {
            if (main.main.name.IndexOf("_move") < 0)
            {
                moveSkill.onClick.Invoke();
            }
        }

    }

    private void Update()
    {
        
        HandleAutomove();
        
        
        if (Input.GetKeyDown("i"))
        {
            var u0 = MainStates.instance.UI_inventory.activeSelf;
            var u1 = MainStates.instance.UI_charEq.activeSelf;
            
            MainStates.instance.UI_inventory.SetActive(!u0);
            MainStates.instance.UI_charEq.SetActive(!u1);
        }
        
        if (Input.GetKeyDown("h"))
        {
            var rr = ResourceHolder.instance.skillsWorld["whirl"];
            var go = Instantiate(rr, MainStates.instance.mainPlayer.main.transform);
        }

        if (inBattle)
        {
            StartCoroutine(MainStates.instance.OneIteration(false, 0.5f, "sword"));
        }

    }
}
