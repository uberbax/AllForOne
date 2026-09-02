using System;
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
        if (buildingOwned != null) buildingOwned.text = all.Count(x => x.it == ItemType.building && x.GetPar("level") > 0).ToString();
        if (totalArmy != null)
        {
            var roster = GUILIB.PlayerInventory()
                .Where(x => x != null && x.it == ItemType.monster && x.dbObj != null && x.GetPar("amount") > 0f)
                .GroupBy(x => x.dbObj.ID)
                .Select(group => $"{LocalizedName(group.Key)} × {group.Sum(x => Mathf.RoundToInt(x.GetPar("amount")))}");
            totalArmy.text = string.Join("  ", roster);
        }
        if (totalWorkers != null) totalWorkers.text = all.Where(x => x.it == ItemType.building).Sum(x => Mathf.RoundToInt(x.GetPar("workers"))).ToString();
    }

    private static string LocalizedName(string id)
    {
        if (ConfigLoader.Instance == null || string.IsNullOrEmpty(id))
            return id;
        var localized = ConfigLoader.Instance.GetMeLocale(id.ToLowerInvariant());
        return string.IsNullOrWhiteSpace(localized) ? id : localized;
    }

}
