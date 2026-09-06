using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIPortalWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public GUIButtUpgrade uprgade;
    public Button back;
    public GUIInfoItem start;
    public GUIInfoItem end;
    public TextMeshProUGUI ptype;
    public GameObject outclosed;
    private RObj runtime;
    [SerializeField] private TextMeshProUGUI startLabel;
    [SerializeField] private TextMeshProUGUI endLabel;

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
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
        FillPortal(false);
    }

    public void FillCaptureResult(RObj value)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        FillPortal(true);
    }

    private void FillPortal(bool captured)
    {
        if (runtime == null)
            return;

        var level = GUILIB.Level(runtime, building.level);
        var enter = runtime.dbObj?.pars.ContainsKey("enter") == true ? runtime.GetPar("enter") > 0 : !building.id.Contains("out");
        var portalId = enter ? "portalin" : "portalout";
        var displayPortal = ResolveEntryPortal(runtime);
        general?.Fill(GUILIB.Id(displayPortal), level, GUILIB.Icon(portalId), "portal_descr");
        outclosed?.SetActive(!enter && level == 0);
        var attackable = MainCycle_WhoHeroes.IsAttackableTarget(runtime);
        var restorable = MainCycle_WhoHeroes.IsRestorableTarget(runtime);
        if (uprgade != null)
        {
            uprgade.gameObject.SetActive(attackable || restorable);
            if (attackable)
            {
                var blocked = MainCycle_WhoHeroes.Instance == null ||
                              !MainCycle_WhoHeroes.Instance.CanStart(runtime);
                uprgade.Fill(new List<Bon>(), block: blocked, showRestriction: false,
                    head: "attack", activeButtonColor: "butred");
                uprgade.upgrade?.costList?.SetActive(false);
            }
            else if (restorable)
            {
                uprgade.Fill(GUILIB.Price(runtime), false,
                    !MainCycle_WhoHeroes.IsManagementActionAllowed("upgrade"), true, "restore");
            }
        }

        var entryPortal = ResolveEntryPortal(runtime);
        var detailsPortal = captured
            ? entryPortal
            : MainCycle_WhoHeroes.Instance?.NextLockedPortal ??
              (GUILIB.Level(entryPortal) <= 0 ? entryPortal : null);
        var territory = ConfigValue(detailsPortal, "found_in");
        var pointsOfInterest = FindPointsOfInterest(territory);
        var addition = FindNightAddition(detailsPortal);
        var addedDemons = CountEnemies(addition);

        if (startLabel != null)
            startLabel.text = MainCycle_WhoHeroes.Text(captured ? "territory_opened" : "next_territory");
        if (endLabel != null)
            endLabel.text = MainCycle_WhoHeroes.Text("points_of_interest");
        SetInfo(start, DisplayName(territory), GUILIB.Icon(territory));
        SetInfo(end, pointsOfInterest.Count.ToString(),
            pointsOfInterest.Count == 0 ? null : GUILIB.Icon(pointsOfInterest[0]));
        if (ptype != null)
            ptype.text = MainCycle_WhoHeroes.Text(enter ? "in" : "out");

        if (general?.description != null)
        {
            general.description.text = MainCycle_WhoHeroes.Text(
                    captured ? "portal_captured_description" : "portal_next_description")
                .Replace("{threat}", CurrentNightThreat().ToString())
                .Replace("{demons}", addedDemons.ToString())
                .Replace("{poi}", FormatPointsOfInterest(pointsOfInterest));
        }
    }

    private static RObj ResolveEntryPortal(RObj value)
    {
        if (value == null || !value.RID.StartsWith("portalout", StringComparison.Ordinal) ||
            MainStates.instance == null)
            return value;

        var entryId = value.RID.Replace("portalout", "portalin");
        return MainStates.instance.all.TryGetValue(entryId, out var entry) ? entry : value;
    }

    private static List<RObj> FindPointsOfInterest(string territory)
    {
        if (string.IsNullOrEmpty(territory) || MainStates.instance == null)
            return new List<RObj>();

        return MainStates.instance.all.Values
            .Where(value => value != null && !value.RID.StartsWith("portal", StringComparison.Ordinal) &&
                            ConfigValue(value, "found_in") == territory)
            .OrderBy(value => GUILIB.Id(value), StringComparer.Ordinal)
            .ToList();
    }

    private static FormatBattles FindNightAddition(RObj portal)
    {
        if (portal == null || ConfigLoader.Instance == null)
            return null;

        var encounter = ConfigValue(portal, "encounter");
        var separator = encounter.IndexOf('|');
        if (separator < 0 || separator + 1 >= encounter.Length)
            return null;

        var battleId = encounter.Substring(separator + 1).Trim();
        return ConfigLoader.Instance.battles.Find(value => value.battleName == battleId);
    }

    private static int CurrentNightThreat()
    {
        if (MainStates.instance == null)
            return 0;

        return MainStates.instance.all.Values
            .Where(value => value != null && value.RID.StartsWith("portalin", StringComparison.Ordinal) &&
                            GUILIB.Level(value) > 0)
            .Sum(value => CountEnemies(FindNightAddition(value)));
    }

    private static int CountEnemies(FormatBattles battle)
    {
        if (battle == null)
            return 0;
        var count = Mathf.Min(battle.enemies.heroLevelPosition.Count, battle.enemies.amounts.Count);
        var total = 0;
        for (var index = 0; index < count; index++)
            total += Mathf.Max(0, battle.enemies.amounts[index]);
        return total;
    }

    private static string FormatPointsOfInterest(List<RObj> values)
    {
        return values.Count == 0
            ? MainCycle_WhoHeroes.Text("none")
            : string.Join(", ", values.Select(value => DisplayName(GUILIB.Id(value))));
    }

    private static string ConfigValue(RObj value, string key)
    {
        var result = GUILIB.StringParam(value, key).Trim();
        return string.Equals(result, "x", StringComparison.OrdinalIgnoreCase) ? string.Empty : result;
    }

    private static string DisplayName(string id)
    {
        if (string.IsNullOrEmpty(id))
            return MainCycle_WhoHeroes.Text("none");
        var projectText = MainCycle_WhoHeroes.Text(id);
        if (!string.IsNullOrWhiteSpace(projectText))
            return projectText;
        var key = id.ToLowerInvariant();
        return ConfigLoader.Instance != null && ConfigLoader.Instance.doctLoc.ContainsKey(key)
            ? ConfigLoader.Instance.GetMeLocale(key)
            : id;
    }

    private static void SetInfo(GUIInfoItem item, string text, Sprite icon)
    {
        if (item?.name != null)
            item.name.text = text;
        if (item?.icon != null && icon != null)
            item.icon.sprite = icon;
    }

}
