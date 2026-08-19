using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIMainScreen : MonoBehaviour
{
    public TextMeshProUGUI castlelvl;
    public TextMeshProUGUI buildingOwned;
    public TextMeshProUGUI totalArmy;
    public TextMeshProUGUI totalWorkers;
    public Slider daynightbar;
    private Coroutine daynightCor;

    private void Start()
    {
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => Fill());
        EventManager.SUB("new_day", _ => RestartDayBar());
        RestartDayBar();
        Fill();
    }

    private void RestartDayBar()
    {
        if (daynightCor != null) StopCoroutine(daynightCor);
        var duration = ConfigLoader.GetMetaParamValue("day_night_duration");
        if (duration <= 0) duration = 60;
        daynightCor = GUILIB.Instance.BarInfinite(daynightbar, 1f / duration);
    }

    public void Fill()
    {
        var all = MainStates.instance?.all.Values.ToList();
        if (all == null) return;
        var castle = all.FirstOrDefault(x => GUILIB.IsId(x, "castle"));
        if (castlelvl != null) castlelvl.text = GUILIB.Level(castle).ToString();
        if (buildingOwned != null) buildingOwned.text = all.Count(x => x.it == ItemType.building && x.GetPar("level") > 0).ToString();
        if (totalArmy != null) totalArmy.text = GUILIB.PlayerInventory().Where(x => x.it == ItemType.monster).Sum(x => Mathf.RoundToInt(x.GetPar("amount"))).ToString();
        if (totalWorkers != null) totalWorkers.text = all.Where(x => x.it == ItemType.building).Sum(x => Mathf.RoundToInt(x.GetPar("workers"))).ToString();
    }
}
