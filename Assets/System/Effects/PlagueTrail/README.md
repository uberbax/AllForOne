# Plague trail

`PlagueTrail.prefab` follows `MainStates.instance.mainPlayer.main` automatically; an optional Target overrides this. It is placed in Game_Exp_Rumble.

The procedural surface has ragged fungal edges, bruised black/purple soil, faint sickly veins, a spreading reveal and a slow dissolve. It uses a single runtime mesh and the supplied material, with no external textures or per-step GameObjects.

Defaults: XY ground, radius 0.85, spacing 0.32 world units, growth 0.55 s, lifetime 90 s, final fade 8 s, maximum 512 patches (2048 vertices). Oldest patches are replaced when the budget is reached. Lifetime 0 keeps patches until replacement or ClearTrail(). Disabling pauses and hides the effect; destroying releases its runtime mesh.

Only travelled distance emits patches. Standing still emits nothing; jumps over 8 units reset the path anchor instead of drawing a connecting strip. Timings follow game time. A replacement mainPlayer resets the path anchor. There is no combat damage or save-data change.

Sorting Order -5 fits Game_Exp_Rumble's ground tilemaps (-10 to -7), below characters and props. Ground Plane supports XY or XZ; adjust Surface Offset for your ground (XY: -0.025 toward camera, XZ: normally a small positive Y).

`RecordPosition(worldPosition)` can drive an external path; `ClearTrail()` clears the current surface; `SetPreviewTime(seconds)` is for preview authoring. Radius, spacing, lifetime, fade and colors are editable. Capacity is allocated once; recreate the effect to apply a different capacity.

