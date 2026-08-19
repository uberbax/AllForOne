# WhoHeroes — source scene transfer manifest

## Source

- Project: `C:\UnityProjects\IdleGame`
- Unity: `6000.5.2f1`
- Scene: `Assets/!Scenes/WhoHeroes 1.unity`
- Prepared package: `C:\UnityProjects\IdleGame\Transfer\WhoHeroes_1_full.unitypackage`
- Package size: `57,926,184` bytes
- SHA-256: `8AA811AB4B86FF5F1334776EF9D8CAEA17F3FB6BCE5238DA97F2B29467D7E81D`

The package was created by Unity `AssetDatabase.ExportPackage` with `IncludeDependencies`. It preserves asset GUIDs and `.meta` data. Do not import it blindly into the partner project; follow the conflicts below.

## Verified scene state

- Scene is saved and not dirty.
- Only one scene is loaded.
- Root GameObjects: `10`.
- GameObjects: `3475` (`975` active in hierarchy).
- Missing Scripts: `0`.
- Unity dependency graph: `862` entries: `838` under `Assets/`, `24` under `Packages/`.
- Export archive: `864` assets, each with `asset.meta` and `pathname`.
- Source data size before package compression: about `159.5 MB`.

Roots:

- active: `camera-main`, `Canvas`, `EventSystem`, `MainLocals`, `Transforms`, `LIBS`;
- inactive but required: `Tavern`, `Castle`, `Tower`, `Expedition`.

Main scene-specific components:

- `SpriterAnim`: 243;
- `BuildingPref`: 52;
- `GUIMapItemPrefab`: 42;
- `SpawnPoint`: 15;
- View/GUI windows, inventory, tasks, wallet, dialogue, building and unit views;
- `_2dxFX_Fire`: 3.

## Package requirements

Direct package dependencies used by scene assets:

- `com.unity.ugui` `2.5.0` — already present in AllForOne;
- `com.unity.2d.sprite` `1.0.0` — already present in AllForOne;
- `com.unity.2d.aseprite` `5.0.3` — absent from AllForOne and required for two `.aseprite` dependencies.

Adding `com.unity.2d.aseprite` to AllForOne requires explicit confirmation before changing `Packages/manifest.json`.

## Existing partner assets: do not overwrite

The following six source assets already exist in AllForOne with the same GUID. Five have different content in the partner project and must be excluded during import:

- `Assets/2DxFX/Scripts/_2dxFX_Fire.cs`;
- `Assets/2DxFX/Resources/_2dxFX_FireTXT.jpg` (content is identical, still no need to import);
- `Assets/TextMesh Pro/Shaders/TMP_SDF.shader`;
- `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader`;
- `Assets/TextMesh Pro/Shaders/TMPro.cginc`;
- `Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc`.

Because the GUIDs match, scene references resolve to the existing AllForOne assets when these files are omitted.

## Legacy code boundary

The package contains 37 directly referenced scripts from `Assets/!Scripts/NEW` plus `_2dxFX_Fire.cs`. The View/GUI scripts depend on the old runtime, including `GameController`, `PlayerController`, `DataBase`, `DynamicData`, `DynamicDataController`, `Mediator`, `Localization`, `ModelLIB`, `GameBuilding`, `GameUnit`, `ResourceList` and related state/DTO classes.

These old Manager/Controller/State/DTO systems must not be imported automatically. Preserve the visual prefab/Inspector wiring, then reconnect it to Minimus through the existing config, `Obj/RObj`, `MainCycle`, events and UI binding mechanisms.

The source scene itself contains no `GameController`, `PlayerController`, `DataBase` or `Mediator` component, so it is not a standalone bootstrap scene.

## SpriterAnim conflict

- Source script GUID: `0bfc35d88d8ca5348b3d720cc2a6a183`.
- Partner system script: `Assets/System/Utils/SpriterAnim.cs`.
- Partner GUID: `56c82c02ba088cd4fb43123b450340ba`.
- Both define global `SpriterAnim` and `UnoAnim`; importing both scripts unchanged creates a type collision.
- The partner implementation is newer and should remain authoritative.

During the actual transfer, remap the 243 source `SpriterAnim` components to the partner implementation with a Unity Editor API migration that copies compatible serialized fields. This is a mass component edit and requires explicit confirmation immediately before execution. Do not rewrite the scene YAML manually.

## Transfer order

1. Install/confirm the required Aseprite package in AllForOne.
2. Import project art, prefab, audio, material and animation dependencies while excluding the six existing partner assets.
3. Put all game-specific assets under `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/` without copying `Assets/System`.
4. Resolve `SpriterAnim` through an Editor API migration, preserving serialized animation data.
5. Keep the source View/GUI wiring as reference; replace old runtime dependencies with Minimus-backed project-local adapters only where existing system UI/config/XD cannot express the behavior.
6. Create the project scene from the selected partner demo bootstrap and move the source world/UI blocks into it; do not replace the system layer.
7. Verify compile-clean, Missing Scripts, Inspector references, `PARSE_ENDED`, `mainPlayer`, `Obj/RObj`, ResourceHolder/XD, UI bindings and Play Mode.
