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
        uprgade?.gameObject.SetActive(false);
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
        if (runtime == null) return;
        var level = GUILIB.Level(runtime, building.level);
        general?.Fill(runtime, "bust");
        grades?.Fill(level, MainCycle_WhoHeroes.BoostPercent() / 100f, 1f, "persent");
        MainCycle_WhoHeroes.TryGetBoostStat(GUILIB.Id(runtime, building.id), out var stat);
        if (bust != null) GUILIB.Instance.Translate(bust, stat);
        if (statIcon != null)
        {
            var icon = GUILIB.Icon(stat);
            if (icon != null) statIcon.sprite = icon;
        }
    }
}
