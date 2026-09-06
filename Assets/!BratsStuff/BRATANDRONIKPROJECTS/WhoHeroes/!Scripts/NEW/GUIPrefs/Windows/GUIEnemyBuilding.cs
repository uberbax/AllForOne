using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUIEnemyBuilding : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public Button back;
    public GUIUnitPrefab defender;
    public Button attack;
    public GUIInventoryList expedition;
    private RObj runtime;

    private void Start()
    {
        expedition?.inventory?.SetUpInventory();
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        attack?.onClick.AddListener(() => MainCycle_WhoHeroes.Instance?.TryStart(runtime));
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
        var guard = runtime.inventory.FirstOrDefault(x => x.it == ItemType.monster);
        var displayId = ResolveDisplayId(runtime);
        var headerId = guard == null ? displayId : GUILIB.Id(guard);
        general?.Fill(headerId, GUILIB.Level(runtime), GUILIB.Icon(runtime), ResolveDescriptionId(runtime));
        if (defender != null)
        {
            defender.gameObject.SetActive(guard != null);
            if (guard != null)
                defender.Fill(guard);
        }
        expedition?.FillChoosen("expedition");
        var canAttack = MainCycle_WhoHeroes.Instance != null &&
                        MainCycle_WhoHeroes.Instance.CanStart(runtime);
        if (attack != null)
        {
            attack.interactable = canAttack;
            var image = attack.GetComponent<Image>();
            if (image != null) image.color = GUILIB.ColorFor(canAttack ? "butred" : "butgrey");
        }
    }

    private static string ResolveDisplayId(RObj value)
    {
        return GUILIB.Id(value);
    }

    private static string ResolveDescriptionId(RObj value)
    {
        var id = GUILIB.Id(value);
        if (!string.IsNullOrEmpty(MainCycle_WhoHeroes.MineResourceId(value)))
            return "factory";
        if (id.StartsWith("portal", System.StringComparison.Ordinal))
            return "portal_descr";
        if (MainCycle_WhoHeroes.TryGetBoostStat(id, out _))
            return "bust";
        if (!string.IsNullOrEmpty(GUILIB.StringParam(value, "story")))
            return "dbuildingstory";
        return "generic_building_descr";
    }
}
