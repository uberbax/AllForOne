# POLY_FantasyMale_SK — аудит геометрии и каталог кастомизации

Источник: `D:/UnityProjects/AssetsView/Assets/Animpic Studio/POLY-FantasyCharacter/Meshes/POLY_FantasyMale_SK.fbx` и одноимённый prefab из `Prefabs`. Оба содержат 65 meshes с исходными именами Cube_###. Исходный FBX/prefab и открытый Blender аудитом не изменялись.

Применён skill `blender-asset-validation`, прочитаны `local-blender.md` и `quality-gates.md`. Blender 5.1 запущен отдельными background процессами с factory startup, disable-autoexec и python-exit-code 1. Все материалы для визуальной проверки в копии связаны с штатной текстурой T_Main_A.

## Выводы и правила покрытия

- **observation:** 4 коротких торса, 8 брюк (4 фасона × 2 длины), 6 пар обуви, 8 перчаток, 23 meshes частей рук, 2 отдельные голые кисти, 1 пара голых стоп, 1 голова, 6 meshes волос, 3 варианта бороды и 3 пары бровей.
- **observation:** бороды — Cube_023 (полная), Cube_087 (борода с открытым контуром подбородка), Cube_092 (усы и эспаньолка в одном mesh). Выбирать одну либо выключать все; голова остаётся включённой. Волосы и брови независимы от бороды. Отдельных масок скрытия головы под бородой не требуется.
- **observation:** волосы имеют три пары full/cropped: A 008/170; B 012/168; C 090/169. Это реальные варианты формы. У 170 все 77 vertices совпадают с 008; у 168 совпадают 43/56 с 012, оставшиеся изменены до 1.58 см; у 169 совпадают 107/113 с 090, оставшиеся изменены до 0.78 см. Cropped удаляет задние области и уплощает часть объёма. Это не автоматически определённые LOD.
- **observation:** капюшона, плаща, длинной туники, шлема и второго head mesh в данном FBX/prefab нет. На основании только этого источника нельзя включить такие части в каталог. Декоративные детали одежды встроены в meshes одежды.
- **observation:** высокая обувь A–D: Cube_011,020,182,116 → короткие брюки. Низкая E/F: Cube_193,195 → длинные брюки. Босиком → длинные брюки + Cube_135. Любая обувь выключает Cube_135: это голые стопы высотой примерно 0.111 м, не голени целиком.
- **observation:** полная рука содержит голую кисть; разделённая рука = upper + forearm, причём forearm также содержит кисть. Нельзя включать full вместе с upper/forearm или добавлять отдельную bare hand поверх встроенной.
- **observation:** все glove A–D занимают всё предплечье от общего локтевого среза. При надевании перчатки включить upper + glove, выключить full и forearm этой стороны. A/C/D закрывают пальцы, B требует companion с открытыми пальцами.

## Дефекты источника и производные части

- **defect:** Cube_129 — левое предплечье D — содержит две совпадающие копии (160 vertices, 148 quads, 296 triangles). После очистки остаётся 148 triangles. Bone weights у всех 80 пар соответствующих vertices совпадают точно. UV шести quad манжеты различаются: выбирать вторую копию (polys86–91), которая по цвету совпадает с full Cube_123; остальные duplicate pairs одинаковы. Исходный файл менять не требуется.
- **defect:** отдельного правого предплечья D в source нет. `MaleRightDForearmMask.json` точно выделяет его из full Cube_145: 72 source quads → 144 triangles. Эти внешние поверхности совпадают с зеркальным левым предплечьем в пределах 0.016 мм. Отдельный левый forearm имеет ещё две внутренние заглушки локтевого торца, которых в full-right нет; они в generated mask не добавляются.
- **observation:** у производного правого предплечья все 6 vertices открытого локтевого края совпадают с upper Cube_156 **точно и по позиции, и по bone weights**. Поэтому upper + generated forearm воспроизводит цельную руку без новой геометрии. Рендер `derived_d_forearms.png` просмотрен.
- **observation:** `MaleGloveBFingerMask.json` содержит по 26 quads → 52 triangles на кисть (source L Cube_026, R Cube_074). Координаты в Blender world, метры: X лево/право, Y вперёд/назад, Z вверх. При Unity generation сохранить source vertices/UV/bind poses/bone weights/blend shapes и менять только triangle lists. Маска убирает ладонь, сохраняя правильные открытые пальцы B. Рендер `male_glove_b_comparison.png` просмотрен: полный hand и fingers-only дают одинаковые видимые пальцы.

