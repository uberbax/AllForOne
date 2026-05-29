using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

public class MainCycleCyclone : MonoBehaviour
{
    public Transform battlePoint;
    public Transform buildPoint;
    public GameObject actSkills;

    private Vector3 dragFieldSize = Vector3.zero;
    private Vector3 savedScale = Vector3.one;
    
    private Dictionary<string, string> mappingStats = new Dictionary<string, string>
    {
        {"military_res","militia"},
        {"mage_res","mage"},
        {"archer_res","archer"},
        {"defender_res","defender"},
        {"priest_res","priest"},
        {"hero_res", "hero"}
    };

    private List<string> statsTrack = new List<string> {"recycler","hospital","farm","defence",
        "house","barracks","dining","shtab","ship"};
    
    private Dictionary<string, List<Vector3>> typePoses = new Dictionary<string, List<Vector3>>();

    private int selectedHero = 0;
    private void Awake()
    {
        EventManager.SUB("PARSE_ENDED", B);
        
        EventManager.SUB("battle_phase", (x) =>
        {
            Camera.main.GetComponent<CameraFollow>().target = battlePoint;
            WaveSpawner.instance.SpawnAnyByStats(mappingStats, "player", MainStates.instance.dragLo, MainStates.instance.dragHi, "my_side", typePoses);
            MainStates.instance.mainPlayer.ResetCDs();
        });
        EventManager.SUB("build_phase", (x) =>
        {
            Camera.main.GetComponent<CameraFollow>().target = buildPoint;
            WaveSpawner.instance.ClearWithMeta("my_side");
            WaveSpawner.instance.ClearWithMeta("drop");
            actSkills.SetActive(false);
            SkillExecutor.instance.CancelAction();
        });
        EventManager.SUB("after_battle", (x) =>
        {
            Camera.main.GetComponent<CameraFollow>().target = buildPoint;
            WaveSpawner.instance.ClearWithMeta("my_side");
            WaveSpawner.instance.ClearWithMeta("drop");
            WaveSpawner.instance.ClearWithMeta("wave");
            actSkills.SetActive(false);
            SkillExecutor.instance.CancelAction();
            
            FunctionTimer.Create(() =>
            {
                EventManager.INV("received_res", new ArgPass()); 
            }, 1);
        });
        
        EventManager.SUB("battle_start", (x) =>
        {
            actSkills.SetActive(true);
            //savePos
            typePoses.Clear();
            
            ModelStatistics.instance.SetStatValue("kill_any", 0);
            ModelStatistics.instance.SetStatValue("lost_any", 0);
            
            
            foreach (var v in mappingStats)
            {
                typePoses.Add(v.Value, new List<Vector3>());
            }
            
            foreach (var v in MainStates.instance.all)
            {
                if (!v.Value.META_TAGS.Contains("my_side")) continue;
                typePoses[v.Value.dbObj.ID].Add(v.Value.Position);
            }
            
        });
        
        EventManager.SUB("select_hero", (x) =>
        {
            selectedHero = x.num;
        });


        //give one building at start
        
        EventManager.SUB("game_start", (x) => StartGame());
        
    }

    public void StartGame()
    {
        MainStates.instance.ReplaceObj(MainStates.instance.mainPlayer, "hero" + (selectedHero == 0 ? "" : selectedHero));

        mappingStats["hero_res"] = "hero" + (selectedHero == 0 ? "" : selectedHero);
        
        foreach (var v in mappingStats)
        {
            typePoses.Add(v.Value, new List<Vector3>());
        }        
        //random building
        var gg = new List<string>(statsTrack);
        ModelStatistics.instance.SetStatValue("ini_buildings", (int)ConfigLoader.GetMetaParamValue("ini_buildings"));
        int hh = ModelStatistics.instance.GetStatValue("ini_buildings");

        for (int i = 0; i < hh; i++)
        {
            var h = gg[Random.Range(0, gg.Count)];
            ModelStatistics.instance.IncreaseStatValue(h,1 );
            gg.Remove(h);
        }
        //

        for (int i = 0; i < statsTrack.Count; i++)
        {
            var h = ModelStatistics.instance.GetStatValue(statsTrack[i]);
            for (int j = 0; j < h; j++)
            {
                string nm = statsTrack[i] + "_" + j;
                if (ConfigLoader.Instance.allDynamic.ContainsKey(nm))
                {
                    MainStates.instance.ExecuteDone(ConfigLoader.Instance.allDynamic[nm]);
                }
            }
        }
        
        //ReceiveResources();
        FunctionTimer.Create(() =>
        {
            EventManager.INV("received_res", new ArgPass()); 
        }, 1);
        
    }

    public void ReceiveResources()
    {
        
    }

    private void Start()
    {
        dragFieldSize = MainStates.instance.dragHi.position - MainStates.instance.dragLo.position;
        savedScale = MainStates.instance.dragZone.localScale;        
        //for spawning hero in battle
        ModelStatistics.instance.SetStatValue("hero_res",1);
        

    }

    private void B(ArgPass obj)
    {
        Debug.Log("haha");
        var main = new RObj("hero" + (selectedHero == 0 ? "" : selectedHero), 1, 1, true, Vector3.zero,true, ItemType.monster, "main_player");
        MainStates.instance.ApplyPlayerConfigParams(main);
        main.AddViz("shadow");
        
        //var str = JsonConvert.SerializeObject(main);
        //Debug.Log(str);

        foreach (var v in ConfigLoader.Instance.allDynamic)
        {
            if (v.Key.IndexOf("item_") < 0) continue;
            var b = UtilsControl.GetLowest(v.Key);
            if (b != v.Key) continue;
            
            var itm = DatabaseAll.instance.CreateItem("empty", 1);
            itm.dynamic = v.Value;
            main.inventory.Add(itm);
        }
        
        ModelStatistics.instance.UpdateAllTasks();
        
    }

    void RecalcDragZone()
    {
        var d = ModelStatistics.instance.GetStatValue("field");
        var d0 = ConfigLoader.GetMetaParamValue("field_scale") * d;

        MainStates.instance.dragHi.position = MainStates.instance.dragLo.position + dragFieldSize + new Vector3(d0, 0, 0);
        MainStates.instance.dragZone.position =
            (MainStates.instance.dragLo.position + MainStates.instance.dragHi.position) / 2;
        
        MainStates.instance.dragZone.localScale = savedScale * (MainStates.instance.dragHi.position.x - MainStates.instance.dragLo.position.x) / (dragFieldSize.x);
    }

    private void Update()
    {
        RecalcDragZone();
        
        if (Input.GetKeyDown("i"))
        {
            MainStates.instance.UI_inventory.SetActive(true);
        }
        
        if (Input.GetKeyDown("u"))
        {
            MainStates.instance.UI_battleSelect.SetActive(true);
        }
        
    }
}
