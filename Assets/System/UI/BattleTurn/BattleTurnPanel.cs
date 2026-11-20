using System.Collections.Generic;
using UnityEngine;

public sealed class BattleTurnPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private ObjHolder holder;
    [SerializeField] private Transform stageRoot;
    private UIfiller[] fillers;
    private CanvasGroup input;
    private IReadOnlyList<GameObject> stageParty;
    private RObj current;
    public RObj CurrentAlly => current;

    private void Awake()
    {
        if (panel == null) return;
        fillers = panel.GetComponentsInChildren<UIfiller>(true);
    }
    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    private bool IsLivingStageAlly(RObj unit) =>
        unit?.main != null && unit.main.activeInHierarchy && unit.tags.Contains("player") &&
        unit.META_TAGS.Contains("sword") && unit.GetPar("health") > 0 &&
        (stageRoot == null || unit.main.transform.IsChildOf(stageRoot));

    private static bool IsActing(RObj unit) => unit?.main != null &&
        unit.main.TryGetComponent<UnitActionTracker>(out var tracker) && tracker.Busy;

    private RObj FindCurrentAlly(out bool acceptsInput)
    {
        acceptsInput = false;
        var state = MainStates.instance;
        if (state == null || !state.inBattle || !state.InIteration || !ConfigLoader.parseEnded) return null;
        // Spending the action removes the input turn before the lunge, animation and effects finish.
        if (IsLivingStageAlly(current) && IsActing(current)) return current;

        foreach (var entry in state.allMetas)
        {
            var unit = entry.Value;
            if (!IsLivingStageAlly(unit) || unit.GetPar("can_cast") <= 0 ||
                !state.awaitUnits.TryGetValue(unit.RID, out var waiting) || waiting <= 0) continue;
            acceptsInput = !IsActing(unit);
            return unit;
        }
        return null;
    }

    public void SetStageParty(IReadOnlyList<GameObject> party) => stageParty = party;

    private RObj FindFirstLivingAlly()
    {
        if (stageParty == null || !ConfigLoader.parseEnded) return null;
        for (int i = 0; i < stageParty.Count; i++)
        {
            var member = stageParty[i];
            if (member != null && member.TryGetComponent<ObjHolder>(out var unit) && IsLivingStageAlly(unit.obj))
                return unit.obj;
        }
        return null;
    }

    private void Refresh()
    {
        if (panel == null || holder == null) return;
        if (input == null && !panel.TryGetComponent(out input)) input = panel.AddComponent<CanvasGroup>();
        var ally = FindCurrentAlly(out bool acceptsInput);
        current = ally;
        // Keep the last ally's details during handoff or an enemy turn, in read-only mode.
        var displayed = ally;
        if (displayed == null && MainStates.instance?.inBattle == true && IsLivingStageAlly(holder.obj))
            displayed = holder.obj;
        displayed ??= FindFirstLivingAlly();
        if (input != null && input.interactable != acceptsInput) input.interactable = acceptsInput;
        if (ReferenceEquals(holder.obj, displayed) && panel.activeSelf == (displayed != null)) return;
        panel.SetActive(false);
        holder.obj = displayed;
        holder.noTrack = true;
        if (displayed == null) return;
        if (fillers == null) fillers = panel.GetComponentsInChildren<UIfiller>(true);
        foreach (var filler in fillers)
            if (filler.command == "GET_SKILLS" || filler.command == "GET_SKILLS_SELECT")
                filler.param = displayed.RID;
        panel.SetActive(true);
    }

    private void OnDisable()
    {
        current = null;
        if (holder != null) holder.obj = null;
        if (panel != null) panel.SetActive(false);
    }
}
