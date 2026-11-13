using System;
using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using LayerLab;
using NavMeshPlus.Extensions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Diagnostics;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MainCycleDark : MonoBehaviour
{
    [HelpBox("Set this", MessageMode.Warning)]  
    public Button oneIteration;
    [HelpBox("Set this", MessageMode.Warning)]  
    public Transform spawnPoint;
    public bool AlwaysMove = true;
    public Button moveSkill;
    private RObj main;
    private RObj secondMain;

    public Camera mainCamera;
    public GameObject otherScene;

    public SampleCharacterMover mover;
    [Header("Other")] 
    [HelpBox("Set this", MessageMode.Warning)]    
    public Button skipTurn;


    [Header("<color=red>MENU</color>")] 
    [HelpBox("Set this", MessageMode.Warning)]
    public Button start;
    [HelpBox("Set this", MessageMode.Warning)]
    public GameObject loading;
    public List<GameObject> activateAfterLoading;
    
    public GameObject logo;
    public GameObject firstSlide;

    [Header("Lift")] 
    public Button goDownPrison;
    public GameObject cages;
    public GameObject lift;
    public GameObject dynScreen;
    public Transform liftLo;
    public Transform liftHi;
    public Transform dynScreenLo;
    public Transform dynScreenHi;
    public GameObject character;
    public GameObject door;
    
    public Transform charEnd;
    public UiLoopVideo charLift;
    
    [Header("Embark")]
    public Button embark;
    public GameObject map;
    public GameObject prev;
    
    [Header("Town Back")]
    public Button townBack;

    [Header("Map Markers")]
    public List<Button> missions = new List<Button>();
    public GameObject infoBattle;
    public Button goBattle;
    public GameObject backs;
    public GameObject heroes;
    public GameObject battleUI;
    
    public GameObject managment;
    
    public void HandleLift()
    {
        if (prisonVisitActive) return;
        CapturePrisonState();
        prisonVisitActive = true;
        int visit = ++prisonVisit;
        canMovePrison = false;
        prisonScrollInitialized = false;
        UpdatePrisonArrows();
        StopPrisonMovement();
        Transitioner.instance.DoFade(1, 1, () =>
        {
            if (prisonVisitActive && visit == prisonVisit) cages.SetActive(true);
        }, () =>
        {
            if (!prisonVisitActive || visit != prisonVisit) return;
            UtilsControl.Instance.MoveTo(lift.transform, 200f, liftLo.position, () =>
            {
                if (!prisonVisitActive || visit != prisonVisit) return;
                UtilsControl.Instance.MoveTo(dynScreen.transform, 200f, dynScreenLo.position, () =>
                {
                    if (!prisonVisitActive || visit != prisonVisit) return;
                    //particles dust ?
                    door.GetComponent<SpriterAnim>().CrossFade("open", 0);
                    //enable movement
                    
                    //we move char
                    charLift.transform.parent = lift.transform.parent; 
                    //lift.transform.parent = lift.transform.parent.parent;
                    
                    prisonExitRoutine = StartCoroutine(WalkFromPrisonLift(visit));
                    
                }, null, useRight:false);
            }, null, useRight:false);
        });
    }

    public bool canMovePrison = false;
    public GameObject buttonNext;
    public GameObject buttonPrev;
    
    [SerializeField, Min(0)] private float prisonMoveSpeed = 200f;
    [SerializeField] private bool prisonVideoFacesLeft = true;

    private PrisonMoveArrow prisonNextArrow;
    private PrisonMoveArrow prisonPrevArrow;
    private bool prisonWalking;
    private bool prisonScrollInitialized;
    private Vector3 prisonBackgroundHome;
    private Vector3 prisonLiftHome;
    private float prisonScrollOffset;
    private float prisonMaxScroll;
    private readonly Vector3[] prisonContentCorners = new Vector3[4];

    private PrisonTransformState[] prisonInitialState;
    private int prisonVisit;
    private bool prisonVisitActive;
    private Coroutine prisonExitRoutine;
    private string prisonDoorState;

    private void CapturePrisonState()
    {
        prisonInitialState = new[]
        {
            new PrisonTransformState(dynScreen.transform),
            new PrisonTransformState(lift.transform),
            new PrisonTransformState(charLift.transform)
        };
        var animation = door != null ? door.GetComponent<SpriterAnim>() : null;
        prisonDoorState = animation != null
            ? string.IsNullOrEmpty(animation.curAnim) ? animation.defaultAnim : animation.curAnim
            : null;
    }

    public void RestorePrisonState()
    {
        prisonVisit++;
        prisonVisitActive = false;
        canMovePrison = false;
        UpdatePrisonArrows();
        StopPrisonMovement();
        if (prisonExitRoutine != null)
        {
            StopCoroutine(prisonExitRoutine);
            prisonExitRoutine = null;
        }

        prisonScrollInitialized = false;
        prisonScrollOffset = 0;
        prisonMaxScroll = 0;
        if (prisonInitialState == null) return;

        foreach (var state in prisonInitialState)
        {
            if (state.Target == null) continue;
            var move = state.Target.GetComponent<MoveDir>();
            if (move != null && move.cr != null)
            {
                if (UtilsControl.Instance != null) UtilsControl.Instance.StopCoroutine(move.cr);
                move.cr = null;
            }

            state.Target.SetParent(state.Parent, false);
        }

        foreach (var state in prisonInitialState) state.Restore();
        Array.Sort(prisonInitialState, (a, b) => a.SiblingIndex.CompareTo(b.SiblingIndex));
        foreach (var state in prisonInitialState)
            if (state.Target != null) state.Target.SetSiblingIndex(state.SiblingIndex);
        prisonInitialState = null;

        var animation = door != null ? door.GetComponent<SpriterAnim>() : null;
        if (animation != null && !string.IsNullOrEmpty(prisonDoorState))
            animation.CrossFade(prisonDoorState, 0);
        if (charLift != null) charLift.Play("idle");
    }

    private sealed class PrisonTransformState
    {
        public Transform Target { get; }
        public Transform Parent { get; }
        public int SiblingIndex { get; }

        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly Vector3 scale;
        private readonly Vector2 anchorMin, anchorMax, pivot, sizeDelta;
        private readonly Vector3 anchoredPosition;
        private readonly string name;
        private readonly bool active;

        public PrisonTransformState(Transform target)
        {
            Target = target;
            Parent = target.parent;
            SiblingIndex = target.GetSiblingIndex();
            position = target.localPosition;
            rotation = target.localRotation;
            scale = target.localScale;
            name = target.name;
            active = target.gameObject.activeSelf;
            if (target is RectTransform rect)
            {
                anchorMin = rect.anchorMin;
                anchorMax = rect.anchorMax;
                pivot = rect.pivot;
                sizeDelta = rect.sizeDelta;
                anchoredPosition = rect.anchoredPosition3D;
            }
        }

        public void Restore()
        {
            if (Target == null) return;
            Target.name = name;
            if (Target is RectTransform rect)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.sizeDelta = sizeDelta;
                rect.anchoredPosition3D = anchoredPosition;
            }
            else Target.localPosition = position;
            Target.localRotation = rotation;
            Target.localScale = scale;
            var move = Target.GetComponent<MoveDir>();
            if (move != null)
            {
                move.where = Target.position;
                move.dir = Vector3.zero;
            }
            Target.gameObject.SetActive(active);
        }
    }


    private void UpdatePrisonArrows()
    {
        if (buttonNext != null && buttonNext.activeSelf != canMovePrison)
            buttonNext.SetActive(canMovePrison);
        if (buttonPrev != null && buttonPrev.activeSelf != canMovePrison)
            buttonPrev.SetActive(canMovePrison);
    }

    private void HandlePrisonMovement()
    {
        UpdatePrisonArrows();
        if (!canMovePrison || charLift == null || cages == null ||
            !cages.activeInHierarchy || !charLift.isActiveAndEnabled)
        {
            SetPrisonWalking(false);
            return;
        }

        int direction = (prisonNextArrow != null && prisonNextArrow.IsHeld ? 1 : 0) -
                        (prisonPrevArrow != null && prisonPrevArrow.IsHeld ? 1 : 0);
        var area = cages.transform as RectTransform;
        var characterRect = charLift.transform as RectTransform;
        if (direction == 0 || area == null || characterRect == null)
        {
            SetPrisonWalking(false);
            return;
        }

        if (!prisonScrollInitialized) InitializePrisonScroll(area);
        var position = area.InverseTransformPoint(characterRect.position);
        float worldX = position.x + prisonScrollOffset;
        float halfWidth = Mathf.Abs(area.InverseTransformVector(
            characterRect.TransformVector(Vector3.right * characterRect.rect.width)).x) * .5f;
        float margin = Mathf.Min(halfWidth, area.rect.width * .5f);
        float nextX = Mathf.Clamp(worldX + direction * prisonMoveSpeed * Time.unscaledDeltaTime,
            area.rect.xMin + margin, area.rect.xMax - margin + prisonMaxScroll);
        bool moving = !Mathf.Approximately(nextX, worldX);
        SetPrisonWalking(moving);
        if (moving)
        {
            FacePrisonDirection(direction);
            if (!charLift.IsAnimationVisible("walk")) return;
            ApplyPrisonScroll(area, characterRect, nextX);
        }
    }

    private void InitializePrisonScroll(RectTransform area)
    {
        prisonScrollOffset = 0;
        prisonMaxScroll = 0;
        if (dynScreen != null)
        {
            prisonBackgroundHome = area.InverseTransformPoint(dynScreen.transform.position);
            var content = dynScreen.transform.Find("total") as RectTransform;
            if (content != null)
            {
                content.GetWorldCorners(prisonContentCorners);
                float rightEdge = area.rect.xMax;
                foreach (var corner in prisonContentCorners)
                    rightEdge = Mathf.Max(rightEdge, area.InverseTransformPoint(corner).x);
                prisonMaxScroll = Mathf.Max(0, rightEdge - area.rect.xMax);
            }
        }

        if (lift != null)
        {
            if (lift.transform.parent != area) lift.transform.SetParent(area, true);
            prisonLiftHome = area.InverseTransformPoint(lift.transform.position);
        }

        prisonScrollInitialized = true;
    }

    private void ApplyPrisonScroll(RectTransform area, RectTransform actor, float worldX)
    {
        prisonScrollOffset = Mathf.Clamp(worldX - area.rect.center.x, 0, prisonMaxScroll);
        if (dynScreen != null)
            dynScreen.transform.position = area.TransformPoint(prisonBackgroundHome - Vector3.right * prisonScrollOffset);
        //if (lift != null) lift.transform.position = area.TransformPoint(prisonLiftHome);

        var position = area.InverseTransformPoint(actor.position);
        position.x = worldX - prisonScrollOffset;
        actor.position = area.TransformPoint(position);
    }

    private IEnumerator WalkFromPrisonLift(int visit)
    {
        var direction = cages.transform.InverseTransformPoint(charEnd.position).x -
                        cages.transform.InverseTransformPoint(charLift.transform.position).x;
        FacePrisonDirection(direction);
        charLift.Play("walk");
        while (!charLift.IsAnimationVisible("walk"))
        {
            if (!prisonVisitActive || visit != prisonVisit ||
                !cages.activeInHierarchy || !charLift.isActiveAndEnabled) yield break;
            yield return null;
        }

        if (!prisonVisitActive || visit != prisonVisit) yield break;
        UtilsControl.Instance.MoveTo(charLift.transform, 200, charEnd.position, () =>
        {
            if (!prisonVisitActive || visit != prisonVisit) return;
            charLift.Play("idle");
            InitializePrisonScroll((RectTransform)cages.transform);
            canMovePrison = true;
            lift.transform.parent = dynScreen.transform;
            UpdatePrisonArrows();
        }, null, useRight:false, ignoreFlip:true);
    }

    private void LateUpdate()
    {
        //if (canMovePrison && prisonScrollInitialized && cages != null && lift != null)
        //    lift.transform.position = cages.transform.TransformPoint(prisonLiftHome);
    }

    private void FacePrisonDirection(float direction)
    {
        if (Mathf.Approximately(direction, 0)) return;
        var characterTransform = charLift.transform;
        var parent = characterTransform.parent;
        var right = cages.transform.TransformVector(Vector3.right);
        float parentDirection = parent != null ? parent.InverseTransformVector(right).x : right.x;
        var scale = characterTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction * parentDirection) *
                  (prisonVideoFacesLeft ? -1 : 1);
        characterTransform.localScale = scale;
    }

    private void SetPrisonWalking(bool walking)
    {
        if (prisonWalking == walking) return;
        prisonWalking = walking;
        if (charLift != null) charLift.Play(walking ? "walk" : "idle");
    }

    private void StopPrisonMovement()
    {
        if (prisonNextArrow != null) prisonNextArrow.Release();
        if (prisonPrevArrow != null) prisonPrevArrow.Release();
        SetPrisonWalking(false);
    }

    private void OnDisable() => StopPrisonMovement();

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) StopPrisonMovement();
    }


    public void ClosePrisonCages()
    {
        RestorePrisonState();
        cages.SetActive(false);
    }

    public void DoMenu(bool hasRun)
    {
        if (hasRun)
        {
            start.onClick.AddListener( ()=>
                Transitioner.instance.Simple(loading, activateAfterLoading)
            );
        }
        else
        {
            start.onClick.AddListener(() =>
                {
                    Transitioner.instance.Simple(loading, activateAfterLoading);
                }
            );
        }
    }
    
    
    private void Awake()
    {
        UpdatePrisonArrows();
        if (buttonNext != null)
            prisonNextArrow = buttonNext.GetComponent<PrisonMoveArrow>() ?? buttonNext.AddComponent<PrisonMoveArrow>();
        if (buttonPrev != null)
            prisonPrevArrow = buttonPrev.GetComponent<PrisonMoveArrow>() ?? buttonPrev.AddComponent<PrisonMoveArrow>();
        EventManager.SUB("PARSE_ENDED", B);

        //give one building at start

        EventManager.SUB("game_start", (x) => StartGame());

        oneIteration.onClick.AddListener(() => StartCoroutine(MainStates.instance.OneIteration(true)));

        EventManager.SUB("main_move", (x) => { PositionSetter.instance.OpenFog(x.pos); });

        EventManager.SUB("battle_start", (x) =>
        {
            MainStates.instance.inBattle = true;
            MainStates.instance.InIteration = false;
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
        });
        EventManager.SUB("battle_press", BattleClicked);

        EventManager.SUB("after_battle", (x) =>
        {
            if (DungeonController.instance.inDungeon)
                return;
            
            MainStates.manualDt = false;
            BattleController.instance.Clean();
            ModelStatistics.instance.SetStatValueForce("battle", 1);
            MainStates.lastBattleTime = Time.time;
            //mainCamera.GetComponent<CameraFollow>().target = main.main.transform;
            secondMain.Destroy();
            MainStates.instance.UI_skills.SetActive(false);
            MainStates.instance.UI_unitsPlaced.SetActive(false);
            if (MainStates.instance.lastBattleResult == 0)
            {
                //update kill chests
                foreach (var v in MainStates.instance.chests)
                {
                    if (v == null) continue;
                    var rr = MainStates.instance.GetDistance(
                        MainStates.instance.lastBattleTrigger.GetComponent<ObjHolder>().obj, v, out float ff);
                    if (rr < 5)
                    {
                        v.ChangePar("kill", 1);
                    }
                }
                Destroy(MainStates.instance.lastBattleTrigger);
                MainStates.instance.inBattle = false;
            }
            else
            {
                MainStates.instance.inBattle = false;
            }
            ActivateOtherScene(false);
            
            //raid_boss ?
            MainStates.instance.curObjs["last_boss"] = null;
            MainStates.instance.dropTables["battle_reward"] = new List<Bon>();
        });

        EventManager.SUB("battle_leave", (x) =>
        {
            DungeonController.instance.inDungeon = false;
            MainStates.manualDt = false;
            BattleController.instance.Flee();
            BattleController.instance.Clean();
            ModelStatistics.instance.SetStatValueForce("battle", 1);
            //mainCamera.GetComponent<CameraFollow>().target = main.main.transform;
            secondMain.Destroy();
            MainStates.instance.UI_skills.SetActive(false);
            //MainStates.instance.UI_unitsPlaced.SetActive(false);
            {
                MainStates.instance.inBattle = false;
            }
            ActivateOtherScene(false);
            MainStates.instance.curObjs["last_boss"] = null;
            MainStates.instance.dropTables["battle_reward"] = new List<Bon>();
        });

        //EventManager.SUB("go_home", (x) => { Camera.main.GetComponent<CameraFollow>().target = basePos; });

        //EventManager.SUB("go_map", (x) => { Camera.main.GetComponent<CameraFollow>().target = main.main.transform; });

        EventManager.SUB("battle_ended", BattleEnded);
        
        EventManager.SUB("potion_used", (x) =>
        {
            MainStates.instance.awaitUnits["second_main"] = 0;
        });

        skipTurn.onClick.AddListener(() => SkipTurn());
        MainStates.handleCDInNoBattle = false;
        
        goDownPrison.onClick.AddListener(HandleLift);
        embark.onClick.AddListener(HandleEmbark);
        townBack.onClick.AddListener(HandleBack);
        
        HandleMarkers();
    }

    public void HandleMarkers()
    {
        foreach (var v in missions)
        {
            v.onClick.AddListener(() =>
            {
                infoBattle.gameObject.SetActive(true);
            });
        }

        goBattle.onClick.AddListener(() =>
            {
                //infoBattle.gameObject.SetActive(false);
                Transitioner.instance.Simple(map, new List<GameObject> { backs, heroes, battleUI }, null, 
                    new List<GameObject> { managment } );
            }
        );
    }

    public void HandleEmbark()
    {
        Transitioner.instance.Simple(prev, new List<GameObject> { map }, null);
    }
    
    public void HandleBack()
    {
        Transitioner.instance.Simple(map, null, null);
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


        //mainCamera.GetComponent<CameraFollow>().target = battlePoint;
        MainStates.instance.mainPlayer.ResetCDs();

        //we need to create player clone basically, but with available skills

        var lvl = MainStates.instance.mainPlayer.GetPar("level");
        
        secondMain = new RObj(MainStates.instance.mainPlayer.dbObj.ID, 1, (int)lvl, true, Vector3.zero, true, ItemType.monster, "second_main");
        //man, we need to clone it
        
        MainStates.instance.ApplyPlayerConfigParams(secondMain);
        secondMain.AddViz("shadow");
        secondMain.AddViz("combat#no:1");
        secondMain.AddViz("hp");
        secondMain.AddViz("mana");
        secondMain.AddViz("ward");
        secondMain.AddViz("death");
        

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
        
        var ff = secondMain.visMain.transform.Find("legs");
        if (ff != null) secondMain.main.transform.position += (secondMain.main.transform.position - ff.position);
        

        for (int i = 0; i < main.inventory.Count; i++)
        {
            if (main.inventory[i].it != ItemType.projectile) continue;
            if (main.inventory[i].GetPar("used_slot") < 0) continue;
            MainStates.instance.AcquireAnySkill(secondMain, main.inventory[i].dbObj.ID);
        }
        
        //for some reason its not ?
        /*
        for (int i = 0; i < main.buffs.Count; i++)
        {
            MainStates.instance.AcquireAnySkill(secondMain, main.buffs[i].dbObj.ID, true);
        }
        */
        
        for (int i = 0; i < main.inventory.Count; i++)
        {
            if (main.inventory[i].it != ItemType.item) continue;
            if (main.inventory[i].GetPar("used_slot") < 0) continue;
            var cc = main.inventory[i].Clone();
            MainStates.instance.Equip(secondMain, cc);
        }
        
        //MainStates.instance.AcquireSkill(secondMain, "basic_melee");

        //?
        var dd = secondMain.main.GetComponentInChildren<XDinvscale>();
        if (dd == null)
            secondMain.SetScale(true);
        
        //place my others ?
        var bb = MainStates.instance.mainPlayer.inventory.FindAll(x => x.it == ItemType.monster && x.GetPar("used_slot") >= 0);

        if (bb.Count > 0)
        {
            var ee = WaveSpawner.instance.DoSpawnAnyPos(new List<Bon> { new Bon { Key = bb[0].dbObj.ID, Value = 1} },
                "player", false, applyExtra: true, overridesViz: MainStates.overridesViz, start: 1);
            
            ee[0].SetScale(true);
            ee[0].visMain.transform.localScale *= 0.5f;
        }

        foreach (var v in secondMain.actSkills)
        {
            v.SetPar("action_req", -1);
        }

        MainStates.instance.UI_unitsPlaced.SetActive(true);

        MainStates.manualDt = true;
        TimeManager.LAST_DT = 1;
        MainStates.summonNextPos = true;
        
        //
        MainStates.instance.awaitUnits.Clear();
        MainStates.instance.awaitUnits.Add("second_main", 1);
        secondMain.main.transform.localScale *= 2;

        //and we do basically start battle
        EventManager.INV("battle_start", null);
        MainStates.instance.UI_skills.SetActive(true);
        
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
        bool succLoad = false;
        if (MainStates.instance.useSave)
        {
            succLoad = MainStates.instance.Load();
        }
        //
        if (!succLoad)
        {
            main = new RObj("hero_mage", 1, 1, true, spawnPoint.position, true, ItemType.monster, "main_player");
            MainStates.instance.ApplyPlayerConfigParams(main);
        }
        else
        {
            main = MainStates.instance.mainPlayer;
        }

        DoMenu(succLoad);

        main.AddViz("shadow");
        main.AddViz("combat#no:1");
        main.AddViz("coll#scale:0.5");
        //main.AddViz("select");
        main.AddViz("animator#pr:1");

        main.AdjustPosition();
        main.AddMeta("my_side");
        //
        main.AddViz("click_move");
        //Camera.main.GetComponent<CameraFollow>().target = main.main.transform;

        //
        var cc = main.main.AddComponent<NavMeshAgent>();
        cc.acceleration = 0;
        main.main.AddComponent<AgentOverride2d>();
        //
        
        
        //equipping basic melee
        if (!succLoad)
        {
            MainStates.instance.AddItems(new List<Bon> { new Bon { Key = "basic_melee", Value = 1 } });
            var skl = main.inventory.Find(x => x.dbObj.ID == "basic_melee");
            MainStates.instance.Equip(main, skl, 50);

            MainStates.instance.AddItems(new List<Bon> { new Bon { Key = "basic_harm", Value = 1 } });
            skl = main.inventory.Find(x => x.dbObj.ID == "basic_harm");
            MainStates.instance.Equip(main, skl, 51);

            MainStates.instance.AddItems(new List<Bon> { new Bon { Key = "crit_strike", Value = 1 } });
            skl = main.inventory.Find(x => x.dbObj.ID == "crit_strike");
            MainStates.instance.Equip(main, skl, 52);
            
            MainStates.instance.AddItems(new List<Bon>{new Bon{Key = "hero_mage", Value = 1}});
            //MainStates.instance.AddItems(new List<Bon>{new Bon{Key = "hero_thief", Value = 1}});
            //MainStates.instance.AddItems(new List<Bon>{new Bon{Key = "hero_warrior", Value = 1}});
            
        }

        //MainStates.instance.AddItems(new List<Bon> { new Bon { Key = "fire_arrow", Value = 1 } });
        //skl = main.inventory.Find(x => x.dbObj.ID == "fire_arrow");
        //MainStates.instance.Equip(main, skl, 52);

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
        
        XDdeath.contributeEach = true;
        XDdeath.autoAddExp = false;
        XDdeath.fadeAfter = 1;

        XDcombat.oneCast = true;

        Tooltip.noTooltip = false;
        
        UtilsControl.overDrop = 0;

        XDloot.asSingeItem = true;
        //move legnth in turn based games
        MainStates.skillBuyingBuff = false;
        MainStates.maxMove = 1;
        MainStates.overridesViz = new List<(string, string)>
        {
            ("hp", ""), ("buff", ""), ("shadow",""),
            ("drop", ""), ("ward", "d3:0.3"), ("status",""),
            ("select","in_battle:1"), ("dmg_track", "mana:0")
        };
        MainStates.inBattleScale = 2;
        
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

            x.AddMeta(MainStates.metaCreateLevel);
        };
        //
        
        ModelStatistics.instance.SetStatValue("unlocked_hero_warrior", 1);
        ModelStatistics.instance.SetStatValue("unlocked_hero_thief", 1);
        ModelStatistics.instance.SetStatValue("unlocked_hero_mage", 1);
        if (!succLoad)
            ModelStatistics.instance.SetStatValueStr("current_class", "hero_warrior");
        
        //pet time
        var main_pet = new RObj("empty", 1, 1, true, Vector3.zero, true, ItemType.monster, "main_pet");
        main_pet.AddViz("pet");
    }

    public void BattleEnded(ArgPass obj)
    {
        if (DungeonController.instance.inDungeon) return;
        var d1 = MainStates.instance.lastBattleTrigger.GetComponent<ObjHolder>().obj;
        
        //MainStates.instance.HandleMonsterKilled(d1);
        
        MainStates.instance.UI_win.GetComponent<ObjHolder>().obj = d1;
        
        MainStates.instance.inBattle = false;
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
        MainStates.instance.Save();
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
        HandlePrisonMovement();
        HandleAutomove();


        /*
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
        
        if (Input.GetKeyDown("g"))
        {
            MainStates.instance.AddItems(new List<Bon>{new Bon{Key = "exp", Value = 200}});
            for (int i = 0; i < 10; i++)
            {
                MainStates.instance.AddItems(new List<Bon>{new Bon{Key = "sword", Value = 1}});
            }
        }
        */

        if (MainStates.instance.inBattle)
        {
            if (!MainStates.instance.InIteration)
            {
                coroutine = StartCoroutine(MainStates.instance.OneIteration(false, 1.7f, "sword", true));
            }
        }
        else
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
        }
    }
}