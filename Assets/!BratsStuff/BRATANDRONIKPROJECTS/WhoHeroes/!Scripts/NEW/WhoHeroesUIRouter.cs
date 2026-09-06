using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WhoHeroesUIRouter : MonoBehaviour
{
    private const string ManagementTabId = "management";
    private const string SettingsTabId = "settings";
    private const string CastleMainTabId = "main";
    private const string CastleHireTabId = "hire";
    private const float BuildingCameraSize = 2f;
    private const float BuildingCameraOffsetY = 1.25f;
    private const float BuildingCameraTransitionSeconds = 0.75f;

    [Header("Persistent HUD")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GUIMainScreen mainScreen;
    [SerializeField] private PopUpList rootTabs = new PopUpList();
    [SerializeField] private PopUpList castleTabs = new PopUpList();

    [Header("Management camera")]
    [SerializeField] private Vector3 managementCameraPosition;
    [SerializeField, Min(0.01f)] private float managementCameraSize = 10f;
    [SerializeField] private Transform buildingFocusRoot;

    [Header("Windows")]
    [SerializeField] private GUICastleWindow castle;
    [SerializeField] private GUIHireBuildingWindow hire;
    [SerializeField] private GUIFactotyWindow factory;
    [SerializeField] private GUIPortalWindow portal;
    [SerializeField] private GUITavernWindow tavern;
    [SerializeField] private GUIMarketWindow market;
    [SerializeField] private GUIEnemyBuilding enemy;
    [SerializeField] private GUIWarBuildWindow warBuilding;
    [SerializeField] private GUITaskBuilWindow taskBuilding;
    [SerializeField] private GUIBuildingInfo genericBuilding;
    [SerializeField] private GUIArmyWindow tower;
    [SerializeField] private GUIArmyWindow expedition;
    [SerializeField] private GUITasksWindow tasks;
    [SerializeField] private GUIPerkWindow trader;
    [SerializeField] private GUIPerkWindow permanentPerks;

    [Header("Minimus UI context")]
    [SerializeField] private ObjHolder actionHolder;

    [Header("Trader arrival")]
    [SerializeField] private GameObject traderVisualPrefab;
    [SerializeField] private Transform traderSpawnPoint;
    [SerializeField] private Transform traderDestination;

    [Header("Scene groups")]
    [SerializeField] private GameObject castleInterior;
    [SerializeField] private GameObject tavernInterior;
    [SerializeField] private GameObject towerInterior;
    [SerializeField] private GameObject expeditionInterior;
    [SerializeField] private List<GameObject> worldRoots = new List<GameObject>();

    private readonly List<GameObject> windowGroups = new List<GameObject>();
    private RObj castleRuntime;
    private RObj portalRuntime;
    private GameObject traderVisual;
    private WhoHeroesTraderStateMachine traderStateMachine;
    private Camera dayCamera;
    private BRATViewMapCameraController dayCameraInput;
    private Vector3 savedCameraPosition;
    private float savedCameraSize;
    private bool savedCameraInputBlocked;
    private bool cameraStateSaved;
    private bool restoreCameraOnClose;
    private Coroutine cameraSizeRoutine;
    private Coroutine buildingWindowRoutine;

    private void Awake()
    {
        CacheDefaultCameraState();
        KeepPersistentHudVisible();
        rootTabs?.SetUpNavigation();
        rootTabs?.ToDefault();
        castleTabs?.SetUpNavigation();
        castleTabs?.ToDefault();
        CacheWindows();
        EventManager.SUB(WhoHeroesEvents.ViewBuilding, OnViewBuilding);
        EventManager.SUB(WhoHeroesEvents.ObserveBuilding, OnObserveBuilding);
        EventManager.SUB(WhoHeroesEvents.DayStartedAfterNight, OnDayStartedAfterNight);
        EventManager.SUB("new_night", OnTraderUnavailable);
        EventManager.SUB("whoheroes_game_over", OnTraderUnavailable);
        EventManager.SUB(WhoHeroesEvents.PermanentPerkOffered, OnPermanentPerkOffered);
        EventManager.SUB(WhoHeroesEvents.PermanentPerkChosen, OnPermanentPerkChosen);
        EventManager.SUB(WhoHeroesEvents.TraderCompleted, OnTraderCompleted);
        EventManager.SUB(WhoHeroesEvents.PortalCaptured, OnPortalCaptured);
    }

    private void Start()
    {
        KeepPersistentHudVisible();
        if (actionHolder == null)
            Debug.LogError("WhoHeroes UI router: Minimus action holder is not assigned in Inspector.", this);
        else
            actionHolder.enabled = true;

        if (HasPendingPermanentPerk() && MainCycle_WhoHeroes.Instance?.Phase == WhoHeroesPhase.Day)
            ShowPermanentPerks();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.ViewBuilding, OnViewBuilding);
        EventManager.UNSUB(WhoHeroesEvents.ObserveBuilding, OnObserveBuilding);
        EventManager.UNSUB(WhoHeroesEvents.DayStartedAfterNight, OnDayStartedAfterNight);
        EventManager.UNSUB("new_night", OnTraderUnavailable);
        EventManager.UNSUB("whoheroes_game_over", OnTraderUnavailable);
        EventManager.UNSUB(WhoHeroesEvents.PermanentPerkOffered, OnPermanentPerkOffered);
        EventManager.UNSUB(WhoHeroesEvents.PermanentPerkChosen, OnPermanentPerkChosen);
        EventManager.UNSUB(WhoHeroesEvents.TraderCompleted, OnTraderCompleted);
        EventManager.UNSUB(WhoHeroesEvents.PortalCaptured, OnPortalCaptured);
        castle?.back?.onClick.RemoveListener(ShowWorld);
        hire?.back?.onClick.RemoveListener(ShowCastleOverview);
        factory?.back?.onClick.RemoveListener(ShowWorld);
        portal?.back?.onClick.RemoveListener(ShowWorld);
        tavern?.back?.onClick.RemoveListener(ShowWorld);
        market?.back?.onClick.RemoveListener(ShowWorld);
        enemy?.back?.onClick.RemoveListener(ShowWorld);
        warBuilding?.back?.onClick.RemoveListener(ShowWorld);
        taskBuilding?.back?.onClick.RemoveListener(ShowWorld);
        tower?.back?.onClick.RemoveListener(ShowWorld);
        expedition?.back?.onClick.RemoveListener(ShowWorld);
        portal?.uprgade?.upgrade?.buy?.onClick.RemoveListener(HandlePortalAction);
        CleanupTraderVisual();
    }

    private void CacheWindows()
    {
        AddWindowGroup(castle);
        AddWindowGroup(hire);
        AddWindowGroup(factory);
        AddWindowGroup(portal);
        AddWindowGroup(tavern);
        AddWindowGroup(market);
        AddWindowGroup(enemy);
        AddWindowGroup(warBuilding);
        AddWindowGroup(taskBuilding);
        AddWindowGroup(genericBuilding);
        AddWindowGroup(tower);
        AddWindowGroup(expedition);
        AddWindowGroup(tasks);

        castle?.back?.onClick.AddListener(ShowWorld);
        hire?.back?.onClick.AddListener(ShowCastleOverview);
        factory?.back?.onClick.AddListener(ShowWorld);
        portal?.back?.onClick.AddListener(ShowWorld);
        tavern?.back?.onClick.AddListener(ShowWorld);
        market?.back?.onClick.AddListener(ShowWorld);
        enemy?.back?.onClick.AddListener(ShowWorld);
        warBuilding?.back?.onClick.AddListener(ShowWorld);
        taskBuilding?.back?.onClick.AddListener(ShowWorld);
        tower?.back?.onClick.AddListener(ShowWorld);
        expedition?.back?.onClick.AddListener(ShowWorld);
        portal?.uprgade?.upgrade?.buy?.onClick.AddListener(HandlePortalAction);
    }

    private void OnDayStartedAfterNight(ArgPass _)
    {
        TryPrepareTraderArrival();
    }

    private void OnTraderUnavailable(ArgPass _)
    {
        HideManagementWindows();
        CleanupTraderVisual();
    }

    private void OnPermanentPerkOffered(ArgPass _)
    {
        if (trader != null)
            trader.gameObject.SetActive(false);
        TryPrepareTraderArrival();
        traderStateMachine?.WaitForPerk();
        ShowPermanentPerks();
    }

    private void OnPermanentPerkChosen(ArgPass _)
    {
        traderStateMachine?.ResumeAfterPerk();
    }

    private void OnTraderCompleted(ArgPass _)
    {
        if (trader != null)
            trader.gameObject.SetActive(false);
        traderStateMachine?.Complete();
        CleanupTraderVisual();
    }

    private void OnPortalCaptured(ArgPass args)
    {
        if (args?.who == null)
            return;

        ShowInterior(null);
        portalRuntime = args.who;
        Show(portal, args.who, value => value.FillCaptureResult(args.who));
    }

    private void TryPrepareTraderArrival()
    {
        var cycle = MainCycle_WhoHeroes.Instance;
        if (trader == null || cycle == null || !cycle.TraderAvailableToday)
        {
            CleanupTraderVisual();
            return;
        }

        if (traderVisual == null)
        {
            if (traderVisualPrefab == null)
            {
                Debug.LogError("WhoHeroes trader: visual prefab is not assigned.", this);
                return;
            }

            traderVisual = Instantiate(traderVisualPrefab, MainStates.instance == null ? null : MainStates.instance.root);
            traderVisual.name = "WhoHeroes Trader";
            var movementZ = traderSpawnPoint == null
                ? traderVisual.transform.position.z
                : traderSpawnPoint.position.z;
            if (!cycle.TryGetTraderRoad(movementZ, out var spawnPosition,
                    out var roadRoute, out var moveSpeed))
            {
                Debug.LogError("WhoHeroes trader: road from the active night portal to the castle was not found.", this);
                CleanupTraderVisual();
                return;
            }
            traderVisual.transform.position = spawnPosition;
            traderStateMachine = traderVisual.GetComponent<WhoHeroesTraderStateMachine>();
            if (traderStateMachine == null || !traderStateMachine.Initialize(
                    roadRoute, moveSpeed,
                    HasPendingPermanentPerk(), OnTraderArrived))
            {
                Debug.LogError("WhoHeroes trader: king prefab state machine could not be initialized.", this);
                CleanupTraderVisual();
                return;
            }
        }
        else if (HasPendingPermanentPerk())
        {
            traderStateMachine?.WaitForPerk();
        }
        else
        {
            traderStateMachine?.ResumeAfterPerk();
        }
    }

    private void OnTraderArrived()
    {
        var cycle = MainCycle_WhoHeroes.Instance;
        if (trader == null || cycle == null || !cycle.TraderAvailableToday || HasPendingPermanentPerk())
            return;

        if (trader.gameObject.activeSelf)
            trader.Fill();
        else
            trader.gameObject.SetActive(true);
    }

    private void ShowPermanentPerks()
    {
        if (permanentPerks == null)
            return;
        if (permanentPerks.gameObject.activeSelf)
            permanentPerks.Fill();
        else
            permanentPerks.gameObject.SetActive(true);
    }

    private void HideManagementWindows()
    {
        foreach (var group in windowGroups)
            if (group != null)
                group.SetActive(false);
        if (trader != null)
            trader.gameObject.SetActive(false);
        if (permanentPerks != null)
            permanentPerks.gameObject.SetActive(false);
        ShowInterior(null);
        ReleaseCameraState();
        rootTabs?.ToDefault();
    }

    private void CleanupTraderVisual()
    {
        traderStateMachine?.Cancel();
        if (traderVisual != null)
            Destroy(traderVisual);
        traderVisual = null;
        traderStateMachine = null;
    }

    private static bool HasPendingPermanentPerk()
    {
        return ModelStatistics.instance != null &&
               ModelStatistics.instance.GetStatValue(MainCycle_WhoHeroes.PendingPerkNightStat, false) > 0;
    }

    private void OnViewBuilding(ArgPass args)
    {
        if (args == null)
            return;

        var runtime = args.who;
        var id = GUILIB.Id(runtime, args.what);
        if (string.IsNullOrEmpty(id))
            return;

        if (id == "castle")
        {
            castleRuntime = runtime;
            ShowCastleOverview();
            return;
        }

        if (MainCycle_WhoHeroes.CastleUnits.ContainsKey(id))
        {
            ShowInterior(castleInterior);
            Show(hire, runtime, value => value.Fill(runtime), true);
            return;
        }

        if (MainCycle_WhoHeroes.HasUndefeatedDefender(runtime))
        {
            Show(enemy, runtime, value => value.Fill(runtime));
            return;
        }

        if (id == "tower")
        {
            ShowInterior(towerInterior);
            Show(tower, runtime, value => value.Fill(runtime), true);
            return;
        }

        if (id == "expedition")
        {
            ShowInterior(expeditionInterior);
            Show(expedition, runtime, value => value.Fill(runtime), true);
            return;
        }

        if (id == "tavern")
        {
            ShowInterior(tavernInterior);
            Show(tavern, runtime, value => value.Fill(runtime), true);
            return;
        }

        if (id.StartsWith("portal", StringComparison.Ordinal))
        {
            portalRuntime = runtime;
            Show(portal, runtime, value => value.Fill(runtime));
            return;
        }

        if (id == "market")
        {
            Show(market, runtime, value => value.Fill());
            return;
        }

        if (!string.IsNullOrEmpty(MainCycle_WhoHeroes.MineResourceId(runtime)))
        {
            Show(factory, runtime, value => value.Fill(runtime));
            return;
        }

        if (!string.IsNullOrEmpty(GUILIB.StringParam(runtime, "story")))
        {
            Show(taskBuilding, runtime, value => value.Fill(runtime));
            return;
        }

        if (MainCycle_WhoHeroes.TryGetBoostStat(id, out _))
        {
            Show(warBuilding, runtime, value => value.Fill(runtime));
            return;
        }

        ShowInterior(null);
        Show(genericBuilding, runtime, value => value.Fill(runtime));
    }

    private void OnObserveBuilding(ArgPass args)
    {
        if (args == null || !MainCycle_WhoHeroes.CastleUnits.ContainsKey(args.what))
            return;

        ShowInterior(castleInterior);
        Show(hire, args.who, value => value.Fill(args.who), true);
        castleTabs?.SwitchTab(CastleHireTabId);
    }

    private void ShowCastleOverview()
    {
        if (castleRuntime == null && MainStates.instance != null)
            MainStates.instance.all.TryGetValue("castle", out castleRuntime);
        ShowInterior(castleInterior);
        Show(castle, castleRuntime, value => value.Fill(castleRuntime), true);
        castleTabs?.SwitchTab(CastleMainTabId);
    }

    private void HandlePortalAction()
    {
        if (MainCycle_WhoHeroes.IsAttackableTarget(portalRuntime))
        {
            Show(enemy, portalRuntime, value => value.Fill(portalRuntime));
            return;
        }

        if (MainCycle_WhoHeroes.IsRestorableTarget(portalRuntime))
            GUILIB.CoreAction(portalRuntime, "upgrade");
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        ToggleSettings();
    }

    private void ToggleSettings()
    {
        if (rootTabs?[SettingsTabId] == null)
            return;
        if (string.Equals(rootTabs.choosen, SettingsTabId, StringComparison.Ordinal))
            rootTabs.ToDefault();
        else
            rootTabs.SwitchTab(SettingsTabId);
    }

    private void ShowWorld()
    {
        ShowInterior(null);
        ReleaseCameraState();
        rootTabs?.ToDefault();
    }

    private void ShowInterior(GameObject target)
    {
        KeepPersistentHudVisible();
        foreach (var worldRoot in worldRoots)
            if (worldRoot != null)
                worldRoot.SetActive(target == null);
        foreach (var interior in new[] { castleInterior, tavernInterior, towerInterior, expeditionInterior })
            if (interior != null)
                interior.SetActive(interior == target);
    }

    private void KeepPersistentHudVisible()
    {
        if (mainCanvas != null)
        {
            mainCanvas.gameObject.SetActive(true);
            mainCanvas.enabled = true;
        }
    }

    private void AddWindowGroup(Component component)
    {
        if (component == null)
            return;

        var root = component.transform;
        while (root.parent != null && root.parent.GetComponent<Canvas>() == null)
            root = root.parent;

        if (!windowGroups.Contains(root.gameObject))
            windowGroups.Add(root.gameObject);
    }

    private void HideOtherGroups(Component target)
    {
        if (target == null)
            return;

        var targetGroup = target.transform;
        while (targetGroup.parent != null && targetGroup.parent.GetComponent<Canvas>() == null)
            targetGroup = targetGroup.parent;

        foreach (var group in windowGroups)
            if (group != null && group != targetGroup.gameObject &&
                (mainScreen == null || group != mainScreen.gameObject))
                group.SetActive(false);

        targetGroup.gameObject.SetActive(true);
        if (target.transform.parent == targetGroup)
        {
            for (var i = 0; i < targetGroup.childCount; i++)
            {
                var child = targetGroup.GetChild(i);
                if (child.GetComponent<GUICastleWindow>() != null || child.GetComponent<GUIHireBuildingWindow>() != null)
                    child.gameObject.SetActive(child == target.transform);
            }
        }
        target.gameObject.SetActive(true);
        KeepPersistentHudVisible();
    }

    private void Show<T>(T target, RObj runtime, Action<T> fill, bool useManagementPreset = false,
        float cameraOffsetY = BuildingCameraOffsetY)
        where T : Component
    {
        if (target == null)
        {
            Debug.LogWarning($"WhoHeroes UI router: no window is wired for '{GUILIB.Id(runtime)}'.", this);
            return;
        }

        if (useManagementPreset)
        {
            EnterManagementView();
            PresentWindow(target, fill);
            return;
        }

        EnterBuildingView(runtime, cameraOffsetY, () => PresentWindow(target, fill));
    }

    private void PresentWindow<T>(T target, Action<T> fill) where T : Component
    {
        HideOtherGroups(target);
        fill(target);
    }

    private void CacheDefaultCameraState()
    {
        dayCamera = Camera.main;
        if (dayCamera == null)
            return;
        dayCameraInput = dayCamera.GetComponent<BRATViewMapCameraController>();
    }

    private void EnterManagementView()
    {
        if (dayCamera == null)
            CacheDefaultCameraState();
        if (dayCamera == null)
            return;

        SaveCameraState();
        StopCameraFocus();
        restoreCameraOnClose = true;

        rootTabs?.SwitchTab(ManagementTabId);
        dayCamera.transform.position = managementCameraPosition;
        dayCamera.orthographicSize = managementCameraSize;
        dayCameraInput?.SetInputBlocked(true);
    }

    private void EnterBuildingView(RObj runtime, float cameraOffsetY, Action onCameraReady)
    {
        if (dayCamera == null)
            CacheDefaultCameraState();
        if (dayCamera == null)
            return;

        var openedFromMap = IsNavigationWindowOpen();
        SaveCameraState();
        if (openedFromMap)
            savedCameraInputBlocked = false;
        StopCameraFocus();
        restoreCameraOnClose = false;
        ShowInterior(null);
        rootTabs?.SwitchTab(ManagementTabId);
        dayCameraInput?.SetInputBlocked(true);

        var focus = FindClosestBuildingFocus(runtime?.main?.transform);
        if (focus == null || UtilsControl.Instance == null)
        {
            Debug.LogWarning($"WhoHeroes UI router: camera focus point was not found for '{GUILIB.Id(runtime)}'.", this);
            onCameraReady?.Invoke();
            return;
        }

        var targetPosition = focus.position + Vector3.up * cameraOffsetY;
        var distance = Vector3.Distance(dayCamera.transform.position, targetPosition);
        if (distance > 0.001f)
        {
            var speed = distance / BuildingCameraTransitionSeconds;
            UtilsControl.Instance.MoveTo(dayCamera.transform, speed, targetPosition, null, null,
                useRight: false, ignoreFlip: true);
        }
        else
            dayCamera.transform.position = targetPosition;
        cameraSizeRoutine = StartCoroutine(ScaleCameraSize(
            BuildingCameraSize, BuildingCameraTransitionSeconds));
        buildingWindowRoutine = StartCoroutine(PresentAfterCameraTransition(
            targetPosition, BuildingCameraSize, onCameraReady));
    }

    private IEnumerator PresentAfterCameraTransition(Vector3 targetPosition, float targetSize, Action present)
    {
        yield return new WaitForSeconds(BuildingCameraTransitionSeconds);
        dayCamera.transform.position = targetPosition;
        dayCamera.orthographicSize = targetSize;
        buildingWindowRoutine = null;
        present?.Invoke();
    }

    private bool IsNavigationWindowOpen()
    {
        if (mainScreen == null)
            return false;

        foreach (var slider in mainScreen.GetComponentsInChildren<WindowSlider>(true))
            if (slider != null && slider.IsOpen &&
                string.Equals(slider.wtype, "navigation", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private void SaveCameraState()
    {
        if (cameraStateSaved || dayCamera == null)
            return;
        savedCameraPosition = dayCamera.transform.position;
        savedCameraSize = dayCamera.orthographicSize;
        savedCameraInputBlocked = dayCameraInput != null && dayCameraInput.InputBlocked;
        cameraStateSaved = true;
    }

    private Transform FindClosestBuildingFocus(Transform building)
    {
        if (building == null || buildingFocusRoot == null || buildingFocusRoot.childCount == 0)
            return null;

        Transform closest = null;
        var bestDistance = float.PositiveInfinity;
        for (var index = 0; index < buildingFocusRoot.childCount; index++)
        {
            var candidate = buildingFocusRoot.GetChild(index);
            var distance = Vector2.SqrMagnitude(candidate.position - building.position);
            if (distance >= bestDistance)
                continue;
            closest = candidate;
            bestDistance = distance;
        }
        return closest;
    }

    private void StopCameraFocus()
    {
        if (dayCamera == null)
            return;
        var movement = dayCamera.GetComponent<MoveDir>();
        if (movement?.cr != null && UtilsControl.Instance != null)
        {
            UtilsControl.Instance.StopCoroutine(movement.cr);
            movement.cr = null;
        }
        if (cameraSizeRoutine != null)
        {
            StopCoroutine(cameraSizeRoutine);
            cameraSizeRoutine = null;
        }
        if (buildingWindowRoutine != null)
        {
            StopCoroutine(buildingWindowRoutine);
            buildingWindowRoutine = null;
        }
        dayCamera.name = dayCamera.name.Replace("_move", string.Empty);
    }

    private IEnumerator ScaleCameraSize(float targetSize, float duration)
    {
        var startSize = dayCamera.orthographicSize;
        for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            dayCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
            yield return null;
        }
        dayCamera.orthographicSize = targetSize;
        cameraSizeRoutine = null;
    }

    private void ReleaseCameraState()
    {
        if (!cameraStateSaved || dayCamera == null)
            return;
        if (restoreCameraOnClose)
        {
            RestoreCameraState();
            return;
        }
        StopCameraFocus();
        dayCameraInput?.SetInputBlocked(savedCameraInputBlocked);
        cameraStateSaved = false;
    }

    private void RestoreCameraState()
    {
        if (!cameraStateSaved || dayCamera == null)
            return;
        StopCameraFocus();
        dayCamera.transform.position = savedCameraPosition;
        dayCamera.orthographicSize = savedCameraSize;
        dayCameraInput?.SetInputBlocked(savedCameraInputBlocked);
        cameraStateSaved = false;
        restoreCameraOnClose = false;
    }

}
