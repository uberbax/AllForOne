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
        attack?.onClick.AddListener(() => EventManager.INV("battle_press", new ArgPass { who = runtime, what = building.id }));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => { if (gameObject.activeInHierarchy) Fill(); });
    }

    public void Fill(RObj value = null)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        if (runtime == null)
            return;
        general?.Fill(runtime, GUILIB.StringParam(runtime, "building_type"));
        defender?.Fill(runtime.inventory.FirstOrDefault(x => x.it == ItemType.monster));
        expedition?.FillChoosen("expedition");
        var canAttack = !runtime.curPars.ContainsKey("can_attack") || runtime.GetPar("can_attack") > 0;
        if (attack != null)
        {
            attack.interactable = canAttack;
            var image = attack.GetComponent<Image>();
            if (image != null) image.color = GUILIB.ColorFor(canAttack ? "butred" : "butgrey");
        }
    }
}
