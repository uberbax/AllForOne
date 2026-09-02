using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIHireBuildingWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public TextMeshProUGUI addLvl;
    public GUIUnitPrefab defender;
    public GUIStatsGrades grade;
    public GUIButtUpgrade upgrade;
    public Button back;
    private RObj runtime;
    private RObj offered;

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        upgrade?.upgrade?.buy?.onClick.AddListener(() => GUILIB.CoreAction(runtime, "upgrade"));
        defender?.unit?.hire?.upgrade?.buy?.onClick.AddListener(() => GUILIB.CoreAction(offered, "buy"));
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
        offered = runtime.inventory.FirstOrDefault(x => x.it == ItemType.monster && x.GetPar("amount") > 0f);
        defender?.Fill(offered);
        if (offered == null)
            defender?.unit?.hire?.Fill(new List<Bon>(), false, true, false, "hire");
        general?.Fill(runtime, GUILIB.StringParam(runtime, "building_type"));
        if (addLvl != null) addLvl.text = level.ToString();
        var maxLevel = runtime.GetPar("max_level");
        upgrade?.Fill(GUILIB.Price(runtime), maxLevel > 0 && level >= maxLevel, false, true,
            level == 0 ? "restore" : "upgrade");
        if (offered != null) grade?.Fill(level, offered, Mathf.Max(1, offered.GetPar("level_multiplier")));
    }
}