## Техническая проверка skill

- **gate:** `skill_metrics.json` → hard_gate_pass=true. 65 meshes, 26 516 evaluated triangles, 15 690 vertices, 1 material, 89 bones. Нет invalid vertices или missing material faces.
- **warning:** 5 degenerate faces и 5 zero-length edges: Cube_082 (2), Cube_090 (2), Cube_121 (1). Они не ремонтировались автоматически вне задачи кастомизации.
- **observation:** open boundaries/раздельные components здесь обусловлены модульной одеждой, волосами, UV/normal швами и low-poly моделированием; требования печатной watertight-модели неприменимы.
- **observation:** выполнен независимый статический round-trip контроль representative assembly из 9 source meshes. `static_blend_metrics.json` и `static_glb_metrics.json`: оба hard_gate_pass=true, 4057 triangles, 9 сохранённых object names, 1 material, bounds 1.848498 × 0.400553 × 1.889973 м совпадают. GLB vertices увеличились с2482 до8349 из-за UV/normal splits. Source/GLB multiview sheets просмотрены и согласуются.
- **not_applicable:** временный `male_static_reference.glb` служит только статической проверке, не новым игровым deliverable. Анимация и изменения rig не экспортировались в этот static snapshot. Рабочая кастомизация должна использовать исходный Unity skinning и проверяться с Animator в Unity.

## Данные и изображения

`male_catalogue_mapping.json` — полный semantic catalogue с исходными именами. `mesh_inventory.json` — bounds, vertex/polygon counts, bone weights и bone vertex counts всех 65 meshes. `topology_audit.json`, `compatibility_detail.json`, `right_d_forearm_analysis.json` — измерения проблемных частей и совместимости.

Проверенные изображения: `head_hair_beards.png`, `torsos.png`, `gloves.png`, `derived_d_forearms.png`, `male_glove_b_comparison.png`, `reference_views/perspective.png`, `reference_views/contact_sheet.png`, `static_glb_views/contact_sheet.png`. Дополнительно созданы `left_arms.png` и `pants_boots.png`; их формы классифицированы по геометрии и соответствиям, без отдельного открытия этих двух файлов в текущем проходе.

Финальные mask inputs: `MaleGloveBFingerMask.json` и `MaleRightDForearmMask.json`. Имена в masks — left/right.faces[].vertices[{x,y,z}], source world coordinates; source indices после Unity import могут измениться, поэтому сопоставлять позиции с учётом осей/масштаба.

## Полный каталог

