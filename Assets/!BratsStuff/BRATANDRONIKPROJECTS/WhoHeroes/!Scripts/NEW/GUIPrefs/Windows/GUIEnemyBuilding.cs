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
        general?.Fill(runtime, GUILIB.StringParam(runtime, "building_type"));
        defender?.Fill(runtime.inventory.FirstOrDefault(x => x.it == ItemType.monster));
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
}
