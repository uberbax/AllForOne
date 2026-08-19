using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUIArmyWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public Button back;
    public Button infoBut;
    public GUIInventoryList choosen;
    public GUIInventoryList all;
    public GUIActivationGroup busy;
    private RObj runtime;

    private void Awake()
    {
        choosen?.inventory?.SetUpInventory();
        all?.inventory?.SetUpInventory();
    }

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        infoBut?.onClick.AddListener(() => GUILIB.Emit(WhoHeroesEvents.ViewBuilding, runtime, building.id));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => { if (gameObject.activeInHierarchy) Fill(); });
    }

    public void Fill(RObj value = null)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        if (runtime == null)
            return;
        general?.Fill(runtime);
        var id = GUILIB.Id(runtime, building.id);
        choosen?.FillChoosen(id);
        all?.FillChoosen("");
        var selected = GUILIB.PlayerInventory().Count(x => x.it == ItemType.monster && x.GetPar("used_slot") >= 20 && x.GetPar("used_slot") <= 23);
        all?.inventory?.UpdateActionState(selected < Mathf.Max(1, Mathf.RoundToInt(runtime.GetPar("max_stack"))));
        busy?.Activate(runtime.GetPar("busy") <= 0);
    }
}
