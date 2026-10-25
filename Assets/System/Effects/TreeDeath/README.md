# Tree death

XDdeath calls TreeDeathEffect.TryPlay for database IDs tree / tree_*. Normal death accounting, on-death skills and drop calls run first. A tree then uses the detached fall/fade instead of the normal fade/fracture, and does not produce blood. Other units keep the original death path.

The effect snapshots SpriteRenderers (including WitheredTree shader property blocks and mirrored/scaled visuals), rotates about the trunk foot away from the attacker, and fades over 1.15 seconds. The original gameplay tree can be removed immediately. The persistent stump has no collider, ObjHolder, HP, combat logic or per-frame updates. Remains last until scene unload, with no save-format change.

The transparent Tree_Stump.png was generated to match the Layer Lab forest style. Tree_Stump_Sprite.asset crops its alpha padding without altering the PNG. Resources/TreeDeath/TreeDeathEffect.prefab includes the stump, fall pivot, SortingGroup and settings. Edit Fall Duration, Fall Angle and Foot Height Fraction on this prefab. The foot offset excludes baked ground shadow in the tree artwork; default is 10% of the visible local bounds height.

Build/review through Tools/Effects/Build Tree Death. The Editor preview and one-shot Play Mode check cover living and withered trees at the tilted Rumble camera orientation.
