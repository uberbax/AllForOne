using System.Collections.Generic;
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
    private Slider productionProgress;
    private TextMeshProUGUI productionProgressText;
    private int displayedProgressPercent = -1;

    private void Awake()
    {
        productionProgress = GetComponentInChildren<Slider>(true);
        productionProgressText = productionProgress == null
            ? null
            : productionProgress.GetComponentInChildren<TextMeshProUGUI>(true);
    }

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

    private void Update()
    {
        RefreshProductionProgress();
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
        var id = GUILIB.Id(runtime);
        general?.Fill(id, level, GUILIB.Icon(string.IsNullOrEmpty(resource) ? id : resource), "factory");
        var maxLevel = runtime.GetPar("max_level");
        uprgade?.Fill(OrderFactoryPrice(GUILIB.Price(runtime)), maxLevel > 0 && level >= maxLevel, false, true,
            level == 0 ? "restore" : "upgrade");
        grades?.Fill(level, 1f, 1f);
        if (speed != null) speed.text = GUILIB.Instance.FillNum(runtime.GetPar("timer"), "time", "", "s");
        if (storage != null)
        {
            var item = GUILIB.PlayerInventory().FirstOrDefault(x => GUILIB.IsId(x, resource));
            storage.text = GUILIB.Instance.FillNum(item?.GetPar("amount") ?? 0, "int");
        }
        RefreshProductionProgress();
    }

    private void RefreshProductionProgress()
    {
        if (productionProgress == null || runtime == null)
            return;
        var progress = MainCycle_WhoHeroes.Instance == null
            ? 0f
            : MainCycle_WhoHeroes.Instance.GetMineProductionProgress01(runtime);
        productionProgress.SetValueWithoutNotify(progress);
        var progressPercent = Mathf.RoundToInt(progress * 100f);
        if (productionProgressText != null && progressPercent != displayedProgressPercent)
            productionProgressText.text = progressPercent + "%";
        displayedProgressPercent = progressPercent;
    }

    private static List<Bon> OrderFactoryPrice(List<Bon> price)
    {
        var values = price?.ToDictionary(value => value.Key, value => value.Value) ??
                     new Dictionary<string, int>();
        return new[]
        {
            MainCycle_WhoHeroes.GoldResourceId,
            MainCycle_WhoHeroes.WoodResourceId,
            MainCycle_WhoHeroes.StoneResourceId,
            "gem",
            "ore"
        }.Select(resourceId => new Bon
        {
            Key = resourceId,
            Value = values.TryGetValue(resourceId, out var value) ? value : 0
        }).ToList();
    }
}
