# POLY-FantasyCharacter — Documentation

Animpic Studio

Fantasy Character models with customization data, ready-made prefabs and a character studio.

## Installation and first use

Import this product’s .unitypackage into your project and allow Unity to finish installing the listed packages and compiling scripts. Content is installed in `Assets/Animpic Studio/POLY-FantasyCharacter`. Keep the product folder and its .meta files together. No other Animpic product is required. Back up an existing project before importing an update.

Open the supplied character studio scene and enter Play mode to use its interface. For editing without Play mode, open Tools > Animpic Studio > Characters > Fantasy Character. Select the scene character before changing parts or palettes. Use the supplied character prefabs and catalogs together. Save scene/prefab changes deliberately; changes made only during Play mode are not a saved customization. See the included character guides for supported combinations and anatomy-specific restrictions.

## Included scenes

- `Scenes/CharacterStudio.unity`

## Folder guide

Meshes contains model sources; Prefabs contains reusable configured objects; Materials and Textures contain their appearance assets. Runtime contains gameplay code and data; Editor contains editor-only tools; UI and Animations contain the corresponding resources where supplied. Scenes contains demonstration scenes and their local data. Dependencies contains this product’s isolated supporting components. Do not replace those copies with another pack’s folders.

## Unity and rendering compatibility

The current working revision was validated in Unity 2022.3.3f1. Checks covered local asset dependencies, prefab scripts and mesh references, shader compilation, and opening and briefly running every included scene using Built-in rendering. The original model bytes, import settings and authored transforms were preserved. This does not establish a complete URP/HDRP visual compatibility matrix or a fresh independent import of the distribution archive. Use the Unity packages matching the target editor; do not force an older local Unity UI or render-pipeline package into Unity 2022.3.

### Unity package versions

- `com.unity.burst`: `1.6.5`
- `com.unity.mathematics`: `1.2.6`
- `com.unity.postprocessing`: `3.2.2`
- `com.unity.render-pipelines.core`: `14.0.8`
- `com.unity.render-pipelines.high-definition`: `14.0.8`
- `com.unity.render-pipelines.high-definition-config`: `14.0.8`
- `com.unity.render-pipelines.universal`: `14.0.8`
- `com.unity.searcher`: `4.9.1`
- `com.unity.shadergraph`: `14.0.8`
- `com.unity.ugui`: `1.0.0`
- `com.unity.visualeffectgraph`: `14.0.8`

These dependencies are declared in the .unitypackage manifest. Unity Registry access may be required if the packages are not cached. No absolute local package path is required. The separate offline working project is not part of this store archive.

## Detailed guides and notices

- [GEOMETRY_AUDIT_RU.md](Documentation/GEOMETRY_AUDIT_RU.md)
- [USER_GUIDE_RU.md](Documentation/USER_GUIDE_RU.md)
- [ATTRIBUTION.md](Studio/Branding/ATTRIBUTION.md)
- [BRAND_REFERENCE.md](Studio/Branding/BRAND_REFERENCE.md)
- [Inter-OFL.txt](Studio/Branding/Licenses/Inter-OFL.txt)
- [Manrope-OFL.txt](Studio/Branding/Licenses/Manrope-OFL.txt)
- [MALE_GEOMETRY_AUDIT_RU.md](Studio/Editor/Audit/MALE_GEOMETRY_AUDIT_RU.md)
- [USER_GUIDE_RU.md](Studio/USER_GUIDE_RU.md)

Some supporting documents record earlier layouts or validation sessions. This README defines the current installation root; the folder-change log records this packaging revision. Third-party license texts included in this product must remain with their components.

## Support

Use the support contact on this product’s Animpic Studio store listing. Include the product version, Unity version, active render pipeline, reproduction steps and relevant Console errors. A local developer disk path is not a support requirement.
