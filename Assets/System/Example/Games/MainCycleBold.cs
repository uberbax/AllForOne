using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MainCycleBold : MonoBehaviour
{
    public Button oneIteration;
    
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
        var main = new RObj("hero", 1, 1, true, new Vector3(-1.16f, 0, 5.285f),true, ItemType.monster, "main_player");
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
        
        
    }
    

    private void Update()
    {
        
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
        
    }
}
