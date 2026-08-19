using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIWarBuildWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public GUIButtUpgrade uprgade;
    public GUIValueGrades grades;
    public Button back;
    public TextMeshProUGUI bust;
    public Image statIcon;
    private RObj runtime;

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        uprgade?.upgrade?.buy?.onClick.AddListener(() => GUILIB.CoreAction(runtime, "upgrade"));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => { if (gameObject.activeInHierarchy) Fill(); });
    }

    public void Fill(RObj value = null)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        if (runtime == null) return;
        var level = GUILIB.Level(runtime, building.level);
        general?.Fill(runtime, "bust");
        var maxLevel = runtime.GetPar("max_level");
        uprgade?.Fill(GUILIB.Price(runtime), maxLevel > 0 && level >= maxLevel, false, true,
            level == 0 ? "restore" : "upgrade");
        grades?.Fill(level, runtime.GetPar("basic_bust"), Mathf.Max(1, runtime.GetPar("level_multiplier")), "persent");
        var stat = GUILIB.StringParam(runtime, "bust_stat");
        if (bust != null) GUILIB.Instance.Translate(bust, stat);
        if (statIcon != null)
        {
            var icon = GUILIB.Icon(stat);
            if (icon != null) statIcon.sprite = icon;
        }
    }
}
