using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum WhoHeroesPhase
{
    Bootstrap,
    Day,
    Night,
    GameOver
}

public enum WhoHeroesExpeditionPhase
{
    Idle,
    Traveling,
    Battle,
    ReturnPending
}

[Serializable]
public sealed class WhoHeroesRuntimeData
{
    public PlayerData gameStatistics = new PlayerData();
    public WhoHeroesRunState run = new WhoHeroesRunState();
    public WhoHeroesRunState nightCheckpoint;
}

[Serializable]
public sealed class WhoHeroesRunState
{
    public List<WhoHeroesRunObjectState> objects = new List<WhoHeroesRunObjectState>();
    public List<WhoHeroesRunInventoryState> inventories = new List<WhoHeroesRunInventoryState>();
}

[Serializable]
public sealed class WhoHeroesRunObjectState
{
    public string id;
    public int level;
}

[Serializable]
public sealed class WhoHeroesRunInventoryState
{
    public string ownerId;
    public List<WhoHeroesRunItemState> items = new List<WhoHeroesRunItemState>();
}

[Serializable]
public sealed class WhoHeroesRunItemState
{
    public string id;
    public int amount;
    public int level;
    public int usedSlot;
}

[Serializable]
public sealed class WhoHeroesMineWorkers
{
    public string mineId;
    public List<GameObject> workers = new List<GameObject>();
}

public static class WhoHeroesModelStatistics
{
    private const string SaveFileName = "whoheroes.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static PlayerData GetWhoHeroesPlayerData(this ModelStatistics model)
    {
        return MainStates.instance?.playerData ?? new PlayerData();
    }

    public static void BindWhoHeroesPlayerData(this ModelStatistics model, PlayerData data)
    {
        if (MainStates.instance != null)
            MainStates.instance.playerData = data ?? new PlayerData();
    }

    public static WhoHeroesRuntimeData LoadWhoHeroesRuntimeData(this ModelStatistics model)
    {
        if (model == null || !File.Exists(SavePath))
            return null;
        var json = File.ReadAllText(SavePath);
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<WhoHeroesRuntimeData>(json);
    }

    public static void SaveWhoHeroesRuntimeData(this ModelStatistics model, WhoHeroesRuntimeData data)
    {
        if (model == null)
            return;
        var payload = new WhoHeroesRuntimeData
        {
            gameStatistics = CaptureWhoHeroesPlayerData(data?.gameStatistics),
            run = data?.run ?? new WhoHeroesRunState(),
            nightCheckpoint = data?.nightCheckpoint
        };
        File.WriteAllText(SavePath, JsonUtility.ToJson(payload));
    }

    public static void StartWhoHeroesTasks(this ModelStatistics model, IEnumerable<string> taskIds)
    {
        if (model == null || MainStates.instance?.playerData?.playerTasks == null || taskIds == null)
            return;

        var requiredIds = new HashSet<string>(taskIds, StringComparer.Ordinal);
        foreach (var progress in MainStates.instance.playerData.playerTasks)
            if (progress != null && progress.started == 0 && requiredIds.Contains(progress.id))
                progress.started = 1;
    }

    private static PlayerData CaptureWhoHeroesPlayerData(PlayerData source)
    {
        if (source == null)
            return new PlayerData();

        return new PlayerData
        {
            pGame = source.pGame ?? new PGame(),
            cGame = source.cGame ?? new CGame(),
            playerStats = source.playerStats?.Select(CloneWhoHeroesBon).ToList() ?? new List<Bon>(),
            dynTaken = source.dynTaken == null ? new List<string>() : new List<string>(source.dynTaken),
            playerTasks = CloneWhoHeroesTasks(source.playerTasks),
            playerShop = CloneWhoHeroesTasks(source.playerShop),
            playerMail = CloneWhoHeroesTasks(source.playerMail),
            buildings = source.buildings == null ? new List<Building>() : new List<Building>(source.buildings),
            inventory = new List<RObj>()
        };
    }

    private static Bon CloneWhoHeroesBon(Bon source)
    {
        return source == null
            ? new Bon()
            : new Bon { Key = source.Key, Value = source.Value, Val2 = source.Val2, Val3 = source.Val3 };
    }

    private static List<TasksProg> CloneWhoHeroesTasks(IEnumerable<TasksProg> source)
    {
        return source == null
            ? new List<TasksProg>()
            : source.Where(value => value != null).Select(value => new TasksProg
            {
                id = value.id,
                completed = value.completed,
                taken = value.taken,
                takenTime = value.takenTime,
                startTime = value.startTime,
                curNum = value.curNum,
                startStat = value.startStat,
                started = value.started
            }).ToList();
    }
}

public sealed partial class MainCycle_WhoHeroes : MonoBehaviour
{
    private const string ParseEndedEvent = "PARSE_ENDED";
    private const string GameStartEvent = "game_start";
    private const string BootstrapReadyEvent = "whoheroes_bootstrap_ready";
    private const string NewDayEvent = "new_day";
    private const string NewNightEvent = "new_night";
    private const string PhaseChangedEvent = "whoheroes_phase_changed";
    private const string DayProgressEvent = WhoHeroesEvents.DayProgress;
    private const string GameOverEvent = "whoheroes_game_over";

    private const string AvailableParam = "whoheroes_available";
    private const string TerritoryConfigParam = "found_in";
    private const string NightAdditionConfigParam = "encounter";

    private const string DayStat = "whoheroes_day";
    private const string NightStat = "whoheroes_night";
    private const string PhaseStat = "whoheroes_phase";
    private const string DayElapsedSecondsStat = "whoheroes_day_elapsed_seconds";
    private const string RunInitializedStat = "whoheroes_run_initialized";
    private const string BestNightStat = "whoheroes_best_night";
    private const string TraderCompletedDayStat = "whoheroes_trader_completed_day";
    private const string DayDurationMeta = "whoheroes_day_duration";
    private const string DailyGoldMeta = "whoheroes_daily_gold";
    private const string TerritoryGoldMeta = "whoheroes_territory_gold";
    private const string MineMaxLevelMeta = "whoheroes_mine_max_level";
    private const string WoodProductionIntervalMeta = "whoheroes_wood_production_interval";
    private const string StoneProductionIntervalMeta = "whoheroes_stone_production_interval";

    private const string ProductionDayParam = "whoheroes_production_day";
    private const string ProductionStartParam = "whoheroes_production_start";
    private const string ProductionCyclesParam = "whoheroes_production_cycles";
    private const string ProductionNextParam = "whoheroes_production_next";
    private const string DeliveryTargetName = "DeliveryOut";
    private const string StatModifierMarkerPrefix = "whoheroes_stat_modifier_";

    private static readonly HashSet<string> ManagementActions = new HashSet<string>(StringComparer.Ordinal)
    {
        "buy",
        "upgrade",
        "restore",
        "reroll",
        "trade",
        "take_quest",
        "start_expedition",
        "equip_exp",
        "unequip_exp"
    };

    public static MainCycle_WhoHeroes Instance { get; private set; }

    [Header("Bootstrap")]
    [SerializeField] private string playerConfigId = "hero";
    [SerializeField] private GameObject playerAnchor;
    [SerializeField] private GUIStartScreen startScreen;
    [SerializeField] private bool applyPlayerConfig = true;
    [SerializeField] private bool autoStartWithoutVisibleStartScreen = true;

    [Header("Economy scene wiring")]
    [SerializeField] private List<SpaumPoint> deliveryPoints = new List<SpaumPoint>();
    [SerializeField] private GameObject deliveryCarrierPrefab;
    [SerializeField] private List<WhoHeroesMineWorkers> mineWorkers = new List<WhoHeroesMineWorkers>();
    [SerializeField, Min(1f)] private float carrierSpeedDivisor = 100f;

    [Header("Expedition Minimus wiring")]
    [SerializeField] private Transform expeditionPathsRoot;
    [SerializeField] private SpaumPoint expeditionDepartureSpawner;
    [SerializeField] private WaveSpawner expeditionBattleSpawner;
    [SerializeField] private BattleController expeditionBattleController;
    [SerializeField, Min(0.1f)] private float expeditionMaxRouteEndDistance = 4f;

    [Header("Night Minimus wiring")]
    [SerializeField] private WaveSpawner nightBattleSpawner;
    [SerializeField] private BattleController nightBattleController;
    [SerializeField] private Transform nightPrinceSpawn;
    [SerializeField] private Transform nightCastleGate;
    [SerializeField] private Transform nightCameraTarget;
    [SerializeField] private List<Transform> nightDefenseRows = new List<Transform>();
    [SerializeField, Min(0.1f)] private float nightFormationUnitSpacing = 0.55f;
    [SerializeField, Min(0.1f)] private float nightFormationShiftSpeed = 3f;
    [SerializeField, Min(0.1f)] private float nightDemonSpawnRadius = 0.35f;
    [SerializeField, Min(0.1f)] private float nightDemonAggroRange = 5f;
    [SerializeField, Min(0.1f)] private float nightCastleGateRadius = 0.75f;
    [SerializeField] private Camera nightCamera;
    [SerializeField] private BRATViewMapCameraController nightViewMapCameraController;
    [SerializeField, Min(0.1f)] private float nightCameraSize = 4f;

    [Header("Run state")]
    [SerializeField] private WhoHeroesPhase phase = WhoHeroesPhase.Bootstrap;
    [SerializeField, Min(1)] private int dayNumber = 1;
    [SerializeField, Min(0)] private int nightNumber;
    [SerializeField] private bool restoreKingHealthAtDawn = true;

    private bool initialized;
    private bool gameStarted;
    private bool gameStartRequested;
    private bool portalProgressionInitializationStarted;
    private bool portalProgressionReady;
    private bool economyReady;
    private bool castleShopReady;
    private bool tavernShopReady;
    private int lastPublishedSecond = -1;
    private bool missingGoldConfigReported;
    private readonly List<RObj> portalProgression = new List<RObj>();
    private readonly List<Bon> nightWaveSnapshot = new List<Bon>();
    private readonly Dictionary<RObj, FormatBattles> nightBattleSnapshot =
        new Dictionary<RObj, FormatBattles>();
    private readonly List<RObj> mines = new List<RObj>();
    private readonly Dictionary<string, SpaumPoint> deliverySpawners =
        new Dictionary<string, SpaumPoint>(StringComparer.OrdinalIgnoreCase);
    private readonly List<PendingDelivery> pendingDeliveries = new List<PendingDelivery>();
    private readonly HashSet<string> reportedPortalConfigErrors = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> reportedEconomyErrors = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> capturedTargetIds = new HashSet<string>(StringComparer.Ordinal);
    private Transform deliveryTarget;
    private Vector3 deliveryResourceIconLocalPosition = new Vector3(0f, 0.42f, -0.1f);
    private Vector2 deliveryResourceIconWorldSize = new Vector2(0.06f, 0.06f);
    private int deliveryResourceIconSortingLayerId;
    private int deliveryResourceIconSortingOrder;
    private WhoHeroesRunState pendingRunSnapshot;
    private bool restoringSavedRun;
    private bool runSnapshotApplied;
    private bool runSaveDirty;
    private float nextRunSaveTime;
    private bool suppressDestroySave;
    private bool shutdownSnapshotSaved;
    private WhoHeroesRuntimeData runtimeData = new WhoHeroesRuntimeData();

    private sealed class PendingDelivery
    {
        public RObj mine;
        public string resourceId;
        public int amount;
        public GameObject carrier;
        public GameObject sourceResourceIcon;
        public GameObject resourceIcon;
        public WhoHeroesCarrierStateMachine stateMachine;
        public bool settled;
    }

    public WhoHeroesPhase Phase => phase;
    public int DayNumber => dayNumber;
    public int NightNumber => nightNumber;
    public float DayDurationSeconds => ResolveDayDuration();
    public float DayElapsedSeconds => Mathf.Clamp(
        TimeManager.instance == null ? 0f : TimeManager.instance.tm, 0f, DayDurationSeconds);
    public float DayRemainingSeconds => Mathf.Max(0f, DayDurationSeconds - DayElapsedSeconds);
    public float DayProgress01 => phase == WhoHeroesPhase.Day
        ? Mathf.Clamp01(DayElapsedSeconds / DayDurationSeconds)
        : phase == WhoHeroesPhase.Night || phase == WhoHeroesPhase.GameOver ? 1f : 0f;
    public bool ManagementLocked => !initialized || phase != WhoHeroesPhase.Day;
    public bool TraderAvailableToday => initialized && phase == WhoHeroesPhase.Day &&
                                        nightNumber >= MainCycle_WhoHeroes.TraderStartNight() &&
                                        (ModelStatistics.instance == null ||
                                         ModelStatistics.instance.GetStatValue(TraderCompletedDayStat, false) != dayNumber);
    public IReadOnlyList<Bon> NightWaveSnapshot => nightWaveSnapshot;

