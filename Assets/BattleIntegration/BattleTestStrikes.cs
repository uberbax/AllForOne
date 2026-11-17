using BRATANDRONIKLIB;
using UnityEngine;

// Routes the cinematic test buttons to the live turn, rather than the demo actors.
public sealed class BattleTestStrikes : MonoBehaviour
{
    [SerializeField] private BattleTurnPanel turns;
    [SerializeField] private BRATEffectBattle effects;
    [SerializeField] private Transform stageRoot;

    public void PlayStrike() => Play(false);
    public void PlayHeavyStrike() => Play(true);

    private bool IsTarget(RObj unit)
    {
        return unit != null && unit.main != null && unit.main.activeInHierarchy &&
            unit.visMain != null && unit.visMain.activeInHierarchy && unit.tags.Contains("enemy") &&
            unit.GetPar("health") > 0 && (stageRoot == null || unit.main.transform.IsChildOf(stageRoot));
    }

    private void Play(bool heavy)
    {
        var state = MainStates.instance;
        var actor = turns != null ? turns.CurrentAlly : null;
        if (state == null || !state.inBattle || effects == null || actor == null ||
            actor.main == null || !actor.main.activeInHierarchy || actor.visMain == null ||
            actor.GetPar("health") <= 0 || actor.GetPar("can_cast") <= 0) return;

        var target = state.lastTargetSelected;
        if (!IsTarget(target))
        {
            target = null;
            foreach (var entry in state.allMetas)
                if (IsTarget(entry.Value)) { target = entry.Value; break; }
        }
        if (target == null) return;
        state.lastTargetSelected = target;
        if (heavy) effects.PlayHeavyStrike(actor.visMain.transform, target.visMain.transform);
        else effects.PlayStrike(actor.visMain.transform, target.visMain.transform);
    }
}
