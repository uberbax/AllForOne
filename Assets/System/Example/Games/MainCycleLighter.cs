using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MainCycleLighter : MonoBehaviour
{
    public List<Dops> spawns = new List<Dops>();
    public List<Dops> except = new List<Dops>();
    
    private void Awake()
    {
        EventManager.SUB("PARSE_ENDED", B);
    }

    private void B(ArgPass obj)
    {
        Debug.Log("haha");

        var main = new RObj("warlord", 1, 1, true, Vector3.zero,true, ItemType.monster, "main_player");
        main.AddViz("move#3");
        main.AddViz("shadow");
        
        //main.AddViz("shoot#0.5");
        //main.AddViz("coll#0.5");
        //main.AddViz("hp");
        //main.AddViz("info");
        
        var bow = MainStates.instance.AddItem(main, "bow", 1);
        MainStates.instance.AddItem(main, "arrow", 2);
        MainStates.instance.Equip(main, bow);
        main.AddViz("weapon#proj:arrow");
        main.AddViz("animator");
        
        
        ModelStatistics.instance.UpdateAllTasks();
        
        UtilsControl.Instance.SpawnArea(MainStates.instance.root, spawns, except, 50, MainStates.instance.minT, MainStates.instance.maxT);
        

        
    }

    private void Update()
    {
        if (Input.GetKeyDown("i"))
        {
            MainStates.instance.UI_inventory.SetActive(true);
        }
        
        if (Input.GetKeyDown("u"))
        {
            MainStates.instance.UI_skilChose.SetActive(true);
        }
        
    }
}