    public float GetMineProductionProgress01(RObj mine)
    {
        if (mine == null || phase != WhoHeroesPhase.Day || GUILIB.Level(mine) <= 0)
            return 0f;
        foreach (var delivery in pendingDeliveries)
            if (delivery.mine == mine && !delivery.settled && delivery.stateMachine != null &&
                delivery.stateMachine.State == WhoHeroesCarrierStateMachine.CarrierState.ToMine)
                return 1f;
        var interval = mine.GetPar("timer");
        var nextProduction = mine.GetPar(ProductionNextParam);
        if (interval <= 0f || nextProduction <= 0f)
            return 0f;
        return Mathf.Clamp01(1f - Mathf.Max(0f, nextProduction - DayElapsedSeconds) / interval);
    }
    public IReadOnlyDictionary<RObj, FormatBattles> NightBattleSnapshot => nightBattleSnapshot;
    public int ActiveNightPortalCount => nightBattleSnapshot.Count;
    public int ForecastNightNumber => phase == WhoHeroesPhase.Day ? nightNumber + 1 : nightNumber;
    public RObj NextLockedPortal => portalProgressionReady
        ? portalProgression.FirstOrDefault(value => GUILIB.Level(value) <= 0 && !IsRestorableTarget(value))
        : null;

    private void Awake()
    {
        Instance = this;
        InitializeExpeditionOrchestration();
        InitializeNightOrchestration();
        EventManager.SUB(ParseEndedEvent, OnParseEnded);
        EventManager.SUB(GameStartEvent, OnGameStart);
        EventManager.SUB(WhoHeroesEvents.Refresh, OnWhoHeroesRefresh);
        EventManager.SUB(WhoHeroesEvents.ResetRequested, OnResetRequested);
        EventManager.SUB(WhoHeroesEvents.RestartRequested, OnRestartRequested);
    }

    private void Start()
    {
        StartExpeditionOrchestration();
        if (ConfigLoader.parseEnded)
        {
            InitializeRuntime();
            TryAutoStart();
        }
    }

