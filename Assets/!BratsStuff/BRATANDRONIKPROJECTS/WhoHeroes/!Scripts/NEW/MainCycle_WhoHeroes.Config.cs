using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class MainCycle_WhoHeroes
{
    private const string GoldResourceMeta = "whoheroes_resource_gold";
    private const string WoodResourceMeta = "whoheroes_resource_wood";
    private const string StoneResourceMeta = "whoheroes_resource_stone";

    public static string GoldResourceId => ReadString(GoldResourceMeta);
    public static string WoodResourceId => ReadString(WoodResourceMeta);
    public static string StoneResourceId => ReadString(StoneResourceMeta);
    public static IReadOnlyList<string> ResourceIds =>
        new[] { GoldResourceId, WoodResourceId, StoneResourceId };
    public const string TraderSetId = "whoheroes_trader_units";
    public const string TavernSetId = "whoheroes_tavern_units";
    public const string UnitsHiredStat = "whoheroes_units_hired";
    public const string CastleRestoredStat = "whoheroes_castle_restored";
    public const string ExpeditionsWonStat = "whoheroes_expeditions_won";
    public const string NightsSurvivedStat = "whoheroes_nights_survived";
    public const string PermanentPerkSetId = "whoheroes_permanent_perks";
    public const string PendingPerkNightStat = "whoheroes_pending_perk_night";
    public const string UnitDamagePerkStat = "whoheroes_perk_unit_damage";
    public const string UnitHealthPerkStat = "whoheroes_perk_unit_health";
    public const string UnitArmorPerkStat = "whoheroes_perk_unit_armor";
    public const string PrinceDamagePerkStat = "whoheroes_perk_prince_damage";
    public const string PrinceHealthPerkStat = "whoheroes_perk_prince_health";
    public const string PrinceArmorPerkStat = "whoheroes_perk_prince_armor";
    public const string UnitCostPerkStat = "whoheroes_perk_unit_cost";
    public const string BuildCostPerkStat = "whoheroes_perk_build_cost";
    public const string CastleOfferSetPrefix = "whoheroes_castle_offer_";
    public const string ExpeditionDefenseSetPrefix = "whoheroes_expedition_defense_";
    public const string BoostStatSetPrefix = "whoheroes_boost_stat_";
    public const string OnboardingTaskSetId = "whoheroes_onboarding_tasks";
    public const string CaptureDynamicId = "whoheroes_capture_target";
    private const string RunBoostStatPrefix = "whoheroes_run_boost_";

    private static ConfigLoader cachedLoader;
    private static IReadOnlyDictionary<string, string> castleUnits =
        new Dictionary<string, string>(StringComparer.Ordinal);
    private static IReadOnlyDictionary<string, ExpeditionDefense> expeditionDefenses =
        new Dictionary<string, ExpeditionDefense>(StringComparer.Ordinal);
    private static IReadOnlyDictionary<string, string> boostSources =
        new Dictionary<string, string>(StringComparer.Ordinal);
    private static IReadOnlyList<string> tavernUnits = Array.Empty<string>();
    private static IReadOnlyDictionary<string, int> tavernUnitAmounts =
        new Dictionary<string, int>(StringComparer.Ordinal);
    private static IReadOnlyList<string> permanentPerkIds = Array.Empty<string>();
    private static IReadOnlyList<string> runBoostStats = Array.Empty<string>();
    private static IReadOnlyList<string> onboardingTaskIds = Array.Empty<string>();

    public readonly struct ExpeditionDefense
    {
        public ExpeditionDefense(string unitId, int level, int count = 1)
        {
            UnitId = unitId;
            Level = level;
            Count = count;
        }

        public string UnitId { get; }
        public int Level { get; }
        public int Count { get; }
    }

    public static IReadOnlyDictionary<string, ExpeditionDefense> ExpeditionDefenses
    {
        get { RefreshCache(); return expeditionDefenses; }
    }

    public static IReadOnlyDictionary<string, string> CastleUnits
    {
        get { RefreshCache(); return castleUnits; }
    }

    public static IReadOnlyList<string> TavernUnits
    {
        get { RefreshCache(); return tavernUnits; }
    }

    public static int TavernUnitAmount(string unitId)
    {
        RefreshCache();
        return unitId != null && tavernUnitAmounts.TryGetValue(unitId, out var amount)
            ? amount
            : 1;
    }

    public static IReadOnlyList<string> PermanentPerkIds
    {
        get { RefreshCache(); return permanentPerkIds; }
    }

    public static IReadOnlyList<string> RunBoostStats
    {
        get { RefreshCache(); return runBoostStats; }
    }

    public static IReadOnlyList<string> OnboardingTaskIds
    {
        get { RefreshCache(); return onboardingTaskIds; }
    }

    public static int TavernOfferCount => ReadPositiveInt("whoheroes_tavern_offer_count");
    public static int ExpeditionMaxStacks => Mathf.Max(1, ReadPositiveInt("whoheroes_expedition_max_stacks"));
    public static string StartingCastleBuildingId => ReadString("whoheroes_starting_castle");
    public static string StartingUnitId => ReadString("whoheroes_starting_unit");

    public static float PermanentStatPercent()
    {
        return ReadPositiveFloat("whoheroes_perk_stat_percent");
    }

    public static float PermanentCostPercent()
    {
        return ReadPositiveFloat("whoheroes_perk_cost_percent");
    }

    public static float PermanentMaxDiscount()
    {
        return ReadPositiveFloat("whoheroes_perk_max_discount");
    }

    public static string PermanentPerkTitle(string id)
    {
        return ReadString("whoheroes_text_" + id + "_title");
    }

    public static string PermanentPerkDescription(string id, int nextLevel)
    {
        return Text(id + "_description")
            .Replace("{level}", Mathf.Max(1, nextLevel).ToString());
    }

    public static string PermanentPerkIcon(string id)
    {
        return ReadString("whoheroes_icon_" + id);
    }

    public static int TraderStartNight()
    {
        return ReadPositiveInt("whoheroes_trader_start_night");
    }

    public static string SteamUrl()
    {
        return ReadString("whoheroes_steam_url");
    }

    public static float TraderTravelSeconds()
    {
        return ReadPositiveFloat("whoheroes_trader_travel_seconds");
    }

    public static int TraderGoldSurcharge()
    {
        return ReadPositiveInt("whoheroes_trader_gold_surcharge");
    }

    public static float TraderPowerMultiplier()
    {
        var configured = ReadPositiveFloat("whoheroes_trader_power_multiplier");
        if (configured > 10f)
            configured /= 100f;
        return configured;
    }

    public static int StartingUnitAmount()
    {
        return ReadPositiveInt("whoheroes_start_unit_amount");
    }

    public static float BoostPercent()
    {
        return ReadPositiveFloat("whoheroes_boost_percent");
    }

    public static bool TryGetBoostStat(string id, out string stat)
    {
        RefreshCache();
        return boostSources.TryGetValue(id ?? string.Empty, out stat);
    }

    public static string RunBoostStat(string stat)
    {
        return RunBoostStatPrefix + stat;
    }

    public static bool TryGetExpeditionDefense(string targetId, out ExpeditionDefense defense)
    {
        if (!string.IsNullOrEmpty(targetId))
            return ExpeditionDefenses.TryGetValue(targetId, out defense);
        defense = default;
        return false;
    }

    public static List<Bon> TavernRerollPrice()
    {
        return new List<Bon>
        {
            new Bon { Key = GoldResourceId, Value = ReadPositiveInt("whoheroes_tavern_reroll_gold") }
        };
    }

    public static string Text(string id)
    {
        return ReadString("whoheroes_text_" + id).Replace("\\n", "\n");
    }

    public static string UnitPurchaseDynamicId(string unitId)
    {
        return "whoheroes_buy_" + unitId;
    }

    public static bool TryGetUnitPurchaseDynamic(string unitId, out FormatDynamic dynamic)
    {
        dynamic = null;
        var id = UnitPurchaseDynamicId(unitId);
        return ConfigLoader.Instance != null && ConfigLoader.Instance.allDynamic.TryGetValue(id, out dynamic);
    }

    private static void RefreshCache()
    {
        if (ConfigLoader.Instance == null || cachedLoader == ConfigLoader.Instance)
            return;

        cachedLoader = ConfigLoader.Instance;
        castleUnits = ReadSingleValueSets(CastleOfferSetPrefix);
        expeditionDefenses = ReadDefenseSets();
        boostSources = ReadSingleValueSets(BoostStatSetPrefix);
        tavernUnits = ReadSet(TavernSetId);
        tavernUnitAmounts = ReadSetAmounts(TavernSetId);
        permanentPerkIds = ReadSet(PermanentPerkSetId);
        runBoostStats = boostSources.Values.Distinct(StringComparer.Ordinal).ToArray();
        onboardingTaskIds = ReadSet(OnboardingTaskSetId);
    }

    private static IReadOnlyDictionary<string, string> ReadSingleValueSets(string setPrefix)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (ConfigLoader.Instance == null)
            return result;

        foreach (var pair in ConfigLoader.Instance.dictSets)
        {
            if (!pair.Key.StartsWith(setPrefix, StringComparison.Ordinal))
                continue;

            var item = pair.Value.FirstOrDefault(value => value != null && !string.IsNullOrWhiteSpace(value.item));
            var targetId = pair.Key.Substring(setPrefix.Length);
            if (item != null && targetId.Length > 0)
                result[targetId] = item.item.Trim();
        }
        return result;
    }

    private static IReadOnlyDictionary<string, ExpeditionDefense> ReadDefenseSets()
    {
        var result = new Dictionary<string, ExpeditionDefense>(StringComparer.Ordinal);
        if (ConfigLoader.Instance == null)
            return result;

        foreach (var pair in ConfigLoader.Instance.dictSets)
        {
            if (!pair.Key.StartsWith(ExpeditionDefenseSetPrefix, StringComparison.Ordinal))
                continue;

            var item = pair.Value.FirstOrDefault(value => value != null && !string.IsNullOrWhiteSpace(value.item));
            var targetId = pair.Key.Substring(ExpeditionDefenseSetPrefix.Length);
            if (item != null && targetId.Length > 0)
                result[targetId] = new ExpeditionDefense(
                    item.item.Trim(), Mathf.Max(1, item.amount2), Mathf.Max(1, item.amount1));
        }
        return result;
    }

    private static IReadOnlyList<string> ReadSet(string setId)
    {
        if (ConfigLoader.Instance == null || !ConfigLoader.Instance.dictSets.TryGetValue(setId, out var entries))
            return Array.Empty<string>();
        return entries.Where(value => value != null && !string.IsNullOrWhiteSpace(value.item))
            .Select(value => value.item.Trim()).Distinct(StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyDictionary<string, int> ReadSetAmounts(string setId)
    {
        if (ConfigLoader.Instance == null || !ConfigLoader.Instance.dictSets.TryGetValue(setId, out var entries))
            return new Dictionary<string, int>(StringComparer.Ordinal);

        return entries.Where(value => value != null && !string.IsNullOrWhiteSpace(value.item))
            .GroupBy(value => value.item.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => Mathf.Max(1, group.First().amount1), StringComparer.Ordinal);
    }

    private static string ReadString(string key)
    {
        return ConfigLoader.Instance == null
            ? string.Empty
            : ConfigLoader.GetMetaParamValueString(key)?.Trim() ?? string.Empty;
    }

    private static int ReadPositiveInt(string key)
    {
        return Mathf.Max(0, Mathf.RoundToInt(ReadPositiveFloat(key)));
    }

    private static float ReadPositiveFloat(string key)
    {
        return ConfigLoader.Instance == null ? 0f : Mathf.Max(0f, ConfigLoader.GetMetaParamValue(key));
    }

}
