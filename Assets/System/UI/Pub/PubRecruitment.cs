using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public sealed class PubHero
{
    public string Id, Name;
    public int Rarity;
    public Obj Definition;
    public int MileageTokens = 5;
    public int RewardMultiplier = 1;
    public int Quantity => (30 - MileageTokens) * RewardMultiplier;
    public int TotalMileageTokens => MileageTokens * RewardMultiplier;
    public bool GrantedHero;
}

/// <summary>Pub offers and transactions use the existing inventory and playerStats save fields.</summary>
public sealed class PubRecruitment
{
    public const string TotalKey = "pub_recruits_total";
    private const string TimerKey = "pub_next_refresh";
    private readonly System.Random random;
    public readonly List<PubHero> Pool = new List<PubHero>();
    public readonly List<PubHero> Offers = new List<PubHero>();
    public long NextRefreshTicks { get; private set; }
    public int TotalRecruits { get; private set; }
    public PubRecruitment(int seed) { random = new System.Random(seed); }

    public static bool IsEligibleId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) &&
            !id.Trim().StartsWith("hero_", StringComparison.OrdinalIgnoreCase) &&
            !id.Trim().StartsWith("tree", StringComparison.OrdinalIgnoreCase);
    }

    public void BuildPool(IEnumerable<FormatHero> rows, IDictionary<string, Obj> definitions, IEnumerable<string> allowedHeroIds = null)
    {
        Pool.Clear();
        HashSet<string> allowed = null;
        if (allowedHeroIds != null)
        {
            foreach (var allowedId in allowedHeroIds)
            {
                // A configured list containing only invalid IDs must not enable the whole pool.
                if (allowed == null) allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(allowedId)) allowed.Add(allowedId.Trim());
            }
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (rows == null || definitions == null) return;
        foreach (var row in rows)
        {
            if (row == null || !IsEligibleId(row.monsterName)) continue;
            string baseId = row.monsterName.Trim().ToLowerInvariant();
            string id = row.level > 1 ? baseId + row.level.ToString(CultureInfo.InvariantCulture) : baseId;
            if (allowed != null && !allowed.Contains(id)) continue;
            if (!IsEligibleId(id) || !seen.Add(id) || !definitions.TryGetValue(id, out var definition)) continue;
            Pool.Add(new PubHero { Id = id, Definition = definition, Rarity = row.rarity,
                Name = string.IsNullOrWhiteSpace(row.displayName) ? Pretty(baseId) : row.displayName });
        }
        Pool.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
    }

    private static string Pretty(string id) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' '));
    private static int Stat(PlayerData data, string key) => data.playerStats.TryGetValue(key, out var value) && value != null ? value.Value : 0;
    private static void SetStat(PlayerData data, string key, int value, string text = null)
    {
        if (!data.playerStats.TryGetValue(key, out var stat) || stat == null)
            data.playerStats[key] = stat = new Bon { Key = key };
        stat.Value = value;
        if (text != null) stat.Val2 = text;
    }

    public bool RestoreOrRefresh(PlayerData data, DateTime utcNow, int refreshMinutes)
    {
        if (data == null || data.playerStats == null || Pool.Count == 0) return false;
        TotalRecruits = Math.Max(0, Stat(data, TotalKey));
        Offers.Clear();
        int count = Math.Min(3, Pool.Count);
        for (int i = 0; i < count; i++)
        {
            if (!data.playerStats.TryGetValue("pub_offer_" + i, out var saved) || saved == null) break;
            var hero = Pool.Find(h => h.Id == saved.Val2);
            if (hero == null || Offers.Any(h => h.Id == hero.Id)) break;
            int mileage = Stat(data, "pub_offer_mileage_" + i);
            if (!ValidMileage(mileage))
            {
                mileage = RollMileage();
                SetStat(data, "pub_offer_mileage_" + i, mileage);
            }
            Offers.Add(Offer(hero, mileage));
        }
        long ticks = 0;
        if (data.playerStats.TryGetValue(TimerKey, out var timer) && timer != null)
            long.TryParse(timer.Val2, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks);
        NextRefreshTicks = ticks;
        if (Offers.Count == count && ticks > utcNow.Ticks && ticks <= utcNow.AddMinutes(Math.Max(1, refreshMinutes) + 1).Ticks) return true;
        Refresh(data, utcNow, refreshMinutes);
        return true;
    }

    public void Refresh(PlayerData data, DateTime utcNow, int refreshMinutes)
    {
        var old = new HashSet<string>(Offers.Select(h => h.Id), StringComparer.Ordinal);
        var choices = Pool.Where(h => !old.Contains(h.Id)).ToList();
        int count = Math.Min(3, Pool.Count);
        if (choices.Count < count) choices = new List<PubHero>(Pool);
        Offers.Clear();
        for (int i = 0; i < count; i++)
        {
            int index = random.Next(choices.Count);
            Offers.Add(Offer(choices[index], RollMileage())); choices.RemoveAt(index);
        }
        NextRefreshTicks = utcNow.AddMinutes(Math.Max(1, refreshMinutes)).Ticks;
        for (int i = 0; i < 3; i++)
        {
            SetStat(data, "pub_offer_" + i, 1, i < Offers.Count ? Offers[i].Id : "");
            SetStat(data, "pub_offer_mileage_" + i, i < Offers.Count ? Offers[i].MileageTokens : 0);
        }
        SetStat(data, TimerKey, 0, NextRefreshTicks.ToString(CultureInfo.InvariantCulture));
    }

    private int RollMileage() => (random.Next(4) + 1) * 5;
    private static bool ValidMileage(int value) => value == 5 || value == 10 || value == 15 || value == 20;
    private static PubHero Offer(PubHero hero, int mileage) => new PubHero
    {
        Id = hero.Id, Name = hero.Name, Rarity = hero.Rarity, Definition = hero.Definition,
        MileageTokens = mileage
    };

    public static int Currency(RObj player, string id)
    {
        long total = 0;
        if (player?.inventory == null) return 0;
        foreach (var item in player.inventory)
            if (item?.dbObj != null && item.dbObj.ID == id && item.upgradePars.TryGetValue("amount", out float amount))
                total += Math.Max(0, (int)amount);
        return (int)Math.Min(int.MaxValue, total);
    }

    private static void Spend(RObj player, string id, int price)
    {
        int left = price;
        for (int i = player.inventory.Count - 1; i >= 0 && left > 0; i--)
        {
            var item = player.inventory[i];
            if (item?.dbObj == null || item.dbObj.ID != id) continue;
            int amount = Math.Max(0, (int)item.GetPar("amount"));
            int take = Math.Min(amount, left);
            if (take > 0) item.ChangePar("amount", -take);
            left -= take;
            if (amount == take) player.inventory.RemoveAt(i);
        }
    }

    public bool TryPaidRefresh(MainStates state, string currency, int price, int minutes, DateTime now, out string error)
    {
        error = "";
        if (!Ready(state) || Pool.Count == 0) { error = "Heroes are loading"; return false; }
        if (price < 0 || Currency(state.mainPlayer, currency) < price) { error = "Not enough " + currency; return false; }
        Spend(state.mainPlayer, currency, price);
        Refresh(state.playerData, now, minutes);Persist(state);return true;
    }

    public bool TryRecruit(MainStates state, int count, string currency, int costPerDraw, int bonusEvery,
        out List<PubHero> rewards, out int bonusCount, out string error)
    {
        rewards = new List<PubHero>(); bonusCount = 0; error = "";
        if (!Ready(state) || Offers.Count == 0 || count < 1 || count > 3 || costPerDraw < 1) { error = "Heroes are loading"; return false; }
        long cost = (long)costPerDraw * count;
        if (cost > int.MaxValue || Currency(state.mainPlayer, currency) < cost) { error = "Not enough " + currency; return false; }
        int previous = Math.Max(0, Stat(state.playerData, TotalKey));
        long total = Math.Min(int.MaxValue, (long)previous + 1);
        int interval = Math.Max(1, bonusEvery);
        bonusCount = (int)(total / interval) - previous / interval;
        // One hero selection per purchase. Multiplier and milestone bonus scale only its resources.
        var offer = Offers[random.Next(Offers.Count)];
        var selected = Offer(offer, offer.MileageTokens);
        selected.RewardMultiplier = count * (1 + bonusCount);
        rewards.Add(selected);
        if (rewards.Any(h => !IsEligibleId(h.Id) || !DatabaseAll.instance.heroes.ContainsKey(h.Id)))
        { error = "Hero data changed. Refresh the offers"; rewards.Clear(); return false; }

        // Stage all creations, stack changes and inventory cells before charging or granting anything.
        var registrations = new HashSet<string>(state.all.Keys, StringComparer.Ordinal);
        var planning = new RObj { RID = state.mainPlayer.RID, inventory = new List<RObj>(state.mainPlayer.inventory) };
        var created = new List<RObj>();
        var amounts = new Dictionary<RObj, float>();
        var owned = new HashSet<string>(state.mainPlayer.inventory.Where(i => i?.dbObj != null && i.it == ItemType.monster && string.IsNullOrEmpty(i.shardID))
            .Select(i => i.dbObj.ID), StringComparer.Ordinal);
        try
        {
            foreach (var reward in rewards)
            {
                if (!ValidMileage(reward.MileageTokens)) throw new InvalidOperationException("Invalid Pub reward quantity");
                StageItem("shard_" + reward.Id, reward.Quantity, planning, created, amounts);
                StageItem("mileage_token", reward.TotalMileageTokens, planning, created, amounts);
                if (!owned.Add(reward.Id)) continue;
                int index = planning.GetFreeIndex(reward.Definition.sizeX, reward.Definition.sizeY);
                if (index < 0) throw new InvalidOperationException("Inventory is full");
                var recruited = DatabaseAll.instance.CreateMonster(reward.Id, 1, false, false);
                if (reward.Rarity > 0) recruited.SetPar("rarity", reward.Rarity);
                recruited.index = index;
                created.Add(recruited); planning.inventory.Add(recruited);
                reward.GrantedHero = true;
            }
        }
        catch (Exception exception)
        {
            foreach (var id in state.all.Keys.Where(id => !registrations.Contains(id)).ToArray()) state.all.Remove(id);
            Debug.LogException(exception); error = exception.Message; rewards.Clear(); return false;
        }
        if (Currency(state.mainPlayer, currency) < cost)
        {
            foreach (var id in state.all.Keys.Where(id => !registrations.Contains(id)).ToArray()) state.all.Remove(id);
            error = "Not enough " + currency; rewards.Clear(); return false;
        }
        var increases = amounts.Where(p => !created.Contains(p.Key))
            .ToDictionary(p => p.Key, p => p.Value - p.Key.GetPar("amount"));
        Spend(state.mainPlayer, currency, (int)cost);
        foreach (var change in increases) change.Key.ChangePar("amount", change.Value);
        foreach (var item in created)
            if (amounts.TryGetValue(item, out float amount)) item.SetPar("amount", amount);
        foreach (var item in created)
        {
            item.owner = state.mainPlayer;
            state.mainPlayer.inventory.Add(item);
        }
        TotalRecruits = (int)total; SetStat(state.playerData, TotalKey, TotalRecruits);
        Persist(state); return true;
    }

    private static void StageItem(string id, int quantity, RObj planning, List<RObj> created, Dictionary<RObj, float> amounts)
    {
        bool shard = id.StartsWith("shard_", StringComparison.Ordinal);
        string definitionId = shard ? "shard" : id;
        string shardId = shard ? id.Substring(6) : "";
        if (!DatabaseAll.instance.items.TryGetValue(definitionId, out var definition))
            throw new InvalidOperationException("Missing item in Items config: " + definitionId);
        if (!definition.pars.TryGetValue("max_stack", out float stack) || stack < 1 || float.IsNaN(stack))
            throw new InvalidOperationException("Invalid max_stack for " + definitionId);
        int maxStack = stack >= int.MaxValue ? int.MaxValue : (int)stack;
        foreach (var item in planning.inventory)
        {
            if (item?.dbObj == null || item.dbObj.ID != definitionId || item.shardID != shardId) continue;
            float current = amounts.TryGetValue(item, out var planned) ? planned : item.GetPar("amount");
            int take = Math.Min(quantity, Math.Max(0, maxStack - (int)current));
            if (take == 0) continue;
            amounts[item] = current + take; quantity -= take;
            if (quantity == 0) return;
        }
        while (quantity > 0)
        {
            int index = planning.GetFreeIndex(definition.sizeX, definition.sizeY);
            if (index < 0) throw new InvalidOperationException("Inventory is full");
            int amount = Math.Min(quantity, maxStack);
            // CreateItem resolves shard_{heroID} as a monster; Pub rewards must use the shard item definition.
            var item = shard
                ? new RObj("shard", amount, 1, false, Vector3.zero, false, ItemType.item) { shardID = shardId }
                : DatabaseAll.instance.CreateItem(id, amount, false, false);
            item.index = index;
            created.Add(item); planning.inventory.Add(item); amounts[item] = amount;
            quantity -= amount;
        }
    }

    public static bool Ready(MainStates state) => state != null && state.all != null && state.all.TryGetValue("main_player", out var player) && player != null &&
        state.playerData?.playerStats != null && DatabaseAll.instance != null;

    public static void Persist(MainStates state)
    {
        if (!state.useSave) return;
        state.playerData.mainPlayer = state.mainPlayer;
        // Same save key and format as MainStates.Save, without logging the entire save to the Console.
        PlayerPrefs.SetString("player", JsonConvert.SerializeObject(state.playerData));
    }
}
