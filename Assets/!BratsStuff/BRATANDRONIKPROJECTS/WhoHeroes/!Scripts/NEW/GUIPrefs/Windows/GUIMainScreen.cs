using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIMainScreen : MonoBehaviour
{
    private static readonly string[] SystemBuildingIds =
    {
        "castle", "tavern", "tower", "expedition", "market", "settings", "hero", "main_player"
    };

    public TextMeshProUGUI castlelvl;
    public TextMeshProUGUI buildingOwned;
    public TextMeshProUGUI totalArmy;
    public TextMeshProUGUI totalWorkers;
    public Slider daynightbar;
    public Button forceNightButton;
    public TextMeshProUGUI daynighttime;

    private void Start()
    {
        EventManager.SUB("PARSE_ENDED", OnParseEnded);
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
        EventManager.SUB(WhoHeroesEvents.DayProgress, OnDayProgress);
        EventManager.SUB("new_day", OnPhaseChanged);
        EventManager.SUB("new_night", OnPhaseChanged);
        EventManager.SUB(WhoHeroesEvents.NightWavePrepared, OnNightWavePrepared);
        forceNightButton?.onClick.AddListener(ForceNight);
        if (daynightbar != null)
            daynightbar.interactable = false;
        Fill();
        if (ConfigLoader.parseEnded)
            SyncDayBar();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB("PARSE_ENDED", OnParseEnded);
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
        EventManager.UNSUB(WhoHeroesEvents.DayProgress, OnDayProgress);
        EventManager.UNSUB("new_day", OnPhaseChanged);
        EventManager.UNSUB("new_night", OnPhaseChanged);
        EventManager.UNSUB(WhoHeroesEvents.NightWavePrepared, OnNightWavePrepared);
        forceNightButton?.onClick.RemoveListener(ForceNight);
    }

    private void OnRefresh(ArgPass _)
    {
        Fill();
    }

    private void OnParseEnded(ArgPass _)
    {
        Fill();
        SyncDayBar();
    }

    private void OnPhaseChanged(ArgPass _)
    {
        SyncDayBar();
        Fill();
    }

    private void OnDayProgress(ArgPass _)
    {
        SyncDayBar();
    }

    private void OnNightWavePrepared(ArgPass _)
    {
        SyncDayBar();
    }

    private void SyncDayBar()
    {
        var cycle = MainCycle_WhoHeroes.Instance;
        if (cycle == null)
            return;

        if (daynightbar != null)
            daynightbar.normalizedValue = cycle.DayProgress01;
        if (daynighttime != null)
            daynighttime.text = FormatTime(cycle.DayRemainingSeconds) + "\n" + FormatNightForecast(cycle);
        if (forceNightButton != null)
            forceNightButton.interactable = cycle.Phase == WhoHeroesPhase.Day;
    }

    private static string FormatTime(float value)
    {
        var seconds = Mathf.Max(0, Mathf.CeilToInt(value));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private static string FormatNightForecast(MainCycle_WhoHeroes cycle)
    {
        var total = cycle.NightWaveSnapshot.Sum(value => Mathf.Max(0, value.Value));
        return MainCycle_WhoHeroes.Text("night_forecast")
            .Replace("{night}", cycle.ForecastNightNumber.ToString())
            .Replace("{portals}", cycle.ActiveNightPortalCount.ToString())
            .Replace("{enemies}", total.ToString());
    }

    private static void ForceNight()
    {
        MainCycle_WhoHeroes.Instance?.ForceNight();
    }

    public void Fill()
    {
        var all = MainStates.instance?.all.Values.ToList();
        if (all == null) return;
        var castle = all.FirstOrDefault(x => GUILIB.IsId(x, "castle"));
        if (castlelvl != null) castlelvl.text = GUILIB.Level(castle).ToString();
        if (buildingOwned != null)
            buildingOwned.text = all.Count(IsCapturedBuilding).ToString();
        if (totalArmy != null)
            totalArmy.text = TotalArmyAmount().ToString();
        if (totalWorkers != null)
            totalWorkers.text = all.Where(x => x != null && GUILIB.Level(x) > 0 &&
                    !string.IsNullOrEmpty(MainCycle_WhoHeroes.MineResourceId(x)))
                .Sum(x => GUILIB.Level(x)).ToString();
    }

    private static int TotalArmyAmount()
    {
        if (MainStates.instance == null)
            return 0;

        var owners = new[] { "main_player", "expedition" }
            .Select(id => MainStates.instance.all.TryGetValue(id, out var owner) ? owner : null)
            .Where(owner => owner != null);
        return owners.SelectMany(owner => owner.inventory)
            .Where(unit => unit != null && unit.it == ItemType.monster && unit.dbObj != null &&
                           unit.GetPar("amount") > 0f)
            .Sum(unit => Mathf.RoundToInt(unit.GetPar("amount")));
    }

    private static bool IsCapturedBuilding(RObj value)
    {
        if (value?.dbObj == null || value.main == null || value.it != ItemType.monster || GUILIB.Level(value) <= 0)
            return false;
        var id = value.dbObj.ID;
        return !string.IsNullOrEmpty(id) &&
               !id.StartsWith("portal", StringComparison.Ordinal) &&
               !SystemBuildingIds.Contains(id) &&
               !MainCycle_WhoHeroes.CastleUnits.ContainsKey(id);
    }

}