    private void Update()
    {
        if (!initialized && ConfigLoader.parseEnded)
        {
            InitializeRuntime();
            TryAutoStart();
        }

        UpdateExpeditionOrchestration();
        UpdateNightOrchestration();
        if (initialized && !castleShopReady)
            TryInitializeCastleShop();
        if (initialized && !tavernShopReady)
            TryInitializeTavernShop();
        if (runSaveDirty && runSnapshotApplied && Time.unscaledTime >= nextRunSaveTime)
            SaveRunSnapshot();

        if (phase == WhoHeroesPhase.Day && Input.GetKeyDown(KeyCode.N))
            ForceNight();

        if (phase == WhoHeroesPhase.Day)
        {
            UpdateDayCycle();
            return;
        }

        if (phase != WhoHeroesPhase.Night || MainStates.instance == null ||
            !MainStates.instance.all.TryGetValue("main_player", out var king))
            return;

        if (king.GetPar("health") <= 0)
            SetGameOver();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(ParseEndedEvent, OnParseEnded);
        EventManager.UNSUB(GameStartEvent, OnGameStart);
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnWhoHeroesRefresh);
        EventManager.UNSUB(WhoHeroesEvents.ResetRequested, OnResetRequested);
        EventManager.UNSUB(WhoHeroesEvents.RestartRequested, OnRestartRequested);
        if (!suppressDestroySave && !shutdownSnapshotSaved)
            SaveBeforeShutdown();
        DisposeNightOrchestration();
        DisposeExpeditionOrchestration();
        if (Instance == this)
            Instance = null;
    }

    public void BeginDay()
    {
        if (!InitializeRuntime() || phase == WhoHeroesPhase.GameOver)
            return;

        var startedAfterNight = phase == WhoHeroesPhase.Night;
        if (startedAfterNight)
        {
            dayNumber++;
            ClearNightCheckpoint();
        }

        phase = WhoHeroesPhase.Day;
        SetPlayerAnchorRenderers(false);
        ResetClock(0f, true);
        lastPublishedSecond = -1;

        if (portalProgressionReady)
            BuildNightWaveSnapshot();

        if (economyReady)
            ResetMineProductionForDay();

        ReturnAtDawn();

        if (restoreKingHealthAtDawn)
            RestorePrinceHealth();

        PublishPhase(NewDayEvent);
        if (startedAfterNight)
            EventManager.INV(WhoHeroesEvents.DayStartedAfterNight,
                new ArgPass { num = dayNumber, what = dayNumber.ToString() });
        PublishDayProgress(true);
    }

    public void BeginNight()
    {
        if (!InitializeRuntime() || phase != WhoHeroesPhase.Day || !CanBeginNight())
            return;
        PrepareForNightTransition();

        SettleCompletedDay();
        phase = WhoHeroesPhase.Night;
        SetPlayerAnchorRenderers(true);
        TimeManager.instance.spd = 0f;
        nightNumber++;
        SaveNightCheckpoint();
        BuildNightWaveSnapshot();
        PublishPhase(NewNightEvent);
    }

    public void ForceNight()
    {
        BeginNight();
    }

    public void SaveNow()
    {
        if (!initialized || !runSnapshotApplied)
            return;
        SyncRunStats();
        SaveRunSnapshot();
    }

    public void CompleteNight(int goldReward)
    {
        if (phase != WhoHeroesPhase.Night)
            return;

        ModelStatistics.instance?.IncreaseStatValue(MainCycle_WhoHeroes.NightsSurvivedStat, 1);
        AddResource(MainCycle_WhoHeroes.GoldResourceId, Mathf.Max(0, goldReward));
        if (nightNumber == 1)
            MakeNextPortalAvailable();
        var bestNight = ModelStatistics.instance == null
            ? 0
            : ModelStatistics.instance.GetStatValue(BestNightStat, false);
        var earnedPermanentPerk = ModelStatistics.instance != null && nightNumber > bestNight;
        if (earnedPermanentPerk)
        {
            ModelStatistics.instance.SetStatValueForce(BestNightStat, nightNumber);
            ModelStatistics.instance.SetStatValueForce(MainCycle_WhoHeroes.PendingPerkNightStat, nightNumber);
        }
        BeginDay();
        SaveRunSnapshot();
        if (earnedPermanentPerk)
            EventManager.INV(WhoHeroesEvents.PermanentPerkOffered,
                new ArgPass { num = nightNumber, what = nightNumber.ToString() });
    }

    public void CompleteTraderForToday()
    {
        if (!TraderAvailableToday || ModelStatistics.instance == null)
            return;

        ModelStatistics.instance.SetStatValueForce(TraderCompletedDayStat, dayNumber);
        MarkRunSaveDirty();
        EventManager.INV(WhoHeroesEvents.TraderCompleted,
            new ArgPass { num = dayNumber, what = dayNumber.ToString() });
    }

    public bool AddResource(string resourceId, int amount)
    {
        if (amount <= 0 || MainStates.instance == null || DatabaseAll.instance == null ||
            !MainStates.instance.all.TryGetValue("main_player", out var player))
            return false;

        if (string.IsNullOrWhiteSpace(resourceId) || !DatabaseAll.instance.items.ContainsKey(resourceId))
        {
            Debug.LogWarning($"WhoHeroes resource '{resourceId}' is missing from runtime config.", this);
            return false;
        }

        var created = MainStates.instance.AddItem(player, resourceId, amount);
        if (created != null && created.owner == null && created.GetPar("amount") <= 0f)
            DisposeRuntimeObject(created);
        UIfiller.GlobalRefresh();
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { num = amount, what = resourceId });
        return true;
    }

    public static void AddOrMergeCityStack(RObj incoming)
    {
        if (incoming?.dbObj == null || incoming.it != ItemType.monster || MainStates.instance == null ||
            !MainStates.instance.all.TryGetValue("main_player", out var player))
            return;

        MainStates.instance.AddItem(player, incoming);
        if (incoming.GetPar("amount") <= 0f)
            DisposeRuntimeObject(incoming);
        NormalizeRosterStacks(player);
    }

    public static void DisposeRuntimeObject(RObj value)
    {
        if (value == null || MainStates.instance == null)
            return;

        if (value.owner != null)
            value.owner.inventory.Remove(value);
        value.owner = null;
        MainStates.instance.combats.Remove(value);
        foreach (var skill in value.actSkills.ToList())
        {
            if (skill == null)
                continue;
            MainStates.instance.all.Remove(skill.RID);
            if (skill.main != null)
                Destroy(skill.main);
        }
        value.actSkills.Clear();
        MainStates.instance.all.Remove(value.RID);
        if (value.main != null)
            Destroy(value.main);
    }

    private static void StaggerSpecialSkillCooldowns(IReadOnlyList<RObj> units)
    {
        if (units == null)
            return;

        foreach (var group in units.Where(value => value?.dbObj != null)
                     .GroupBy(value => value.dbObj.ID, StringComparer.Ordinal))
        {
            var groupUnits = group.ToList();
            for (var index = 0; index < groupUnits.Count; index++)
            {
                foreach (var skill in groupUnits[index].actSkills.Where(value => value?.dbObj != null &&
                             !value.dbObj.ID.StartsWith("basic_", StringComparison.Ordinal)))
                {
                    var cooldown = Mathf.Max(0f, skill.GetPar("cooldown"));
                    skill.SetPar("cd", cooldown * index / Mathf.Max(1, groupUnits.Count));
                }
            }
        }
    }

    public bool TryRegisterTerritoryOpened(RObj portal)
    {
        if (ManagementLocked || !portalProgressionReady || portal == null ||
            !portalProgression.Contains(portal) || portal.GetPar(AvailableParam) <= 0f ||
            GUILIB.Level(portal) > 0)
            return false;

        capturedTargetIds.Add(portal.RID);
        SetAvailable(portal, false);
        EventManager.INV(WhoHeroesEvents.PortalCaptured, new ArgPass
        {
            who = portal,
            what = portal.RID,
            num = 1
        });
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = portal });
        MarkRunSaveDirty();
        return true;
    }

    public bool TryRegisterPointOfInterestCaptured(RObj pointOfInterest)
    {
        if (ManagementLocked || !portalProgressionReady || pointOfInterest == null ||
            IsPortal(pointOfInterest) || pointOfInterest.GetPar(AvailableParam) <= 0f ||
            GUILIB.Level(pointOfInterest) > 0)
            return false;

        capturedTargetIds.Add(pointOfInterest.RID);
        SetAvailable(pointOfInterest, false);
        EventManager.INV(WhoHeroesEvents.PointOfInterestCaptured, new ArgPass
        {
            who = pointOfInterest,
            what = pointOfInterest.RID,
            num = 1
        });
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = pointOfInterest });
        MarkRunSaveDirty();
        return true;
    }

    public static bool IsRestorableTarget(RObj target)
    {
        return target != null && GUILIB.Level(target) <= 0 &&
               Instance != null && Instance.capturedTargetIds.Contains(target.RID);
    }

    public bool TryFinalizeRestoration(RObj target)
    {
        if (target == null || GUILIB.Level(target) <= 0 || !capturedTargetIds.Contains(target.RID))
            return false;

        ClearCaptureDynamic(target);
        SetAvailable(target, false);
        if (IsEnterPortal(target))
        {
            SyncExitPortal(target, true);
            MakeTerritoryAvailable(target);
            MakeNextPortalAvailable();
            BuildNightWaveSnapshot();
        }
        else
        {
            if (TryGetMineDefinition(target, out _, out _))
                ResetMineProduction(target, DayElapsedSeconds);
            ApplyCapturedBoostsToCurrentRun();
        }
        MarkRunSaveDirty();
        return true;
    }

    public static bool IsManagementActionAllowed(string actionName)
    {
        return Instance == null || !ManagementActions.Contains(actionName) || !Instance.ManagementLocked;
    }

    private void OnParseEnded(ArgPass _)
    {
        InitializeRuntime();
        TryAutoStart();
    }

    private void OnGameStart(ArgPass _)
    {
        gameStartRequested = true;
        if (!InitializeRuntime())
            return;

        TryStartRequestedGame();
    }

    private void TryStartRequestedGame()
    {
        if (!gameStartRequested || gameStarted || !initialized || !runSnapshotApplied)
            return;

        gameStarted = true;
        if (phase == WhoHeroesPhase.Bootstrap)
        {
            BeginDay();
            return;
        }

        if (phase == WhoHeroesPhase.Day)
        {
            TimeManager.instance.spd = 1f;
            PublishPhase(NewDayEvent);
            PublishDayProgress(true);
        }
        else if (phase == WhoHeroesPhase.Night)
        {
            TimeManager.instance.spd = 0f;
            BuildNightWaveSnapshot();
            PublishPhase(NewNightEvent);
        }
        else if (phase == WhoHeroesPhase.GameOver)
        {
            TimeManager.instance.spd = 0f;
            PublishGameOver();
        }
    }

    private bool InitializeRuntime()
    {
        if (initialized)
            return true;

        if (MainStates.instance == null || DatabaseAll.instance == null || TimeManager.instance == null ||
            ModelStatistics.instance == null)
            return false;


        if (MainStates.instance.all.ContainsKey("main_player"))
        {
            initialized = true;
            RestoreOrInitializeRunState();
            SetPlayerAnchorRenderers(phase == WhoHeroesPhase.Night);
            ApplyPermanentPerksToCurrentRun();
            RefreshOnboardingTasks();
            StartPortalProgressionInitialization();
            return true;
        }

        if (playerAnchor == null)
        {
            Debug.LogError("WhoHeroes bootstrap: playerAnchor is not assigned.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(playerConfigId) ||
            !DatabaseAll.instance.heroes.ContainsKey(playerConfigId))
        {
            Debug.LogError($"WhoHeroes bootstrap: hero config '{playerConfigId}' was not found.", this);
            return false;
        }

        var player = DatabaseAll.instance.CreateAny(
            playerConfigId,
            false,
            1,
            playerAnchor,
            "main_player",
            playerAnchor,
            false,
            true);

        if (player == null || player.it == ItemType.unknown)
        {
            Debug.LogError($"WhoHeroes bootstrap: failed to create main_player from '{playerConfigId}'.", this);
            return false;
        }

        if (applyPlayerConfig && ConfigLoader.Instance.allPlayer.Count > 0)
            MainStates.instance.ApplyPlayerConfigParams(player);

        initialized = true;
        RestoreOrInitializeRunState();
        SetPlayerAnchorRenderers(phase == WhoHeroesPhase.Night);
        ApplyPermanentPerksToCurrentRun();
        RefreshOnboardingTasks();
        StartPortalProgressionInitialization();
        EventManager.INV(BootstrapReadyEvent, new ArgPass { who = player });
        return true;
    }

    public static void NormalizeNewDefenseSlot(RObj unit)
    {
        if (unit == null || MainStates.instance == null ||
            !MainStates.instance.all.TryGetValue("main_player", out var player))
            return;

        var occupied = new HashSet<int>(player.inventory
            .Where(value => value != null && value != unit)
            .Select(value => Mathf.RoundToInt(value.GetPar("used_slot")))
            .Where(slot => slot >= FirstDefenseSlot && slot <= LastDefenseSlot));

        for (var slot = FirstDefenseSlot;
             slot <= LastDefenseSlot;
             slot++)
        {
            if (occupied.Contains(slot))
                continue;
            unit.SetPar("used_slot", slot);
            return;
        }
    }

    public void OnPermanentPerkChosen(string perkId)
    {
        if (!MainCycle_WhoHeroes.PermanentPerkIds.Contains(perkId) || ModelStatistics.instance == null ||
            ModelStatistics.instance.GetStatValue(MainCycle_WhoHeroes.PendingPerkNightStat, false) <= 0)
            return;

        ModelStatistics.instance.SetStatValueForce(MainCycle_WhoHeroes.PendingPerkNightStat, 0);
        ApplyPermanentPerksToCurrentRun();
        SaveRunSnapshot();
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { what = perkId });
    }

    public static void ApplyPermanentPerksToUnit(RObj unit, bool prince)
    {
        if (unit == null || ModelStatistics.instance == null)
            return;

        var damageFactor = PermanentStatFactor(
            prince ? MainCycle_WhoHeroes.PrinceDamagePerkStat : MainCycle_WhoHeroes.UnitDamagePerkStat);
        var healthFactor = PermanentStatFactor(
            prince ? MainCycle_WhoHeroes.PrinceHealthPerkStat : MainCycle_WhoHeroes.UnitHealthPerkStat);
        var armorFactor = PermanentStatFactor(
            prince ? MainCycle_WhoHeroes.PrinceArmorPerkStat : MainCycle_WhoHeroes.UnitArmorPerkStat);

        if (!prince && Instance != null)
        {
            damageFactor *= Instance.RunBoostFactor("damage");
            healthFactor *= Instance.RunBoostFactor("health");
            armorFactor *= Instance.RunBoostFactor("armor");
        }

        ApplyStatFactor(unit, damageFactor, "damage", "attack");
        ApplyHealthFactor(unit, healthFactor);
        ApplyStatFactor(unit, armorFactor, "armor", "def");
    }

    public static List<Bon> AdjustPermanentPrice(RObj value, string actionName, List<Bon> basePrice)
    {
        var source = basePrice ?? new List<Bon>();
        if (value == null || ModelStatistics.instance == null)
            return source;

        var level = 0;
        if (actionName == "buy" && value.it == ItemType.monster)
            level = ModelStatistics.instance.GetStatValue(MainCycle_WhoHeroes.UnitCostPerkStat, false);
        else if (actionName == "upgrade")
            level = ModelStatistics.instance.GetStatValue(MainCycle_WhoHeroes.BuildCostPerkStat, false);
        if (level <= 0)
            return source;

        var discount = Mathf.Min(MainCycle_WhoHeroes.PermanentMaxDiscount(),
            level * MainCycle_WhoHeroes.PermanentCostPercent()) / 100f;
        return source.Select(price => new Bon
        {
            Key = price.Key,
            Value = price.Value <= 0 ? price.Value : Mathf.Max(1, Mathf.FloorToInt(price.Value * (1f - discount))),
            Val2 = price.Val2,
            Val3 = price.Val3
        }).ToList();
    }

    public static bool TryExecutePermanentDiscountAction(
        RObj value, string actionName, UnoAll action = null, ObjHolder holder = null)
    {
        if (value == null || MainStates.instance == null || UpgradeSystem.instance == null ||
            ModelStatistics.instance == null)
            return false;

        var perkStat = actionName == "buy" && value.it == ItemType.monster
            ? MainCycle_WhoHeroes.UnitCostPerkStat
            : actionName == "upgrade" ? MainCycle_WhoHeroes.BuildCostPerkStat : string.Empty;
        if (string.IsNullOrEmpty(perkStat) || ModelStatistics.instance.GetStatValue(perkStat, false) <= 0)
            return false;

        var basePrice = UpgradeSystem.instance.GetPrice(value, actionName);
        var price = AdjustPermanentPrice(value, actionName, basePrice);
        if (action == null || holder == null)
            return false;

        var pricedDynamic = value.dynamic;
        var originalDynamicPrice = pricedDynamic?.price;
        var originalDatabasePrice = value.dbObj?.price;
        FormatMeta upgradeCostMeta = null;
        string originalUpgradeCost = null;

        if (pricedDynamic != null)
            pricedDynamic.price = price;
        else if (actionName == "buy" && value.dbObj != null)
            value.dbObj.price = price;
        else if (actionName == "upgrade" && ConfigLoader.Instance != null)
        {
            upgradeCostMeta = ConfigLoader.Instance.metaConf.Find(meta => meta.parName == "upgrade_cost");
            if (upgradeCostMeta == null)
                return false;
            originalUpgradeCost = upgradeCostMeta.stringVal;
            upgradeCostMeta.stringVal = string.Join("#", price.Select(entry => $"{entry.Key},{entry.Value}"));
        }
        else
            return false;

        try
        {
            MainStates.instance.ClickedSome(value, action, holder, true);
        }
        finally
        {
            if (pricedDynamic != null)
                pricedDynamic.price = originalDynamicPrice;
            else if (actionName == "buy" && value.dbObj != null)
                value.dbObj.price = originalDatabasePrice;
            if (upgradeCostMeta != null)
                upgradeCostMeta.stringVal = originalUpgradeCost;
        }

        if (actionName == "buy" && MainStates.instance.all.TryGetValue("main_player", out var boughtForPlayer))
            ApplyPermanentPerksToRoster(boughtForPlayer);
        return true;
    }

    private void ApplyPermanentPerksToCurrentRun()
    {
        if (MainStates.instance == null)
            return;
        if (MainStates.instance.all.TryGetValue("main_player", out var prince))
        {
            ApplyPermanentPerksToUnit(prince, true);
            ApplyPermanentPerksToRoster(prince);
        }
        if (MainStates.instance.all.TryGetValue("expedition", out var expedition))
            ApplyPermanentPerksToRoster(expedition);
    }

    private void EnsureStartingCastleAndRoster()
    {
        if (restoringSavedRun || MainStates.instance == null || DatabaseAll.instance == null ||
            !MainStates.instance.all.TryGetValue("main_player", out var player) ||
            !MainStates.instance.all.TryGetValue(MainCycle_WhoHeroes.StartingCastleBuildingId, out var building))
            return;

        if (player.inventory.Any(value => value?.dbObj != null &&
                value.dbObj.ID == MainCycle_WhoHeroes.StartingUnitId && value.GetPar("amount") > 0f))
            return;

        var stack = DatabaseAll.instance.CreateMonster(
            MainCycle_WhoHeroes.StartingUnitId,
            MainCycle_WhoHeroes.StartingUnitAmount(),
            false,
            false);
        stack.SetPar("used_slot", -1f);
        ApplyPermanentPerksToUnit(stack, false);
        AddOrMergeCityStack(stack);
        MarkRunSaveDirty();
    }

    private void SyncRunBoostStats()
    {
        if (MainStates.instance == null || ModelStatistics.instance == null)
            return;
        foreach (var stat in MainCycle_WhoHeroes.RunBoostStats)
        {
            var captured = MainStates.instance.all.Values.Count(value => value != null &&
                GUILIB.Level(value) > 0 && MainCycle_WhoHeroes.TryGetBoostStat(value.RID, out var configured) &&
                configured == stat);
            ModelStatistics.instance.SetStatValueForce(MainCycle_WhoHeroes.RunBoostStat(stat), captured);
        }
    }

    private void ApplyCapturedBoostsToCurrentRun()
    {
        if (MainStates.instance == null || ModelStatistics.instance == null)
            return;
        SyncRunBoostStats();
        var units = new List<RObj>();
        if (MainStates.instance.all.TryGetValue("main_player", out var player))
            units.AddRange(player.inventory.Where(value => value != null && value.it == ItemType.monster));
        if (MainStates.instance.all.TryGetValue("expedition", out var expedition))
            units.AddRange(expedition.inventory.Where(value => value != null && value.it == ItemType.monster));
        foreach (var unit in units.Distinct())
            ApplyRunBoostsToUnit(unit);
    }

    private void ApplyRunBoostsToUnit(RObj unit)
    {
        ApplyPermanentPerksToUnit(unit, false);
    }

    private static void ApplyPermanentPerksToRoster(RObj owner)
    {
        if (owner == null)
            return;
        foreach (var unit in owner.inventory.Where(value => value != null && value.it == ItemType.monster))
            ApplyPermanentPerksToUnit(unit, false);
    }

    private static float PermanentStatFactor(string perkStat)
    {
        var level = Mathf.Max(0, ModelStatistics.instance.GetStatValue(perkStat, false));
        return Mathf.Pow(1f + MainCycle_WhoHeroes.PermanentStatPercent() / 100f, level);
    }

    private float RunBoostFactor(string stat)
    {
        var captured = Mathf.Max(0,
            ModelStatistics.instance.GetStatValue(MainCycle_WhoHeroes.RunBoostStat(stat), false));
        return Mathf.Pow(1f + MainCycle_WhoHeroes.BoostPercent() / 100f, captured);
    }

    private static void ApplyStatFactor(RObj unit, float factor, params string[] parameterNames)
    {
        foreach (var parameter in parameterNames)
            SetWhoHeroesStatModifier(unit, parameter, factor);
    }

    private static void ApplyHealthFactor(RObj unit, float factor)
    {
        var registeredDamage = Mathf.Max(0f, unit.GetPar("registered_damage"));
        var currentHealth = Mathf.Max(0f, unit.GetPar("health"));
        var totalHealth = Mathf.Max(1f, currentHealth + registeredDamage);
        var currentRatio = Mathf.Clamp01(currentHealth / totalHealth);

        SetWhoHeroesStatModifier(unit, "health", factor);
        SetWhoHeroesStatModifier(unit, "max_health", factor);
        SetWhoHeroesStatModifier(unit, "hp", factor);

        var scaledTotalHealth = Mathf.Max(1f,
            unit.GetPar("health") + Mathf.Max(0f, unit.GetPar("registered_damage")));
        unit.SetPar("registered_damage", scaledTotalHealth * (1f - currentRatio));
    }

    private static void SetWhoHeroesStatModifier(RObj unit, string parameter, float factor)
    {
        if (unit?.dbObj == null)
            return;

        var marker = StatModifierMarkerPrefix + parameter;
        unit.upgradePars.TryGetValue(marker, out var previousModifier);
        unit.upgradePars.TryGetValue(parameter, out var currentUpgrade);
        var nonWhoHeroesUpgrade = currentUpgrade - previousModifier;

        var configuredValue = 0f;
        if (unit.dbObj.pars.TryGetValue(parameter, out var baseValue))
        {
            configuredValue = baseValue;
            if (RObj.scalablePars.Contains(parameter))
            {
                var level = Mathf.Max(1, Mathf.RoundToInt(unit.GetPar("level")));
                configuredValue *= Mathf.Pow(1.1f, level - 1);
            }
        }

        var intrinsicValue = configuredValue + nonWhoHeroesUpgrade;
        var modifier = intrinsicValue * (Mathf.Max(0f, factor) - 1f);
        unit.SetPar(parameter, nonWhoHeroesUpgrade + modifier);
        unit.SetPar(marker, modifier);
    }

    private void OnWhoHeroesRefresh(ArgPass _)
    {
        if (!initialized)
            return;
        CleanupSpentResourceObjects();
        if (MainStates.instance != null)
            foreach (var ownerId in new[] { "main_player", "expedition" })
                if (MainStates.instance.all.TryGetValue(ownerId, out var owner))
                    NormalizeRosterStacks(owner);
        UpdateMineWorkers();
        ApplyCapturedBoostsToCurrentRun();
        RefreshOnboardingTasks();
        if (!castleShopReady)
            TryInitializeCastleShop();
        else
            EnsureCastleOffers();
        if (!tavernShopReady)
            TryInitializeTavernShop();
        else
            EnsureTavernOffers();
        MarkRunSaveDirty();
    }

    public void RefreshOnboardingTasks()
    {
        if (ModelStatistics.instance == null || MainStates.instance == null)
            return;

        ModelStatistics.instance.UpdateAllTasks();
        ModelStatistics.instance.StartWhoHeroesTasks(MainCycle_WhoHeroes.OnboardingTaskIds);
    }

    private void TryInitializeCastleShop()
    {
        if (MainStates.instance == null || DatabaseAll.instance == null)
            return;

        if (MainCycle_WhoHeroes.CastleUnits.Keys.Any(id => !MainStates.instance.all.ContainsKey(id)))
            return;

        castleShopReady = true;
        EnsureStartingCastleAndRoster();
        EnsureCastleOffers();
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { what = "castle_shop_ready" });
    }

    private void EnsureCastleOffers()
    {
        foreach (var pair in MainCycle_WhoHeroes.CastleUnits)
        {
            if (!MainStates.instance.all.TryGetValue(pair.Key, out var building))
                continue;

            var available = GUILIB.Level(building) > 0;
            if (available)
                UpgradeOwnedUnitStacks(pair.Value, GUILIB.Level(building));
            RemoveSpentOffers(building, pair.Value, available);
            var current = building.inventory.FirstOrDefault(value => value != null &&
                value.it == ItemType.monster && value.dbObj != null &&
                string.Equals(value.dbObj.ID, pair.Value, StringComparison.Ordinal) &&
                value.GetPar("amount") > 0f);

            if (!available)
                continue;

            if (current != null)
            {
                SetExactLevel(current, GUILIB.Level(building));
                ConfigureUnitOffer(current);
                continue;
            }

            var offer = DatabaseAll.instance.CreateMonster(pair.Value, 1, false, false);
            SetExactLevel(offer, GUILIB.Level(building));
            if (!ConfigureUnitOffer(offer))
                continue;
            MainStates.instance.AddItem(building, offer);
        }
    }

    private static void UpgradeOwnedUnitStacks(string unitId, int targetLevel)
    {
        if (string.IsNullOrEmpty(unitId) || targetLevel <= 0 || MainStates.instance == null)
            return;

        foreach (var ownerId in new[] { "main_player", "expedition" })
        {
            if (!MainStates.instance.all.TryGetValue(ownerId, out var owner))
                continue;

            var changed = false;
            foreach (var unit in owner.inventory.Where(value => value?.dbObj != null &&
                         value.it == ItemType.monster && value.dbObj.ID == unitId))
            {
                if (GUILIB.Level(unit) >= targetLevel)
                    continue;
                SetExactLevel(unit, targetLevel);
                ApplyPermanentPerksToUnit(unit, false);
                changed = true;
            }

            if (changed)
                owner.RecalcPars();
        }
    }

    private void TryInitializeTavernShop()
    {
        if (MainStates.instance == null || DatabaseAll.instance == null ||
            !MainStates.instance.all.ContainsKey("tavern"))
            return;

        tavernShopReady = true;
        EnsureTavernOffers();
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { what = "tavern_shop_ready" });
    }

    private void EnsureTavernOffers()
    {
        if (MainStates.instance == null || DatabaseAll.instance == null ||
            !MainStates.instance.all.TryGetValue("tavern", out var tavern))
            return;

        RemoveSpentOffers(tavern);
        var activeOffers = tavern.inventory.Where(IsActiveTavernOffer).ToList();
        for (var index = activeOffers.Count - 1; index >= MainCycle_WhoHeroes.TavernOfferCount; index--)
            DisposeRuntimeObject(activeOffers[index]);
        foreach (var existing in tavern.inventory.Where(IsActiveTavernOffer))
            ConfigureUnitOffer(existing);
        if (MainCycle_WhoHeroes.TavernUnits.Count == 0)
            return;

        var activeIds = new HashSet<string>(tavern.inventory.Where(IsActiveTavernOffer)
            .Select(value => value.dbObj.ID), StringComparer.Ordinal);
        var missingCount = Mathf.Max(0, MainCycle_WhoHeroes.TavernOfferCount - activeIds.Count);
        var available = MainCycle_WhoHeroes.TavernUnits.Where(id => !activeIds.Contains(id)).ToList();
        var selected = ModelSet.GetMeNonRepeat(available, Mathf.Min(missingCount, available.Count));
        foreach (var id in selected)
        {
            var offer = DatabaseAll.instance.CreateMonster(
                id, MainCycle_WhoHeroes.TavernUnitAmount(id), false, false);
            if (!ConfigureUnitOffer(offer))
                break;
            MainStates.instance.AddItem(tavern, offer);
        }
    }

    public bool TryRerollTavern()
    {
        var allowed = IsManagementActionAllowed("reroll");
        var tavern = MainStates.instance != null && MainStates.instance.all.TryGetValue("tavern", out var value)
            ? value
            : null;
        if (!tavernShopReady || !allowed || tavern == null)
        {
            if (!allowed)
                EventManager.INV(WhoHeroesEvents.ManagementBlocked,
                    new ArgPass { who = tavern, what = "reroll" });
            return false;
        }

        var paid = false;
        MainStates.instance.Buy(MainCycle_WhoHeroes.TavernRerollPrice(), null, () => paid = true);
        if (!paid)
        {
            EventManager.INV(WhoHeroesEvents.ActionFailed,
                new ArgPass { who = tavern, what = "reroll" });
            return false;
        }

        for (var index = tavern.inventory.Count - 1; index >= 0; index--)
        {
            var offer = tavern.inventory[index];
            if (!IsTavernUnit(offer))
                continue;
            DisposeRuntimeObject(offer);
        }

        EnsureTavernOffers();
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = tavern, what = "reroll" });
        return true;
    }

    public static bool IsActiveTavernOffer(RObj offer)
    {
        return IsTavernUnit(offer) && offer.GetPar("amount") > 0f;
    }

    private static bool IsTavernUnit(RObj offer)
    {
        return offer != null && offer.it == ItemType.monster && offer.dbObj != null &&
               MainCycle_WhoHeroes.TavernUnits.Contains(offer.dbObj.ID);
    }

    private static bool ConfigureUnitOffer(RObj offer)
    {
        if (offer?.dbObj == null ||
            !MainCycle_WhoHeroes.TryGetUnitPurchaseDynamic(offer.dbObj.ID, out var purchase))
        {
            Debug.LogError($"WhoHeroes config: purchase data is missing for '{offer?.dbObj?.ID}'.");
            return false;
        }

        offer.dynamic = purchase;
        return true;
    }

    private static void RemoveSpentOffers(RObj holder, string unitId = null, bool keepActive = true)
    {
        for (var index = holder.inventory.Count - 1; index >= 0; index--)
        {
            var offer = holder.inventory[index];
            if (offer == null)
            {
                holder.inventory.RemoveAt(index);
                continue;
            }

            if (offer.it != ItemType.monster || offer.dbObj == null ||
                unitId != null && !string.Equals(offer.dbObj.ID, unitId, StringComparison.Ordinal) ||
                keepActive && offer.GetPar("amount") > 0f)
                continue;

            DisposeRuntimeObject(offer);
        }
    }

    private void StartPortalProgressionInitialization()
    {
        if (portalProgressionInitializationStarted)
            return;

        portalProgressionInitializationStarted = true;
        StartCoroutine(InitializePortalProgressionWhenReady());
    }

    private IEnumerator InitializePortalProgressionWhenReady()
    {
        const int timeoutFrames = 600;
        var configuredIds = CollectConfiguredPortalIds();
        for (var frame = 0; frame < timeoutFrames; frame++)
        {
            if (ConfiguredPortalsExist(configuredIds))
                break;
            yield return null;
        }

        portalProgression.Clear();
        foreach (var id in configuredIds)
            if (MainStates.instance.all.TryGetValue(id, out var portal))
                portalProgression.Add(portal);

        portalProgression.Sort((left, right) =>
            PortalOrder(left).CompareTo(PortalOrder(right)));

        if (portalProgression.Count != configuredIds.Count)
        {
            Debug.LogError(
                $"WhoHeroes portal progression: only {portalProgression.Count}/{configuredIds.Count} configured portals exist in runtime.",
                this);
            yield break;
        }

        if (portalProgression.Count == 0)
        {
            Debug.LogError("WhoHeroes portal progression: no enter portals have an ordered ENCOUNTER value ('order|battleId') in Heroes.", this);
            yield break;
        }

        ValidatePortalProgression();
        if (pendingRunSnapshot != null)
            ApplyRunSnapshot(pendingRunSnapshot);
        else
            InitializeNewPortalState();

        portalProgressionReady = true;
        BuildNightWaveSnapshot();
        InitializeEconomy();
        runSnapshotApplied = true;
        SyncRestoredState();
        EnsureStartingCastleAndRoster();
        ApplyPermanentPerksToCurrentRun();
        ApplyCapturedBoostsToCurrentRun();
        MarkRunSaveDirty();
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = portalProgression[0] });
        TryStartRequestedGame();
    }

    public static string MineResourceId(RObj value)
    {
        return TryGetMineDefinition(value, out var resourceId, out _) ? resourceId : string.Empty;
    }

    private void InitializeEconomy()
    {
        mines.Clear();
        deliverySpawners.Clear();
        deliveryTarget = null;

        foreach (var value in MainStates.instance.all.Values)
        {
            if (!TryGetMineDefinition(value, out _, out var interval))
                continue;

            SetExactParam(value, "timer", interval);
            SetExactParam(value, "workers", 1f);
            SetExactParam(value, "level_multiplier", 1f);
            SetExactParam(value, "max_level", Mathf.Max(1f,
                ConfigLoader.GetMetaParamValue(MineMaxLevelMeta)));
            mines.Add(value);
        }

        foreach (var point in deliveryPoints)
        {
            if (point == null || !string.Equals(point.type, "delivery", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(point.name, DeliveryTargetName, StringComparison.OrdinalIgnoreCase))
                deliveryTarget = point.transform;
            else
                deliverySpawners[point.name] = point;
        }

        if (deliveryTarget == null)
            ReportEconomyError(DeliveryTargetName,
                $"WhoHeroes economy: delivery target '{DeliveryTargetName}' was not found in the active scene.");
        if (deliveryCarrierPrefab == null)
            ReportEconomyError("keeper_prefab",
                "WhoHeroes economy: keeper carrier prefab is not assigned in MainCycle_WhoHeroes.");

        foreach (var mine in mines)
        {
            if (Mathf.RoundToInt(mine.GetPar(ProductionDayParam)) != dayNumber)
                ResetMineProduction(mine, DayElapsedSeconds);
        }

        CacheDeliveryResourceIconStyle();
        ApplyCapturedBoostsToCurrentRun();
        UpdateMineWorkers();
        economyReady = mines.Count > 0;
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass());
    }

    private static bool TryGetMineDefinition(RObj value, out string resourceId, out float interval)
    {
        resourceId = string.Empty;
        interval = 0f;
        if (value == null)
            return false;

        var id = value.RID ?? string.Empty;
        if (HasNumericSuffix(id, "wood"))
        {
            resourceId = MainCycle_WhoHeroes.WoodResourceId;
            interval = ResolvePositiveMeta(WoodProductionIntervalMeta);
            return interval > 0f;
        }

        if (HasNumericSuffix(id, "stone"))
        {
            resourceId = MainCycle_WhoHeroes.StoneResourceId;
            interval = ResolvePositiveMeta(StoneProductionIntervalMeta);
            return interval > 0f;
        }

        return false;
    }

    private static bool HasNumericSuffix(string value, string prefix)
    {
        return value.Length > prefix.Length &&
               value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(value.Substring(prefix.Length), out _);
    }

    private static float ResolvePositiveMeta(string key)
    {
        var configured = ConfigLoader.Instance == null ? 0f : ConfigLoader.GetMetaParamValue(key);
        if (configured <= 0f)
            Debug.LogError($"WhoHeroes config: required positive METACONF value '{key}' is missing.");
        return Mathf.Max(0f, configured);
    }

    private void ResetMineProductionForDay()
    {
        SettlePendingDeliveries(false);
        foreach (var mine in mines)
            ResetMineProduction(mine, 0f);
    }

    private void ResetMineProduction(RObj mine, float startTime)
    {
        var safeStart = Mathf.Max(0f, startTime);
        SetExactParam(mine, ProductionDayParam, dayNumber);
        SetExactParam(mine, ProductionStartParam, safeStart);
        SetExactParam(mine, ProductionCyclesParam, 0f);
        if (TryGetMineDefinition(mine, out _, out var interval))
            SetExactParam(mine, ProductionNextParam,
                safeStart + interval / Mathf.Max(1, GUILIB.Level(mine)));
    }

    private void ProcessMineProduction(bool settleImmediately)
    {
        if (!economyReady)
            return;

        foreach (var mine in mines)
        {
            var level = GUILIB.Level(mine);
            if (level <= 0 || !TryGetMineDefinition(mine, out var resourceId, out var interval))
                continue;

            if (Mathf.RoundToInt(mine.GetPar(ProductionDayParam)) != dayNumber)
                ResetMineProduction(mine, 0f);

            var effectiveInterval = interval / Mathf.Max(1, level);
            SetExactParam(mine, "timer", effectiveInterval);
            var completedCycles = Mathf.Max(0, Mathf.RoundToInt(mine.GetPar(ProductionCyclesParam)));
            var nextProduction = mine.GetPar(ProductionNextParam);
            if (nextProduction <= 0f)
            {
                nextProduction = Mathf.Max(0f, mine.GetPar(ProductionStartParam)) +
                                 effectiveInterval * (completedCycles + 1);
                SetExactParam(mine, ProductionNextParam, nextProduction);
            }
            var amount = Mathf.Max(1, Mathf.RoundToInt(
                Mathf.Max(1f, mine.GetPar("workers")) *
                Mathf.Max(1f, mine.GetPar("level_multiplier"))));

            while (DayElapsedSeconds + 0.001f >= nextProduction)
            {
                if (!DispatchDelivery(mine, resourceId, amount, settleImmediately))
                    break;

                completedCycles++;
                SetExactParam(mine, ProductionCyclesParam, completedCycles);
                nextProduction += effectiveInterval;
                SetExactParam(mine, ProductionNextParam, nextProduction);
            }
        }
    }

    private bool DispatchDelivery(RObj mine, string resourceId, int amount, bool fastForward)
    {
        if (deliveryTarget == null || !fastForward && UtilsControl.Instance == null)
            return false;

        deliverySpawners.TryGetValue(mine.RID, out var spawner);
        var pickupTarget = mine.main?.transform;
        var routeRoot = FindDeliveryRoute(mine, resourceId);
        if (pickupTarget == null || routeRoot == null || routeRoot.childCount == 0)
        {
            ReportEconomyError(mine.RID + "_route",
                $"WhoHeroes economy: road route for '{mine.RID}' was not found under Transforms/Paths.");
            return false;
        }

        var carrierSpawn = spawner == null
            ? FindRouteEndpointFarthestFrom(routeRoot, pickupTarget.position)
            : spawner.transform;
        var carrierDestination = spawner == null ? carrierSpawn : deliveryTarget;
        if (carrierSpawn == null || carrierDestination == null)
            return false;

        var movementZ = carrierSpawn.position.z;
        var routeToMine = BuildDeliveryRoute(routeRoot, carrierSpawn.position, movementZ);
        var routeToCastle = BuildDeliveryRoute(routeRoot, pickupTarget.position, movementZ);

        var carrier = CreateCarrierVisual(mine, carrierSpawn, resourceId);
        if (carrier == null)
        {
            ReportEconomyError(mine.RID + "_carrier",
                $"WhoHeroes economy: scene carrier sample for '{mine.RID}' was not found.");
            return false;
        }

        var speed = fastForward
            ? 0f
            : ConfigLoader.GetMetaParamValue("global_move") / Mathf.Max(1f, carrierSpeedDivisor);
        if (!fastForward && speed <= 0f)
        {
            Destroy(carrier);
            ReportEconomyError("global_move",
                "WhoHeroes economy: Minimus METACONF 'global_move' must be greater than zero.");
            return false;
        }

        var delivery = new PendingDelivery
        {
            mine = mine,
            resourceId = resourceId,
            amount = amount,
            carrier = carrier,
            sourceResourceIcon = CreateBuildingResourceIcon(mine, resourceId),
            resourceIcon = CreateCarrierResourceIcon(carrier, resourceId),
            stateMachine = carrier.GetComponent<WhoHeroesCarrierStateMachine>()
        };
        if (delivery.stateMachine == null)
        {
            if (delivery.sourceResourceIcon != null)
                Destroy(delivery.sourceResourceIcon);
            Destroy(carrier);
            ReportEconomyError("keeper_state_machine",
                "WhoHeroes economy: keeper prefab has no WhoHeroesCarrierStateMachine.");
            return false;
        }

        pendingDeliveries.Add(delivery);
        if (!delivery.stateMachine.Initialize(
                pickupTarget, carrierDestination, delivery.sourceResourceIcon, delivery.resourceIcon,
                routeToMine, routeToCastle, speed,
                () => CompleteDelivery(delivery), fastForward))
        {
            pendingDeliveries.Remove(delivery);
            if (delivery.sourceResourceIcon != null)
                Destroy(delivery.sourceResourceIcon);
            Destroy(carrier);
            ReportEconomyError("keeper_state_machine_init",
                "WhoHeroes economy: keeper state machine could not be initialized.");
            return false;
        }
        return true;
    }

    private GameObject CreateCarrierVisual(RObj mine, Transform carrierSpawn, string resourceId)
    {
        if (mine?.main == null || carrierSpawn == null || deliveryCarrierPrefab == null)
            return null;

        var parent = MainStates.instance?.root;
        var carrier = Instantiate(deliveryCarrierPrefab, parent);
        carrier.name = $"WhoHeroes Carrier {resourceId}";
        carrier.SetActive(true);
        carrier.transform.position = carrierSpawn.position;
        foreach (var collider in carrier.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;
        return carrier;
    }

    private void UpdateMineWorkers()
    {
        if (MainStates.instance == null)
            return;
        foreach (var binding in mineWorkers)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.mineId) ||
                !MainStates.instance.all.TryGetValue(binding.mineId, out var mine))
                continue;
            var activeWorkers = Mathf.Max(0, GUILIB.Level(mine));
            for (var index = 0; index < binding.workers.Count; index++)
                if (binding.workers[index] != null)
                    binding.workers[index].SetActive(index < activeWorkers);
        }
    }

    private void CacheDeliveryResourceIconStyle()
    {
        var template = mines
            .Where(value => value?.main != null)
            .Select(value => value.main.transform.Find("resource"))
            .FirstOrDefault(value => value != null && value.GetComponent<SpriteRenderer>() != null);
        if (template == null)
            return;

        var renderer = template.GetComponent<SpriteRenderer>();
        deliveryResourceIconLocalPosition = template.localPosition;
        deliveryResourceIconWorldSize = renderer.bounds.size;
        deliveryResourceIconSortingLayerId = renderer.sortingLayerID;
        deliveryResourceIconSortingOrder = renderer.sortingOrder;
    }

    private Transform FindDeliveryRoute(RObj mine, string resourceId)
    {
        if (mine?.main == null || expeditionPathsRoot == null)
            return null;

        for (var islandIndex = 0; islandIndex < expeditionPathsRoot.childCount; islandIndex++)
        {
            var island = expeditionPathsRoot.GetChild(islandIndex);
            if (!HasNamedAncestor(mine.main.transform, island.name))
                continue;

            for (var routeIndex = 0; routeIndex < island.childCount; routeIndex++)
            {
                var route = island.GetChild(routeIndex);
                if (string.Equals(route.name, resourceId, StringComparison.OrdinalIgnoreCase))
                    return route;
            }
        }

        return null;
    }

    private static bool HasNamedAncestor(Transform value, string name)
    {
        for (var current = value; current != null; current = current.parent)
            if (string.Equals(current.name, name, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static Transform FindRouteEndpointFarthestFrom(Transform route, Vector3 target)
    {
        if (route == null || route.childCount == 0)
            return null;
        var first = route.GetChild(0);
        var last = route.GetChild(route.childCount - 1);
        return Vector3.SqrMagnitude(first.position - target) >= Vector3.SqrMagnitude(last.position - target)
            ? first
            : last;
    }

    private static List<(float, float, float)> BuildDeliveryRoute(
        Transform route, Vector3 start, float movementZ)
    {
        var result = new List<(float, float, float)>();
        if (route == null || route.childCount == 0)
            return result;

        var firstDistance = Vector3.SqrMagnitude(route.GetChild(0).position - start);
        var lastDistance = Vector3.SqrMagnitude(route.GetChild(route.childCount - 1).position - start);
        var forward = firstDistance <= lastDistance;
        for (var index = 0; index < route.childCount; index++)
        {
            var waypoint = route.GetChild(forward ? index : route.childCount - 1 - index).position;
            result.Add((waypoint.x, waypoint.y, movementZ));
        }

        return result;
    }

    public bool TryGetTraderRoad(
        float movementZ,
        out Vector3 spawnPosition,
        out List<(float, float, float)> roadRoute,
        out float moveSpeed)
    {
        spawnPosition = Vector3.zero;
        roadRoute = null;
        moveSpeed = ConfigLoader.GetMetaParamValue("global_move") / Mathf.Max(1f, carrierSpeedDivisor);
        if (!portalProgressionReady || expeditionPathsRoot == null || moveSpeed <= 0f)
            return false;

        var sourcePortal = portalProgression.FirstOrDefault(value => value?.main != null &&
            GUILIB.Level(value) > 0 && nightBattleSnapshot.ContainsKey(value));
        if (sourcePortal == null)
            return false;

        Transform selectedRoute = null;
        Transform selectedEndpoint = null;
        var bestDistance = float.PositiveInfinity;
        foreach (Transform island in expeditionPathsRoot)
        foreach (Transform route in island)
        {
            if (route.childCount == 0)
                continue;
            foreach (var endpoint in new[] { route.GetChild(0), route.GetChild(route.childCount - 1) })
            {
                var distance = Vector2.SqrMagnitude(endpoint.position - sourcePortal.main.transform.position);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                selectedRoute = route;
                selectedEndpoint = endpoint;
            }
        }

        if (selectedRoute == null || selectedEndpoint == null || bestDistance > 1f)
            return false;
        spawnPosition = selectedEndpoint.position;
        spawnPosition.z = movementZ;
        roadRoute = BuildDeliveryRoute(selectedRoute, selectedEndpoint.position, movementZ);
        return roadRoute.Count > 0;
    }

    private GameObject CreateCarrierResourceIcon(GameObject carrier, string resourceId)
    {
        if (carrier == null)
            return null;
        var renderer = carrier.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(value => value.transform != carrier.transform);
        var icon = CreateResourceIcon(carrier.transform, "ResourceIcon", resourceId, false,
            renderer == null ? null : renderer.gameObject);
        if (icon == null)
            return null;
        var localPosition = icon.transform.localPosition;
        icon.transform.localPosition = new Vector3(0f, Mathf.Lerp(localPosition.y, 0f, 0.2f), localPosition.z);
        icon.transform.localScale *= 0.625f;
        return icon;
    }

    private GameObject CreateBuildingResourceIcon(RObj mine, string resourceId)
    {
        var icon = mine?.main == null
            ? null
            : CreateResourceIcon(mine.main.transform, "ReadyResourceIcon", resourceId, true);
        if (icon != null && string.Equals(resourceId, MainCycle_WhoHeroes.StoneResourceId,
                StringComparison.Ordinal))
        {
            var localPosition = icon.transform.localPosition;
            icon.transform.localPosition = new Vector3(0f, localPosition.y, localPosition.z);
        }
        return icon;
    }

    private void SetPlayerAnchorRenderers(bool visible)
    {
        if (playerAnchor == null)
            return;
        foreach (var renderer in playerAnchor.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = visible;
    }

    private GameObject CreateResourceIcon(
        Transform parent, string objectName, string resourceId, bool active, GameObject existing = null)
    {
        var sprite = ResolveResourceIcon(resourceId);
        if (parent == null || sprite == null)
            return null;
        var icon = existing == null ? new GameObject(objectName) : existing;
        icon.transform.SetParent(parent, false);
        icon.transform.localPosition = deliveryResourceIconLocalPosition;
        var renderer = icon.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = icon.AddComponent<SpriteRenderer>();
        var movement = icon.GetComponent<ItemsMovement>();
        if (movement != null)
        {
            movement.ChangeSettings(false, false, false, false);
            movement.enabled = false;
        }
        renderer.sprite = sprite;
        var spriteSize = sprite.bounds.size;
        var parentScale = parent.lossyScale;
        icon.transform.localScale = new Vector3(
            deliveryResourceIconWorldSize.x /
            Mathf.Max(0.0001f, Mathf.Abs(spriteSize.x * parentScale.x)),
            deliveryResourceIconWorldSize.y /
            Mathf.Max(0.0001f, Mathf.Abs(spriteSize.y * parentScale.y)),
            1f);
        var renderedSize = renderer.bounds.size;
        icon.transform.localScale = new Vector3(
            icon.transform.localScale.x * deliveryResourceIconWorldSize.x /
            Mathf.Max(0.0001f, renderedSize.x),
            icon.transform.localScale.y * deliveryResourceIconWorldSize.y /
            Mathf.Max(0.0001f, renderedSize.y),
            icon.transform.localScale.z);
        renderer.sortingLayerID = deliveryResourceIconSortingLayerId;
        renderer.sortingOrder = deliveryResourceIconSortingOrder;
        icon.SetActive(active);
        return icon;
    }

    private static Sprite ResolveResourceIcon(string resourceId)
    {
        return GUILIB.Icon(resourceId);
    }

    private void FastForwardDelivery(PendingDelivery delivery)
    {
        if (delivery == null || delivery.settled)
            return;
        if (delivery.stateMachine == null)
        {
            ReportEconomyError("keeper_state_machine_runtime",
                "WhoHeroes economy: active delivery lost its carrier state machine.");
            return;
        }
        delivery.stateMachine.FastForward();
    }

    private void CompleteDelivery(PendingDelivery delivery)
    {
        if (delivery == null || delivery.settled)
            return;

        delivery.settled = true;
        pendingDeliveries.Remove(delivery);
        if (AddResource(delivery.resourceId, delivery.amount))
        {
            ModelStatistics.instance?.IncreaseStatValue("get_" + delivery.resourceId, delivery.amount);
            EventManager.INV(WhoHeroesEvents.ResourceDelivered,
                new ArgPass { what = delivery.resourceId, num = delivery.amount });
        }
        if (delivery.sourceResourceIcon != null)
            Destroy(delivery.sourceResourceIcon);
        if (delivery.carrier != null)
            Destroy(delivery.carrier);
    }

    private void SettlePendingDeliveries(bool credit = true)
    {
        foreach (var delivery in pendingDeliveries.ToArray())
        {
            if (delivery == null || delivery.settled)
                continue;
            if (credit)
            {
                FastForwardDelivery(delivery);
                continue;
            }
            delivery.settled = true;
            delivery.stateMachine?.Cancel();
            if (delivery.sourceResourceIcon != null)
                Destroy(delivery.sourceResourceIcon);
            if (delivery.carrier != null)
                Destroy(delivery.carrier);
        }
        pendingDeliveries.Clear();
    }

    private void ReportEconomyError(string key, string message)
    {
        if (reportedEconomyErrors.Add(key))
            Debug.LogError(message, this);
    }

    private List<string> CollectConfiguredPortalIds()
    {
        var result = new List<string>();
        if (DatabaseAll.instance == null)
            return result;

        foreach (var pair in DatabaseAll.instance.heroes)
        {
            if (!pair.Key.StartsWith("portalin", StringComparison.Ordinal) ||
                !pair.Value.parsStr.TryGetValue(NightAdditionConfigParam, out var encounter) ||
                ParsePortalOrder(encounter) <= 0)
                continue;
            result.Add(pair.Key);
        }
        return result;
    }

    private static bool ConfiguredPortalsExist(List<string> ids)
    {
        if (MainStates.instance == null)
            return false;
        foreach (var id in ids)
            if (!MainStates.instance.all.ContainsKey(id))
                return false;
        return true;
    }

    private void ValidatePortalProgression()
    {
        var previousOrder = 0;
        foreach (var portal in portalProgression)
        {
            var order = PortalOrder(portal);
            if (order <= previousOrder)
                Debug.LogError($"WhoHeroes portal progression order is not unique and ascending at '{portal.RID}'.", this);
            previousOrder = order;

            var territory = ConfigString(portal, TerritoryConfigParam);
            if (string.IsNullOrEmpty(territory))
                Debug.LogError($"WhoHeroes portal '{portal.RID}' has no target territory in FOUND_IN.", this);

            var encounter = NightAdditionBattleId(portal);
            if (string.IsNullOrEmpty(encounter))
                ReportPortalConfigError(portal.RID,
                    $"WhoHeroes portal '{portal.RID}' has no Night Addition battle in ENCOUNTER.");
            else if (ConfigLoader.Instance.battles.Find(value => value.battleName == encounter) == null)
                ReportPortalConfigError(portal.RID,
                    $"WhoHeroes portal '{portal.RID}' references missing BATTLES entry '{encounter}'.");
        }
    }

    private void InitializeNewPortalState()
    {
        capturedTargetIds.Clear();
        foreach (var value in MainStates.instance.all.Values)
        {
            if (IsPortal(value) || !string.IsNullOrEmpty(ConfigString(value, TerritoryConfigParam)))
            {
                SetExactLevel(value, 0);
                SetAvailable(value, false);
            }
        }

        foreach (var id in MainCycle_WhoHeroes.CastleUnits.Keys)
            if (MainStates.instance.all.TryGetValue(id, out var building))
                SetExactLevel(building, 0);

        var configuredStartCount = ConfigLoader.GetMetaParamValue("whoheroes_start_active_portals");
        var startCount = Mathf.Clamp(Mathf.RoundToInt(configuredStartCount), 1, portalProgression.Count);
        for (var index = 0; index < startCount; index++)
        {
            var portal = portalProgression[index];
            SetExactLevel(portal, 0);
            capturedTargetIds.Add(portal.RID);
            SyncExitPortal(portal, false);
        }
    }

    private void MakeNextPortalAvailable()
    {
        if (!portalProgressionReady)
            return;

        foreach (var portal in portalProgression)
        {
            if (GUILIB.Level(portal) > 0 || IsRestorableTarget(portal) ||
                portal.GetPar(AvailableParam) > 0f)
                continue;

            SetAvailable(portal, true);
            EventManager.INV(WhoHeroesEvents.PortalAvailable, new ArgPass
            {
                who = portal,
                what = portal.RID,
                num = 1
            });
            EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = portal });
            return;
        }
    }

    private void MakeTerritoryAvailable(RObj portal)
    {
        var territory = ConfigString(portal, TerritoryConfigParam);
        if (string.IsNullOrEmpty(territory))
            return;

        foreach (var value in MainStates.instance.all.Values)
        {
            if (IsEnterPortal(value) || ConfigString(value, TerritoryConfigParam) != territory)
                continue;

            if (value.RID.StartsWith("portalout", StringComparison.Ordinal))
                SetExactLevel(value, 1);
            else
                SetAvailable(value, true);
        }

        EventManager.INV(WhoHeroesEvents.TerritoryAvailable, new ArgPass
        {
            who = portal,
            what = territory,
            num = 1
        });
    }

    private void SyncExitPortal(RObj enterPortal, bool active)
    {
        var exitId = enterPortal.RID.Replace("portalin", "portalout");
        if (MainStates.instance.all.TryGetValue(exitId, out var exitPortal))
            SetExactLevel(exitPortal, active ? 1 : 0);
    }

    private void BuildNightWaveSnapshot()
    {
        nightWaveSnapshot.Clear();
        nightBattleSnapshot.Clear();
        if (!portalProgressionReady)
            return;

        var amounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var portal in portalProgression)
        {
            if (GUILIB.Level(portal) <= 0)
                continue;

            var encounterId = NightAdditionBattleId(portal);
            if (string.IsNullOrEmpty(encounterId))
                continue;
            var battle = ConfigLoader.Instance.battles.Find(value => value.battleName == encounterId);
            if (battle == null)
                continue;
            nightBattleSnapshot[portal] = battle;

            var rowCount = Mathf.Min(battle.enemies.heroLevelPosition.Count, battle.enemies.amounts.Count);
            for (var index = 0; index < rowCount; index++)
            {
                var enemyId = battle.enemies.heroLevelPosition[index].Item1;
                var amount = Mathf.Max(0, battle.enemies.amounts[index]);
                if (amount == 0 || !DatabaseAll.instance.heroes.ContainsKey(enemyId))
                    continue;
                amounts[enemyId] = amounts.TryGetValue(enemyId, out var current) ? current + amount : amount;
            }
        }

        foreach (var pair in amounts)
            nightWaveSnapshot.Add(new Bon
            {
                Key = pair.Key,
                Value = pair.Value,
                Val3 = Mathf.Max(1, ForecastNightNumber)
            });

        var total = 0;
        foreach (var value in nightWaveSnapshot)
            total += value.Value;
        EventManager.INV(WhoHeroesEvents.NightWavePrepared, new ArgPass
        {
            num = total,
            what = ForecastNightNumber.ToString(),
            what1 = nightWaveSnapshot.Count.ToString()
        });
    }

    private void ReportPortalConfigError(string key, string message)
    {
        if (reportedPortalConfigErrors.Add(key))
            Debug.LogError(message, this);
    }

    private static string ConfigString(RObj value, string key)
    {
        var result = GUILIB.StringParam(value, key).Trim();
        return string.Equals(result, "x", StringComparison.OrdinalIgnoreCase) ? string.Empty : result;
    }

    private static int PortalOrder(RObj value)
    {
        return ParsePortalOrder(ConfigString(value, NightAdditionConfigParam));
    }

    private static int ParsePortalOrder(string encounter)
    {
        if (string.IsNullOrWhiteSpace(encounter))
            return 0;
        var separator = encounter.IndexOf('|');
        return separator > 0 && int.TryParse(encounter.Substring(0, separator), out var order) ? order : 0;
    }

    private static string NightAdditionBattleId(RObj value)
    {
        var encounter = ConfigString(value, NightAdditionConfigParam);
        var separator = encounter.IndexOf('|');
        return separator >= 0 && separator + 1 < encounter.Length
            ? encounter.Substring(separator + 1).Trim()
            : string.Empty;
    }

    private static bool IsPortal(RObj value)
    {
        return value != null && value.RID.StartsWith("portal", StringComparison.Ordinal);
    }

    private static bool IsEnterPortal(RObj value)
    {
        return value != null && value.RID.StartsWith("portalin", StringComparison.Ordinal);
    }

    private static void SetAvailable(RObj value, bool available)
    {
        value.SetPar(AvailableParam, available ? 1f : 0f);
    }

    private static void SetExactLevel(RObj value, int level)
    {
        SetExactParam(value, "level", Mathf.Max(0, level));
        if (level > 0)
            ClearCaptureDynamic(value);
    }

    private static void ClearCaptureDynamic(RObj value)
    {
        if (value?.dynamic != null &&
            string.Equals(value.dynamic.id, CaptureDynamicId, StringComparison.Ordinal))
            value.dynamic = null;
    }

    private static void SetExactParam(RObj value, string key, float exactValue)
    {
        var configuredValue = 0f;
        if (value.dbObj != null && value.dbObj.pars.TryGetValue(key, out var baseValue))
            configuredValue = baseValue;
        value.SetPar(key, exactValue - configuredValue);
    }

    private void TryAutoStart()
    {
        if (!autoStartWithoutVisibleStartScreen || !initialized || gameStarted)
            return;

        if (startScreen != null && startScreen.gameObject.activeInHierarchy)
            return;

        gameStartRequested = true;
        TryStartRequestedGame();
    }

    private void UpdateDayCycle()
    {
        ProcessMineProduction(false);
        PublishDayProgress();
        if (DayElapsedSeconds >= DayDurationSeconds)
            BeginNight();
    }

    private void SettleCompletedDay()
    {
        var remainingSeconds = DayRemainingSeconds;
        if (remainingSeconds > 0f)
            TimeManager.instance.AddForceTime(remainingSeconds);
        ProcessMineProduction(true);
        SettlePendingDeliveries();
        PublishDayProgress(true);

        var gold = ResolveDailyGold();
        if (gold > 0)
            AddGold(gold);

    }

    private void AddGold(int amount)
    {
        if (!AddResource(MainCycle_WhoHeroes.GoldResourceId, amount) && !missingGoldConfigReported)
        {
            missingGoldConfigReported = true;
            Debug.LogWarning($"WhoHeroes day settlement: item config '{MainCycle_WhoHeroes.GoldResourceId}' was not found.", this);
        }
    }

    private float ResolveDayDuration()
    {
        var configured = ConfigLoader.Instance == null ? 0f : ConfigLoader.GetMetaParamValue(DayDurationMeta);
        if (configured <= 0f)
            Debug.LogError($"WhoHeroes config: required positive METACONF value '{DayDurationMeta}' is missing.", this);
        return Mathf.Max(1f, configured);
    }

    private int ResolveDailyGold()
    {
        var configured = ConfigLoader.Instance == null ? 0f : ConfigLoader.GetMetaParamValue(DailyGoldMeta);
        if (configured < 0f)
            Debug.LogError($"WhoHeroes config: METACONF value '{DailyGoldMeta}' cannot be negative.", this);
        var passive = Mathf.Max(0, Mathf.RoundToInt(configured));
        var territoryRate = ConfigLoader.Instance == null ? 0f : ConfigLoader.GetMetaParamValue(TerritoryGoldMeta);
        var activeTerritories = portalProgression.Count(value => value != null && GUILIB.Level(value) > 0);
        return passive + Mathf.Max(0, Mathf.RoundToInt(territoryRate)) * activeTerritories;
    }

    private void PublishDayProgress(bool force = false)
    {
        var second = Mathf.FloorToInt(DayElapsedSeconds);
        if (!force && second == lastPublishedSecond)
            return;

        lastPublishedSecond = second;
        EventManager.INV(DayProgressEvent, new ArgPass
        {
            num = Mathf.RoundToInt(DayProgress01 * 1000f),
            what = DayRemainingSeconds.ToString("0"),
            what1 = DayDurationSeconds.ToString("0")
        });
    }

    private void RestorePrinceHealth()
    {
        if (MainStates.instance == null || !MainStates.instance.all.TryGetValue("main_player", out var player))
            return;

        RestoreIfAlive(player);
    }

    private static void RestoreIfAlive(RObj unit)
    {
        if (unit.GetPar("health") > 0f)
            unit.SetPar("registered_damage", 0f);
    }

    private void RestoreOrInitializeRunState()
    {
        runtimeData = new WhoHeroesRuntimeData();
        try
        {
            var loaded = ModelStatistics.instance.LoadWhoHeroesRuntimeData();
            if (loaded != null)
            {
                runtimeData = loaded;
                runtimeData.gameStatistics ??= new PlayerData();
                runtimeData.run ??= new WhoHeroesRunState();
                ModelStatistics.instance.BindWhoHeroesPlayerData(runtimeData.gameStatistics);
                restoringSavedRun = ModelStatistics.instance.GetStatValue(RunInitializedStat, false) > 0;
                pendingRunSnapshot = restoringSavedRun ? runtimeData.run : null;
                if (!restoringSavedRun)
                    runtimeData.run = new WhoHeroesRunState();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"WhoHeroes save: runtime data could not be loaded. {exception.Message}", this);
            runtimeData = new WhoHeroesRuntimeData();
            pendingRunSnapshot = null;
            restoringSavedRun = false;
        }

        if (!restoringSavedRun)
        {
            runtimeData.gameStatistics = ModelStatistics.instance.GetWhoHeroesPlayerData();
            ModelStatistics.instance.BindWhoHeroesPlayerData(runtimeData.gameStatistics);
        }

        if (ModelStatistics.instance == null ||
            ModelStatistics.instance.GetStatValue(RunInitializedStat, false) <= 0)
        {
            phase = WhoHeroesPhase.Bootstrap;
            dayNumber = Mathf.Max(1, dayNumber);
            nightNumber = Mathf.Max(0, nightNumber);
            ResetClock(0f, false);
            SyncRunStats();
            return;
        }

        dayNumber = Mathf.Max(1, ModelStatistics.instance.GetStatValue(DayStat, false));
        nightNumber = Mathf.Max(0, ModelStatistics.instance.GetStatValue(NightStat, false));
        var savedElapsedSeconds = Mathf.Clamp(
            ModelStatistics.instance.GetStatValue(DayElapsedSecondsStat, false), 0f, DayDurationSeconds);

        var savedPhase = ModelStatistics.instance.GetStatValue(PhaseStat, false);
        phase = Enum.IsDefined(typeof(WhoHeroesPhase), savedPhase)
            ? (WhoHeroesPhase)savedPhase
            : WhoHeroesPhase.Bootstrap;
        if (phase == WhoHeroesPhase.Night && runtimeData.nightCheckpoint != null)
            pendingRunSnapshot = runtimeData.nightCheckpoint;
        ResetClock(savedElapsedSeconds, phase == WhoHeroesPhase.Day);
    }

    private void PublishPhase(string phaseEvent)
    {
        SyncRunStats();
        MarkRunSaveDirty();
        var args = new ArgPass
        {
            num = (int)phase,
            what = phase.ToString(),
            what1 = (phase == WhoHeroesPhase.Night ? nightNumber : dayNumber).ToString()
        };
        EventManager.INV(PhaseChangedEvent, args);
        EventManager.INV(phaseEvent, args);
    }

    private void SyncRunStats()
    {
        if (ModelStatistics.instance == null)
            return;

        ModelStatistics.instance.SetStatValueForce(RunInitializedStat, 1);
        ModelStatistics.instance.SetStatValueForce(DayStat, dayNumber);
        ModelStatistics.instance.SetStatValueForce(NightStat, nightNumber);
        ModelStatistics.instance.SetStatValueForce(PhaseStat, (int)phase);
        ModelStatistics.instance.SetStatValueForce(DayElapsedSecondsStat,
            phase == WhoHeroesPhase.Day ? Mathf.FloorToInt(DayElapsedSeconds) : 0);
    }

    private void MarkRunSaveDirty()
    {
        if (!initialized)
            return;
        runSaveDirty = true;
        nextRunSaveTime = Time.unscaledTime + 0.25f;
    }

    private void SaveRunSnapshot()
    {
        if (!initialized || !runSnapshotApplied || MainStates.instance == null)
            return;

        SyncRunStats();
        var snapshot = CaptureRunSnapshot();

        runtimeData ??= new WhoHeroesRuntimeData();
        runtimeData.gameStatistics = ModelStatistics.instance.GetWhoHeroesPlayerData();
        runtimeData.run = snapshot;
        ModelStatistics.instance.SaveWhoHeroesRuntimeData(runtimeData);
        pendingRunSnapshot = snapshot;
        runSaveDirty = false;
    }

    private WhoHeroesRunState CaptureRunSnapshot()
    {
        var snapshot = new WhoHeroesRunState();
        var expeditionSavedAmounts = CaptureExpeditionBattleStackAmounts();

        foreach (var value in MainStates.instance.all.Values.Where(ShouldSaveRunObject))
        {
            snapshot.objects.Add(new WhoHeroesRunObjectState
            {
                id = value.RID,
                level = GUILIB.Level(value)
            });
        }

        foreach (var ownerId in new[] { "main_player", "expedition" })
        {
            if (!MainStates.instance.all.TryGetValue(ownerId, out var owner))
                continue;
            var inventory = new WhoHeroesRunInventoryState { ownerId = ownerId };
            foreach (var item in owner.inventory.Where(value => value?.dbObj != null))
            {
                var amount = ResolveSavedStackAmount(owner, item, expeditionSavedAmounts);
                if (amount <= 0)
                    continue;
                inventory.items.Add(new WhoHeroesRunItemState
                {
                    id = item.dbObj.ID,
                    amount = amount,
                    level = Mathf.Max(1, Mathf.RoundToInt(item.GetPar("level"))),
                    usedSlot = Mathf.RoundToInt(item.GetPar("used_slot"))
                });
            }
            snapshot.inventories.Add(inventory);
        }
        foreach (var targetId in capturedTargetIds)
            if (MainStates.instance.all.ContainsKey(targetId))
                snapshot.inventories.Add(new WhoHeroesRunInventoryState { ownerId = targetId });
        return snapshot;
    }

    private void SaveNightCheckpoint()
    {
        if (!initialized || !runSnapshotApplied || MainStates.instance == null || ModelStatistics.instance == null)
            return;

        SyncRunStats();
        var checkpoint = CaptureRunSnapshot();
        runtimeData ??= new WhoHeroesRuntimeData();
        runtimeData.gameStatistics = ModelStatistics.instance.GetWhoHeroesPlayerData();
        runtimeData.run = checkpoint;
        runtimeData.nightCheckpoint = checkpoint;
        ModelStatistics.instance.SaveWhoHeroesRuntimeData(runtimeData);
        pendingRunSnapshot = checkpoint;
        runSaveDirty = false;
    }

    private void ClearNightCheckpoint()
    {
        if (runtimeData != null)
            runtimeData.nightCheckpoint = null;
    }

    private static bool ShouldSaveRunObject(RObj value)
    {
        if (value?.dbObj == null)
            return false;
        if (MainCycle_WhoHeroes.CastleUnits.ContainsKey(value.RID))
            return true;
        return value.main != null && (value.GetPar("building") > 0f || IsPortal(value) ||
               !string.IsNullOrEmpty(ConfigString(value, TerritoryConfigParam)));
    }

    private int ResolveSavedStackAmount(RObj owner, RObj item,
        IReadOnlyDictionary<RObj, int> expeditionSavedAmounts)
    {
        if (TryGetNightSavedStackAmount(owner, item, out var nightAmount))
            return nightAmount;
        if (expeditionSavedAmounts != null && expeditionSavedAmounts.TryGetValue(item, out var expeditionAmount))
            return expeditionAmount;
        return Mathf.Max(0, Mathf.RoundToInt(item.GetPar("amount")));
    }

    private static void NormalizeRosterStacks(RObj owner)
    {
        if (owner == null || DatabaseAll.instance == null || MainStates.instance == null)
            return;

        foreach (var empty in owner.inventory.Where(value => value?.dbObj != null &&
                     value.it == ItemType.monster && value.GetPar("amount") <= 0f).ToList())
            DisposeRuntimeObject(empty);

        foreach (var group in owner.inventory
                     .Where(value => value?.dbObj != null && value.it == ItemType.monster &&
                                     value.GetPar("amount") > 0f)
                     .GroupBy(value => new
                     {
                          value.dbObj.ID,
                          value.shardID,
                          Level = Mathf.Max(1, GUILIB.Level(value)),
                          UsedSlot = Mathf.RoundToInt(value.GetPar("used_slot"))
                     })
                     .ToList())
        {
            var stacks = group
                .OrderBy(value =>
                {
                    var slot = Mathf.RoundToInt(value.GetPar("used_slot"));
                    return slot >= 0 ? slot : int.MaxValue;
                })
                .ToList();
            var maxStack = ResolveMaxStack(stacks[0]);
            var total = stacks.Sum(value => Mathf.Max(0, Mathf.RoundToInt(value.GetPar("amount"))));
            var requiredStacks = Mathf.CeilToInt(total / (float)maxStack);

            for (var index = 0; index < Mathf.Min(requiredStacks, stacks.Count); index++)
                stacks[index].SetPar("amount", Mathf.Min(maxStack, total - index * maxStack));

            for (var index = stacks.Count; index < requiredStacks; index++)
            {
                var amount = Mathf.Min(maxStack, total - index * maxStack);
                var created = DatabaseAll.instance.CreateMonster(group.Key.ID, amount, false, false);
                created.shardID = group.Key.shardID;
                SetExactLevel(created, group.Key.Level);
                created.SetPar("used_slot", group.Key.UsedSlot);
                ApplyPermanentPerksToUnit(created, false);
                AttachRosterStack(owner, created);
            }

            for (var index = stacks.Count - 1; index >= requiredStacks; index--)
                DisposeRuntimeObject(stacks[index]);
        }

        owner.RecalcPars();
    }

    public static void KeepPurchasedUnitsInFreeRoster(
        RObj owner,
        RObj purchasedUnit,
        int purchasedAmount,
        IReadOnlyDictionary<RObj, int> assignedAmountsBefore)
    {
        if (owner == null || purchasedUnit?.dbObj == null || purchasedAmount <= 0 ||
            assignedAmountsBefore == null)
            return;

        var misplaced = 0;
        foreach (var pair in assignedAmountsBefore)
        {
            var stack = pair.Key;
            if (stack == null || stack.owner != owner)
                continue;
            var current = Mathf.Max(0, Mathf.RoundToInt(stack.GetPar("amount")));
            var added = Mathf.Min(purchasedAmount - misplaced, Mathf.Max(0, current - pair.Value));
            if (added <= 0)
                continue;
            stack.SetPar("amount", current - added);
            misplaced += added;
            if (misplaced >= purchasedAmount)
                break;
        }

        if (misplaced > 0)
            AddFreeRosterAmount(owner, purchasedUnit, misplaced);
        NormalizeRosterStacks(owner);
    }

    private static void AddFreeRosterAmount(RObj owner, RObj template, int amount)
    {
        var level = Mathf.Max(1, GUILIB.Level(template));
        var maxStack = ResolveMaxStack(template);
        foreach (var stack in owner.inventory.Where(value => value?.dbObj != null &&
                     value.dbObj.ID == template.dbObj.ID && value.shardID == template.shardID &&
                     Mathf.RoundToInt(value.GetPar("used_slot")) < 0 &&
                     Mathf.Max(1, GUILIB.Level(value)) == level).ToList())
        {
            var current = Mathf.Max(0, Mathf.RoundToInt(stack.GetPar("amount")));
            var added = Mathf.Min(amount, Mathf.Max(0, maxStack - current));
            if (added <= 0)
                continue;
            stack.SetPar("amount", current + added);
            amount -= added;
            if (amount <= 0)
                return;
        }

        while (amount > 0)
        {
            var stackAmount = Mathf.Min(maxStack, amount);
            var created = DatabaseAll.instance.CreateMonster(template.dbObj.ID, stackAmount, false, false);
            created.shardID = template.shardID;
            SetExactLevel(created, level);
            created.SetPar("used_slot", -1f);
            ApplyPermanentPerksToUnit(created, false);
            AttachRosterStack(owner, created);
            amount -= stackAmount;
        }
    }

    private static void AttachRosterStack(RObj owner, RObj stack)
    {
        if (owner == null || stack == null)
            return;
        if (stack.owner != null)
            stack.owner.inventory.Remove(stack);
        stack.index = owner.GetFreeIndex(stack.dbObj.sizeX, stack.dbObj.sizeY);
        owner.inventory.Add(stack);
        stack.owner = owner;
    }

    private static int ResolveMaxStack(RObj value)
    {
        return value?.dbObj != null && value.dbObj.pars.TryGetValue("max_stack", out var configured)
            ? Mathf.Max(1, Mathf.RoundToInt(configured))
            : 1;
    }

    private static void CleanupSpentResourceObjects()
    {
        if (MainStates.instance == null)
            return;
        foreach (var value in MainStates.instance.all.Values.Where(value => value != null &&
                     value.it == ItemType.item && value.GetPar("amount") <= 0f &&
                     (value.owner == null || !value.owner.inventory.Contains(value))).ToList())
            DisposeRuntimeObject(value);
    }

    private void RestoreProgressionAvailabilityFromLevels()
    {
        foreach (var value in MainStates.instance.all.Values)
            if (IsPortal(value) || !string.IsNullOrEmpty(ConfigString(value, TerritoryConfigParam)))
                SetAvailable(value, false);

        foreach (var portal in portalProgression.Where(value => GUILIB.Level(value) > 0))
        {
            SyncExitPortal(portal, true);
            MakeTerritoryAvailable(portal);
        }

        foreach (var value in MainStates.instance.all.Values)
            if (GUILIB.Level(value) > 0)
                SetAvailable(value, false);

        var configuredStartCount = Mathf.Clamp(
            Mathf.RoundToInt(ConfigLoader.GetMetaParamValue("whoheroes_start_active_portals")),
            1,
            portalProgression.Count);
        var capturedCount = portalProgression.Count(value => GUILIB.Level(value) > 0);
        if (nightNumber <= 0 && capturedCount <= configuredStartCount)
            return;

        var next = portalProgression.FirstOrDefault(value =>
            GUILIB.Level(value) <= 0 && !IsRestorableTarget(value));
        if (next != null)
            SetAvailable(next, true);
    }

    private void ApplyRunSnapshot(WhoHeroesRunState snapshot)
    {
        if (snapshot == null || MainStates.instance == null || DatabaseAll.instance == null)
            return;

        capturedTargetIds.Clear();
        foreach (var savedInventory in snapshot.inventories)
        {
            if (savedInventory == null || string.IsNullOrEmpty(savedInventory.ownerId) ||
                savedInventory.items.Count != 0)
                continue;
            if (MainCycle_WhoHeroes.TryGetExpeditionDefense(savedInventory.ownerId, out _) ||
                portalProgression.Any(value => value.RID == savedInventory.ownerId))
                capturedTargetIds.Add(savedInventory.ownerId);
        }

        foreach (var saved in snapshot.objects)
        {
            if (saved == null || string.IsNullOrEmpty(saved.id) ||
                !MainStates.instance.all.TryGetValue(saved.id, out var value))
                continue;
            SetExactLevel(value, saved.level);
        }

        foreach (var savedInventory in snapshot.inventories)
        {
            if (savedInventory == null || string.IsNullOrEmpty(savedInventory.ownerId) ||
                !MainStates.instance.all.TryGetValue(savedInventory.ownerId, out var owner))
                continue;
            foreach (var old in owner.inventory.ToList())
                DisposeRuntimeObject(old);
            foreach (var saved in savedInventory.items)
            {
                if (saved == null || string.IsNullOrEmpty(saved.id))
                    continue;
                RObj item = null;
                if (DatabaseAll.instance.heroes.ContainsKey(saved.id))
                    item = DatabaseAll.instance.CreateMonster(saved.id, Mathf.Max(1, saved.amount), false, false);
                else if (DatabaseAll.instance.items.ContainsKey(saved.id) ||
                         DatabaseAll.instance.skills.ContainsKey(saved.id) ||
                         DatabaseAll.instance.buildings.ContainsKey(saved.id))
                    item = DatabaseAll.instance.CreateItem(saved.id, Mathf.Max(1, saved.amount), false, false);
                if (item == null)
                    continue;
                SetExactParam(item, "level", Mathf.Max(1, saved.level));
                item.SetPar("used_slot", saved.usedSlot);
                MainStates.instance.AddItem(owner, item);
            }
            NormalizeRosterStacks(owner);
            owner.RecalcPars();
        }

        RestoreProgressionAvailabilityFromLevels();
        targetDefendersReady = EnsureTargetDefenders();

        SyncRunStats();
        ApplyPermanentPerksToCurrentRun();
        ApplyCapturedBoostsToCurrentRun();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && initialized)
        {
            SyncRunStats();
            SaveRunSnapshot();
        }
    }

    private void OnApplicationQuit()
    {
        SaveBeforeShutdown();
    }

    private void SaveBeforeShutdown()
    {
        if (!initialized || !runSnapshotApplied || MainStates.instance == null ||
            ModelStatistics.instance == null)
            return;

        SyncRunStats();
        SaveRunSnapshot();
        shutdownSnapshotSaved = true;
    }

    private void OnRestartRequested(ArgPass _)
    {
        RestartRun();
    }

    private void OnResetRequested(ArgPass _)
    {
        RestartRun();
    }

    private void RestartRun()
    {
        var preservedStatistics = new PlayerData();
        if (ModelStatistics.instance != null)
        {
            preservedStatistics.playerStats.Add(new Bon
            {
                Key = BestNightStat,
                Value = ModelStatistics.instance.GetStatValue(BestNightStat, false)
            });
            foreach (var id in MainCycle_WhoHeroes.PermanentPerkIds)
                preservedStatistics.playerStats.Add(new Bon
                {
                    Key = id,
                    Value = ModelStatistics.instance.GetStatValue(id, false)
                });
        }

        runtimeData = new WhoHeroesRuntimeData
        {
            gameStatistics = preservedStatistics,
            run = new WhoHeroesRunState()
        };
        ModelStatistics.instance.SaveWhoHeroesRuntimeData(runtimeData);
        ModelStatistics.instance.BindWhoHeroesPlayerData(preservedStatistics);
        suppressDestroySave = true;
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            SceneManager.LoadScene(scene.name);
    }

    private void SetGameOver()
    {
        if (phase == WhoHeroesPhase.GameOver)
            return;

        phase = WhoHeroesPhase.GameOver;
        if (TimeManager.instance != null)
            TimeManager.instance.spd = 0f;
        ClearNightCheckpoint();
        SyncRunStats();
        SaveRunSnapshot();
        PublishGameOver();
    }

    private void PublishGameOver()
    {
        var bestNight = ModelStatistics.instance == null
            ? 0
            : ModelStatistics.instance.GetStatValue(BestNightStat, false);

        EventManager.INV(PhaseChangedEvent, new ArgPass { num = (int)phase, what = phase.ToString() });
        EventManager.INV(GameOverEvent, new ArgPass
        {
            who = MainStates.instance == null ? null : MainStates.instance.mainPlayer,
            num = nightNumber,
            what = nightNumber.ToString(),
            what1 = bestNight.ToString()
        });
    }

    private static void ResetClock(float elapsedSeconds, bool running)
    {
        if (TimeManager.instance == null)
            return;

        TimeManager.instance.ResetTime();
        if (elapsedSeconds > 0f)
            TimeManager.instance.AddForceTime(elapsedSeconds);
        TimeManager.instance.spd = running ? 1f : 0f;
    }

}
