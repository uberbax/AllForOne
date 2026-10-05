using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public sealed class CharacterTraitDefinition
{
    [SerializeField] private string parameter;
    [SerializeField] private string label;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(0)] private int goldCost = 100;
    [SerializeField, Min(0)] private int res1Cost = 10;

    public string Parameter => parameter == "crit_dmg" ? "crit_damage" : parameter;
    public string RuntimeParameter => Parameter == "crit_damage" ? "crit_dmg" : Parameter;
    public string StatKey => "trait_" + Parameter;
    public string Label => string.IsNullOrEmpty(label) ? Parameter : label;
    public Sprite Icon => icon;
    public int GoldCost => goldCost;
    public int Res1Cost => res1Cost;
    // Combat uses a 0..1 fraction for critical chance, percentage points for crit_dmg/dodge.
    public float Increment => Parameter == "crit_chance" ? 0.1f : 10f;
    public bool IsValid => !string.IsNullOrWhiteSpace(Parameter) && goldCost >= 0 && res1Cost >= 0 &&
                           (goldCost > 0 || res1Cost > 0);

    public CharacterTraitDefinition(string parameter, string label, int goldCost = 100, int res1Cost = 10)
    { this.parameter = parameter; this.label = label; this.goldCost = goldCost; this.res1Cost = res1Cost; }

    public string Format(float value)
    {
        if (Parameter == "crit_chance") value *= 100f;
        var suffix = Parameter == "crit_chance" || Parameter == "crit_damage" || Parameter == "dodge" ? "%" : "";
        return value.ToString("0.##", CultureInfo.InvariantCulture) + suffix;
    }

    public static CharacterTraitDefinition[] Defaults() => new[]
    {
        new CharacterTraitDefinition("attack", "Attack"),
        new CharacterTraitDefinition("max_health", "Max Health"),
        new CharacterTraitDefinition("max_mana", "Max Mana"),
        new CharacterTraitDefinition("magic", "Magic"),
        new CharacterTraitDefinition("def", "Defense"),
        new CharacterTraitDefinition("res", "Resistance"),
        new CharacterTraitDefinition("crit_chance", "Critical Chance"),
        new CharacterTraitDefinition("crit_damage", "Critical Damage"),
        new CharacterTraitDefinition("dodge", "Dodge"),
        new CharacterTraitDefinition("accuracy", "Accuracy")
    };
}

/// <summary>Changes existing inventory, upgradePars and playerStats; introduces no new save format.</summary>
public static class CharacterTraitProgression
{
    public const int MaxLevel = 10;

    public static int GetLevel(PlayerData data, CharacterTraitDefinition trait)
    {
        return data?.playerStats != null && trait != null &&
               data.playerStats.TryGetValue(trait.StatKey, out var stat) && stat != null
            ? Mathf.Clamp(stat.Value, 0, MaxLevel) : 0;
    }

    public static int GetAmount(RObj player, string resource)
    {
        var item = FindResource(player, resource);
        return item != null && item.upgradePars.TryGetValue("amount", out var amount)
            ? Mathf.Max(0, (int)amount) : 0;
    }

    public static bool CanAfford(RObj player, CharacterTraitDefinition trait) =>
        trait != null && trait.IsValid && GetAmount(player, "gold") >= trait.GoldCost &&
        GetAmount(player, "res1") >= trait.Res1Cost;

    // expectedLevel rejects a stale confirmation or a duplicate click even across two panels.
    public static bool TryUpgrade(PlayerData data, RObj player, CharacterTraitDefinition trait, int expectedLevel)
    {
        if (data?.playerStats == null || player == null || trait == null || !trait.IsValid ||
            expectedLevel < 0 || expectedLevel >= MaxLevel || GetLevel(data, trait) != expectedLevel ||
            !CanAfford(player, trait)) return false;

        player.ChangePar(trait.RuntimeParameter, trait.Increment);
        Spend(player, "gold", trait.GoldCost);
        Spend(player, "res1", trait.Res1Cost);
        if (data.playerStats.TryGetValue(trait.StatKey, out var stat) && stat != null)
            stat.Value = Math.Max(stat.Value, expectedLevel + 1);
        else data.playerStats[trait.StatKey] = new Bon { Key = trait.StatKey, Value = expectedLevel + 1 };
        return true;
    }

    private static RObj FindResource(RObj player, string resource) =>
        player?.inventory?.Find(item => item != null && item.dbObj != null && item.dbObj.ID == resource);

    private static void Spend(RObj player, string resource, int amount)
    {
        if (amount == 0) return;
        var item = FindResource(player, resource);
        item.ChangePar("amount", -amount);
        if (item.GetPar("amount") <= 0) player.inventory.Remove(item);
    }
}
