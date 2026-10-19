# Blue Shield

Instantiate `Assets/System/Effects/Shield_Blue.prefab` at the character's body center, or parent it to the character. The default radius is 1.2 world units. The shader uses ordinary alpha transparency, with brighter edges, an animated hexagonal pattern and moving energy bands.

`BlueShieldVFX` exposes Radius, Opacity, fade durations and Lifetime in the Inspector. Lifetime = 0 keeps the shield active indefinitely. Call `FadeOut()` when protection ends. A positive Lifetime triggers the fade automatically. Disable Destroy On Finish for pooling; re-enable the object to replay its fade-in.

The prefab is visual only and contains no colliders. The orange capsule in `Shield_Preview.png` demonstrates transparency and is not included in the prefab. Material colors are editable in `Shield_Blue.mat`.
