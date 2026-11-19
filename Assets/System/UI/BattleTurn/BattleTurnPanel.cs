using UnityEngine;

public sealed class BattleTurnPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private ObjHolder holder;
    [SerializeField] private Transform stageRoot;
    private UIfiller[] fillers;
    private CanvasGroup input;
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

    private void Refresh()
    {
        if (panel == null || holder == null) return;
        if (input == null && !panel.TryGetComponent(out input)) input = panel.AddComponent<CanvasGroup>();
        var ally = FindCurrentAlly(out bool acceptsInput);
        if (input != null && input.interactable != acceptsInput) input.interactable = acceptsInput;
        if (ReferenceEquals(current, ally) && panel.activeSelf == (ally != null)) return;
        panel.SetActive(false);
        current = ally;
        holder.obj = ally;
        holder.noTrack = true;
        if (ally == null) return;
        if (fillers == null) fillers = panel.GetComponentsInChildren<UIfiller>(true);
        foreach (var filler in fillers)
            if (filler.command == "GET_SKILLS" || filler.command == "GET_SKILLS_SELECT")
                filler.param = ally.RID;
        panel.SetActive(true);
    }

    private void OnDisable()
    {
        current = null;
        if (holder != null) holder.obj = null;
        if (panel != null) panel.SetActive(false);
    }
}
