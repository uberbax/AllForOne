using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIPortalWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public GUIButtUpgrade uprgade;
    public Button back;
    public GUIInfoItem start;
    public GUIInfoItem end;
    public TextMeshProUGUI ptype;
    public GameObject outclosed;
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
        var enter = runtime.dbObj?.pars.ContainsKey("enter") == true ? runtime.GetPar("enter") > 0 : !building.id.Contains("out");
        var portalId = enter ? "portalin" : "portalout";
        general?.Fill("portal", level, GUILIB.Icon(portalId), "portal");
        outclosed?.SetActive(!enter && level == 0);
        uprgade?.gameObject.SetActive(enter);
        if (enter)
        {
            var maxLevel = runtime.GetPar("max_level");
            uprgade?.Fill(GUILIB.Price(runtime), maxLevel > 0 && level >= maxLevel, false, true,
                level == 0 ? "restore" : "upgrade");
        }
        var from = GUILIB.StringParam(runtime, "from");
        var to = GUILIB.StringParam(runtime, "to");
        start?.Fill(from, 0, GUILIB.Icon(from));
        end?.Fill(to, 0, GUILIB.Icon(to));
        if (ptype != null) GUILIB.Instance.Translate(ptype, portalId);
    }
}
