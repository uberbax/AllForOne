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
    public List<Transform> demonGuis = new List<Transform>();
    [SerializeField, Min(0)] private float demonGuiDistance = 300f;
    [SerializeField, Min(0)] private float demonGuiDistanceHysteresis = 50f;
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
        UpdateDemonGuis();
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

    public List<GameObject> actives;
    public AudioClip musicMain;
    public void MusicCheck()
    {
        bool q = false;
        foreach (var a in actives) if (a.activeInHierarchy) q = true;
        if (!q && musicMain != SoundManager.instance.source.clip)
        {
            SoundManager.instance.source.clip = musicMain;
            SoundManager.instance.source.Play();
        }
    }
    
    private void LateUpdate()
    {
        UpdateDemonGuis();
        UpdateBattleHeroBars();
        MusicCheck();
        //if (canMovePrison && prisonScrollInitialized && cages != null && lift != null)
        //    lift.transform.position = cages.transform.TransformPoint(prisonLiftHome);
    }

    private void UpdateDemonGuis()
    {
        if (demonGuis == null) return;
        bool canShow = canMovePrison && cages != null && cages.activeInHierarchy &&
                       charLift != null && charLift.isActiveAndEnabled;
        var area = cages != null ? cages.transform : null;
        float characterX = canShow ? area.InverseTransformPoint(charLift.transform.position).x : 0;
        float distance = Mathf.Max(0, demonGuiDistance);
        float hysteresis = Mathf.Max(0, demonGuiDistanceHysteresis);
        foreach (var gui in demonGuis)
        {
            if (gui == null) continue;
            bool visible = canShow && Mathf.Abs(area.InverseTransformPoint(gui.position).x - characterX) <=
                distance + (gui.gameObject.activeSelf ? hysteresis : 0);
            if (gui.gameObject.activeSelf != visible) gui.gameObject.SetActive(visible);
        }
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

    private void OnDisable()
    {
        StopPrisonMovement();
        SetBattleWalking(false);
        StopStageCombat();
        HideBattleVictory();
        ClearBattleLootPouches();
        StopTrackingBattleLoot();
        RestoreBattleCamera();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            StopPrisonMovement();
            SetBattleWalking(false);
        }
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
        
        List<GameObject> toDeactiv = new List<GameObject>();
        toDeactiv.Add(managment);
        toDeactiv.AddRange(deactivateBattle);

        goBattle.onClick.AddListener(() =>
            {
                List<GameObject> pap = new List<GameObject>{backs, heroes, battleUI};
                pap.AddRange(battleBgs);
                //infoBattle.gameObject.SetActive(false);
                Transitioner.instance.Simple(map, pap, ()=>
                {
                    SpawnInventoryHeroes();
                    
                }, 
                    toDeactiv);
            }
        );
    }

    private readonly List<GameObject> battleInventoryHeroes = new List<GameObject>();
    private readonly List<RObj> battleGroundLoot = new List<RObj>();
    [SerializeField] private GameObject battleMinimap;
    [SerializeField] private ScrollRect battleItems;
    private readonly HashSet<RObj> pendingBattleLoot = new HashSet<RObj>();
    private readonly List<RObj> collectedBattleLoot = new List<RObj>();
    private MainStates battleLootState;

    public void ShowBattleMap()
    {
        if (battleItems != null) battleItems.gameObject.SetActive(false);
        if (battleMinimap != null) battleMinimap.SetActive(true);
    }

    public void ShowBattleItems()
    {
        if (battleItems == null || battleMinimap == null) return;
        battleMinimap.SetActive(false);
        battleItems.gameObject.SetActive(true);
        RefreshBattleItems();
    }

    private void BeginBattleLootTracking(MainStates state)
    {
        StopTrackingBattleLoot();
        collectedBattleLoot.Clear();
        ShowBattleMap();
        if (battleItems != null)
        {
            battleItems.StopMovement();
            battleItems.content.anchoredPosition = Vector2.zero;
        }
        battleLootState = state;
        battleLootState.ItemReceived += TrackBattleItem;
    }

    private void StopTrackingBattleLoot()
    {
        if (battleLootState != null) battleLootState.ItemReceived -= TrackBattleItem;
        battleLootState = null;
        pendingBattleLoot.Clear();
    }

    private void TrackBattleItem(RObj recipient, RObj item, float amount)
    {
        if (battleItems == null || recipient != battleLootState.mainPlayer || !pendingBattleLoot.Remove(item)) return;
        var entry = item.GetPar("max_stack") > 1f
            ? collectedBattleLoot.Find(o => o.dbObj == item.dbObj && o.shardID == item.shardID &&
                                           o.GetPar("rarity") == item.GetPar("rarity"))
            : null;
        if (entry == null)
        {
            entry = new RObj
            {
                RID = item.RID,
                dbID = item.dbID,
                dbObj = item.dbObj,
                it = item.it,
                shardID = item.shardID,
                curPars = new Dictionary<string, float>(item.curPars),
                upgradePars = new Dictionary<string, float>(item.upgradePars)
            };
            collectedBattleLoot.Add(entry);
        }
        else
        {
            amount += entry.GetPar("amount");
        }
        entry.curPars["amount"] = amount;
        entry.upgradePars["amount"] = amount;
        if (battleItems.isActiveAndEnabled) RefreshBattleItems();
    }

    private void RefreshBattleItems()
    {
        var grid = battleItems.content;
        const int columns = 3;
        const int minimumCells = 12;
        int cells = Mathf.Max(minimumCells, (collectedBattleLoot.Count + columns - 1) / columns * columns);
        for (int i = 0; i < cells; i++)
        {
            var cell = i < grid.childCount ? grid.GetChild(i) : Instantiate(grid.GetChild(0), grid);
            cell.gameObject.SetActive(true);
            var holder = cell.GetComponentInChildren<ObjHolder>(true);
            holder.obj = i < collectedBattleLoot.Count ? collectedBattleLoot[i] : null;
            holder.gameObject.SetActive(holder.obj != null);
            if (holder.obj != null) holder.OnEnable();
        }
        for (int i = cells; i < grid.childCount; i++)
            grid.GetChild(i).gameObject.SetActive(false);
    }
    [SerializeField] private Sprite battleLootPouchSprite;
    private int originalChestSiblingIndex = -1;

    [SerializeField, Min(0)] private float battleMoveSpeed = 2.5f;
    [SerializeField] private bool battleVideoFacesLeft = true;
    private readonly List<BattleHeroMovement> battleMovingHeroes = new List<BattleHeroMovement>();
    private bool battleWalking;
    private Camera battleMovementCamera;
    private float battleCameraHomeX;
    private bool battleCameraMoved;

    private sealed class BattleHeroMovement
    {
        public RObj Hero;
        public UiLoopVideo Video;
        public bool HasWalk;
    }

    private void HandleBattleMovement()
    {
        int direction = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
                        (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        MoveBattleHeroes(direction, Time.unscaledDeltaTime);
    }

    private void MoveBattleHeroes(int direction, float deltaTime)
    {
        if (heroes == null || !heroes.activeInHierarchy || battleUI == null || !battleUI.activeInHierarchy)
        {
            SetBattleWalking(false);
            RestoreBattleCamera();
            return;
        }
        if (stageCombatActive || stageCombatPending || MainStates.instance?.inBattle == true)
        {
            SetBattleWalking(false);
            return;
        }
        if (direction == 0 || battleMoveSpeed <= 0 || battleMovingHeroes.Count == 0)
        {
            SetBattleWalking(false);
            return;
        }

        if (battleMovementCamera == null || !battleMovementCamera.isActiveAndEnabled)
            battleMovementCamera = Camera.main;
        float delta = direction * battleMoveSpeed * deltaTime;
        if (battleMovementCamera == null) { SetBattleWalking(false); return; }
        if (Mathf.Approximately(delta, 0)) { SetBattleWalking(false); return; }

        SetBattleWalking(true);
        bool ready = true;
        bool hasActiveHero = false;
        foreach (var member in battleMovingHeroes)
        {
            if (member.Hero.main == null || !member.Hero.main.activeInHierarchy || member.Hero.GetPar("health") <= 0) continue;
            hasActiveHero = true;
            if (member.Video != null)
            {
                var scale = member.Video.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * direction * (battleVideoFacesLeft ? -1 : 1);
                member.Video.transform.localScale = scale;
                if (member.HasWalk && !member.Video.IsAnimationVisible("walk")) ready = false;
            }
            else member.Hero.SetScale(direction > 0);
        }
        // Wait for the decoded walk frame so the party never slides in an idle pose.
        if (!ready || !hasActiveHero) return;
        if (!battleCameraMoved)
        {
            battleCameraHomeX = battleMovementCamera.transform.position.x;
            battleCameraMoved = true;
        }
        battleMovementCamera.transform.position += Vector3.right * delta;
        foreach (var member in battleMovingHeroes)
        {
            if (member.Hero.main == null || !member.Hero.main.activeInHierarchy || member.Hero.GetPar("health") <= 0) continue;
            member.Hero.main.transform.position += Vector3.right * delta;
            member.Hero.Position = member.Hero.main.transform.position;
        }
    }

    private void RestoreBattleCamera()
    {
        if (battleCameraMoved && battleMovementCamera != null)
        {
            var position = battleMovementCamera.transform.position;
            position.x = battleCameraHomeX;
            battleMovementCamera.transform.position = position;
        }
        battleCameraMoved = false;
    }

    private void SetBattleWalking(bool walking)
    {
        if (battleWalking == walking) return;
        battleWalking = walking;
        foreach (var member in battleMovingHeroes)
        {
            if (member.Video != null)
            {
                if (!walking || member.HasWalk) member.Video.Play(walking ? "walk" : "idle");
            }
            else if (member.Hero.HasVis("animator"))
                member.Hero.visuals["animator"].GetComponent<XDanimator>()?.SetState(walking ? "walk" : "idle");
        }
    }


    private bool stageCombatActive;
    private bool stageCombatPending;
    [SerializeField] private CanvasGroup battleVictoryBanner;
    private Coroutine battleVictoryRoutine;

    private static bool HasLivingStageUnit(List<GameObject> units)
    {
        foreach (var unit in units)
            if (unit != null && unit.TryGetComponent<ObjHolder>(out var holder) &&
                holder.obj != null && holder.obj.GetPar("health") > 0)
                return true;
        return false;
    }

    private static bool HasStageActionInProgress(List<GameObject> units)
    {
        foreach (var unit in units)
            if (unit != null && unit.TryGetComponent<UnitActionTracker>(out var tracker) && tracker.Busy)
                return true;
        return false;
    }

    private void CheckStageCombatOutcome()
    {
        if (!stageCombatActive) return;
        bool alliesAlive = HasLivingStageUnit(battleInventoryHeroes);
        bool enemiesAlive = HasLivingStageUnit(battleSpawnedEnemies);
        if (alliesAlive && enemiesAlive) return;
        if (HasStageActionInProgress(battleInventoryHeroes) || HasStageActionInProgress(battleSpawnedEnemies)) return;

        StopStageCombat();
        ModelStatistics.instance.SetStatValueForce("battle", 1);
        if (!alliesAlive || enemiesAlive) return;
        if (battleVictoryBanner != null)
            battleVictoryRoutine = StartCoroutine(AnimateBattleVictory());
    }

    private void HideBattleVictory()
    {
        if (battleVictoryRoutine != null) StopCoroutine(battleVictoryRoutine);
        battleVictoryRoutine = null;
        if (battleVictoryBanner != null) battleVictoryBanner.gameObject.SetActive(false);
    }

    private IEnumerator AnimateBattleVictory()
    {
        const float fadeIn = .4f;
        const float hold = 2.7f;
        const float fadeOut = .65f;
        battleVictoryBanner.alpha = 0;
        battleVictoryBanner.transform.SetAsLastSibling();
        battleVictoryBanner.gameObject.SetActive(true);
        float started = Time.unscaledTime;
        while (battleVictoryBanner != null && battleUI.activeInHierarchy)
        {
            float elapsed = Time.unscaledTime - started;
            if (elapsed >= fadeIn + hold + fadeOut) break;
            float appear = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / fadeIn));
            float disappear = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - fadeIn - hold) / fadeOut));
            battleVictoryBanner.alpha = appear * (1 - disappear);
            battleVictoryBanner.transform.localScale = Vector3.one * Mathf.Lerp(.94f, 1, appear);
            yield return null;
        }
        if (battleVictoryBanner != null) battleVictoryBanner.gameObject.SetActive(false);
        battleVictoryRoutine = null;
    }

    private void PrepareStageCombat(RObj unit)
    {
        // Inventory clones may inherit this tag, but their new RID must be registered separately.
        unit.META_TAGS.RemoveAll(meta => meta == "sword");
        unit.AddMeta("sword");
        MainStates.instance.all[unit.RID] = unit;
        unit.AddViz("combat#no:1");
        foreach (var skill in unit.actSkills) skill.SetPar("action_req", -1);
    }

    private bool StageCombatReady()
    {
        foreach (var bar in battleHeroBars)
        {
            var unit = bar.Hero;
            if (unit?.main == null || !unit.HasVis("combat")) return false;
            var combat = unit.visuals["combat"].GetComponent<XDcombat>();
            if (combat == null || combat.mon != unit) return false;
        }
        return battleHeroBars.Count > 0;
    }

    private void StopStageCombat()
    {
        if (!stageCombatActive && !stageCombatPending) return;
        stageCombatActive = stageCombatPending = false;
        var state = MainStates.instance;
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
        if (state != null)
        {
            state.inBattle = false;
            state.InIteration = false;
            foreach (var bar in battleHeroBars)
            {
                var unit = bar.Hero;
                if (unit == null) continue;
                state.awaitUnits.Remove(unit.RID);
                if (state.allMetas.TryGetValue(unit.RID, out var meta) && ReferenceEquals(meta, unit))
                    state.allMetas.Remove(unit.RID);
            }
        }
    }


    private void SpawnInventoryHeroes()
    {
        var state = MainStates.instance;
        var resources = ResourceHolder.instance;
        var positions = heroes != null ? heroes.transform.Find("heroes") : null;
        if (state == null || state.mainPlayer == null || resources == null ||
            DatabaseAll.instance == null || positions == null || positions.childCount == 0)
        {
            Debug.LogWarning("Cannot spawn inventory heroes: missing player, resources or HEROES/heroes positions.", this);
            return;
        }

        StopStageCombat();
        HideBattleVictory();
        RestoreBattleCamera();
        foreach (var spawned in battleInventoryHeroes)
        {
            if (spawned == null) continue;
            var oldHolder = spawned.GetComponent<ObjHolder>();
            if (oldHolder != null && oldHolder.obj != null) RemoveBattleEnemyData(oldHolder.obj);
            spawned.SetActive(false);
            Destroy(spawned);
        }
        battleInventoryHeroes.Clear();
        BeginBattleLootTracking(state);
        battleMovingHeroes.Clear();
        battleWalking = false;

        int index = 0;
        int columns = positions.childCount;
        foreach (var hero in state.mainPlayer.inventory)
        {
            if (hero == null || hero.it != ItemType.monster || hero.dbObj == null) continue;
            if (!resources.monsters.TryGetValue(hero.dbObj.ID, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"No monster prefab for inventory hero '{hero.dbObj.ID}'.", this);
                continue;
            }

            var slot = positions.GetChild(index % columns);
            var position = slot.position + positions.TransformVector(Vector3.down * (1.5f * (index / columns)));
            var copy = hero.Clone();
            copy.RID = "battle_hero_" + Guid.NewGuid().ToString("N");
            var visual = DatabaseAll.instance.CreateOnlyVizual(copy, position);
            battleInventoryHeroes.Add(visual);
            visual.transform.SetParent(heroes.transform, true);
            copy.Position = position;
            copy.SetScale(true);
            PrepareBattleHeroCanvas(copy, slot);
            AddBattleHeroShadow(copy, slot);
            var video = copy.visMain.GetComponent<UiLoopVideo>();
            copy.AddViz(video != null ? "animator" : "animator#pr:1");
            copy.AddViz("coll#val:1.3,scale:1.3");
            copy.AddViz("dmg_track");
            copy.AddViz("death");
            PrepareStageCombat(copy);
            state.awaitUnits[copy.RID] = 1;
            battleMovingHeroes.Add(new BattleHeroMovement
            {
                Hero = copy, Video = video, HasWalk = video != null && video.HasAnimation("walk")
            });
            index++;
        }

        SpawnBattleEnemies();
        BindBattleHeroBars();

        // Keep the position markers, but hide their placeholder portraits.
        for (int i = 0; i < columns; i++)
        {
            var placeholder = positions.GetChild(i).GetComponent<SpriteRenderer>();
            if (placeholder != null) placeholder.enabled = false;
        }
    }


    private readonly List<GameObject> battleSpawnedEnemies = new List<GameObject>();

    private void SpawnBattleEnemies()
    {
        var state = MainStates.instance;
        if (state != null) state.lastTargetSelected = null;
        ClearBattleLootPouches();
        foreach (var spawned in battleSpawnedEnemies)
        {
            if (spawned == null) continue;
            var holder = spawned.GetComponent<ObjHolder>();
            if (holder != null && holder.obj != null) RemoveBattleEnemyData(holder.obj);
            spawned.SetActive(false);
            Destroy(spawned);
        }
        battleSpawnedEnemies.Clear();
        var positions = heroes != null ? heroes.transform.Find("enemy") : null;
        if (positions == null || state == null || ResourceHolder.instance == null || DatabaseAll.instance == null) return;
        string[] ids = { "zombie1", "zombie3" };
        for (int i = 0; i < ids.Length && i < positions.childCount; i++)
        {
            var id = ids[i];
            if (!DatabaseAll.instance.heroes.ContainsKey(id) ||
                !ResourceHolder.instance.monsters.TryGetValue(id, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"Cannot spawn battle enemy '{id}': missing hero config or monster prefab.", this);
                continue;
            }
            var slot = positions.GetChild(i);
            var position = slot.position;
            if (battleInventoryHeroes.Count > 0) position.y = battleInventoryHeroes[0].transform.position.y;
            // Battle stage visuals do not participate in the overworld distance culling (allMons).
            var enemy = new RObj(id, 1, 1, false, position, false, ItemType.monster, isEnemy: true);
            DatabaseAll.instance.CreateOnlyVizual(enemy, position);
            battleSpawnedEnemies.Add(enemy.main);
            enemy.main.transform.SetParent(heroes.transform, true);
            enemy.Position = position;
            enemy.SetScale(false);
            PrepareBattleHeroCanvas(enemy, slot);
            AddBattleHeroShadow(enemy, slot);
            enemy.AddViz(enemy.visMain.GetComponent<UiLoopVideo>() != null ? "animator" : "animator#pr:1");
            enemy.AddViz("coll#val:1.3,scale:1.3");
            enemy.AddViz("dmg_track");
            enemy.AddViz("drop");
            if (battleLootPouchSprite != null && enemy.visuals.TryGetValue("drop", out var drop) && drop != null &&
                drop.TryGetComponent<XDdrop>(out var dropBehavior))
            {
                var marker = slot.GetComponent<SpriteRenderer>();
                int layer = marker != null ? marker.sortingLayerID : 0;
                int order = marker != null ? marker.sortingOrder + 1 : 101;
                dropBehavior.UseGroundPouch((unit, items) => SpawnBattleLootPouch(unit, items, layer, order));
            }
            enemy.AddViz("death");
            PrepareStageCombat(enemy);
            BattleEnemyClick.Attach(enemy);
        }
        for (int i = 0; i < positions.childCount; i++)
        {
            var placeholder = positions.GetChild(i).GetComponent<SpriteRenderer>();
            if (placeholder != null) placeholder.enabled = false;
        }
        
        //
        //extra
        ModelStatistics.instance.SetStatValue("battle", 2);
        stageCombatPending = battleSpawnedEnemies.Count > 0 && battleInventoryHeroes.Count > 0;
        stageCombatActive = stageCombatPending;
        MainStates.instance.inBattle = stageCombatActive;
    }

    private void SpawnBattleLootPouch(RObj unit, List<Bon> drops, int sortingLayer, int sortingOrder)
    {
        var inventory = new RObj();
        var state = MainStates.instance;
        foreach (var drop in drops)
        {
            if (drop.Value <= 0 || !ResourceHolder.instance.items.ContainsKey(drop.Key)) continue;
            var item = DatabaseAll.instance.CreateItem(drop.Key, drop.Value, withEmpty: false, withVisual: false);
            if (item.it == ItemType.item && ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.randomize_drop_stats) > 0)
                state.RandomizeItemStats(item);
            item.index = inventory.GetFreeIndex(item.dbObj.sizeX, item.dbObj.sizeY);
            item.owner = inventory;
            inventory.inventory.Add(item);
            if (battleItems != null) pendingBattleLoot.Add(item);
        }
        if (inventory.inventory.Count == 0) return;

        var ground = unit.visMain != null ? unit.visMain.transform.Find("legs") : null;
        inventory.Position = ground != null ? ground.position : unit.main.transform.position;
        var pouch = new GameObject("DarkPouch", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster),
            typeof(Image), typeof(Button));
        inventory.main = pouch;
        pouch.layer = battleUI.layer;
        var rect = (RectTransform)pouch.transform;
        rect.SetParent(heroes.transform, false);
        rect.pivot = new Vector2(.5f, 0);
        rect.sizeDelta = new Vector2(64, 64);
        rect.localScale = Vector3.one * .009f;
        rect.position = inventory.Position;
        var canvas = pouch.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingLayerID = sortingLayer;
        canvas.sortingOrder = sortingOrder;

        var image = pouch.GetComponent<Image>();
        image.sprite = battleLootPouchSprite;
        image.preserveAspect = true;
        var button = pouch.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => OpenBattleLootPouch(inventory));
        battleGroundLoot.Add(inventory);
    }

    private void OpenBattleLootPouch(RObj pouch)
    {
        var state = MainStates.instance;
        if (state == null || state.UI_second == null || pouch.main == null) return;
        var chest = state.UI_second;
        if (originalChestSiblingIndex < 0) originalChestSiblingIndex = chest.transform.GetSiblingIndex();
        SoundManager.instance.PlayAny("chest_open");
        state.curLoot = pouch;
        chest.transform.SetAsLastSibling();
        if (!chest.activeSelf) chest.SetActive(true);
        else UIfiller.GlobalRefresh();
    }

    private void RemoveBattleLootPouch(int index)
    {
        var pouch = battleGroundLoot[index];
        var state = MainStates.instance;
        if (state != null)
        {
            if (ReferenceEquals(state.curLoot, pouch))
            {
                if (state.UI_second != null) state.UI_second.SetActive(false);
                state.curLoot = null;
            }
            foreach (var item in pouch.inventory)
                if (ReferenceEquals(item.owner, pouch))
                {
                    pendingBattleLoot.Remove(item);
                    state.all.Remove(item.RID);
                }
        }
        if (pouch.main != null) Destroy(pouch.main);
        battleGroundLoot.RemoveAt(index);
    }

    private void UpdateBattleLootPouches()
    {
        for (int i = battleGroundLoot.Count - 1; i >= 0; i--)
            if (!battleGroundLoot[i].inventory.Exists(item => item.GetPar("amount") > 0))
                RemoveBattleLootPouch(i);
        RestoreChestOrderIfClosed();
    }

    private void ClearBattleLootPouches()
    {
        for (int i = battleGroundLoot.Count - 1; i >= 0; i--)
            RemoveBattleLootPouch(i);
        RestoreChestOrderIfClosed();
    }

    private void RestoreChestOrderIfClosed()
    {
        if (originalChestSiblingIndex < 0) return;
        var chest = MainStates.instance?.UI_second;
        if (chest != null && chest.activeSelf) return;
        if (chest != null) chest.transform.SetSiblingIndex(originalChestSiblingIndex);
        originalChestSiblingIndex = -1;
    }

    private void RemoveBattleEnemyData(RObj enemy)
    {
        // These objects and their owned skills were created only for this battle entry.
        foreach (var skill in enemy.actSkills) RemoveBattleEnemyData(skill);
        foreach (var passive in enemy.buffs) RemoveBattleEnemyData(passive);
        var state = MainStates.instance;
        if (state == null) return;
        state.awaitUnits.Remove(enemy.RID);
        state.combats.Remove(enemy);
        if (state.allMetas.TryGetValue(enemy.RID, out var meta) && ReferenceEquals(meta, enemy))
            state.allMetas.Remove(enemy.RID);
        if (state.all.TryGetValue(enemy.RID, out var registered) && ReferenceEquals(registered, enemy))
            state.all.Remove(enemy.RID);
        if (state.allMons.TryGetValue(enemy.RID, out var monster) && ReferenceEquals(monster, enemy))
            state.allMons.Remove(enemy.RID);
    }


    private readonly List<BattleHeroBar> battleHeroBars = new List<BattleHeroBar>();
    private readonly List<RectTransform> battleBarSlots = new List<RectTransform>();
    private Camera battleBarsCamera;

    private sealed class BattleHeroBar
    {
        public RObj Hero;
        public RectTransform Rect;
        public RectTransform Parent;
        public Canvas Canvas;
        public Slider Hp;
        public Slider Mp;
    }

    private void BindBattleHeroBars()
    {
        battleHeroBars.Clear();
        BindBattleBars("Team", battleInventoryHeroes);
        BindBattleBars("Enemy", battleSpawnedEnemies);
        UpdateBattleHeroBars();
    }

    private void BindBattleBars(string groupName, List<GameObject> units)
    {
        var team = battleUI != null ? battleUI.transform.Find("bars/" + groupName) : null;
        if (team == null)
        {
            Debug.LogWarning($"Cannot bind battle bars: BATTLE/bars/{groupName} is missing.", this);
            return;
        }

        battleBarSlots.Clear();
        for (int i = 0; i < team.childCount; i++)
        {
            var child = team.GetChild(i) as RectTransform;
            if (child != null && child.Find("HpBar") != null && child.Find("MpBar") != null)
                battleBarSlots.Add(child);
        }
        if (battleBarSlots.Count == 0) return;
        Canvas.ForceUpdateCanvases();
        while (battleBarSlots.Count < units.Count)
            battleBarSlots.Add(Instantiate(battleBarSlots[0], team));

        battleBarsCamera = Camera.main;
        for (int i = 0; i < battleBarSlots.Count; i++)
        {
            var rect = battleBarSlots[i];
            // The bars follow projected hero positions instead of the horizontal layout.
            var layout = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            bool used = i < units.Count;
            var holder = rect.GetComponent<ObjHolder>() ?? rect.gameObject.AddComponent<ObjHolder>();
            holder.noTrack = true;
            holder.obj = used ? units[i].GetComponent<ObjHolder>().obj : null;
            rect.gameObject.SetActive(used);
            if (!used) continue;

            var hp = rect.Find("HpBar").GetComponent<Slider>();
            var mp = rect.Find("MpBar").GetComponent<Slider>();
            if (hp == null || mp == null) continue;
            hp.minValue = mp.minValue = 0;
            hp.maxValue = mp.maxValue = 1;
            hp.wholeNumbers = mp.wholeNumbers = false;
            hp.interactable = mp.interactable = false;
            // Disabled Sliders accept values but do not update their fill graphics.
            hp.enabled = mp.enabled = true;
            battleHeroBars.Add(new BattleHeroBar
            {
                Hero = holder.obj, Rect = rect, Parent = rect.parent as RectTransform,
                Canvas = rect.GetComponentInParent<Canvas>(), Hp = hp, Mp = mp
            });
        }
    }

    private void UpdateBattleHeroBars()
    {
        if (battleHeroBars.Count == 0 || battleUI == null || !battleUI.activeInHierarchy) return;
        if (battleBarsCamera == null || !battleBarsCamera.isActiveAndEnabled) battleBarsCamera = Camera.main;
        foreach (var bar in battleHeroBars)
        {
            if (bar.Rect == null) continue;
            bool visible = bar.Hero?.main != null && bar.Hero.main.activeInHierarchy;
            if (bar.Rect.gameObject.activeSelf != visible) bar.Rect.gameObject.SetActive(visible);
            if (!visible) continue;

            float maxHp = bar.Hero.GetPar("max_health");
            float maxMp = bar.Hero.GetPar("max_mana");
            bar.Hp.SetValueWithoutNotify(maxHp > 0 ? Mathf.Clamp01(bar.Hero.GetPar("health") / maxHp) : 0);
            bar.Mp.SetValueWithoutNotify(maxMp > 0 ? Mathf.Clamp01(bar.Hero.GetPar("mana") / maxMp) : 0);
            if (battleBarsCamera == null || bar.Parent == null || bar.Canvas == null) continue;
            var screen = battleBarsCamera.WorldToScreenPoint(bar.Hero.main.transform.position);
            var canvas = bar.Canvas.rootCanvas;
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bar.Parent, screen, uiCamera, out var point))
            {
                var position = bar.Rect.localPosition;
                position.x = point.x;
                bar.Rect.localPosition = position;
            }
        }
    }


    [SerializeField] private Sprite battleShadowSprite;

    private void AddBattleHeroShadow(RObj hero, Transform slot)
    {
        var canvas = hero.visMain.GetComponentInParent<Canvas>();
        var videoImage = hero.visMain.GetComponent<RawImage>();
        if (canvas != null && videoImage != null)
        {
            if (!ResourceHolder.instance.XD.TryGetValue("shadow", out var template) || template == null) return;
            var source = template.GetComponentInChildren<SpriteRenderer>(true);
            if (source == null || source.sprite == null) return;

            var shadow = new GameObject("Foot Shadow", typeof(RectTransform), typeof(Image));
            shadow.layer = videoImage.gameObject.layer;
            var rect = (RectTransform)shadow.transform;
            rect.SetParent(canvas.transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            var legs = hero.visMain.transform.Find("legs");
            rect.position = legs != null ? legs.position : hero.main.transform.position;
            var scale = canvas.transform.lossyScale;
            var size = source.sprite.bounds.size * 1.4f;
            rect.sizeDelta = new Vector2(size.x / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
                size.y / Mathf.Max(.0001f, Mathf.Abs(scale.y)));
            var image = shadow.GetComponent<Image>();
            image.sprite = battleShadowSprite != null ? battleShadowSprite : source.sprite;
            rect.localScale = new Vector3(1, .7f, 1);
            image.color = new Color(0, 0, 0, .65f);
            image.raycastTarget = false;
            hero.visuals["shadow"] = shadow;
            return;
        }

        hero.AddViz("shadow");
        if (!hero.HasVis("shadow")) return;
        var placeholder = slot.GetComponent<SpriteRenderer>();
        foreach (var renderer in hero.visuals["shadow"].GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingLayerID = placeholder != null ? placeholder.sortingLayerID : 0;
            renderer.sortingOrder = (placeholder != null ? placeholder.sortingOrder : 100) - 1;
        }
    }

    private void PrepareBattleHeroCanvas(RObj hero, Transform slot)
    {
        var image = hero.visMain.GetComponent<RawImage>();
        if (image == null) return;

        var rect = image.rectTransform;
        var placeholder = slot.GetComponent<SpriteRenderer>();
        float targetHeight = placeholder != null && placeholder.sprite != null
            ? placeholder.bounds.size.y : 3.3f;
        float sourceHeight = rect.rect.height * Mathf.Abs(rect.localScale.y);
        var canvasObject = new GameObject("Hero Video Canvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(hero.main.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerID = placeholder != null ? placeholder.sortingLayerID : 0;
        canvas.sortingOrder = placeholder != null ? placeholder.sortingOrder : 100;
        var canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.sizeDelta = rect.rect.size;
        canvasRect.localScale = Vector3.one * (targetHeight / Mathf.Max(.001f, sourceHeight));
        rect.SetParent(canvasRect, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition3D = Vector3.zero;
        image.raycastTarget = false;

        var legs = rect.Find("legs");
        if (legs != null)
            canvasRect.position += hero.main.transform.position - legs.position;
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

    public List<GameObject> battleBgs = new List<GameObject>();
    public List<GameObject> deactivateBattle = new List<GameObject>();

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
        //main.AddViz("click_move");
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

        XDdmgTrack.scaleMult = 0.3f;
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
        if (AlwaysMove && !stageCombatActive && !stageCombatPending && MainStates.instance?.inBattle != true)
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
        HandleBattleMovement();
        HandleAutomove();
        UpdateBattleLootPouches();


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

        if (stageCombatActive && (battleUI == null || !battleUI.activeInHierarchy ||
            heroes == null || !heroes.activeInHierarchy)) StopStageCombat();
        if (stageCombatPending)
        {
            if (!StageCombatReady()) return;
            stageCombatPending = false;
        }
        CheckStageCombatOutcome();
        if (MainStates.instance.inBattle)
        {
            if (!MainStates.instance.InIteration)
            {
                coroutine = StartCoroutine(MainStates.instance.OneIteration(tm: 0, metaContain: "sword", awaitUnits: true));
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
