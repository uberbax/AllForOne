Tilemap Water 2D — Built-in Render Pipeline

INSTALL
1. Copy TilemapWater_BuiltIn.shader into any folder inside Assets.
2. Wait until Unity finishes shader compilation.
3. Create Material: Assets > Create > Material.
4. Select shader: Custom > BuiltIn > Tilemap Water 2D.
5. Put the material into Tilemap Renderer > Material.

RECOMMENDED START VALUES
Water Tint: white, or a very light blue tint
Wobble Amount: 0.006–0.018
Wobble Frequency: 2.5–4.5
Wobble Speed: 0.7–1.4
Wave Color: white with Alpha 0.55–0.85
Line Density: 6–10
Line Width: 0.07–0.15
Line Strength: 0.45–0.8
Segment Density: 3–6
Segment Length: 0.25–0.5
Appear / Disappear Speed: 0.6–1.5

NOTES
- This is an unlit sprite shader, not a Surface Shader. That is intentional: a TilemapRenderer with pixel-art sprites should normally preserve the source sprite colors and should not receive 3D Lambert lighting.
- White lines are generated in world space, so they continue across neighboring tiles instead of restarting inside every tile.
- Keep Tilemap Renderer Mode = Chunk for normal use.
- If the very top/bottom of the animated tilemap is clipped, set Detect Chunk Culling Bounds to Manual and give Y a small value such as 0.1–0.2, or reduce Wobble Amount.
- If the water looks too smooth for pixel art, enable Pixel Snap and reduce Line Width.
