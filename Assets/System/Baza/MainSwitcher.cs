using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainSwitcher : MonoBehaviour
{
    public static MainSwitcher instance;
    public Transform mazePoint;
    public Transform battlePoint;
    public Transform playerPos;
    //initial state for MainCycleStone is manhattan


    [Header("PRESS 1: do runtime")]
    public bool doRuntime;

    
    
    
    private void Awake()
    {
        instance = this;
        
        EventManager.SUB("battle_press", BattleClicked);
    }

    private void BattleClicked(ArgPass obj)
    {
        MainStates.instance.UI_battleSelect.SetActive(false);
        WaveSpawner.ClearExcept();

        MainStates.instance.lastBattle = obj.what;
        //WaveSpawner.instance.DoSpawnAll(MainStates.instance.lastBattle);
        
        MainStates.instance.UI_squadList.SetActive(false);
        MainStates.instance.UI_skills.SetActive(false);
        
        Camera.main.GetComponent<CameraFollow>().target = battlePoint;
        MainStates.instance.mainPlayer.ResetCDs();

        MainStates.instance.mainPlayer.main.transform.position = playerPos.position;
        MainStates.instance.mainPlayer.Position = playerPos.position;
        MainStates.instance.mainPlayer.AdjustPosition();

    }

    // Update is called once per frame
    void Update()
    {
        //runtime
       if (Input.GetKeyDown("1"))
       {
           doRuntime = true;
           var d = ConfigLoader.Instance.metaConf.Find(x => x.parName == "mode_manhattan");
           d.val = 0;
           MainStates.instance.mainPlayer.AddViz("move");

           foreach (var v in MainStates.instance.combats)
           {
               if (v.RID == "main_player") continue;
               foreach (var v1 in v.actSkills)
               {
                   v1.SetPar("action_req", 0);
               }
               
           }
           //
           EventManager.INV("battle_start", new ArgPass());

           MainStates.instance.freeTargeting = true;
           
           EventManager.SUB("casted", (x) =>
           {
               MainStates.instance.mainPlayer.ResetCDs();
               MainStates.instance.UI_skills.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
           });
           
           //first cast enable
           MainStates.instance.UI_skills.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();

           //example of skills extension
           var rPas = DatabaseAll.instance.CreateProjectile(MainStates.instance.mainPlayer, "empty", Vector3.zero, false, false);
           rPas.addToWhom.Add("basic_range");
           rPas.addToWhom.Add("basic_melee");
           rPas.addToWhom.Add("weapon_skill");
           
           //rPas.addWhat.Add( new Bon{Key = "exec_push"});
           //rPas.addWhat.Add( new Bon{Key = "", Value = 1, Val2 = "ricochet"});
           rPas.addWhat.Add( new Bon{Key = "", Value = 2, Val2 = "bounce"});
           
           MainStates.instance.mainPlayer.buffs.Add(rPas);

       }

       if (Input.GetKeyDown("0"))
       {
           var rPas = DatabaseAll.instance.CreateProjectile(MainStates.instance.mainPlayer, "empty", Vector3.zero, false, false);
           rPas.addToWhom.Add("basic_range");
           rPas.addToWhom.Add("basic_melee");
           rPas.addToWhom.Add("weapon_skill");
           
           //rPas.addWhat.Add( new Bon{Key = "exec_push"});
           //rPas.addWhat.Add( new Bon{Key = "", Value = 1, Val2 = "ricochet"});
           rPas.addWhat.Add( new Bon{Key = "", Value = 2, Val2 = "bounce"});
           MainStates.instance.mainPlayer.buffs.Add(rPas);
           
           Debug.Log("0 ADDED");
           
       }

       if (Input.GetKeyDown("8"))
       {
           ConfigLoader.SetMetaParamValue("mode_manhattan", 0);
           ConfigLoader.SetMetaParamValue("mode_isometric", 1);
       }
       
       if (Input.GetKeyDown("9"))
       {
           ConfigLoader.SetMetaParamValue("mode_manhattan", 0);
           ConfigLoader.SetMetaParamValue("mode_hex", 1);
       }
       
        //battle selection
        if (Input.GetKeyDown("u"))
        {
            MainStates.instance.UI_battleSelect.SetActive(true);
        }

        if (Input.GetKeyDown("2"))
        {
            EventManager.SUB("after_battle", (x) =>
            {
                MainStates.instance.UI_battleSelect.SetActive(true);
            });

            //List<string> whoPlace = new List<string> { "militia", "archer", "priest" };
            List<string> whoPlace = new List<string> { "barracks" };
            MainStates.instance.availableMeRoot = null;
            
            foreach (var v in whoPlace)
            {
                var oo = DatabaseAll.instance.CreateMonster(v,  1, false, false);
                MainStates.instance.mainPlayer.inventory.Add(oo);
            }
            
            MainStates.instance.UI_unitsPlaced.SetActive(true);

            PlacerSystem.instance.onDragEnded = (x) =>
            {
                //add shit
                x.SetPar("on_field", 1);
                
                x.AddViz("hp#notext:1");
                x.AddViz("coll");
                x.AddViz("dmg_track");
                x.AddViz("death");
                x.AddViz("combat");
                x.AddViz("animator#pr:1");

                foreach (var v in x.actSkills)
                {
                    v.SetPar("action_req", -1);
                }

                if (x.dbObj.dynamic != "")
                {
                    x.AddViz("timer");
                    var g = x.main.GetComponentInChildren<Xdtimer>();
                    g.onEnd = () =>
                    {
                        x.AddViz("select");
                    };
                }
            };

            PlacerSystem.instance.onDragEach = (x) =>
            {
                XDdrag.Boogey(x.main.transform);
            };
            
            MainStates.instance.CreateLevelAtPos(2, 30, "LEVEL_1");
            
            EventManager.SUB("click_ally", (x) =>
            {
                if (MainStates.instance.lastAllySelected.GetPar("building") > 0)
                {
                    MainStates.instance.UI_buildingInterface.SetActive(true);
                }
            });
        }
        
    }
}
