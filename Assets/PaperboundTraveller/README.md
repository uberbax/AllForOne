# Paperbound Traveller

Standalone paper-cut character from Paperbound. Target: **Unity 6, Built-in Render Pipeline**. The asset does not install packages or change project settings on import. No glTF importer, Blender installation, rigging package, Cinemachine or NavMesh is required.

## Quick start

1. Import `PaperboundTraveller-Unity6-BuiltIn.unitypackage` into a Built-in project.
2. Drag `Prefabs/PaperboundTraveller.prefab` into a scene, with its origin just above the floor.
3. Provide a floor collider on a layer included in the motor's **Walkable Layers** (Default by default), a camera tagged **MainCamera**, and scene lighting.
4. Press Play. Use **WASD / arrows**, or click/tap the floor. Movement speed is 1.62 units/s. Keyboard movement is relative to the camera's horizontal heading.

Open `Demo/TravellerDemo.unity` to inspect the full setup. **F** and the demo button fold/unfold the character; the demo also provides automatic walking, sound, and dust/snow toggles. The demo contains a small paper stage, not the book game.

The prefab automatically uses the **Input System** if active, otherwise the legacy input backend if active. It does not change Active Input Handling. With neither backend enabled, use the public input API.

## What is included

- `Models/Traveller.fbx`: the original eight paper meshes, triangulated, with thickness, UVs and separate material slots. No humanoid skeleton; the source animations are transform based.
- `Animations/Idle.anim`: 0.008 m breathing/bobbing and 0.012 rad sway.
- `Animations/Walk.anim`: 0.045 m step bob, 0.04 rad sway, original 12 rad/s cycle.
- `Animations/Fold.anim`: flatten to 3% height in 0.6 s, matching the page-turn effect.
- `Animations/Unfold.anim`: smooth inverse for standalone reuse.
- `Animations/Traveller.controller`: Idle / Walk / Fold / Unfold, `Walking` bool and `Fold` / `Unfold` triggers. No root motion.
- Paper-fiber materials and textures; two-sided paper rendering and shadow casting.
- Lantern point light, animated flicker (0.8 ± 0.08, 9 rad/s), soft transparent camera-facing halo, emissive lantern paper.
- Paper footstep/folding audio, click destination marker.
- Optional `PaperDust` and `PaperSnow` particle prefabs. These were ambient scene effects in the web game; they are intentionally separate from the character.

`PaperboundTraveller.prefab` is the ready-to-play version with CharacterController. `TravellerVisual.prefab` contains just the presentation and effects for an existing motor. Both have fully assigned references; no Inspector wiring is required inside the prefab.

## Hierarchy and integration

The top-level object belongs to locomotion. Animation runs under:

```
Presentation (Animator)
  FoldPivot
    Bob
      Sway
        Facing
          Model
          LanternHalo
          LanternLight
```

Do not animate the top-level collider transform. Foot origin is at Y=0; visible model height is about 0.87 m. X is horizontal, Y is up. The artwork is a paper silhouette; view its front from negative Z as in the demo. Facing is mirrored along local X. Keep the character root's rotation at identity for the supplied world-X facing behavior.

For an existing input/movement system, disable **Read Player Input** on `TravellerMotor` and call:

```csharp
motor.SetMoveInput(new Vector2(horizontal, forward)); // normalized camera-relative input
motor.MoveTo(worldPoint);                            // direct point, no pathfinding
motor.Stop();
motor.Teleport(worldPoint);                          // safely toggles CharacterController
presentation.SetFolded(true);                        // animation and movement lock
presentation.SetFolded(false);
```

For the visual-only prefab, your motor calls `presentation.SetWalking(isMoving)` and `presentation.Face(worldVelocity.x)`. Call `PlayStep()` at your desired cadence. `CanMove` becomes false during folding and unfolding. Your motor must honor it itself when using the visual-only prefab.

Automatic updates run on the main thread. Disabling the motor cancels its destination; its owned marker is cleaned up on destruction. Collider references and animation references are serialized. There are no static gameplay singletons or event subscriptions. The scripts are local components, not a network authority system: only the owner should read input; replicated presentation can be driven separately.

The motor uses CharacterController collision and gravity, not root motion. Click movement goes straight to the destination, so obstacles block it rather than producing a path around them. Optional XZ bounds are disabled on the prefab and configured only in the demo. For project UI, set `BlockPointerInput` while the pointer is over your UI, or route input through your existing controller.

## Rendering

The supplied paper materials use the **Built-in Standard shader**, with zero metallic and smoothness. The geometry has real thickness and back faces; renderers cast two-sided shadows. Lantern halo, click marker, dust and snow use the included transparent unlit shader `Paperbound/SoftSprite`.

The demo uses the Built-in Forward renderer with a directional key light, ambient lighting and a point light for the lantern. No URP/HDRP, SRP renderer assets, Volume components, Shader Graph or post-processing package is required. No manual material conversion is needed. The package does not change the receiving project's render settings.

## Source and verification

The separate source archive contains the Blender model, GLB, FBX, exporter and Unity build script. Unity prefab, scene, animation and controller assets were generated through Editor APIs, not by editing scene/prefab YAML.

See `validation.txt` and `playmode-validation.txt` alongside the delivered package for the exact installed Unity version's checks. Visual parity between Three.js and Unity still depends on the receiving scene's exposure, lighting and color space; the demo supplies a calibrated reference setup.
