# Stylized Hit & Slash

Original animated effects inspired by the supplied visual reference. Built for the project's Built-in renderer; no external textures or post-processing required.

## Prefabs

`StylizedHitSlash/Prefabs` contains 24 prefabs: `Slash_*` and `Hit_*` in Crimson, Ice, Emerald, Scarlet, Void, Silver, Rose, Toxic, Amber, Aether, Flame and Holy colors.

Instantiate a prefab at the impact position. It plays on enable and destroys itself after 1.15 seconds. The long slash is vertical in camera space. Adjust the root scale to fit the target. The effects use additive blending, so dark backgrounds show the glow best.

For pooling, disable **Auto Destroy** in `StylizedHitSlash`; the object disables itself at the end. Re-enable it to replay. Turn off **Face Camera** to control its world orientation yourself.

Each effect combines tapered mesh streaks, an expanding shockwave, needle sparks, elemental fragments, soft embers and an impact flash. Particle systems and materials are editable in the prefab. Changes to generated assets are overwritten if you explicitly rebuild them with **Tools > Effects > Build Stylized Hit and Slash**.

Preview order, left to right, top to bottom: Crimson, Ice, Emerald, Scarlet / Void, Silver, Rose, Toxic / Amber, Aether, Flame, Holy.
