using UnityEngine;
using UnityEngine.UI;

public class GUIMapItemPrefab : MonoBehaviour
{
    public WhoHeroesObjectRef build = new WhoHeroesObjectRef();
    public Button info;
    public GameObject occupied;
    public GameObject notoccupied;
    private RObj runtime;

    private void Start()
    {
        info?.onClick.AddListener(() => GUILIB.Emit(WhoHeroesEvents.ViewBuilding, runtime, build.id));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => Fill());
        Fill();
    }

    public void Fill()
    {
        runtime = GUILIB.Resolve(build, gameObject);
        var isOccupied = runtime != null && (runtime.GetPar("level") > 0 || build.own);
        occupied?.SetActive(isOccupied);
        notoccupied?.SetActive(!isOccupied);
    }
}
