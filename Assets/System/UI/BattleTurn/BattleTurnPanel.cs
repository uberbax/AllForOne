using UnityEngine;

public sealed class BattleTurnPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private ObjHolder holder;
    [SerializeField] private Transform stageRoot;
    private UIfiller[] fillers;
    private RObj current;
    public RObj CurrentAlly => current;

    private void Awake()
    {
        if (panel != null) fillers = panel.GetComponentsInChildren<UIfiller>(true);
    }
    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    private RObj FindCurrentAlly()
    {
        var state = MainStates.instance;
        if (state == null || !state.inBattle || !state.InIteration || !ConfigLoader.parseEnded) return null;
        foreach (var entry in state.allMetas)
        {
            var unit = entry.Value;
            if (unit == null || unit.main == null || !unit.main.activeInHierarchy ||
                !unit.tags.Contains("player") || !unit.META_TAGS.Contains("sword") ||
                unit.GetPar("can_cast") <= 0 || unit.GetPar("health") <= 0) continue;
            if (stageRoot != null && !unit.main.transform.IsChildOf(stageRoot)) continue;
            if (state.awaitUnits.TryGetValue(unit.RID, out var waiting) && waiting > 0) return unit;
        }
        return null;
    }

    private void Refresh()
    {
        if (panel == null || holder == null) return;
        var ally = FindCurrentAlly();
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
