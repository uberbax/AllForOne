using System;
using System.Collections;
using System.Collections.Generic;
using LayerLab;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MainCycleExp : MonoBehaviour
{
    public Button oneIteration;

    public bool AlwaysMove = true;
    public Button moveSkill;
    private RObj main;
    private RObj secondMain;

    private bool inBattle = false;

    public Camera mainCamera;
    public GameObject otherScene;

    public SampleCharacterMover mover;
    [Header("Other")] public Button skipTurn;

    private void Awake()
    {
        EventManager.SUB("PARSE_ENDED", B);

        //give one building at start

        EventManager.SUB("game_start", (x) => StartGame());

        oneIteration.onClick.AddListener(() => StartCoroutine(MainStates.instance.OneIteration(true)));

        EventManager.SUB("main_move", (x) => { PositionSetter.instance.OpenFog(x.pos); });

        EventManager.SUB("battle_start", (x) =>
        {
            inBattle = true;
            MainStates.instance.InIteration = false;
        });
        EventManager.SUB("battle_press", BattleClicked);

        EventManager.SUB("after_battle", (x) =>
        {
            MainStates.manualDt = false;
            BattleController.instance.Clean();
            ModelStatistics.instance.SetStatValueForce("battle", 1);
            mainCamera.GetComponent<CameraFollow>().target = main.main.transform;
            secondMain.Destroy();
            MainStates.instance.UI_skills.SetActive(false);
            MainStates.instance.UI_unitsPlaced.SetActive(false);
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

        EventManager.SUB("battle_leave", (x) =>
        {
            MainStates.manualDt = false;
            BattleController.instance.Flee();
            BattleController.instance.Clean();
            ModelStatistics.instance.SetStatValueForce("battle", 1);
            mainCamera.GetComponent<CameraFollow>().target = main.main.transform;
            secondMain.Destroy();
            MainStates.instance.UI_skills.SetActive(false);
            //MainStates.instance.UI_unitsPlaced.SetActive(false);
            {
                MainStates.instance.inBattle = false;
                inBattle = false;
            }
            ActivateOtherScene(false);
        });

        EventManager.SUB("go_home", (x) => { Camera.main.GetComponent<CameraFollow>().target = basePos; });

        EventManager.SUB("go_map", (x) => { Camera.main.GetComponent<CameraFollow>().target = main.main.transform; });

        EventManager.SUB("battle_ended", BattleEnded);
        
        EventManager.SUB("potion_used", (x) =>
        {
            MainStates.instance.awaitUnits["second_main"] = 0;
        });

        skipTurn.onClick.AddListener(() => SkipTurn());
    }

    public Transform battlePoint;
    public Transform playerPos;
    public Transform basePos;


    public void SkipTurn()
    {
        MainStates.instance.awaitUnits["second_main"] = 0;
    }

    private void BattleClicked(ArgPass obj)
    {
        MainStates.instance.UI_battleSelect.SetActive(false);
        WaveSpawner.ClearExcept("sword");

        MainStates.instance.lastBattle = obj.what;
        //WaveSpawner.instance.DoSpawnAll(MainStates.instance.lastBattle);

        MainStates.instance.UI_squadList.SetActive(false);


        mainCamera.GetComponent<CameraFollow>().target = battlePoint;
        MainStates.instance.mainPlayer.ResetCDs();

        //we need to create player clone basically, but with available skills

        secondMain = new RObj("hero", 1, 1, true, Vector3.zero, true, ItemType.monster, "second_main");
        MainStates.instance.ApplyPlayerConfigParams(secondMain);
        secondMain.AddViz("shadow");
        secondMain.AddViz("combat#no:1");
        secondMain.AddViz("hp");
        secondMain.AddViz("mana");

        secondMain.AddViz("coll#scale:0.5");
        secondMain.AddViz("animator#pr:1");
        secondMain.AddViz("drag");

        secondMain.AddViz("flash");
        secondMain.AddViz("dmg_track");

        secondMain.AddViz("buff");

        secondMain.AdjustPosition();
        secondMain.AddMeta("my_side");
        secondMain.AddMeta("sword");

        secondMain.main.transform.position = playerPos.position;
        secondMain.Position = playerPos.position;
        secondMain.AdjustPosition();

        secondMain.actSkills.Clear();

        for (int i = 0; i < main.inventory.Count; i++)
        {
            if (main.inventory[i].it != ItemType.projectile) continue;
            if (main.inventory[i].GetPar("used_slot") < 0) continue;
            MainStates.instance.AcquireAnySkill(secondMain, main.inventory[i].dbObj.ID);
        }
        //MainStates.instance.AcquireSkill(secondMain, "basic_melee");

        secondMain.SetScale(true);
        //MainStates.instance.mainPlayer.main.transform.position = playerPos.position;
        //MainStates.instance.mainPlayer.Position = playerPos.position;
        //MainStates.instance.mainPlayer.AdjustPosition();

        MainStates.instance.UI_unitsPlaced.SetActive(true);

        MainStates.manualDt = true;
        TimeManager.LAST_DT = 1;

        MainStates.instance.UI_skills.SetActive(true);
        //
        MainStates.instance.awaitUnits.Clear();
        MainStates.instance.awaitUnits.Add("second_main", 1);

        //and we do basically start battle
        EventManager.INV("battle_start", null);

        //we activate other SCENE
        ActivateOtherScene(true);
    }

    public void ActivateOtherScene(bool val)
    {
        mainCamera.gameObject.SetActive(!val);
        otherScene.SetActive(val);
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
        main = new RObj("hero", 1, 1, true, Vector3.zero, true, ItemType.monster, "main_player");
        MainStates.instance.ApplyPlayerConfigParams(main);
        main.AddViz("shadow");
        main.AddViz("combat#no:1");
        main.AddViz("coll#scale:0.5");
        //main.AddViz("select");
        main.AddViz("animator#pr:1");

        main.AdjustPosition();
        main.AddMeta("my_side");
        //
        main.AddViz("click_move");
        Camera.main.GetComponent<CameraFollow>().target = main.main.transform;

        //equipping basic melee
        MainStates.instance.AddItems(new List<Bon> { new Bon { Key = "basic_melee", Value = 1 } });
        var skl = main.inventory.Find(x => x.dbObj.ID == "basic_melee");
        MainStates.instance.Equip(main, skl, 50);

        MainStates.instance.AddItems(new List<Bon> { new Bon { Key = "basic_aoe", Value = 1 } });
        skl = main.inventory.Find(x => x.dbObj.ID == "basic_aoe");
        MainStates.instance.Equip(main, skl, 51);

        MainStates.instance.AddItems(new List<Bon> { new Bon { Key = "basic_buff_atk", Value = 1 } });
        skl = main.inventory.Find(x => x.dbObj.ID == "basic_buff_atk");
        MainStates.instance.Equip(main, skl, 52);

        //

        MainStates.instance.UI_skills.SetActive(false);
        foreach (var v in main.actSkills)
        {
            v.SetPar("action_req", 1);
        }

        FunctionTimer.Create(() => { PositionSetter.instance.OpenFog(main.Position); }, 0,
            () => PositionSetter.instance.wallsParsed);

        //params
        MainStates.lootTakeShowReward = true;
        MainStates.disappearLootOnTake = true;
        MainStates.allowAutoIterate = false;
        MainStates.metaCreateLevel = "sword";
        BattleController.reqTag = "sword";
        MainStates.anyPickAdd = new Bon { Key = "exp", Value = 10 };
        MainStates.pickOverHead = true;
        //move legnth in turn based games
        MainStates.maxMove = 1;
        MainStates.overridesViz = new List<(string, string)> { ("hp", ""), ("buff", "") };

        Animato.GlobalTm = 0.33f;
        XDloot.doMagnet = true;

        //set all ranges to 100
        foreach (var v in DatabaseAll.instance.skills)
        {
            v.Value.pars["range"] = 100;
        }

        PlacerSystem.instance.onDragEach = (x) => { XDdrag.Boogey(x.main.transform); };

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
                g.onEnd = () => { x.AddViz("select"); };
            }

            x.META_TAGS.Add("sword");
        };
        //
    }

    public void BattleEnded(ArgPass obj)
    {
        var d1 = MainStates.instance.lastBattleTrigger.GetComponent<ObjHolder>().obj;
        var d = d1.dbObj.drop;
        var aa = ModelSet.GetMeItemsBon(d);
        //mark loot as taken
        ModelStatistics.instance.Codex_LootMet(d1.dbObj.ID, aa);

        MainStates.instance.UI_win.GetComponent<ObjHolder>().obj = d1;
        MainStates.instance.dropTables["battle_reward"] = aa;

        inBattle = false;
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
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

    Coroutine coroutine;

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

        if (Input.GetKeyDown("j"))
        {
            var u0 = MainStates.instance.UI_skillsAssign.activeSelf;

            MainStates.instance.UI_skillsAssign.SetActive(!u0);
        }

        if (Input.GetKeyDown("h"))
        {
            var rr = ResourceHolder.instance.skillsWorld["whirl"];
            var go = Instantiate(rr, MainStates.instance.mainPlayer.main.transform);
        }

        if (inBattle)
        {
            if (!MainStates.instance.InIteration)
            {
                coroutine = StartCoroutine(MainStates.instance.OneIteration(false, 1f, "sword", true));
            }
        }
    }
}