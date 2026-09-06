using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class MainCycle_WhoHeroes
{

    private const float NightPrinceVisualScale = 2f;
    private const float NightPrinceMoveSpeed = 3f;
    private const string NightStartedEvent = "new_night";
    private const string NightBattleEndedEvent = "battle_ended";
    private const string NightGameOverEvent = "whoheroes_game_over";
    private const string NightBattleMeta = "whoheroes_night_battle";
    private const string PrinceMeta = "whoheroes_prince";
    private const string DemonStateVisual = "whoheroes_demon_state";
    private const float NightDemonDecisionInterval = 0.1f;
    private const float NightCameraTransitionDuration = 0.75f;
    private const float NightPreClashDelay = 0.5f;
    private const float NightCarrierSpeedMultiplier = 5f;
    private const int NightDemonFormationColumns = 8;
    private const float NightDemonFormationSpacing = 0.55f;
    public const int FirstDefenseSlot = 20;
    public const int LastDefenseSlot = 23;
    public const int DefenseSlotCount = LastDefenseSlot - FirstDefenseSlot + 1;

    private static readonly List<(string, string)> NightBattleVisualOverrides =
        new List<(string, string)> { ("drop", "x") };

    private readonly List<RObj> nightUnits = new List<RObj>();
    private readonly List<RObj> demons = new List<RObj>();
    private readonly List<RObj> nightSuspendedCombats = new List<RObj>();
    private readonly Dictionary<RObj, List<RObj>> defenseUnits = new Dictionary<RObj, List<RObj>>();
    private readonly Dictionary<RObj, int> defenseSourceSlots = new Dictionary<RObj, int>();
    private readonly Dictionary<RObj, int> currentFormationRows = new Dictionary<RObj, int>();
    private readonly List<RObj> orderedDefenseStacks = new List<RObj>();
    private readonly List<string> addedPrinceVisuals = new List<string>();
    private List<WaveSpawner> savedSpawners = new List<WaveSpawner>();
    private Coroutine spawnRoutine;
    private RObj prince;
    private RObj gateTarget;
    private GameObject gateTargetObject;
    private Vector3 princeDayPosition;
    private float princeSavedNoMove;
    private Vector3 savedCameraPosition;
    private float savedCameraSize;
    private int nightSavedSpawnerCount;
    private string nightSavedBattleTag = string.Empty;
    private string nightSavedWinTag = string.Empty;
    private string nightSavedLoseTag = string.Empty;
    private bool savedSpawnerCommand;
    private bool savedSpawnerDone;
    private bool nightSavedAutoAddExp;
    private bool nightSavedOneCast;
    private bool nightHasSavedOneCast;
    private bool savedCamera;
    private bool savedViewMapCameraEnabled;
    private bool savedViewMapCameraInputBlocked;
    private bool addedPrinceMainVisual;
    private bool nightActive;
    private bool nightCleaningUp;
    private bool nightCombatStarted;
    private BRATMotionMouseMove nightPrinceMouseMove;
    private BRATViewCameraFollow nightCameraFollow;

    public bool NightActive => nightActive;
    public int LivingDemons => demons.Count(IsNightUnitAlive);

    private void InitializeNightOrchestration()
    {
        ResolveNightSceneReferences();
        EventManager.SUB(NightStartedEvent, OnNewNight);
        EventManager.SUB(NightBattleEndedEvent, OnNightBattleEnded);
        EventManager.SUB(NightGameOverEvent, OnNightGameOver);
    }

    private void UpdateNightOrchestration()
    {
        if (!nightActive)
            return;

        if (nightViewMapCameraController != null && !nightViewMapCameraController.InputBlocked)
            nightViewMapCameraController.SetInputBlocked(true);
        SyncRuntimePositions();
        if (nightCombatStarted)
            UpdateFormationRows();

        if (prince != null && prince.GetPar("health") <= 0f && nightBattleController != null)
            nightBattleController.startDo = false;
    }

    private void DisposeNightOrchestration()
    {
        EventManager.UNSUB(NightStartedEvent, OnNewNight);
        EventManager.UNSUB(NightBattleEndedEvent, OnNightBattleEnded);
        EventManager.UNSUB(NightGameOverEvent, OnNightGameOver);

        if (nightActive)
        {
            if (MainStates.instance != null && ConfigLoader.Instance != null)
                CleanupNight(false);
            else
            {
                nightActive = false;
                BattleController.reqTag = nightSavedBattleTag;
                XDdeath.autoAddExp = nightSavedAutoAddExp;
            }
        }
    }

    public bool TryGetNightSavedStackAmount(RObj owner, RObj stack, out int amount)
    {
        amount = 0;
        if (!nightActive || owner != prince || stack == null || !defenseUnits.TryGetValue(stack, out var units))
            return false;
        amount = units.Count(IsNightUnitAlive);
        return true;
    }

    public bool CanBeginNight()
    {
        ResolveNightSceneReferences();
        if (!isActiveAndEnabled || !ValidateSceneReferences() || TimeManager.instance == null ||
            MainStates.instance == null || DatabaseAll.instance == null || ResourceHolder.instance == null ||
            !ResolvePrince())
            return false;

        var hasGateConfig = DatabaseAll.instance.heroes.ContainsKey("empty") ||
                            DatabaseAll.instance.items.ContainsKey("empty") ||
                            DatabaseAll.instance.buildings.ContainsKey("empty") ||
                            DatabaseAll.instance.skills.ContainsKey("empty");
        if (!hasGateConfig)
        {
            Debug.LogError("WhoHeroes night: runtime config 'empty' for the castle gate target is missing.", this);
            return false;
        }

        if (!prince.HasVis("vis_main") && (prince.dbObj == null ||
            !ResourceHolder.instance.monsters.TryGetValue(prince.dbObj.ID, out var prefab) || prefab == null))
        {
            Debug.LogError($"WhoHeroes night: visual for Prince '{prince.dbObj?.ID}' is missing.", this);
            return false;
        }
        return true;
    }

    private void OnNewNight(ArgPass _)
    {
        if (nightActive || MainCycle_WhoHeroes.Instance == null ||
            MainCycle_WhoHeroes.Instance.Phase != WhoHeroesPhase.Night)
            return;

        StartCoroutine(BeginNightBattle());
    }

    private IEnumerator BeginNightBattle()
    {
        while (awaitingExpeditionBattleEndBeforeNight)
            yield return null;

        if (MainCycle_WhoHeroes.Instance == null || MainCycle_WhoHeroes.Instance.Phase != WhoHeroesPhase.Night)
            yield break;

        if (!CanBeginNight())
            yield break;

        var setupComplete = false;
        try
        {
            nightActive = true;
            nightCombatStarted = false;
            IsolateBattleState();
            CreateGateTarget();
            ConfigurePrince();
            SpawnDefense();
            SpawnAllNightDemons();
            yield return FocusNightCamera();
            yield return MoveNightForcesToStaging();
            yield return new WaitForSeconds(NightPreClashDelay);

            ConfigureCombatTargets();
            ActivateNightCombat();
            TimeManager.instance.ResetTime();
            TimeManager.instance.spd = 1f;
            nightBattleSpawner.spawnByCommand = true;
            nightBattleSpawner.IsDone = true;
            nightBattleController.startDo = true;
            nightCombatStarted = true;
            setupComplete = true;
        }
        finally
        {
            if (!setupComplete && nightActive && MainStates.instance != null && ConfigLoader.Instance != null)
                CleanupNight(false);
        }
    }

    private IEnumerator SpawnNightWaves()
    {
        var schedule = BuildSpawnSchedule();
        foreach (var request in schedule)
        {
            while (nightActive && TimeManager.instance != null && TimeManager.instance.tm < request.time)
                yield return null;
            if (!nightActive)
                yield break;

            SpawnDemonBatch(request.portal, request.batch);
        }

        nightBattleSpawner.IsDone = true;
        spawnRoutine = null;
    }

    private List<(float time, RObj portal, Bon batch)> BuildSpawnSchedule()
    {
        var result = new List<(float time, RObj portal, Bon batch)>();
        var snapshot = MainCycle_WhoHeroes.Instance.NightBattleSnapshot;
        foreach (var pair in snapshot)
        {
            var battle = pair.Value;
            var count = Mathf.Min(battle.enemies.heroLevelPosition.Count, battle.enemies.amounts.Count);
            for (var index = 0; index < count; index++)
            {
                var enemyId = battle.enemies.heroLevelPosition[index].Item1;
                var amount = Mathf.Max(0, battle.enemies.amounts[index]);
                if (amount == 0 || !DatabaseAll.instance.heroes.ContainsKey(enemyId))
                    continue;

                var time = index < battle.enemies.timeSpawns.Count
                    ? Mathf.Max(0f, battle.enemies.timeSpawns[index])
                    : 0f;
                result.Add((time, pair.Key, new Bon
                {
                    Key = enemyId,
                    Value = amount,
                    Val3 = Mathf.Max(1, MainCycle_WhoHeroes.Instance.NightNumber)
                }));
            }
        }

        return result.OrderBy(value => value.time).ToList();
    }

    private void SpawnDemonBatch(RObj portal, Bon batch)
    {
        var center = portal?.main == null ? nightCastleGate.position + Vector3.up * 6f : portal.main.transform.position;
        var min = center - new Vector3(nightDemonSpawnRadius, nightDemonSpawnRadius, 0f);
        var max = center + new Vector3(nightDemonSpawnRadius, nightDemonSpawnRadius, 0f);
        var spawned = nightBattleSpawner.DoSpawnAny(
            new List<Bon> { batch }, "enemy", null, null, true, min, max,
            overridesViz: NightBattleVisualOverrides);

        foreach (var unit in spawned)
        {
            unit.AddMeta(NightBattleMeta);
            unit.SetPar("no_move", 1f);
            if (unit.visuals.TryGetValue("combat", out var combatVisual) && combatVisual != null)
                combatVisual.GetComponent<XDcombat>().curTg = "chill";
            demons.Add(unit);
            nightUnits.Add(unit);
        }
    }

    private void SpawnDefense()
    {
        defenseUnits.Clear();
        defenseSourceSlots.Clear();
        currentFormationRows.Clear();
        orderedDefenseStacks.Clear();
        var stacks = prince.inventory
            .Where(value => value != null && value.it == ItemType.monster && value.GetPar("amount") > 0f)
            .Select(value => (stack: value, slot: Mathf.RoundToInt(value.GetPar("used_slot"))))
            .Where(value => value.slot >= FirstDefenseSlot && value.slot <= LastDefenseSlot)
            .OrderBy(value => value.slot)
            .ToList();

        foreach (var entry in stacks)
        {
            var level = Mathf.Max(1, Mathf.RoundToInt(entry.stack.GetPar("level")));
            var amount = Mathf.Max(1, Mathf.RoundToInt(entry.stack.GetPar("amount")));
            var rowIndex = entry.slot - FirstDefenseSlot;
            var spawnPoint = ResolveNightArmySpawn();
            if (spawnPoint == null)
                throw new InvalidOperationException("WhoHeroes night: Tower road start is missing.");
            var spawned = nightBattleSpawner.DoSpawnAny(
                new List<Bon> { new Bon { Key = entry.stack.dbObj.ID, Value = amount, Val3 = level } },
                "player", null, null, true, spawnPoint.position, spawnPoint.position,
                overridesViz: NightBattleVisualOverrides);

            defenseUnits[entry.stack] = spawned;
            defenseSourceSlots[entry.stack] = entry.slot;
            currentFormationRows[entry.stack] = rowIndex;
            orderedDefenseStacks.Add(entry.stack);
            foreach (var unit in spawned)
            {
                MainCycle_WhoHeroes.ApplyPermanentPerksToUnit(unit, false);
                unit.AddMeta(NightBattleMeta);
                unit.SetPar("no_move", 1f);
                nightUnits.Add(unit);
            }
            StaggerSpecialSkillCooldowns(spawned);
        }
    }

    private IEnumerator MoveNightForcesToStaging()
    {
        if (UtilsControl.Instance == null)
            yield break;

        var speed = ResolveNightStageSpeed();
        var pendingMoves = 0;
        foreach (var stack in orderedDefenseStacks)
        {
            if (!defenseUnits.TryGetValue(stack, out var units))
                continue;

            var rowIndex = currentFormationRows[stack];
            var center = (units.Count - 1) * 0.5f;
            for (var index = 0; index < units.Count; index++)
            {
                var unit = units[index];
                if (!IsNightUnitAlive(unit))
                    continue;

                var destination = nightDefenseRows[rowIndex].position +
                                  Vector3.right * ((index - center) * nightFormationUnitSpacing);
                var route = BuildNightAllyRoute(unit.main.transform.position, destination);
                pendingMoves++;
                UtilsControl.Instance.MoveToMany(unit.main.transform, speed, route, 0,
                    () => pendingMoves--);
            }
        }

        var demonCenter = nightCameraTarget.position + Vector3.up * 0.75f;
        for (var index = 0; index < demons.Count; index++)
        {
            var demon = demons[index];
            if (!IsNightUnitAlive(demon))
                continue;

            var row = index / NightDemonFormationColumns;
            var column = index % NightDemonFormationColumns;
            var rowCount = Mathf.Min(
                NightDemonFormationColumns, demons.Count - row * NightDemonFormationColumns);
            var destination = demonCenter +
                              Vector3.right * ((column - (rowCount - 1) * 0.5f) * NightDemonFormationSpacing) +
                              Vector3.up * (row * NightDemonFormationSpacing);
            var route = new List<(float, float, float)>
            {
                (destination.x, destination.y, demon.main.transform.position.z)
            };
            pendingMoves++;
            UtilsControl.Instance.MoveToMany(demon.main.transform, speed, route, 0,
                () => pendingMoves--);
        }

        while (nightActive && pendingMoves > 0)
            yield return null;
    }

    private List<(float, float, float)> BuildNightAllyRoute(Vector3 start, Vector3 destination)
    {
        var result = new List<(float, float, float)>();
        AppendNightRoute(result, FindNightRoute("Tower"), start, start.z);
        var connectorStart = result.Count == 0
            ? start
            : new Vector3(result[result.Count - 1].Item1, result[result.Count - 1].Item2, start.z);
        AppendNightRoute(result, FindNightRoute("Fight"), connectorStart, start.z);
        result.Add((destination.x, destination.y, start.z));
        return result;
    }

    private static void AppendNightRoute(
        List<(float, float, float)> destination, Transform route, Vector3 start, float movementZ)
    {
        if (route != null)
            destination.AddRange(BuildDeliveryRoute(route, start, movementZ));
    }

    private Transform FindNightRoute(string routeName)
    {
        if (expeditionPathsRoot == null)
            return null;

        foreach (Transform island in expeditionPathsRoot)
        foreach (Transform route in island)
            if (string.Equals(route.name, routeName, StringComparison.OrdinalIgnoreCase))
                return route;
        return null;
    }

    private Transform ResolveNightArmySpawn()
    {
        var route = FindNightRoute("Tower");
        return route == null || route.childCount == 0 ? null : route.GetChild(0);
    }

    private float ResolveNightStageSpeed()
    {
        var carrierSpeed = ConfigLoader.GetMetaParamValue("global_move") /
                           Mathf.Max(1f, carrierSpeedDivisor);
        return Mathf.Max(0.1f, carrierSpeed * NightCarrierSpeedMultiplier);
    }

    private void ActivateNightCombat()
    {
        var combatSpeed = ResolveNightStageSpeed();
        foreach (var unit in nightUnits)
        {
            if (!IsNightUnitAlive(unit))
                continue;
            unit.SetPar("speed", Mathf.Max(unit.GetPar("speed"), combatSpeed));
            unit.SetPar("no_move", 0f);
        }

        foreach (var demon in demons)
        {
            if (!IsNightUnitAlive(demon))
                continue;

            demon.AddViz(DemonStateVisual);
            if (!demon.visuals.TryGetValue(DemonStateVisual, out var stateVisual) || stateVisual == null ||
                !stateVisual.TryGetComponent<WhoHeroesDemonStateMachine>(out var stateMachine) ||
                !stateMachine.Initialize(
                    demon, gateTarget, prince, NightBattleMeta, PrinceMeta,
                    nightDemonAggroRange, nightCastleGateRadius, NightDemonDecisionInterval))
            {
                Debug.LogError($"WhoHeroes night: demon state machine was not initialized for '{demon.RID}'.", this);
                if (demon.visuals.TryGetValue("combat", out var combatVisual) && combatVisual != null)
                    combatVisual.GetComponent<XDcombat>().curTg = MainStates.instance.tgBattle;
            }
        }
    }

    private void UpdateFormationRows()
    {
        var fallenEarlierRows = 0;
        for (var index = 0; index < orderedDefenseStacks.Count; index++)
        {
            var stack = orderedDefenseStacks[index];
            var living = false;
            var units = defenseUnits[stack];
            for (var unitIndex = 0; unitIndex < units.Count; unitIndex++)
                if (IsNightUnitAlive(units[unitIndex]))
                {
                    living = true;
                    break;
                }
            if (!living)
            {
                fallenEarlierRows++;
                continue;
            }
            var sourceSlot = defenseSourceSlots[stack];
            var targetRow = sourceSlot - FirstDefenseSlot - fallenEarlierRows;
            if (currentFormationRows.TryGetValue(stack, out var currentRow) && currentRow == targetRow)
                continue;
            currentFormationRows[stack] = targetRow;
            PositionFormation(stack, targetRow, true);
        }
    }

    private void PositionFormation(RObj sourceStack, int rowIndex, bool animate)
    {
        if (!defenseUnits.TryGetValue(sourceStack, out var units) ||
            rowIndex < 0 || rowIndex >= nightDefenseRows.Count)
            return;

        var livingCount = 0;
        for (var index = 0; index < units.Count; index++)
            if (IsNightUnitAlive(units[index]))
                livingCount++;
        var center = (livingCount - 1) * 0.5f;
        var livingIndex = 0;
        for (var index = 0; index < units.Count; index++)
        {
            var unit = units[index];
            if (!IsNightUnitAlive(unit))
                continue;
            var target = nightDefenseRows[rowIndex].position +
                         Vector3.right * ((livingIndex - center) * nightFormationUnitSpacing);
            if (animate && UtilsControl.Instance != null)
                UtilsControl.Instance.MoveTo(unit.main.transform, nightFormationShiftSpeed, target, null, null);
            else
                unit.main.transform.position = target;
            unit.Position = unit.main.transform.position;
            livingIndex++;
        }
    }

    private bool ResolvePrince()
    {
        if (MainStates.instance == null ||
            !MainStates.instance.all.TryGetValue("main_player", out prince) || prince?.main == null)
        {
            Debug.LogError("WhoHeroes night: main_player is missing.", this);
            return false;
        }
        return true;
    }

    private void ConfigurePrince()
    {
        MainCycle_WhoHeroes.ApplyPermanentPerksToUnit(prince, true);
        princeDayPosition = prince.main.transform.position;
        prince.main.transform.position = nightPrinceSpawn.position;
        prince.Position = nightPrinceSpawn.position;
        princeSavedNoMove = prince.GetPar("no_move");
        prince.SetPar("no_move", 1f);
        prince.AddMeta(NightBattleMeta);
        prince.AddMeta(PrinceMeta);

        EnsurePrinceMainVisual();
        EnsurePrinceVisual("hp#notext:1");
        EnsurePrinceVisual("coll#val:0.5");
        EnsurePrinceVisual("dmg_track");
        EnsurePrinceVisual("flash");
        EnsurePrinceVisual("death");
        EnsurePrinceVisual("combat");
        EnsurePrinceVisual("animator#pr:1");
        nightPrinceMouseMove = prince.main.GetComponent<BRATMotionMouseMove>();
        if (nightPrinceMouseMove == null)
            nightPrinceMouseMove = prince.main.AddComponent<BRATMotionMouseMove>();
        nightPrinceMouseMove.controlledCamera = nightCamera;
        nightPrinceMouseMove.speed = NightPrinceMoveSpeed;
        nightPrinceMouseMove.clickToMove = true;
        nightPrinceMouseMove.MovementStarted += OnPrinceMovementStarted;
        nightPrinceMouseMove.MovementStopped += OnPrinceMovementStopped;
        nightPrinceMouseMove.enabled = true;
    }

    private void EnsurePrinceMainVisual()
    {
        if (prince.visuals.ContainsKey("vis_main"))
            return;
        if (ResourceHolder.instance == null || prince.dbObj == null ||
            !ResourceHolder.instance.monsters.TryGetValue(prince.dbObj.ID, out var prefab) || prefab == null)
            throw new InvalidOperationException($"WhoHeroes night: visual for Prince '{prince.dbObj?.ID}' is missing.");

        var visual = Instantiate(prefab, prince.main.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale *= NightPrinceVisualScale;
        prince.visuals.Add("vis_main", visual);
        addedPrinceMainVisual = true;
    }

    private void EnsurePrinceVisual(string descriptor)
    {
        var separator = descriptor.IndexOf('#');
        var key = separator < 0 ? descriptor : descriptor.Substring(0, separator);
        if (prince.HasVis(key))
            return;
        prince.AddViz(descriptor);
        addedPrinceVisuals.Add(key);
    }

    private void ConfigureCombatTargets()
    {
        foreach (var unit in nightUnits)
            if (unit?.main != null && unit.visuals.TryGetValue("combat", out var visual) && visual != null)
                visual.GetComponent<XDcombat>().curTg = MainStates.instance.tgBattle;

        if (prince.visuals.TryGetValue("combat", out var princeCombat) && princeCombat != null)
            princeCombat.GetComponent<XDcombat>().curTg = MainStates.instance.tgBattle;
        foreach (var demon in demons)
            if (demon?.main != null && demon.visuals.TryGetValue("combat", out var demonCombat) && demonCombat != null)
                demonCombat.GetComponent<XDcombat>().curTg = "chill";
    }

    private void OnNightBattleEnded(ArgPass args)
    {
        if (!nightActive || !nightCombatStarted || nightCleaningUp || MainCycle_WhoHeroes.Instance == null ||
            MainCycle_WhoHeroes.Instance.Phase != WhoHeroesPhase.Night)
            return;

        if (prince == null || prince.GetPar("health") <= 0f)
            return;

        var victory = args != null && args.num == 0;
        if (!victory)
            return;

        var reward = ResolveNightGoldReward();
        ApplyDefenseSurvivors();
        CleanupNight(true);
        MainCycle_WhoHeroes.Instance.CompleteNight(reward);
    }

    private void OnNightGameOver(ArgPass _)
    {
        if (nightActive)
        {
            ApplyDefenseSurvivors();
            CleanupNight(false);
        }
    }

    private int ResolveNightGoldReward()
    {
        var total = 0;
        foreach (var battle in MainCycle_WhoHeroes.Instance.NightBattleSnapshot.Values)
            foreach (var reward in battle.firstReward)
                if (string.Equals(reward.Key, MainCycle_WhoHeroes.GoldResourceId, StringComparison.OrdinalIgnoreCase))
                    total += Mathf.Max(0, reward.Value);
        return total;
    }

    private void ApplyDefenseSurvivors()
    {
        foreach (var pair in defenseUnits)
            pair.Key.SetPar("amount", pair.Value.Count(IsNightUnitAlive));
        var emptyStacks = prince.inventory
            .Where(value => value != null && value.it == ItemType.monster && value.GetPar("amount") <= 0f)
            .ToList();
        foreach (var stack in emptyStacks)
        {
            prince.inventory.Remove(stack);
            RemoveNightRuntimeObject(stack);
        }
    }

    private void IsolateBattleState()
    {
        nightSavedSpawnerCount = MainStates.instance.curSp;
        savedSpawners = new List<WaveSpawner>(MainStates.instance.spawners);
        nightSavedBattleTag = BattleController.reqTag;
        nightSavedWinTag = nightBattleController.winTag;
        nightSavedLoseTag = nightBattleController.loseTag;
        savedSpawnerCommand = nightBattleSpawner.spawnByCommand;
        savedSpawnerDone = nightBattleSpawner.IsDone;
        nightSavedAutoAddExp = XDdeath.autoAddExp;
        nightSavedOneCast = XDcombat.oneCast;
        nightHasSavedOneCast = true;

        XDdeath.autoAddExp = false;
        XDcombat.oneCast = true;

        nightSuspendedCombats.Clear();
        nightSuspendedCombats.AddRange(MainStates.instance.combats);
        MainStates.instance.combats.Clear();
        foreach (var value in nightSuspendedCombats)
            if (value != null && value.visuals.TryGetValue("combat", out var visual) && visual != null)
                visual.GetComponent<XDcombat>().curTg = "chill";

        MainStates.instance.spawners.Clear();
        MainStates.instance.spawners.Add(nightBattleSpawner);
        MainStates.instance.curSp = 1;
        BattleController.reqTag = NightBattleMeta;
        nightBattleController.winTag = "wave";
        nightBattleController.loseTag = PrinceMeta;
        MainStates.instance.lastBattle = "whoheroes_night_" + MainCycle_WhoHeroes.Instance.NightNumber;
    }

    private void CleanupNight(bool restoreDayPosition)
    {
        if (nightCleaningUp)
            return;
        nightCleaningUp = true;
        nightActive = false;
        nightCombatStarted = false;
        CleanupNightControls();
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
        spawnRoutine = null;
        if (nightBattleController != null)
            nightBattleController.startDo = false;

        foreach (var unit in nightUnits.ToList())
            RemoveNightRuntimeObject(unit);
        nightUnits.Clear();
        demons.Clear();
        defenseUnits.Clear();
        defenseSourceSlots.Clear();
        currentFormationRows.Clear();
        orderedDefenseStacks.Clear();

        if (prince != null)
        {
            prince.SetPar("no_move", princeSavedNoMove);
            prince.META_TAGS.Remove(NightBattleMeta);
            prince.META_TAGS.Remove(PrinceMeta);
            if (restoreDayPosition && prince.main != null)
            {
                prince.main.transform.position = princeDayPosition;
                prince.Position = princeDayPosition;
            }
            foreach (var key in addedPrinceVisuals)
                prince.RemoveViz(key);
            if (addedPrinceMainVisual && prince.visuals.TryGetValue("vis_main", out var mainVisual))
            {
                prince.visuals.Remove("vis_main");
                if (mainVisual != null)
                    Destroy(mainVisual);
            }
        }
        addedPrinceVisuals.Clear();
        addedPrinceMainVisual = false;

        DestroyGateTarget();
        MainStates.instance.combats.Clear();
        foreach (var value in nightSuspendedCombats)
            if (value != null && !MainStates.instance.combats.Contains(value))
                MainStates.instance.combats.Add(value);
        nightSuspendedCombats.Clear();

        MainStates.instance.spawners.Clear();
        MainStates.instance.spawners.AddRange(savedSpawners);
        MainStates.instance.curSp = nightSavedSpawnerCount;
        BattleController.reqTag = nightSavedBattleTag;
        nightBattleController.winTag = nightSavedWinTag;
        nightBattleController.loseTag = nightSavedLoseTag;
        nightBattleSpawner.spawnByCommand = savedSpawnerCommand;
        nightBattleSpawner.IsDone = savedSpawnerDone;
        XDdeath.autoAddExp = nightSavedAutoAddExp;
        if (nightHasSavedOneCast)
        {
            XDcombat.oneCast = nightSavedOneCast;
            nightHasSavedOneCast = false;
        }
        RestoreCamera();
        nightCleaningUp = false;
    }

    private void CreateGateTarget()
    {
        gateTargetObject = new GameObject("WhoHeroes_NightGateTarget");
        gateTargetObject.transform.SetParent(transform, false);
        gateTargetObject.transform.position = nightCastleGate.position;
        gateTarget = DatabaseAll.instance.CreateAny(
            "empty", false, 1, gateTargetObject, "whoheroes_night_gate_target",
            gateTargetObject, false, false);
        gateTarget.Position = nightCastleGate.position;
    }

    private void DestroyGateTarget()
    {
        if (gateTarget != null && MainStates.instance != null)
            MainStates.instance.all.Remove(gateTarget.RID);
        gateTarget = null;
        if (gateTargetObject != null)
            Destroy(gateTargetObject);
        gateTargetObject = null;
    }

    private IEnumerator FocusNightCamera()
    {
        if (nightCamera == null)
            yield break;
        savedCamera = true;
        savedCameraPosition = nightCamera.transform.position;
        savedCameraSize = nightCamera.orthographicSize;
        if (nightViewMapCameraController != null)
        {
            savedViewMapCameraEnabled = nightViewMapCameraController.enabled;
            savedViewMapCameraInputBlocked = nightViewMapCameraController.InputBlocked;
            nightViewMapCameraController.SetInputBlocked(true);
        }

        var targetPosition = nightCameraTarget.position;
        targetPosition.z = savedCameraPosition.z;
        var elapsed = 0f;
        while (elapsed < NightCameraTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / NightCameraTransitionDuration);
            var eased = progress * progress * (3f - 2f * progress);
            nightCamera.transform.position = Vector3.Lerp(savedCameraPosition, targetPosition, eased);
            if (nightCamera.orthographic)
                nightCamera.orthographicSize = Mathf.Lerp(savedCameraSize, nightCameraSize, eased);
            yield return null;
        }

        nightCamera.transform.position = targetPosition;
        if (nightCamera.orthographic)
            nightCamera.orthographicSize = nightCameraSize;
    }

    private void RestoreCamera()
    {
        if (!savedCamera || nightCamera == null)
            return;
        nightCamera.transform.position = savedCameraPosition;
        if (nightCamera.orthographic)
            nightCamera.orthographicSize = savedCameraSize;
        if (nightViewMapCameraController != null)
        {
            nightViewMapCameraController.enabled = savedViewMapCameraEnabled;
            nightViewMapCameraController.SetInputBlocked(savedViewMapCameraInputBlocked);
        }
        savedCamera = false;
    }

    private void SyncRuntimePositions()
    {
        if (prince?.main != null)
            prince.Position = prince.main.transform.position;
        foreach (var unit in nightUnits)
            if (unit?.main != null)
                unit.Position = unit.main.transform.position;
    }

    private void OnPrinceMovementStarted(Vector3 direction)
    {
        if (prince != null && Mathf.Abs(direction.x) > Mathf.Epsilon)
            prince.SetScale(direction.x > 0f);
        SetPrinceAnimation("walk");
    }

    private void OnPrinceMovementStopped() => SetPrinceAnimation("idle");

    private void SetPrinceAnimation(string state)
    {
        if (prince != null && prince.visuals.TryGetValue("animator", out var visual) && visual != null)
            visual.GetComponent<XDanimator>()?.SetState(state);
    }

    private void CleanupNightControls()
    {
        if (nightPrinceMouseMove != null)
        {
            nightPrinceMouseMove.MovementStarted -= OnPrinceMovementStarted;
            nightPrinceMouseMove.MovementStopped -= OnPrinceMovementStopped;
            nightPrinceMouseMove.enabled = false;
            Destroy(nightPrinceMouseMove);
            nightPrinceMouseMove = null;
        }
        if (nightCameraFollow != null)
        {
            nightCameraFollow.enabled = false;
            Destroy(nightCameraFollow);
            nightCameraFollow = null;
        }
    }

    private void SpawnAllNightDemons()
    {
        foreach (var request in BuildSpawnSchedule())
            SpawnDemonBatch(request.portal, request.batch);
    }

    private bool ValidateSceneReferences()
    {
        if (nightBattleSpawner != null && nightBattleController != null && nightCamera != null &&
            nightViewMapCameraController != null && nightPrinceSpawn != null && nightCastleGate != null &&
            nightCameraTarget != null && FindNightRoute("Tower") != null && FindNightRoute("Fight") != null &&
            nightDefenseRows.Count == LastDefenseSlot - FirstDefenseSlot + 1 && nightDefenseRows.All(value => value != null))
            return true;

        Debug.LogError("WhoHeroes night: scene references are incomplete.", this);
        return false;
    }

    private void ResolveNightSceneReferences()
    {
        if (nightBattleSpawner == null || nightBattleController == null || nightCamera == null ||
            nightViewMapCameraController == null || nightPrinceSpawn == null || nightCastleGate == null ||
            nightCameraTarget == null)
            Debug.LogError("WhoHeroes night: Inspector references are incomplete.", this);
    }

    private void RemoveNightRuntimeObject(RObj value)
    {
        MainCycle_WhoHeroes.DisposeRuntimeObject(value);
    }

    private static bool IsNightUnitAlive(RObj value)
    {
        return value?.main != null && value.GetPar("health") > 0f;
    }

}
