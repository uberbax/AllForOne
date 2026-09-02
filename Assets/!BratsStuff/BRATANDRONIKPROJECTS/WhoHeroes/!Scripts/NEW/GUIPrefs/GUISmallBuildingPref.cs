using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUISmallBuildingPref : MonoBehaviour
{
    public bool blocked;
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public GUIUnitShort unit;
    public Button infobut;
    private RObj runtime;

    private void Start()
    {
        if (infobut != null)
            infobut.interactable = !blocked;
        infobut?.onClick.AddListener(() => GUILIB.Emit(WhoHeroesEvents.ObserveBuilding, runtime, building.id));
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

    public void Fill()
    {
        if (blocked)
            return;
        runtime = GUILIB.Resolve(building, gameObject);
        if (runtime == null)
            return;
        var offeredUnit = runtime.inventory.FirstOrDefault(x => x.it == ItemType.monster && x.GetPar("amount") > 0f);
        unit?.Fill(offeredUnit);
        general?.Fill(runtime);
    }
}
