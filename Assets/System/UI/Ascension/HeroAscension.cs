using System;

public static class HeroAscension
{
    public const string Parameter = "ascend";
    public static int Level(RObj hero) => hero == null ? 0 : Math.Max(0, (int)hero.GetPar(Parameter));
    // Acquiring levels 1..10 costs 5, levels 11..20 costs 10, etc.
    public static int NextCost(int currentLevel) => (int)Math.Min(int.MaxValue, ((long)Math.Max(0, currentLevel) / 10 + 1) * 5);
    public static RObj FindOwned(RObj player, string id)
    {
        if (player?.inventory == null || string.IsNullOrEmpty(id)) return null;
        RObj best = null;
        foreach (var item in player.inventory)
            if (item?.dbObj != null && item.dbObj.ID == id && item.it == ItemType.monster && string.IsNullOrEmpty(item.shardID) &&
                (best == null || Level(item) > Level(best))) best = item;
        return best;
    }
    private static bool Matches(RObj item, string heroId, string itemId) => item?.dbObj != null &&
        ((item.dbObj.ID == "shard" && item.shardID == heroId) || item.dbObj.ID == itemId);
    public static int Shards(RObj player, string heroId)
    {
        if (player?.inventory == null || string.IsNullOrEmpty(heroId)) return 0;
        long total = 0; string itemId = "shard_" + heroId;
        foreach (var item in player.inventory)
            if (Matches(item, heroId, itemId)) total += Math.Max(0, (int)item.GetPar("amount"));
        return (int)Math.Min(int.MaxValue, total);
    }
    public static bool TryAscend(RObj player, RObj hero, out string error)
    {
        error = "";
        if (hero?.dbObj == null || hero.it != ItemType.monster || !string.IsNullOrEmpty(hero.shardID) ||
            player?.inventory == null || !player.inventory.Contains(hero))
        { error = "Hero is not owned"; return false; }
        int level = Level(hero), cost = NextCost(level);
        if (level == int.MaxValue) { error = "Ascension limit reached"; return false; }
        if (Shards(player, hero.dbObj.ID) < cost) { error = "Not enough shards"; return false; }
        // DelItems does not resolve shard_ID or spend across multiple stacks.
        int remaining = cost; string itemId = "shard_" + hero.dbObj.ID;
        for (int i = player.inventory.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var item = player.inventory[i];
            if (!Matches(item, hero.dbObj.ID, itemId)) continue;
            int amount = Math.Max(0, (int)item.GetPar("amount"));
            int take = Math.Min(amount, remaining);
            if (take == 0) continue;
            item.ChangePar("amount", -take);
            remaining -= take;
            if (take == amount) player.inventory.RemoveAt(i);
        }
        hero.ChangePar(Parameter, 1);
        return true;
    }
}
