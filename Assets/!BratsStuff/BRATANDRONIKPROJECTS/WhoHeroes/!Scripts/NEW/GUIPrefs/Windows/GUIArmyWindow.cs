using System.Linq;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI busyHeader;
    private string busyHeaderDefault;

    private void Awake()
    {
        choosen?.inventory?.SetUpInventory();
        all?.inventory?.SetUpInventory();
        if (busyHeader != null)
            busyHeaderDefault = busyHeader.text;
    }

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        infoBut?.onClick.AddListener(() => GUILIB.Emit(WhoHeroesEvents.ViewBuilding, runtime, building.id));
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
        general?.Fill(runtime);
        var id = GUILIB.Id(runtime, building.id);
        choosen?.FillChoosen(id);
        all?.FillChoosen("");
        if (id == "expedition")
        {
            var controller = MainCycle_WhoHeroes.Instance;
            if (busyHeader != null)
                busyHeader.text = controller != null &&
                    controller.ExpeditionPhase == WhoHeroesExpeditionPhase.ReturnPending
                    ? MainCycle_WhoHeroes.Text("return_next_morning")
                    : busyHeaderDefault;
            all?.inventory?.UpdateActionState(controller != null && !controller.ExpeditionBusy &&
                controller.SelectedUnits.Count < MainCycle_WhoHeroes.ExpeditionMaxStacks &&
                MainCycle_WhoHeroes.Instance?.Phase == WhoHeroesPhase.Day);
            busy?.Activate(controller == null || !controller.ExpeditionBusy);
            return;
        }

        var selected = GUILIB.PlayerInventory().Count(x => x.it == ItemType.monster &&
            x.GetPar("used_slot") >= 20 && x.GetPar("used_slot") <= 23);
        all?.inventory?.UpdateActionState(selected < MainCycle_WhoHeroes.DefenseSlotCount);
        busy?.Activate(runtime.GetPar("busy") <= 0);
    }
}
