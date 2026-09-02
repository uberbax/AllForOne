using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WhoHeroesUIRouter : MonoBehaviour
{
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
    [SerializeField] private List<GameObject> worldRoots = new List<GameObject>();

    private readonly List<GameObject> windowGroups = new List<GameObject>();
    private RObj castleRuntime;
    private RObj portalRuntime;
    private GameObject traderVisual;
    private WhoHeroesTraderStateMachine traderStateMachine;

    private void Awake()
    {
        CacheWindows();
        EventManager.SUB(WhoHeroesEvents.ViewBuilding, OnViewBuilding);
        EventManager.SUB(WhoHeroesEvents.ObserveBuilding, OnObserveBuilding);
        EventManager.SUB("new_day", OnNewDay);
        EventManager.SUB("new_night", OnTraderUnavailable);
        EventManager.SUB("whoheroes_game_over", OnTraderUnavailable);
        EventManager.SUB(WhoHeroesEvents.PermanentPerkOffered, OnPermanentPerkOffered);
        EventManager.SUB(WhoHeroesEvents.PermanentPerkChosen, OnPermanentPerkChosen);
        EventManager.SUB(WhoHeroesEvents.TraderCompleted, OnTraderCompleted);
        EventManager.SUB(WhoHeroesEvents.PortalCaptured, OnPortalCaptured);
    }

    private void Start()
    {
        if (actionHolder == null)
            Debug.LogError("WhoHeroes UI router: Minimus action holder is not assigned in Inspector.", this);
        else
            actionHolder.enabled = true;

        TryPrepareTraderArrival();
        if (HasPendingPermanentPerk() && MainCycle_WhoHeroes.Instance?.Phase == WhoHeroesPhase.Day)
            ShowPermanentPerks();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.ViewBuilding, OnViewBuilding);
        EventManager.UNSUB(WhoHeroesEvents.ObserveBuilding, OnObserveBuilding);
        EventManager.UNSUB("new_day", OnNewDay);
        EventManager.UNSUB("new_night", OnTraderUnavailable);
        EventManager.UNSUB("whoheroes_game_over", OnTraderUnavailable);
        EventManager.UNSUB(WhoHeroesEvents.PermanentPerkOffered, OnPermanentPerkOffered);
        EventManager.UNSUB(WhoHeroesEvents.PermanentPerkChosen, OnPermanentPerkChosen);
        EventManager.UNSUB(WhoHeroesEvents.TraderCompleted, OnTraderCompleted);
        EventManager.UNSUB(WhoHeroesEvents.PortalCaptured, OnPortalCaptured);
        castle?.back?.onClick.RemoveListener(ShowWorld);
        hire?.back?.onClick.RemoveListener(ShowCastleOverview);
        tavern?.back?.onClick.RemoveListener(ShowWorld);
        tower?.back?.onClick.RemoveListener(ShowWorld);
        expedition?.back?.onClick.RemoveListener(ShowWorld);
        portal?.uprgade?.upgrade?.buy?.onClick.RemoveListener(ShowPortalAttack);
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
        tavern?.back?.onClick.AddListener(ShowWorld);
        tower?.back?.onClick.AddListener(ShowWorld);
        expedition?.back?.onClick.AddListener(ShowWorld);
        portal?.uprgade?.upgrade?.buy?.onClick.AddListener(ShowPortalAttack);
    }

    private void OnNewDay(ArgPass _)
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
            if (traderVisualPrefab == null || traderSpawnPoint == null || traderDestination == null)
            {
                Debug.LogError("WhoHeroes trader: visual prefab, gate spawn or castle destination is not assigned.", this);
                return;
            }

            traderVisual = Instantiate(traderVisualPrefab, MainStates.instance == null ? null : MainStates.instance.root);
            traderVisual.name = "WhoHeroes Trader";
            traderVisual.transform.position = traderSpawnPoint.position;
            traderStateMachine = traderVisual.GetComponent<WhoHeroesTraderStateMachine>();
            if (traderStateMachine == null || !traderStateMachine.Initialize(
                    traderDestination, MainCycle_WhoHeroes.TraderTravelSeconds(),
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
            ShowInterior(castleInterior);
            Show(castle, runtime, value => value.Fill(runtime));
            return;
        }

        if (MainCycle_WhoHeroes.CastleUnits.ContainsKey(id))
        {
            ShowInterior(castleInterior);
            Show(hire, runtime, value => value.Fill(runtime));
            return;
        }

        if (id == "tower")
        {
            ShowInterior(towerInterior);
            Show(tower, runtime, value => value.Fill(runtime));
            return;
        }

        if (id == "expedition")
        {
            Show(expedition, runtime, value => value.Fill(runtime));
            return;
        }

        if (id == "tavern")
        {
            ShowInterior(tavernInterior);
            Show(tavern, runtime, value => value.Fill(runtime));
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

        if (MainCycle_WhoHeroes.IsAttackableTarget(runtime))
        {
            Show(enemy, runtime, value => value.Fill(runtime));
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
        Show(hire, args.who, value => value.Fill(args.who));
    }

    private void ShowCastleOverview()
    {
        if (castleRuntime == null && MainStates.instance != null)
            MainStates.instance.all.TryGetValue("castle", out castleRuntime);
        ShowInterior(castleInterior);
        Show(castle, castleRuntime, value => value.Fill(castleRuntime));
    }

    private void ShowPortalAttack()
    {
        if (!MainCycle_WhoHeroes.IsAttackableTarget(portalRuntime))
            return;
        Show(enemy, portalRuntime, value => value.Fill(portalRuntime));
    }

    private void ShowWorld()
    {
        ShowInterior(null);
    }

    private void ShowInterior(GameObject target)
    {
        foreach (var worldRoot in worldRoots)
            if (worldRoot != null)
                worldRoot.SetActive(target == null);
        foreach (var interior in new[] { castleInterior, tavernInterior, towerInterior })
            if (interior != null)
                interior.SetActive(interior == target);
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
            if (group != null && group != targetGroup.gameObject)
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
    }

    private void Show<T>(T target, RObj runtime, Action<T> fill) where T : Component
    {
        if (target == null)
        {
            Debug.LogWarning($"WhoHeroes UI router: no window is wired for '{GUILIB.Id(runtime)}'.", this);
            return;
        }

        HideOtherGroups(target);
        fill(target);
    }

}
