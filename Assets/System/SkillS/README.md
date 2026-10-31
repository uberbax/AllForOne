# Rumble skills

The 20 casts from skills.txt are dispatched from SkillExecutor.ExecuteSkill. Existing skills keep their original path. No prefab/scene wiring is required. New icons reuse ResourceHolder.skills icons and keep any icon already assigned to the new ID.

Config: Assets/StreamingAssets/Config_exp_rumble.xlsx, SKILLS2 rows 112–136. DISPLAYNAME and DESCRIPTION are editor-facing metadata; the existing loader ignores these columns. Assign a cast ID through a hero's SKILLBASIC, SKILLULT or SKILLOTHER as usual. Hero assignments are deliberately left to the game's roster design.

| ID | Behavior |
| --- | --- |
| water_strike | Moving water pillar, hits each intersected enemy once |
| waterspout | Target-centered 3-second area, pulls enemies and damages every 0.5 seconds |
| venom_flask | Arcing flask, 3 bounces to distinct enemies, applies poison |
| venom_zone | Target-centered damage field for 5 seconds |
| crush | Leap to target, area impact and brief airborne crowd control |
| iron_whirlwind | 3-second spin around caster, damage and pull |
| frost_nova | Ice sphere with area explosion |
| blizzard | 5-second ice storm, area damage and frostbite |
| bombard | Arcing cannonball with area impact |
| blazing_cannonball | 3 explosive cannonballs with burn |
| feather_barrage | 2 consecutive piercing feathers |
| poisonous_fan | Blades toward up to 5 enemies, poison, leap toward nearest other ally |
| mysterious_potion | Random burn/frostbite/poison potion |
| energy_torrent | 8 rapidly thrown random potions |
| dual_pistols | 4-shot bullet burst |
| sniper_rifle | Single high-damage long-range shot |
| healing_strike | 3 attacks; each hit heals nearby allies |
| tranquility | 4-second invincibility for allies around caster |
| heal | Single-target heal, prioritizes lowest health ratio |
| light_of_life | Ray and area healing around selected ally |

Balance uses normal SKILLS2 fields: ATTACK_PRC/MAGIC_PRC, HEALTH, RANGE, AOE, TIME, DMG_EVERY, AMOUNT, DT, SPEED, TRAVEL, RICOCHET, COOLDOWN, MANACOST and TARGETS. Unspecified values in the text have starter balance. Custom knobs use **EXTRA_PARS**, never PARS (PARS routes existing CastSkill into its unique/summoning path): pull_speed, bounce_range, leap_range, leap_ms, heal_flat, heal_prc. The latter is a percentage of caster magic added to healing.

Native statuses rumble_poison, rumble_burn and rumble_frostbite tick through MainStates.HandleCds. Reapplication refreshes duration instead of stacking. Frostbite also applies rumble_chilled (speed -1). Tranquility uses rumble_invincible (immortal +1), expires through the same native timer. Damage and healing go through MainStates to preserve mitigation, shield, death, loot and Changed notifications.

Coroutine effects belong to the casting unit and stop when it is disabled/destroyed. Movement suspension is reference-counted and restored on interruption. Data-only projectiles/statuses are removed from the global world registry immediately; active skills and buffs retain their own references. Area target searches use a reusable buffer at 10 Hz, while pull movement updates every frame. Effects are lightweight runtime lines/rings, with no external VFX dependency.

Tools/Skills/Validate Rumble Skills loads the actual Excel through ExcelHelper, ConfigLoader and DatabaseAll and runs isolated native data/damage/status/expiry checks without writing player saves. Full timed choreography still needs a Play Mode gameplay pass.
