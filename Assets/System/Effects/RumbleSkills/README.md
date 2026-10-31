# Rumble skill VFX

Runtime library: Resources/RumbleSkills/VfxLibrary.asset. Edit its stage references in Inspector to substitute effects. These are visual-only adapters; gameplay remains in RumbleSkills/SkillExecutor.

| Skill group | Selected assets |
| --- | --- |
| Water strike / WaterSpout | Hovl water projectile and splash; Epic Toon whirlwind + blue magic circle |
| Venom flask / Venom Zone | Pixel Arsenal acid projectile and impact; Epic Toon green field and aura |
| Crush / Iron whirlwind | Epic Toon body slam and white sword whirlwind |
| Frost Nova / Blizzard | Hovl blue diamond; Epic Toon frost explosion, snowstorm; Pixel Arsenal blue aura |
| Bombard / Blazing Cannonball | Hovl circle bomb and fire projectile; Pixel Arsenal explosive impact and muzzle flash |
| Feather Barrage / Poisonous fan | Hovl dagger and nature arrow; Epic Toon sword hit / Pixel Arsenal acid hit |
| Mysterious Potion / Energy Torrent | Fire, ice or poison projectile/impact palette matching the actual rolled status |
| Dual Pistols / Sniper Rifle | Hovl rapid blue bolt and red laser; Pixel Arsenal bullet sparks and textured laser beam |
| Healing Strike / Tranquility | Epic Toon golden slash/hit, healing burst, blue spherical shield |
| Heal / Light of life | Epic Toon healing crosses/burst/stream; Pixel Arsenal textured gold beam |

Derived prefabs are in Prefabs/. Original package prefabs, textures and materials remain untouched. The adapters preserve their particle textures and authored modules, with gameplay movement/collision/demo scripts removed. Fields and projectiles loop under the game's lifetime; impacts play once. Effects are sized to the configured radius, use the XY camera orientation, and share a bounded runtime pool. Source selection and per-part orientation/scale are recorded in Assets/System/SkillS/Editor/RumbleVfxSelection.json.
