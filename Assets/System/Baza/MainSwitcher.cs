using System;
using System.Collections.Generic;
using RPGCharacterAnims;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.AI;
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

    public bool is3D = false;

    [Header("Rumble CASE")] 
    public bool isRumble;
    public void AfterLoad()
    {
        if (is3D)
        {
            MainStates.is3D = true;
            MainStates.instance.mainPlayer.RemoveViz("click_move");
            
            //MainStates.instance.mainPlayer.AddViz("shoot");
            MainStates.instance.mainPlayer.AddViz("fight");
            
            MainStates.pickOverHead = true;

            XDrand_move.distMult = 2;
            XDdmgTrack.scaleMult = 0.3f;
            
            Destroy(MainStates.instance.mainPlayer.main.GetComponent<NavMeshAgent>());
        }

        if (isRumble)
        {
            Make3D();
            MainStates.instance.mainPlayer.RemoveViz("click_move");
            MainStates.instance.mainPlayer.AddViz("move");
        }
    }
    
    
    private void Awake()
    {
        instance = this;
        
        //EventManager.SUB("battle_press", BattleClicked);
        EventManager.SUB("after_load", pass => AfterLoad());
    }

    [EditorAttributes.Button("Equip Pistols")]
    public void EquipPistols()
    {
          var g = MainStates.instance.mainPlayer.main.GetComponentInChildren<GUIControls>();
          g.EquipDualWeapons(Weapon.LeftPistol, Weapon.RightPistol);
    }
    

    [EditorAttributes.Button("Make vampire")]
    public void MakeVampire()
    {
        ModelStatistics.instance.SetStatValue("battle", 2);

        foreach (var v in MainStates.instance.all)
        {
            var dir = MainStates.instance.mainPlayer.Position - v.Value.Position;
            dir.z = 0;
            if (dir.magnitude < MainStates.instance.visDistCheck)
            {
                v.Value.AddMeta("sword");
                v.Value.RemoveViz("rand_move");
                v.Value.RemoveViz("info");
            }
        }
        
        MainStates.instance.mainPlayer.AddMeta("sword");
        
        MainStates.instance.inBattle = true;
        MainStates.instance.InIteration = false;
        
        ConfigLoader.SetMetaParamValue("use_naprig", 0);
        //
        
        foreach (var v in DatabaseAll.instance.skills)
        {
            v.Value.pars["range"] = v.Value.pars["saved_range"];
        }

        MainCycleExp.tm = 0;
        MainCycleExp.waitMove = false;
        MainCycleExp.exceptMain = true;
        
        MainStates.instance.mainPlayer.RemoveViz("click_move");

        var ee = MainStates.instance.mainPlayer.main.GetComponentInChildren<RPGCharacterMovementController>();
        if (ee == null)
            MainStates.instance.mainPlayer.AddViz("move#val:3");
        
        MainStates.instance.mainPlayer.AddViz("shoot");
        
        Destroy(MainStates.instance.mainPlayer.main.GetComponent<NavMeshAgent>());
        //awaietrs
        //MainStates.instance.awaitUnits.Clear();
        //MainStates.instance.awaitUnits.Add("second_main", 1);
        //

        if (isHex)
        {
            MainStates.instance.mainPlayer.RemoveViz("move");
            MainStates.instance.mainPlayer.RemoveViz("shoot");
            
            MainStates.instance.awaitUnits.Clear();
            MainStates.instance.awaitUnits.Add("main_player", 1);
            
            MainCycleExp.tm = 1.7f;
            MainCycleExp.waitMove = true;
            MainCycleExp.exceptMain = false;
        }
        
    }

    public List<Transform> toRotate = new List<Transform>();
    public List<Transform> toExclude = new List<Transform>();
    
    
    [EditorAttributes.Button("Make 3D")]
    public void Make3D()
    {
        Camera.main.orthographic = false;
        Camera.main.GetComponent<CameraFollow>().useOffscreen = false;
        Camera.main.transform.localEulerAngles = new Vector3(-30, 0, 0);
        Camera.main.GetComponent<CameraFollow>().delta = new Vector3(0, -5.22f, 0);
        //
        MainStates.isCameraFace = true;
        MainStates.is3D = true;
        
        
        foreach (var v in MainStates.instance.all)
        {
            if (v.Value.HasVisMain())
                v.Value.visMain.transform.forward = Camera.main.transform.forward;
        }

        foreach (var v in toRotate)
        {
            for (int u = 0; u < v.childCount; u++)
            {
                var b = v.GetChild(u);
                if (!b.gameObject.activeInHierarchy) continue;
                if (toExclude.Contains(b)) continue;
                
                b.transform.forward = Camera.main.transform.forward;
            }
        }

    }
    
    //nado еще make manhatta, isometric, hex
    public GameObject skills;
    public GameObject manhattanTarg;
    private bool isHex = false;
    
    [EditorAttributes.Button("Make Hex")]
    public void MakeHex()
    {
        ConfigLoader.SetMetaParamValue("mode_hex", 1);
        
        PositionSetter.instance.RecreatePByTile();
        PositionSetter.instance.Awake();
        //?
        //PositionSetter.instance.n--;
        //PositionSetter.instance.m--;
        
        
        
        foreach (var v in MainStates.instance.all)
        {
            if (v.Value.HasVisMain())
            {
                var hh = PositionSetter.instance.GetClosestPos(v.Value.Position);
                v.Value.Position = hh.Item3;
                v.Value.ref_pos_x = v.Value.ref_pos_x;
                v.Value.ref_pos_y = v.Value.ref_pos_y;
                v.Value.AdjustPosition();
            }
        }
        
        skills.SetActive(true);
        manhattanTarg.SetActive(true);
        MainStates.instance.mainPlayer.RemoveViz("click_move");
        isHex = true;
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
        
        //
        if (Input.GetKeyDown("3"))
        {
            //swap weapon
            var v = MainStates.instance.mainPlayer.GetNextWeapon();
            if (v != null)
            {
                MainStates.instance.Equip(MainStates.instance.mainPlayer, v);
                var fb = MainStates.instance.mainPlayer.main.GetComponentInChildren<GUIControls>();
                fb.EquipWeapon(MainStates.instance.GetWeaponByString(v.dbID));
                //GUIControls
            }
        }
    }
}
