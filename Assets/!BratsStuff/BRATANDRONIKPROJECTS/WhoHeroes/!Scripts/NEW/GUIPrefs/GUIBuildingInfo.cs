using UnityEngine;

public class GUIBuildingInfo : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public GUIButtUpgrade uprgade;
    public GUIValueGrades grades;
    private RObj runtime;

    private void Start()
    {
        var button = uprgade?.upgrade?.buy;
        button?.onClick.AddListener(() => GUILIB.CoreAction(runtime, "upgrade"));
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    private void OnRefresh(ArgPass _)
    {
        if (gameObject.activeInHierarchy) Fill();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    public void Fill(RObj value = null)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        if (runtime == null)
            return;
        var level = GUILIB.Level(runtime, building.level);
        general?.Fill(runtime);
        var maxLevel = runtime.GetPar("max_level");
        uprgade?.Fill(GUILIB.Price(runtime), maxLevel > 0 && level >= maxLevel, false, true,
            level > 0 ? "upgrade" : "restore");
        var id = GUILIB.Id(runtime, building.id);
        var basic = id == "castle" ? runtime.GetPar("workers") : runtime.GetPar("max_stack");
        grades?.Fill(level, basic, Mathf.Max(1, runtime.GetPar("level_multiplier")), "int", "", "", id);
    }

    private void OnDisable()
    {
        runtime = null;
    }
}
