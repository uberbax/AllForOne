# Hero details

Prefab: `Resources/UI_HeroDetails.prefab`. Instantiate under an existing Canvas, then call `HeroDetailsController.Show(hero)`. Its root ObjHolder has noTrack=true. You can also assign `ObjHolder.obj` directly and activate the window. Child HeroDetailsPart components resolve the nearest parent ObjHolder, including inactive parents, and read its current object on each refresh. No child stores a separate hero reference.

The black/white/blue profile theme uses the existing Knights Quest font and profile frame. Hero portraits come from ResourceHolder.avas, skill icons from ResourceHolder.skills, currency icons from ResourceHolder.GetIcon. For the 20 new Rumble skills, Resources/HeroSkillIcons contains fallback icons rendered from their adapted VFX. ResourceHolder.skills has priority. A skill with neither icon uses an explicit 01/02 placeholder. No separate hero portraits are generated.

The two skills are dbObj.skills[0] (Heroes.skillbasic) and dbObj.skills[1] (first Heroes.skillother). The scroll list alternates:

- Level 10: first skill, rank 1
- Level 20: second skill, rank 1
- Level 30: first skill, rank 2
- Level 40: second skill, rank 2

The prefab has milestones through level 200. Ranks are derived from the current hero level; this is the progression display, and it does not change combat formulas or add a separate skill-rank save field. There is no new UI level cap by default (maxLevel=0).

Upgrade uses UpgradeSystem.GetPrice(hero, "upgrade"), merges repeated currency keys, spends through MainStates.DelItems, increases hero level through ChangePar, sends evt_full_upgrade, refreshes the UI and saves through the existing save path when useSave is enabled. Only an owned, non-shard monster can be upgraded. MAX buys affordable levels sequentially, recalculating cost after each purchase; maxLevelsPerClick defaults to 100. The cost row shows the next level's price. Three currency slots are included.

HeroDetailsLayout fits a 1080x1920 design into the containing Canvas. No Canvas or EventSystem is duplicated by the prefab.

Rebuild using Tools/UI/Hero Details/Build Prefab. The builder creates a preview scene and does not alter the currently open gameplay scene or any player save. The existing scene is not automatically modified to open this window.
