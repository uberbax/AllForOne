using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIFactotyWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public GUIButtUpgrade uprgade;
    public GUIValueGrades grades;
    public Button back;
    public TextMeshProUGUI speed;
    public TextMeshProUGUI storage;
    private RObj runtime;

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        uprgade?.upgrade?.buy?.onClick.AddListener(() => GUILIB.CoreAction(runtime, "upgrade"));
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
        var resource = MainCycle_WhoHeroes.MineResourceId(runtime);
        var id = string.IsNullOrEmpty(resource) ? GUILIB.Id(runtime) : "factory" + resource;
        general?.Fill(id, level, GUILIB.Icon(string.IsNullOrEmpty(resource) ? id : resource), "factory");
        var maxLevel = runtime.GetPar("max_level");
        uprgade?.Fill(GUILIB.Price(runtime), maxLevel > 0 && level >= maxLevel, false, true,
            level == 0 ? "restore" : "upgrade");
        grades?.FillInverse(level, runtime.GetPar("timer") * Mathf.Max(1, level), "time", "", "s");
        if (speed != null) speed.text = GUILIB.Instance.FillNum(runtime.GetPar("timer"), "time", "", "s");
        if (storage != null)
        {
            var item = GUILIB.PlayerInventory().FirstOrDefault(x => GUILIB.IsId(x, resource));
            storage.text = GUILIB.Instance.FillNum(item?.GetPar("amount") ?? 0, "int");
        }
    }
}
