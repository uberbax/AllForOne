# Loot rarity beams

Runtime hook: XDdrop attaches LootRarityBeam after a normal drop lands, after RandomizeItemStats has selected the rarity. The beam is a child of the world loot root. Pickup paths XDpick.Pick, MainStates.PickDrop and MainStates.AddItem remove it immediately. Destroying the loot also destroys its beam.

Classification follows the existing item config: slot < 0 with subtype currency or none is a resource, so gold/res1/res2/exp are excluded. Equipment and other non-resource types (usable, potion, adorn, etc.) qualify. Beam selection uses item.GetPar("rarity"), clamped to supported rarities 0..5; display color uses ResourceHolder.rareColors.

| Rarity | Sprite source | Display color |
|---|---|---|
| 0 Common | Glow-White-01 (generated) | White |
| 1 Uncommon | Glow-Green-01 | Green |
| 2 Rare | Glow-Blue-01 | Blue |
| 3 Epic | Glow-Violet-01 | Violet |
| 4 Legendary | Glow-Orange-01 | UI rarity gold/yellow |
| 5 Mythic | Glow-Red-01 (generated) | Red |

Prefabs are in Resources/LootBeams and loaded lazily. Generated PNG originals are retained; cropped Sprite assets remove transparent padding via Unity's sprite rect without rewriting PNG pixels. Existing four PNG/import settings are unchanged.

Each prefab contains a 3.5-unit column, additive ground glow and SortingGroup (-4, sort at root) to show above Game_Exp_Rumble ground while staying beneath actors. Parent loot scale is cancelled, preventing different item icon sizes from affecting beam size. Shared materials, property blocks and six cached prefab references are used; no per-frame searches for loot.

Rebuild with Tools/Effects/Build Loot Rarity Beams. Change prefab column/glow transforms or Fade In Duration for visual tuning. All files are local to System/Art/Beams.
