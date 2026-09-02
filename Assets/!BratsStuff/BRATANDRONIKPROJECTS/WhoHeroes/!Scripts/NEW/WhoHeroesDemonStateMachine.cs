using UnityEngine;

public sealed class WhoHeroesDemonStateMachine : ComponentBehavior
{
    public enum DemonState
    {
        Disabled,
        ApproachGate,
        AttackAggroTarget,
        ChasePrince
    }

    [SerializeField] private DemonState state = DemonState.Disabled;

    private RObj unit;
    private RObj gateTarget;
    private RObj prince;
    private XDcombat combat;
    private string battleMeta;
    private string princeMeta;
    private float aggroRange;
    private float gateRadius;
    private float decisionInterval;
    private float nextDecisionTime;
    private bool passedGate;

    public DemonState State => state;

    public bool Initialize(
        RObj owner,
        RObj gate,
        RObj princeTarget,
        string nightBattleMeta,
        string princeTargetMeta,
        float configuredAggroRange,
        float configuredGateRadius,
        float configuredDecisionInterval)
    {
        unit = owner;
        gateTarget = gate;
        prince = princeTarget;
        battleMeta = nightBattleMeta;
        princeMeta = princeTargetMeta;
        aggroRange = Mathf.Max(0.1f, configuredAggroRange);
        gateRadius = Mathf.Max(0.1f, configuredGateRadius);
        decisionInterval = Mathf.Max(0.02f, configuredDecisionInterval);
        passedGate = false;

        if (unit == null || gateTarget == null || prince == null ||
            !unit.visuals.TryGetValue("combat", out var combatVisual) || combatVisual == null)
        {
            Disable();
            return false;
        }

        combat = combatVisual.GetComponent<XDcombat>();
        if (combat == null)
        {
            Disable();
            return false;
        }

        combat.curTg = "chill";
        state = DemonState.ApproachGate;
        nextDecisionTime = 0f;
        return true;
    }

    public void Disable()
    {
        state = DemonState.Disabled;
        if (combat != null)
            combat.curTg = "chill";
    }

    private void Update()
    {
        if (state == DemonState.Disabled || unit?.main == null || MainStates.instance == null ||
            unit.GetPar("health") <= 0f)
            return;

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + decisionInterval;
            UpdateState();
        }

        ExecuteState();
    }

    private void UpdateState()
    {
        if (!passedGate)
        {
            var gateDistance = MainStates.instance.GetDistance(unit, gateTarget, out var straightGateDistance);
            passedGate = gateDistance <= gateRadius || straightGateDistance <= gateRadius;
        }

        if (passedGate)
        {
            state = DemonState.ChasePrince;
            return;
        }

        var nearest = MainStates.instance.GetClosestEnemy(unit, out var distance, reqTag: battleMeta);
        state = nearest != null && distance <= aggroRange
            ? DemonState.AttackAggroTarget
            : DemonState.ApproachGate;
    }

    private void ExecuteState()
    {
        switch (state)
        {
            case DemonState.ApproachGate:
                MainStates.instance.MovePath(unit, gateTarget);
                break;
            case DemonState.AttackAggroTarget:
                combat.Iteration(true, true, reqTag: battleMeta);
                break;
            case DemonState.ChasePrince:
                combat.Iteration(true, true, reqTag: princeMeta);
                break;
        }
    }
}