| Source mesh | Назначение | Vertices | Polygons | Z min–max, м |
|---|---|---:|---:|---:|
| `Cube_002` | Arm A left full | 223 | 208 | 1.2074–1.5491 |
| `Cube_003` | Pants A short | 828 | 618 | 0.3299–1.1335 |
| `Cube_004` | Pants B short | 956 | 802 | 0.3358–1.1357 |
| `Cube_006` | Arm A right full | 223 | 208 | 1.2074–1.5491 |
| `Cube_008` | Hair A full | 126 | 221 | 1.8122–1.8900 |
| `Cube_010` | Torso A | 283 | 254 | 1.0832–1.6166 |
| `Cube_011` | Footwear A high | 484 | 419 | 0.0000–0.3547 |
| `Cube_012` | Hair B full | 165 | 246 | 1.8011–1.9085 |
| `Cube_013` | Glove A right | 98 | 96 | 1.2023–1.4310 |
| `Cube_015` | Pants A long | 804 | 594 | 0.1124–1.1335 |
| `Cube_016` | Arm A left upper | 78 | 87 | 1.3187–1.5491 |
| `Cube_017` | Glove A left | 98 | 96 | 1.2023–1.4310 |
| `Cube_018` | Arm A right upper | 78 | 87 | 1.3187–1.5491 |
| `Cube_020` | Footwear B high | 270 | 260 | 0.0050–0.3437 |
| `Cube_023` | Beard A / Full beard | 90 | 88 | 1.5730–1.7456 |
| `Cube_026` | Bare hand L | 56 | 52 | 1.2074–1.3350 |
| `Cube_063` | Pants B long | 802 | 690 | 0.1133–1.1357 |
| `Cube_074` | Bare hand R | 56 | 52 | 1.2074–1.3350 |
| `Cube_078` | Torso B | 191 | 172 | 1.0782–1.5824 |
| `Cube_082` | Torso C | 825 | 655 | 1.0782–1.6299 |
| `Cube_087` | Beard B / Outlined beard | 89 | 78 | 1.5961–1.7456 |
| `Cube_088` | Arm C left full | 161 | 156 | 1.2074–1.5507 |
| `Cube_090` | Hair C full | 305 | 422 | 1.6472–1.9051 |
| `Cube_092` | Beard C / Mustache and goatee | 29 | 20 | 1.6215–1.6820 |
| `Cube_094` | Arm B right full | 167 | 168 | 1.2074–1.5636 |
| `Cube_095` | Arm B left full | 167 | 168 | 1.2074–1.5636 |
| `Cube_096` | Arm B right upper | 93 | 100 | 1.3206–1.5636 |
| `Cube_097` | Arm B left upper | 93 | 100 | 1.3206–1.5636 |
| `Cube_099` | Arm C right full | 161 | 156 | 1.2074–1.5507 |
| `Cube_100` | Arm C left upper | 75 | 80 | 1.3132–1.5507 |
| `Cube_103` | Glove B left fingerless | 66 | 64 | 1.2489–1.4310 |
| `Cube_104` | Glove B right fingerless | 66 | 64 | 1.2489–1.4310 |
| `Cube_111` | Arm C right upper | 75 | 80 | 1.3132–1.5507 |
| `Cube_112` | Arm C left forearm | 110 | 104 | 1.2074–1.4271 |
| `Cube_113` | Arm C right forearm | 110 | 104 | 1.2074–1.4271 |
| `Cube_116` | Footwear D high | 614 | 624 | 0.0050–0.3549 |
| `Cube_118` | Pants D long | 596 | 529 | 0.1128–1.1357 |
| `Cube_121` | Torso D | 294 | 283 | 1.0832–1.5725 |
| `Cube_123` | Arm D left full | 143 | 140 | 1.2074–1.5476 |
| `Cube_129` | Arm D left forearm | 160 | 148 | 1.2074–1.4271 |
| `Cube_130` | Arm D left upper | 69 | 70 | 1.3132–1.5476 |
| `Cube_132` | Pants C short | 888 | 765 | 0.3352–1.1461 |
| `Cube_133` | Pants C long | 882 | 753 | 0.1128–1.1461 |
| `Cube_134` | Arm A left forearm | 150 | 125 | 1.2074–1.4209 |
| `Cube_135` | Bare feet | 46 | 40 | 0.0046–0.1160 |
| `Cube_137` | Arm A right forearm | 150 | 125 | 1.2074–1.4209 |
| `Cube_140` | Arm B left forearm | 80 | 72 | 1.2074–1.4190 |
| `Cube_141` | Arm B right forearm | 80 | 72 | 1.2074–1.4190 |
| `Cube_144` | Head | 203 | 200 | 1.5398–1.8803 |
| `Cube_145` | Arm D right full | 143 | 140 | 1.2074–1.5476 |
| `Cube_156` | Arm D right upper | 69 | 70 | 1.3132–1.5476 |
| `Cube_168` | Hair B cropped | 56 | 74 | 1.8122–1.8926 |
| `Cube_169` | Hair C cropped | 113 | 138 | 1.7440–1.8944 |
| `Cube_170` | Hair A cropped | 77 | 127 | 1.8122–1.8900 |
| `Cube_175` | Glove D left | 168 | 142 | 1.2023–1.4293 |
| `Cube_176` | Glove D right | 168 | 142 | 1.2023–1.4293 |
| `Cube_182` | Footwear C high | 464 | 390 | 0.0050–0.3437 |
| `Cube_186` | Glove C left | 137 | 139 | 1.2023–1.4310 |
| `Cube_189` | Glove C right | 137 | 139 | 1.2023–1.4310 |
| `Cube_193` | Footwear E low | 472 | 558 | 0.0049–0.1495 |
| `Cube_195` | Footwear F low | 216 | 308 | 0.0050–0.1268 |
| `Cube_202` | Pants D short | 552 | 483 | 0.3352–1.1357 |
| `Cube_207` | Brows 1 | 22 | 16 | 1.7753–1.7957 |
| `Cube_229` | Brows 2 | 22 | 24 | 1.7753–1.7957 |
| `Cube_232` | Brows 3 | 18 | 12 | 1.7753–1.7944 |
