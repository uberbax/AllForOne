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
        infobut?.onClick.AddListener(() => GUILIB.Emit(WhoHeroesEvents.ObserveBuilding, runtime, building.id));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => { if (gameObject.activeInHierarchy) Fill(); });
    }

    public void Fill()
    {
        if (blocked)
            return;
        runtime = GUILIB.Resolve(building, gameObject);
        if (runtime == null)
            return;
        var offeredUnit = runtime.inventory.FirstOrDefault(x => x.it == ItemType.monster);
        unit?.Fill(offeredUnit);
        general?.Fill(runtime);
    }
}
