using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reuse the project's skill icons for the new IDs; no scene wiring is required.</summary>
public static class RumbleSkillResources
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
        Install(ResourceHolder.instance);
    }
    private static void SceneLoaded(Scene scene,LoadSceneMode mode) => Install(ResourceHolder.instance);
    public static void Install(ResourceHolder resources)
    {
        if(resources==null||resources.skills==null)return;
        Add(resources,"water_strike","ice_arrow","basic_magic");
        Add(resources,"waterspout","whirl","basic_aoe");
        Add(resources,"venom_flask","basic_poison","basic_magic");
        Add(resources,"venom_zone","poison","basic_aoe");
        Add(resources,"crush","basic_melee","basic_aoe");
        Add(resources,"iron_whirlwind","whirl","basic_melee");
        Add(resources,"frost_nova","ice_arrow","basic_magic");
        Add(resources,"blizzard","frozen","basic_aoe");
        Add(resources,"bombard","basic_range","basic_aoe");
        Add(resources,"blazing_cannonball","fireball","basic_aoe");
        Add(resources,"feather_barrage","bow_skill","basic_range");
        Add(resources,"poisonous_fan","basic_poison","basic_range");
        Add(resources,"mysterious_potion","cure","basic_magic");
        Add(resources,"energy_torrent","basic_magic","basic_range");
        Add(resources,"dual_pistols","basic_range","bow_skill");
        Add(resources,"sniper_rifle","aim_strike","basic_range");
        Add(resources,"healing_strike","drain_strike","basic_melee");
        Add(resources,"tranquility","shield_spell","basic_heal");
        Add(resources,"heal","basic_heal","cure");
        Add(resources,"light_of_life","basic_heal","light_arrow");
        Add(resources,"rumble_poison","poison","basic_poison");
        Add(resources,"rumble_burn","burn","fireball");
        Add(resources,"rumble_frostbite","frozen","ice_arrow");
        Add(resources,"rumble_chilled","frozen","ice_arrow");
        Add(resources,"rumble_invincible","shield_spell","basic_heal");
    }
    private static void Add(ResourceHolder resources,string id,string preferred,string fallback)
    {
        if(resources.skills.TryGetValue(id,out var existing)&&existing!=null)return;
        if((resources.skills.TryGetValue(preferred,out var sprite)&&sprite!=null)||
           (resources.skills.TryGetValue(fallback,out sprite)&&sprite!=null))resources.skills[id]=sprite;
        else if(resources.defaultTaskIcon!=null)resources.skills[id]=resources.defaultTaskIcon;
    }
}
