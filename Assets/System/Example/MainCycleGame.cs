using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MainCycleGame : MonoBehaviour
{

    private void Awake()
    {
        EventManager.SUB("PARSE_ENDED", B);
    }

    private void B(ArgPass obj)
    {
        Debug.Log("haha");

        
        var main = new RObj("warlord", 1, 1, true, Vector3.zero,true, ItemType.monster, "main_player");
        main.AddViz("move");
        main.AddViz("shoot#val:0.5");
        main.AddViz("coll#val:0.5");
        main.AddViz("hp");
        main.AddViz("info");
        main.AddViz("combat#main:0");
        main.AddViz("animator#pr:1");
        
        /*
        var enm = new RObj("gunner", 1, 1, true, Vector3.zero - new Vector3(2, 0, 0),true, ItemType.monster, isEnemy:true);
        enm.AddViz("hp");
        enm.AddViz("coll#0.5");
        enm.AddViz("dmg_track");
        enm.AddViz("flash");
        enm.AddViz("death");
        enm.AddViz("drop");
        enm.AddViz("combat");
        enm.AddViz("animator#pr:1");
        enm.AddViz("realcol");
        */
        
        
        for (int i = 0; i < 1; i++)
        {
            var enm1 = new RObj("wolf", 1, 1, true, Vector3.zero - new Vector3(2, 0, 0) + 
                                                    new Vector3(Random.Range(-0.2f, 0.2f),Random.Range(-0.2f, 0.2f), 0),true, ItemType.monster, isEnemy:true);
            enm1.AddViz("hp");
            enm1.AddViz("coll#val:0.5");
            enm1.AddViz("dmg_track");
            enm1.AddViz("flash");
            enm1.AddViz("death");
            enm1.AddViz("drop");
            enm1.AddViz("combat");
            enm1.AddViz("animator#pr:1");
            enm1.AddViz("realcol");
            enm1.AddViz("changemat");  
            
            //enm1.AddViz("invscale");
            //enm1.AddViz("animator");            

        }
        

        ModelStatistics.instance.UpdateAllTasks();
        
        //adding squad
        for (int i = 0; i < 4; i++)
        {
            var enm1 = new RObj("wolf", 1, 1, false, Vector3.zero,false, ItemType.monster, isEnemy:true);
            MainStates.instance.AddItems(new List<RObj>{enm1});
        }
        
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
