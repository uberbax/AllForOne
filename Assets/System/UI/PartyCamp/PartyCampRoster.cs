using System;
using System.Collections.Generic;

public static class PartyCampRoster
{
    public static bool IsHero(RObj hero) => hero?.dbObj != null && hero.it == ItemType.monster &&
        string.IsNullOrEmpty(hero.shardID) && hero.GetPar("amount") > 0;
    public static bool IsAssigned(RObj hero, int lo, int hi) => IsHero(hero) && hero.GetPar("used_slot") >= lo && hero.GetPar("used_slot") < hi;
    public static bool CanAssign(RObj player, int lo, int hi)
    {
        for (int slot = lo; slot < hi; slot++)
        {
            bool occupied = false;
            foreach (var item in player.inventory)
                if (item != null && item.GetPar("used_slot") == slot) { occupied = true; break; }
            if (!occupied) return true;
        }
        return false;
    }
    public static bool Toggle(MainStates state, RObj hero, out string message)
    {
        message = "";
        if (state == null || !state.HasMain() || !IsHero(hero) || !state.mainPlayer.inventory.Contains(hero))
        { message = "Hero is unavailable"; return false; }
        if (hero.GetPar("used_slot") >= 0)
        { hero.SetPar("used_slot", -1); message = "Hero removed from party"; return true; }
        if (!CanAssign(state.mainPlayer, state.petsLoIndex, state.petsHiIndex))
        { message = "Party is full. Remove a hero first"; return false; }
        // Same assignment path used by UI_AssignPets / equip_skills.
        state.EquipSkills(state.mainPlayer, hero);
        message = "Hero added to party"; return true;
    }
    public static int Compare(RObj a, RObj b)
    {
        int result = b.GetPar("rarity").CompareTo(a.GetPar("rarity"));
        if (result == 0) result = b.GetPar("ascend").CompareTo(a.GetPar("ascend"));
        if (result == 0) result = b.GetPar("level").CompareTo(a.GetPar("level"));
        if (result == 0) result = (b.GetPar("attack") + b.GetPar("magic")).CompareTo(a.GetPar("attack") + a.GetPar("magic"));
        return result != 0 ? result : string.CompareOrdinal(a.RID, b.RID);
    }
    public static bool AutoAssign(MainStates state)
    {
        if (state == null || !state.HasMain()) return false;
        var heroes = new List<RObj>();
        foreach (var item in state.mainPlayer.inventory) if (IsHero(item)) heroes.Add(item);
        heroes.Sort(Compare);
        foreach (var hero in heroes) if (hero.GetPar("used_slot") >= 0) hero.SetPar("used_slot", -1);
        foreach (var hero in heroes)
        {
            if (!CanAssign(state.mainPlayer, state.petsLoIndex, state.petsHiIndex)) break;
            state.EquipSkills(state.mainPlayer, hero);
        }
        return true;
    }
}
