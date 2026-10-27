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

    public void BuildPool(IEnumerable<FormatHero> rows, IDictionary<string, Obj> definitions)
    {
        Pool.Clear();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (rows == null || definitions == null) return;
        foreach (var row in rows)
        {
            if (row == null || !IsEligibleId(row.monsterName)) continue;
            string baseId = row.monsterName.Trim().ToLowerInvariant();
            string id = row.level > 1 ? baseId + row.level.ToString(CultureInfo.InvariantCulture) : baseId;
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
            if (hero == null || Offers.Contains(hero)) break;
            Offers.Add(hero);
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
            Offers.Add(choices[index]); choices.RemoveAt(index);
        }
        NextRefreshTicks = utcNow.AddMinutes(Math.Max(1, refreshMinutes)).Ticks;
        for (int i = 0; i < 3; i++) SetStat(data, "pub_offer_" + i, 1, i < Offers.Count ? Offers[i].Id : "");
        SetStat(data, TimerKey, 0, NextRefreshTicks.ToString(CultureInfo.InvariantCulture));
    }

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
        long total = Math.Min(int.MaxValue, (long)previous + count);
        int interval = Math.Max(1, bonusEvery);
        bonusCount = (int)(total / interval) - previous / interval;
        for (int i = 0; i < count + bonusCount; i++) rewards.Add(Offers[random.Next(Offers.Count)]);
        if (rewards.Any(h => !IsEligibleId(h.Id) || !DatabaseAll.instance.heroes.ContainsKey(h.Id)))
        { error = "Hero data changed. Refresh the offers"; rewards.Clear(); return false; }

        // Plan free inventory cells without modifying the real inventory or spending currency.
        var planning = new RObj { RID = state.mainPlayer.RID, inventory = new List<RObj>(state.mainPlayer.inventory) };
        var indices = new List<int>();
        foreach (var hero in rewards)
        {
            int index = planning.GetFreeIndex(hero.Definition.sizeX, hero.Definition.sizeY);
            if (index < 0) { error = "Inventory is full"; rewards.Clear(); return false; }
            var placeholder = new RObj { dbObj = hero.Definition, index = index };
            placeholder.upgradePars["used_slot"] = -1;
            planning.inventory.Add(placeholder); indices.Add(index);
        }

        // Creation also registers skills/traits in MainStates.all. Roll back new registrations on invalid config.
        var registrations = new HashSet<string>(state.all.Keys, StringComparer.Ordinal);
        var created = new List<RObj>();
        try
        {
            foreach (var hero in rewards)
            {
                var recruited = DatabaseAll.instance.CreateMonster(hero.Id, 1, false, false);
                if (hero.Rarity > 0) recruited.SetPar("rarity", hero.Rarity);
                created.Add(recruited);
            }
        }
        catch (Exception exception)
        {
            foreach (var id in state.all.Keys.Where(id => !registrations.Contains(id)).ToArray()) state.all.Remove(id);
            Debug.LogException(exception); error = "Cannot recruit this hero. Check Heroes/Skills config"; rewards.Clear(); return false;
        }
        if (Currency(state.mainPlayer, currency) < cost)
        {
            foreach (var id in state.all.Keys.Where(id => !registrations.Contains(id)).ToArray()) state.all.Remove(id);
            error = "Not enough " + currency; rewards.Clear(); return false;
        }
        Spend(state.mainPlayer, currency, (int)cost);
        for (int i = 0; i < created.Count; i++)
        {
            // Heroes are individual companions, matching MainSwitcher's monster-inventory convention.
            created[i].owner = state.mainPlayer; created[i].index = indices[i];
            state.mainPlayer.inventory.Add(created[i]);
        }
        TotalRecruits = (int)total; SetStat(state.playerData, TotalKey, TotalRecruits);
        Persist(state); return true;
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
