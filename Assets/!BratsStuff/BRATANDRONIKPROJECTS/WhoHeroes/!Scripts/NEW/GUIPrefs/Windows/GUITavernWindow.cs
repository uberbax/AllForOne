using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUITavernWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public Button back;
    public GUIValueGrades grade;
    public GUIInventoryList guests;
    public GUIButtUpgrade upgrade;
    public TextMeshProUGUI dopLvl;
    private RObj runtime;

    private void Awake()
    {
        guests?.inventory?.SetUpInventory();
    }

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        upgrade?.upgrade?.buy?.onClick.AddListener(() => GUILIB.CoreAction(runtime, "upgrade"));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => { if (gameObject.activeInHierarchy) Fill(); });
    }

    public void Fill(RObj value = null)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        if (runtime == null) return;
        var level = GUILIB.Level(runtime, building.level);
        general?.Fill(runtime);
        if (dopLvl != null) dopLvl.text = level.ToString();
        grade?.Fill(level, runtime.GetPar("max_stack"), Mathf.Max(1, runtime.GetPar("level_multiplier")));
        var maxLevel = runtime.GetPar("max_level");
        upgrade?.Fill(GUILIB.Price(runtime), maxLevel > 0 && level >= maxLevel, false, true,
            level > 0 ? "upgrade" : "restore");
        guests?.FillChoosen("tavern");
    }
}
