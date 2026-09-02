using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class MainCycle_WhoHeroes
{
    private const string ExpeditionParseEndedEvent = "PARSE_ENDED";
    private const string ExpeditionBattleEndedEvent = "battle_ended";
    private const string ExpeditionBattleStartedEvent = "battle_start";
    private const string ExpeditionId = "expedition";
    private const string PlayerId = "main_player";
    private const string PhaseParam = "whoheroes_expedition_phase";
    private const string TargetMetaPrefix = "whoheroes_expedition_target:";
    private const string BattleMeta = "whoheroes_expedition_battle";
    private static readonly List<(string, string)> ExpeditionBattleVisualOverrides =
        new List<(string, string)> { ("drop", "x") };

    private WhoHeroesExpeditionPhase expeditionPhase;
    private RObj expedition;
    private RObj currentTarget;
    private readonly List<GameObject> travelMarkers = new List<GameObject>();
    private bool expeditionInitialized;
    private bool targetDefendersReady;
    private readonly List<RObj> battleUnits = new List<RObj>();
    private readonly List<RObj> expeditionSuspendedCombats = new List<RObj>();
    private int expeditionSavedSpawnerCount;
    private string expeditionSavedBattleTag = string.Empty;
    private string expeditionSavedWinTag = string.Empty;
    private string expeditionSavedLoseTag = string.Empty;
    private float expeditionSavedDayClock;
    private float expeditionBattleStartedAt;
    private bool expeditionSavedAutoAddExp;
    private bool expeditionHasSavedBattleOverrides;
    private bool expeditionCleaningUp;
    private Coroutine expeditionBattleStartRoutine;
    private bool awaitingExpeditionBattleEndBeforeNight;

    public WhoHeroesExpeditionPhase ExpeditionPhase => expeditionPhase;
    public bool ExpeditionBusy => expeditionPhase != WhoHeroesExpeditionPhase.Idle;
    public IReadOnlyList<RObj> SelectedUnits => expedition == null ? Array.Empty<RObj>() : expedition.inventory;

    private void InitializeExpeditionOrchestration()
    {
        ResolveExpeditionSceneReferences();
        EventManager.SUB(ExpeditionParseEndedEvent, OnExpeditionParseEnded);
        EventManager.SUB(ExpeditionBattleEndedEvent, OnExpeditionBattleEnded);
    }

    private void StartExpeditionOrchestration()
    {
        if (ConfigLoader.parseEnded)
            InitializeExpeditionRuntime();
    }

    private void UpdateExpeditionOrchestration()
    {
        if (!expeditionInitialized && ConfigLoader.parseEnded)
            InitializeExpeditionRuntime();
        if (expeditionInitialized && !targetDefendersReady)
            targetDefendersReady = EnsureTargetDefenders();
        if (expeditionPhase == WhoHeroesExpeditionPhase.Battle)
            SyncBattlePositions();
    }

    private void SyncBattlePositions()
    {
        foreach (var unit in battleUnits)
            if (unit?.main != null)
                unit.Position = unit.main.transform.position;
    }

    private void DisposeExpeditionOrchestration()
    {
        EventManager.UNSUB(ExpeditionParseEndedEvent, OnExpeditionParseEnded);
        EventManager.UNSUB(ExpeditionBattleEndedEvent, OnExpeditionBattleEnded);
        CleanupExpeditionForShutdown();
    }

    public static bool IsAttackableTarget(RObj target)
    {
        return target != null && GUILIB.Level(target) <= 0 && target.GetPar(AvailableParam) > 0f &&
               MainCycle_WhoHeroes.TryGetExpeditionDefense(GUILIB.Id(target), out _);
    }

    public static IReadOnlyList<RObj> GetSelectedUnits()
    {
        return MainCycle_WhoHeroes.Instance?.SelectedUnits ?? Array.Empty<RObj>();
    }

    public bool CanSelectUnit(RObj unit)
    {
        return InitializeExpeditionRuntime() && expeditionPhase == WhoHeroesExpeditionPhase.Idle &&
               MainCycle_WhoHeroes.Instance != null && MainCycle_WhoHeroes.Instance.Phase == WhoHeroesPhase.Day &&
               unit != null && unit.owner != null && unit.owner.RID == PlayerId &&
               unit.it == ItemType.monster && unit.GetPar("used_slot") < 0f &&
               expedition.inventory.Count(value => value != null && value.GetPar("amount") > 0f) <
               MainCycle_WhoHeroes.ExpeditionMaxStacks;
    }

    public bool TrySelectUnit(RObj unit)
    {
        if (!CanSelectUnit(unit))
            return false;

        Transfer(unit, expedition);
        PublishRefresh(unit, "addexp");
        return true;
    }

    public bool TryDeselectUnit(RObj unit)
    {
        if (!InitializeExpeditionRuntime() || expeditionPhase != WhoHeroesExpeditionPhase.Idle || unit == null ||
            MainCycle_WhoHeroes.Instance == null || MainCycle_WhoHeroes.Instance.Phase != WhoHeroesPhase.Day ||
            unit.owner != expedition || !MainStates.instance.all.TryGetValue(PlayerId, out var player))
            return false;

        Transfer(unit, player);
        PublishRefresh(unit, "removeexp");
        return true;
    }

    public bool CanStart(RObj target)
    {
        return InitializeExpeditionRuntime() && expeditionPhase == WhoHeroesExpeditionPhase.Idle &&
               MainCycle_WhoHeroes.Instance != null && MainCycle_WhoHeroes.Instance.Phase == WhoHeroesPhase.Day &&
               IsAttackableTarget(target) && expedition.inventory.Any(IsLivingStack) &&
               expeditionBattleSpawner != null && expeditionBattleController != null && UtilsControl.Instance != null &&
               ResolveRouteSpeed() > 0f && TryResolveRoute(target, out _);
    }

    public bool TryStart(RObj target)
    {
        if (!CanStart(target) || !TryResolveRoute(target, out var route))
            return false;

        currentTarget = target;
        SetExpeditionPhase(WhoHeroesExpeditionPhase.Traveling);
        SetTargetMeta(target.RID);

        var marker = SpawnTravelMarkers();
        if (marker == null)
        {
            Debug.LogError("WhoHeroes expedition: ExpeditionGo spawner did not create the travel marker.", this);
            AbortToIdle();
            return false;
        }

        var path = new List<(float, float, float)>();
        foreach (Transform waypoint in route)
            path.Add((waypoint.position.x, waypoint.position.y, waypoint.position.z));

        var targetPosition = target.main.transform.position;
        if (path.Count == 0 || Vector3.Distance(ToVector(path[path.Count - 1]), targetPosition) > 0.1f)
            path.Add((targetPosition.x, targetPosition.y, targetPosition.z));

        for (var index = 0; index < travelMarkers.Count; index++)
        {
            var travelMarker = travelMarkers[index];
            var callback = index == 0 ? (System.Action)StartBattle : null;
            UtilsControl.Instance.MoveToMany(travelMarker.transform, ResolveRouteSpeed(), path, 0, callback);
        }
        PublishRefresh(target, "start_expedition");
        return true;
    }

    public void PrepareForNightTransition()
    {
        if (expeditionPhase == WhoHeroesExpeditionPhase.Idle)
        {
            ReturnSelectedUnitsToCity();
            return;
        }

        if (expeditionPhase == WhoHeroesExpeditionPhase.Traveling)
        {
            DestroyTravelMarkers();
            currentTarget = null;
            ClearTargetMeta();
            SetExpeditionPhase(WhoHeroesExpeditionPhase.ReturnPending);
            PublishRefresh(expedition, "expedition_return_pending");
            return;
        }

        if (expeditionPhase != WhoHeroesExpeditionPhase.Battle)
            return;

        var battleAlreadyResolved = expeditionBattleStartRoutine == null &&
                                    expeditionBattleController != null &&
                                    !expeditionBattleController.startDo;
        if (battleAlreadyResolved)
        {
            awaitingExpeditionBattleEndBeforeNight = true;
            ResolveExpeditionBattle(MainStates.instance != null && MainStates.instance.lastBattleResult == 0);
            return;
        }

        ApplyBattleSurvivorsToExpedition();
        CleanupBattleState();
        currentTarget = null;
        ClearTargetMeta();
        SetExpeditionPhase(WhoHeroesExpeditionPhase.ReturnPending);
        PublishRefresh(expedition, "expedition_return_pending");
    }

    public void ReturnAtDawn()
    {
        if (!InitializeExpeditionRuntime() || expeditionPhase != WhoHeroesExpeditionPhase.ReturnPending ||
            !MainStates.instance.all.TryGetValue(PlayerId, out var player))
            return;

        foreach (var stack in expedition.inventory.ToList())
        {
            if (stack == null || stack.GetPar("amount") <= 0f)
            {
                expedition.inventory.Remove(stack);
                RemoveExpeditionRuntimeObject(stack);
                continue;
            }

            MainCycle_WhoHeroes.AddOrMergeCityStack(stack);
        }

        DestroyTravelMarkers();
        currentTarget = null;
        ClearTargetMeta();
        SetExpeditionPhase(WhoHeroesExpeditionPhase.Idle);
        PublishRefresh(expedition, "expedition_returned");
    }

    private void ReturnSelectedUnitsToCity()
    {
        if (expedition == null || MainStates.instance == null ||
            !MainStates.instance.all.TryGetValue(PlayerId, out var player))
            return;

        foreach (var stack in expedition.inventory.ToList())
        {
            if (stack == null || stack.GetPar("amount") <= 0f)
            {
                expedition.inventory.Remove(stack);
                RemoveExpeditionRuntimeObject(stack);
                continue;
            }

            Transfer(stack, player);
        }

        PublishRefresh(expedition, "expedition_selection_cleared");
    }

    private void OnExpeditionParseEnded(ArgPass _)
    {
        InitializeExpeditionRuntime();
    }

    public void SyncRestoredState()
    {
        if (!InitializeExpeditionRuntime() || expedition == null)
            return;

        DestroyTravelMarkers();
        currentTarget = null;
        ClearTargetMeta();
        var restoredPhase = expedition.inventory.Any(IsLivingStack)
            ? WhoHeroesExpeditionPhase.ReturnPending
            : WhoHeroesExpeditionPhase.Idle;
        SetExpeditionPhase(restoredPhase);
    }

    public bool TryGetExpeditionSavedStackAmount(RObj owner, RObj stack, out int amount)
    {
        amount = 0;
        if (expeditionPhase != WhoHeroesExpeditionPhase.Battle || owner == null || stack?.dbObj == null)
            return false;
        if (owner != expedition && owner != currentTarget)
            return false;

        return CaptureExpeditionBattleStackAmounts(owner).TryGetValue(stack, out amount);
    }

    private IReadOnlyDictionary<RObj, int> CaptureExpeditionBattleStackAmounts(RObj owner = null)
    {
        var result = new Dictionary<RObj, int>();
        owner ??= expedition;
        if (expeditionPhase != WhoHeroesExpeditionPhase.Battle || owner == null)
            return result;

        var requiredTag = owner == expedition ? "player" : "enemy";
        var survivors = battleUnits
            .Where(value => value?.dbObj != null && value.tags.Contains(requiredTag) &&
                            value.GetPar("health") > 0f)
            .GroupBy(value => (value.dbObj.ID,
                Level: Mathf.Max(1, Mathf.RoundToInt(value.GetPar("level")))))
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var group in owner.inventory
                     .Where(value => value?.dbObj != null && value.it == ItemType.monster)
                     .GroupBy(value => (value.dbObj.ID,
                         Level: Mathf.Max(1, Mathf.RoundToInt(value.GetPar("level"))))))
        {
            survivors.TryGetValue(group.Key, out var remaining);
            foreach (var stack in group)
            {
                var sourceAmount = Mathf.Max(0, Mathf.RoundToInt(stack.GetPar("amount")));
                var survivorAmount = Mathf.Min(sourceAmount, remaining);
                result[stack] = survivorAmount;
                remaining -= survivorAmount;
            }
        }

        return result;
    }

    private bool InitializeExpeditionRuntime()
    {
        if (expeditionInitialized)
            return true;
        if (MainStates.instance == null || DatabaseAll.instance == null ||
            !MainStates.instance.all.TryGetValue(ExpeditionId, out expedition))
            return false;

        targetDefendersReady = EnsureTargetDefenders();
        expeditionPhase = (WhoHeroesExpeditionPhase)Mathf.Clamp(
            Mathf.RoundToInt(expedition.GetPar(PhaseParam)),
            (int)WhoHeroesExpeditionPhase.Idle,
            (int)WhoHeroesExpeditionPhase.ReturnPending);
        if (expeditionPhase == WhoHeroesExpeditionPhase.Traveling || expeditionPhase == WhoHeroesExpeditionPhase.Battle)
            expeditionPhase = WhoHeroesExpeditionPhase.ReturnPending;
        SetExpeditionPhase(expeditionPhase);
        expeditionInitialized = true;
        return true;
    }

    private bool EnsureTargetDefenders()
    {
        var allReady = true;
        foreach (var pair in MainCycle_WhoHeroes.ExpeditionDefenses)
        {
            if (!MainStates.instance.all.TryGetValue(pair.Key, out var target))
            {
                allReady = false;
                continue;
            }
            if (!DatabaseAll.instance.heroes.ContainsKey(pair.Value.UnitId))
                continue;

            var current = target.inventory.FirstOrDefault(value => value != null && value.it == ItemType.monster);
            if (current != null && current.dbObj != null && current.dbObj.ID == pair.Value.UnitId)
            {
                ApplyLevel(current, pair.Value.Level);
                continue;
            }

            if (current != null)
                MainCycle_WhoHeroes.DisposeRuntimeObject(current);
            var defender = DatabaseAll.instance.CreateMonster(pair.Value.UnitId, pair.Value.Count, false, false);
            ApplyLevel(defender, pair.Value.Level);
            MainStates.instance.AddItem(target, defender);
        }
        return allReady;
    }

    private void StartBattle()
    {
        if (expeditionPhase != WhoHeroesExpeditionPhase.Traveling || currentTarget == null)
            return;

        DestroyTravelMarkers();

        var defender = currentTarget.inventory.FirstOrDefault(value => value != null && value.it == ItemType.monster);
        if (defender == null || !expedition.inventory.Any(IsLivingStack))
        {
            Debug.LogError($"WhoHeroes expedition: target '{GUILIB.Id(currentTarget)}' has no defender or the expedition is empty.", this);
            AbortToIdle();
            return;
        }

        SetExpeditionPhase(WhoHeroesExpeditionPhase.Battle);
        IsolateExpeditionBattleState();

        var targetPosition = currentTarget.main.transform.position;
        var allies = expedition.inventory.Where(IsLivingStack)
            .Select(value => new Bon
            {
                Key = value.dbObj.ID,
                Value = Mathf.Max(1, Mathf.RoundToInt(value.GetPar("amount"))),
                Val3 = Mathf.Max(1, Mathf.RoundToInt(value.GetPar("level")))
            }).ToList();
        var enemies = new List<Bon>
        {
            new Bon
            {
                Key = defender.dbObj.ID,
                Value = Mathf.Max(1, Mathf.RoundToInt(defender.GetPar("amount"))),
                Val3 = Mathf.Max(1, Mathf.RoundToInt(defender.GetPar("level")))
            }
        };

        var allyBattleUnits = expeditionBattleSpawner.DoSpawnAny(
            allies, "player", null, null, true,
            targetPosition + new Vector3(-2f, -1f), targetPosition + new Vector3(-1f, 1f),
            overridesViz: ExpeditionBattleVisualOverrides);
        foreach (var ally in allyBattleUnits)
            MainCycle_WhoHeroes.ApplyPermanentPerksToUnit(ally, false);
        battleUnits.AddRange(allyBattleUnits);
        battleUnits.AddRange(expeditionBattleSpawner.DoSpawnAny(
            enemies, "enemy", null, null, true,
            targetPosition + new Vector3(1f, -1f), targetPosition + new Vector3(2f, 1f),
            overridesViz: ExpeditionBattleVisualOverrides));
        foreach (var unit in battleUnits)
            unit.AddMeta(BattleMeta);

        MainStates.instance.lastBattle = "whoheroes_expedition_" + GUILIB.Id(currentTarget);
        expeditionBattleStartRoutine = StartCoroutine(BeginBattleNextFrame());
    }

    private IEnumerator BeginBattleNextFrame()
    {
        yield return null;
        expeditionBattleStartRoutine = null;
        EventManager.INV(ExpeditionBattleStartedEvent, new ArgPass { who = currentTarget, what = GUILIB.Id(currentTarget) });
    }

    private void OnExpeditionBattleEnded(ArgPass args)
    {
        if (awaitingExpeditionBattleEndBeforeNight && expeditionPhase != WhoHeroesExpeditionPhase.Battle)
        {
            awaitingExpeditionBattleEndBeforeNight = false;
            return;
        }

        if (expeditionPhase != WhoHeroesExpeditionPhase.Battle)
            return;

        ResolveExpeditionBattle(args != null && args.num == 0);
    }

    private void ResolveExpeditionBattle(bool victory)
    {
        ApplyBattleSurvivorsToExpedition();
        var resolvedTarget = currentTarget;
        if (victory)
        {
            RegisterCapture();
            ModelStatistics.instance?.IncreaseStatValue(MainCycle_WhoHeroes.ExpeditionsWonStat, 1);
        }

        CleanupBattleState();
        SetExpeditionPhase(WhoHeroesExpeditionPhase.ReturnPending);
        PublishRefresh(resolvedTarget, victory ? "expedition_victory" : "expedition_loss");
    }

    private void ApplyBattleSurvivorsToExpedition()
    {
        if (expedition == null || battleUnits.Count == 0)
            return;

        foreach (var pair in CaptureExpeditionBattleStackAmounts())
            pair.Key.SetPar("amount", pair.Value);
    }

    private void RegisterCapture()
    {
        var id = GUILIB.Id(currentTarget);
        var captured = id.StartsWith("portalin", StringComparison.Ordinal)
            ? MainCycle_WhoHeroes.Instance.TryRegisterTerritoryOpened(currentTarget)
            : MainCycle_WhoHeroes.Instance.TryRegisterPointOfInterestCaptured(currentTarget);
        if (!captured)
        {
            Debug.LogError($"WhoHeroes expedition: victory over '{id}' was not registered by the main cycle.", this);
            return;
        }

        foreach (var defender in currentTarget.inventory
                     .Where(value => value != null && value.it == ItemType.monster).ToList())
            MainCycle_WhoHeroes.DisposeRuntimeObject(defender);
    }

    private void IsolateExpeditionBattleState()
    {
        expeditionSavedSpawnerCount = MainStates.instance.curSp;
        expeditionSavedBattleTag = BattleController.reqTag;
        expeditionSavedWinTag = expeditionBattleController.winTag;
        expeditionSavedLoseTag = expeditionBattleController.loseTag;
        expeditionSavedDayClock = TimeManager.instance == null ? 0f : TimeManager.instance.tm;
        expeditionBattleStartedAt = Time.unscaledTime;
        expeditionSavedAutoAddExp = XDdeath.autoAddExp;
        expeditionHasSavedBattleOverrides = true;
        XDdeath.autoAddExp = false;

        expeditionSuspendedCombats.Clear();
        expeditionSuspendedCombats.AddRange(MainStates.instance.combats);
        MainStates.instance.combats.Clear();
        foreach (var value in expeditionSuspendedCombats)
            if (value != null && value.visuals.TryGetValue("combat", out var visual))
                visual.GetComponent<XDcombat>().curTg = "chill";

        MainStates.instance.curSp = 0;
        BattleController.reqTag = BattleMeta;
        expeditionBattleController.winTag = "wave";
        expeditionBattleController.loseTag = "my_side";
    }

    private void CleanupBattleState()
    {
        CancelExpeditionBattleStartRoutine();
        if (expeditionBattleController != null)
            expeditionBattleController.startDo = false;
        foreach (var unit in battleUnits)
            RemoveExpeditionRuntimeObject(unit);
        battleUnits.Clear();

        if (MainStates.instance != null)
        {
            foreach (var value in expeditionSuspendedCombats)
                if (value != null && !MainStates.instance.combats.Contains(value))
                    MainStates.instance.combats.Add(value);
            MainStates.instance.curSp = expeditionSavedSpawnerCount;
        }
        expeditionSuspendedCombats.Clear();

        BattleController.reqTag = expeditionSavedBattleTag;
        if (expeditionBattleController != null)
        {
            expeditionBattleController.winTag = expeditionSavedWinTag;
            expeditionBattleController.loseTag = expeditionSavedLoseTag;
        }
        RestoreBattleOverrides();
        if (TimeManager.instance != null)
            TimeManager.instance.tm = expeditionSavedDayClock + Mathf.Max(0f, Time.unscaledTime - expeditionBattleStartedAt);
    }

    private void CleanupExpeditionForShutdown()
    {
        if (expeditionCleaningUp)
            return;

        expeditionCleaningUp = true;
        awaitingExpeditionBattleEndBeforeNight = false;
        CancelExpeditionBattleStartRoutine();
        DestroyTravelMarkers();

        if (expeditionPhase == WhoHeroesExpeditionPhase.Battle || expeditionHasSavedBattleOverrides || battleUnits.Count > 0 ||
            expeditionSuspendedCombats.Count > 0)
            CleanupBattleState();
        else
            RestoreBattleOverrides();

        if (expedition != null &&
            (expeditionPhase == WhoHeroesExpeditionPhase.Traveling || expeditionPhase == WhoHeroesExpeditionPhase.Battle))
            SetExpeditionPhase(WhoHeroesExpeditionPhase.ReturnPending);
        currentTarget = null;
        ClearTargetMeta();
        expeditionCleaningUp = false;
    }

    private void CancelExpeditionBattleStartRoutine()
    {
        if (expeditionBattleStartRoutine == null)
            return;

        StopCoroutine(expeditionBattleStartRoutine);
        expeditionBattleStartRoutine = null;
    }

    private void RestoreBattleOverrides()
    {
        if (!expeditionHasSavedBattleOverrides)
            return;

        XDdeath.autoAddExp = expeditionSavedAutoAddExp;
        expeditionHasSavedBattleOverrides = false;
    }

    private void RemoveExpeditionRuntimeObject(RObj value)
    {
        MainCycle_WhoHeroes.DisposeRuntimeObject(value);
    }

    private bool TryResolveRoute(RObj target, out Transform route)
    {
        route = null;
        if (expeditionPathsRoot == null || target?.main == null)
            return false;

        var bestDistance = float.MaxValue;
        foreach (Transform group in expeditionPathsRoot)
        foreach (Transform candidate in group)
        {
            if (candidate.childCount == 0)
                continue;
            var distance = Vector2.Distance(
                candidate.GetChild(candidate.childCount - 1).position,
                target.main.transform.position);
            if (distance >= bestDistance)
                continue;
            bestDistance = distance;
            route = candidate;
        }

        return route != null && bestDistance <= expeditionMaxRouteEndDistance;
    }

    private void ResolveExpeditionSceneReferences()
    {
        if (expeditionPathsRoot == null || expeditionDepartureSpawner == null || expeditionBattleSpawner == null || expeditionBattleController == null)
            Debug.LogError("WhoHeroes expedition: Inspector references are incomplete.", this);
    }

    private void SetExpeditionPhase(WhoHeroesExpeditionPhase value)
    {
        expeditionPhase = value;
        if (expedition == null)
            return;
        expedition.SetPar(PhaseParam, (int)value);
        expedition.SetPar("busy", value == WhoHeroesExpeditionPhase.Idle ? 0f : 1f);
    }

    private void SetTargetMeta(string targetId)
    {
        ClearTargetMeta();
        expedition.META_TAGS.Add(TargetMetaPrefix + targetId);
    }

    private void ClearTargetMeta()
    {
        if (expedition == null)
            return;
        expedition.META_TAGS.RemoveAll(value => value.StartsWith(TargetMetaPrefix, StringComparison.Ordinal));
    }

    private void AbortToIdle()
    {
        DestroyTravelMarkers();
        currentTarget = null;
        ClearTargetMeta();
        SetExpeditionPhase(WhoHeroesExpeditionPhase.Idle);
        PublishRefresh(expedition, "expedition_aborted");
    }

    private GameObject SpawnTravelMarkers()
    {
        DestroyTravelMarkers();
        if (expeditionDepartureSpawner == null)
            return null;

        var selected = expedition.inventory.Where(IsLivingStack)
            .Take(MainCycle_WhoHeroes.ExpeditionMaxStacks).ToList();
        for (var index = 0; index < selected.Count; index++)
        {
            var stack = selected[index];
            GameObject created = null;
            if (ResourceHolder.instance != null && stack.dbObj != null &&
                ResourceHolder.instance.monsters.TryGetValue(stack.dbObj.ID, out var prefab) && prefab != null)
                created = Instantiate(prefab, MainStates.instance.root);
            created ??= expeditionDepartureSpawner.SpawnOne();
            if (created == null)
                continue;

            created.name = $"WhoHeroes Expedition {stack.dbObj.ID}";
            created.transform.position = expeditionDepartureSpawner.transform.position + new Vector3(0f, index * 0.18f, 0f);
            travelMarkers.Add(created);
        }
        return travelMarkers.FirstOrDefault();
    }

    private void DestroyTravelMarkers()
    {
        foreach (var value in travelMarkers)
            if (value != null)
                Destroy(value);
        travelMarkers.Clear();
    }

    private static void Transfer(RObj unit, RObj destination)
    {
        if (destination != null && destination.RID == PlayerId)
        {
            MainCycle_WhoHeroes.AddOrMergeCityStack(unit);
            return;
        }
        MainStates.instance.AddItem(destination, unit);
        if (unit.GetPar("amount") <= 0f)
            MainCycle_WhoHeroes.DisposeRuntimeObject(unit);
    }

    private static void ApplyLevel(RObj unit, int level)
    {
        unit.SetPar("level", Mathf.Max(0, level - 1));
    }

    private static bool IsLivingStack(RObj value)
    {
        return value != null && value.it == ItemType.monster && value.GetPar("amount") > 0f;
    }

    private float ResolveRouteSpeed()
    {
        var minimusSpeed = ConfigLoader.GetMetaParamValue("global_move");
        if (minimusSpeed <= 0f)
            Debug.LogError("WhoHeroes expedition: Minimus METACONF 'global_move' must be greater than zero.", this);
        return Mathf.Max(0f, minimusSpeed);
    }

    private static Vector3 ToVector((float, float, float) value)
    {
        return new Vector3(value.Item1, value.Item2, value.Item3);
    }

    private static void PublishRefresh(RObj value, string action)
    {
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = value, what = action });
    }

}
