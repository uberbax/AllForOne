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
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
        Fill();
    }

    private void OnRefresh(ArgPass _) => Fill();

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    public void Fill()
    {
        runtime = GUILIB.Resolve(build, gameObject);
        var isOccupied = runtime != null && (runtime.GetPar("level") > 0 || build.own);
        occupied?.SetActive(isOccupied);
        notoccupied?.SetActive(!isOccupied);
    }
}
