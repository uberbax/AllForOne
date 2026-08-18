BRATANDRONIKPROJECTS — AGENTS.MD

BratAndronikProjects — папка игровых проектов внутри общего Unity-проекта партнёра. Каждый проект хранится здесь целиком, но строится на существующей системе партнёра. Цель — максимум готовой системы, минимум собственного кода.

1. ОБЯЗАТЕЛЬНЫЕ ПРАВИЛА
• Существующие методы и их поведение изменять запрещено. Системные классы не редактировать, кроме добавления нового метода как абсолютного последнего резерва по п. 5.
• DTO, state, gameplay-правила и системная логика используют существующие модели и сервисы партнёра. Не создавать параллельные элементы ядра, если задача уже покрыта системой.
• Каждый проект целиком хранится в BratAndronikProjects/<ProjectName>/. Файлы ядра Assets/System не копировать, не перемещать и не переименовывать; demo-сцену разрешено дублировать в папку проекта как основу рабочей сцены.

Приоритет решения задачи:
1) Если уже есть рабочий локальный GUI/View-код проекта или BratAndronikLIB — использовать его без переписывания.
2) Если рабочего локального GUI/View-решения нет или задача новая — сначала собрать её готовыми средствами системы партнёра: Inspector, Prefab, Excel/METACONF, конфигурация, события, DYNAMIC_ID и готовые компоненты/XD.
3) Если готовой настройки недостаточно — использовать существующий класс/API системы партнёра.
4) Новый минимальный собственный скрипт в BratAndronikProjects/<ProjectName>/ — только если готовые средства не покрывают задачу.
5) Новый метод в существующем системном скрипте — только крайний резерв, если отдельным расширением задачу решить невозможно; крайне не рекомендуется.

Перед реализацией агент обязан:
• найти существующие классы, компоненты, события, таблицы и prefab, которые решают задачу;
• определить, что именно будет переиспользовано;
• если нужен новый скрипт — указать, почему готовые средства не подходят и оставить ему минимальную ответственность;
• после изменения проверить, что системное ядро не изменено и не появилась дублирующая архитектура; если применён п. 5 — изменение ограничено новым методом и не меняет существующее поведение.

2. АРХИТЕКТУРА

Основное собственное ядро находится в Assets/System и собирается в assembly Minimus. В Assets/System также есть сторонние и demo-части, поэтому не каждый файл этой папки относится к ядру.

Система data-driven и строится на композиции, а не на отдельной иерархии классов для каждого типа сущности.

Поток данных:
Config_*.xlsx + loc.xlsx
↓
ConfigLoader
↓
DatabaseAll
↓
Obj — шаблон сущности
↓
RObj — runtime-состояние экземпляра
↓
ObjHolder — связь RObj с GameObject
↓
vis_main (основной визуальный prefab сущности) + XD-модули (подключаемые prefab-компоненты поведения)
↓
combat / movement / UI / loot / tasks / events

Основные сервисы:
• MainStates — runtime-фасад и реестр объектов.
• ResourceHolder — Inspector-реестр prefab, sprite, VFX, звуков и XD-модулей.
• EventManager — строковая шина событий.
• ModelStatistics — статистика, условия и задания.
• SkillExecutor — выполнение способностей.
• TimeManager — непрерывное и дискретное время.
• PositionSetter — grid, карта, fog и pathfinding.
• MainCycle* — правила конкретной игры (например MainCycleGame, MainCycleSword, MainCycleStone).

3. КЛЮЧЕВЫЕ КЛАССЫ

ConfigLoader — Assets/System/Config/ConfigLoader.cs
Загружает Config_*.xlsx и loc.xlsx. Имя игрового конфига задаётся через CONFIG_NAME. После загрузки данных вызывает PARSE_ENDED.

DatabaseAll — Assets/System/Baza/DatabaseAll.cs
Преобразует DTO из ConfigLoader в шаблоны Obj, хранит базы heroes/skills/items/buildings и фабрики runtime-сущностей.

Obj / RObj — Assets/System/Baza/Obj.cs
Obj — шаблон сущности из базы. RObj — состояние конкретного runtime-экземпляра: RID, dbObj, параметры, inventory, buffs, skills, owner, tags, position, GameObject и подключённые модули.

ObjHolder — Assets/System/Baza/ObjHolder.cs
Связывает RObj с GameObject. Основная точка поиска runtime-сущности в сцене/prefab.

AddedObject — Assets/System/Extras/AddedObject.cs
Для заранее расставленных scene-объектов. После PARSE_ENDED вызывает DatabaseAll.CreateAny(id), получает RObj и через Inspector задаёт addedVis, parameters, meta-tags, loot/inventory, state и callbacks.

AbsHolder — Assets/System/Tasks/AbsHolder.cs
Создаёт ObjHolder-оболочку для данных, которые не являются обычной world-сущностью, например заданий и отображаемых способностей.

ResourceHolder — Assets/System/Baza/ResourceHolder.cs
Сопоставляет строковые ID с prefab, icons/avatars, projectile/hit effects, world skills, sounds, UI prefab и XD-модулями. Если ID есть в данных, но нет в нужном словаре ResourceHolder, нужный ресурс не будет найден.

MainStates — Assets/System/Baza/MainStates.cs
Центральный runtime-фасад: all, mainPlayer, inventory, equip, buffs, combat, damage, cooldown, movement, UI-команды и dynamic actions. Сначала искать готовую универсальную операцию здесь; существующие методы MainStates не менять.

EventManager — Assets/System/Baza/EventManager.cs
Строковая event bus: SUB — подписка, INV — вызов, ArgPass — данные.

MainCycle*
Сценарный слой конкретной игры. Подписывается на PARSE_ENDED и задаёт main_player, AddViz, стартовые items/UI, камеру, игровые фазы и callbacks. Для новой игры собственный MainCycle хранится в её папке; demo MainCycle не изменяются.

SkillExecutor / Targeter
SkillExecutor фильтрует цели и исполняет способности. Targeter и его специализации реализуют выбор точки/цели.

ModelStatistics — Assets/System/Config/ModelStatistics.cs
Stats, conditions, tasks и dynamic unlocks; используется TASKS, CONDITIONS и DYNAMIC_ID.

UIfiller / UnoAll / GBind
Data-binding UI: UIfiller получает список RObj по команде, ObjHolder хранит контекст слота, UnoAll выводит поле или вызывает действие, GBind хранит именованные ссылки prefab.

4. DATA / STATE / SAVE

ConfigLoader содержит DTO импорта таблиц, включая FormatHero, FormatArtefact, FormatSkill, FormatBuilding, FormatBattles, FormatDynamic, FormatCutscene и FormatPlayer. Gameplay в основном работает с Obj, RObj, FormatDynamic и структурами прогресса.

Runtime-state:
• базовые параметры — Obj.pars;
• изменяемые — RObj.upgradePars;
• рассчитанные — RObj.curPars;
• читать итоговое значение через GetPar/GetMainPar;
• устанавливать через SetPar;
• изменять относительно текущего через ChangePar;
• curPars напрямую не менять — RecalcPars его пересоберёт.

PlayerData в ModelStatistics содержит player stats, dynamic IDs, progress tasks/shop/mail, buildings, inventory, PGame и CGame.

Save-system: общей универсальной save/load системы сейчас нет. Save конкретной игры остаётся project-specific и управляется через MainCycle<ProjectName> (например MainCycleFantasyTower), не меняя Assets/System.

Project-specific алгоритм save/load по текущей архитектуре:
1. MainCycle<ProjectName> инициирует Save/Load и определяет состав snapshot.
2. Сохранять mainPlayer (RObj), PlayerData/ModelStatistics и только нужные runtime entries; в snapshot хранить значения и стабильные ID, не Unity runtime-ссылки. Весь MainStates.all автоматически не сохранять.
3. Load выполнять после PARSE_ENDED, когда ConfigLoader, DatabaseAll и ResourceHolder готовы.
4. По ID восстановить нужные RObj и данные; dbObj/owner/GameObject/ObjHolder/visual-XD связи пересобрать штатным pipeline DatabaseAll → RObj → ObjHolder → AddViz, а не десериализацией Unity-ссылок.

5. GAMEPLAY И ГОТОВЫЕ МЕХАНИКИ

Создание сущности: DatabaseAll.Create* или RObj → ObjHolder → visual prefab → регистрация в MainStates.all → skills/modules.

Нестандартный world object: отдельного типа/factory Dynamic Object нет. Штатный generic-путь — Heroes → RObj типа monster; для scene object — AddedObject → DatabaseAll.CreateAny(id), поведение — addedVis → RObj.AddViz → ResourceHolder.XD; modules-поля в Excel нет. CreateAny ищет ID в порядке heroes → items → buildings → skills, поэтому ID между ними не дублировать. Если ID не найден, получается ItemType.unknown без dbObj; такой объект не использовать как полноценную XD-сущность.

Минимум для Heroes: NAME, ORIGIN, CLASS, SKILLBASIC; неиспользуемые ячейки заполнять x. type определяется листом/factory; enemy/neutral — через isEnemy/isNeutral. ITEMS использовать только для inventory entity, SKILLS2 — для projectile/effect. BUILDINGS сейчас не использовать как generic RObj path: в нём не заполняется pars["level"], который требует RecalcPars().

DYNAMIC_ID — отдельный механизм actions/unlocks, не тип объекта.

Формат параметров модуля: module#key:value,key2:value2
Примеры: combat; coll#scale:0.5; hp#notext:1; animator#pr:1.
Старый формат вида move#3 не использовать.

Готовые XD-модули (XD — подключаемые prefab-модули поведения; например XDcombat, XDdrop, XDdrag; подключаются к RObj через AddViz):
• movement/select: move, click_move, select, drag;
• combat: combat, coll, realcol, shoot, weapon, stater;
• state: hp, buff, level, timer;
• feedback: animator, flash, dmg_track, info, shadow, changemat;
• lifecycle/loot: death, drop, loot, take, pick;
• other: invscale, largesmall, track, trap, portal, receiver.

Combat: XDcombat + SkillExecutor; MainStates.DealDamage содержит готовую обработку damage/resist/dodge/crit/shields/lifesteal/buffs.

Skills: данные идут из SKILLS2; поддерживаются range, cooldown, target/filter, AOE, projectile count, travel, mana, bounce, ricochet и другие параметры. action_req определяет ручной выбор действия.

Movement: free XY, XZ, 2D NavMesh, Manhattan, isometric, hex. Режим задаётся через METACONF; маршрутизация — MainStates.MovePath; grid/fog/path — PositionSetter.

Time: realtime deltaTime и ручной tick через TimeManager/MainStates.OneIteration.

Inventory/loot: готовые операции inventory, stacking, equip, sell, buy, upgrade + XDdrop/XDloot/XDtake/XDpick.

Placement/spawn: PlacerSystem, XDdrag, PositionHolder, WaveSpawner.

Tasks/conditions: ModelStatistics + TASKS/CONDITIONS/DYNAMIC_ID.

Dialogue/cutscene: Dialoguer и Cutscener; CUTSCENES поддерживает movement, animation, wait, activation, parenting, dialog, camera, zoom, creation, bark, effects и parallel branches.

6. ПАТТЕРНЫ И ИНТЕГРАЦИЯ

• Composition over inheritance: новая gameplay-сущность обычно остаётся Obj/RObj и получает готовые XD-модули вместо нового большого класса; чисто визуальным объектам RObj не нужен.
• Основные сервисы — scene singletons.
• Рабочая сцена проекта создаётся копией ближайшей подходящей demo-сцены; полный workflow см. 6.1.
• Контент, зависящий от базы, создаётся после PARSE_ENDED; не создавать его произвольно в Awake до загрузки данных.
• События: перед SUB найти существующий источник INV. У EventManager нет адресной отписки; повторный OnEnable/долгоживущие подписки могут дублировать callbacks.
• Для нового UI и подключения данных к существующему View сначала использовать UIfiller + ObjHolder + UnoAll + GBind. Рабочую локальную View/GUI-часть не переписывать.

6.1. SCENE USE — БАЗОВАЯ СЦЕНА ПРОЕКТА

Выбрать demo-сцену, наиболее близкую по gameplay/movement/UI wiring, сделать её копию внутри папки своего проекта и использовать эту копию как системную основу новой игры.

В копии сцены сохранить уже настроенные scene-singleton services и их Inspector-ссылки: ConfigLoader, DatabaseAll, EventManager, MainStates, ModelStatistics, ResourceHolder, SkillExecutor, TimeManager, UISystem, UpgradeSystem, UtilsControl и другие реально используемые сервисы этой сцены.

Demo-specific World/UI/игровые сущности удалять только после проверки ссылок. ACTUAL_GAME/MainCycle заменить своим project MainCycle. Map/grid сохранять, если они соответствуют нужному режиму; иначе взять подходящую demo-сцену как reference и перенастроить аналогично.

После сборки проверить в Play Mode singleton initialization, PARSE_ENDED, CONFIG_NAME, ResourceHolder mappings, ObjHolder/AddedObject, UI bindings и Inspector-ссылки. Demo-сцена остаётся reference; рабочая сцена живёт в папке проекта.

6.1.1. ВЫБОР БАЗОВОЙ DEMO-СЦЕНЫ

Для игры выбирать сцену по уже настроенным gameplay/movement/UI wiring.

• SampleScene — action, free XY; Obj/RObj, combat/shoot, loot, inventory, equipment, UI. Для минимального action/inventory prototype.
• Game_Lighter — shooter, free XY; projectile weapons, area spawning, inventory. Для top-down shooter/arena.
• Game_Cyclone — strategy, free XY; build/battle phases, buildings, production, resources, upgrades, waves. Для base defence/colony-lite.
• Game_Stone — tactical RPG, XY Manhattan; grid, pathfinding, fog, combat, skills, waves, buildings, inventory, dialogues/tasks. Универсальная база для 2D tactical RPG.
• Game_Stone_iso — tactical RPG, isometric; тот же основной stack, isometric grid, более компактный UI. Для изометрической тактики.
• Game_Stone_hex — tactical RPG, hex; hex grid, pathfinding, fog, полный combat stack. Для hex strategy/tactics.
• Game_Bold — tactical, XZ Manhattan; XZ-grid, 3D XD-варианты, 4 WaveSpawner, dialogues. Для 3D/2.5D grid tactics.
• Game_Sword — party RPG, Manhattan; world → отдельный бой, squad placement, equipment-driven skills, manual turns. Для party/squad RPG.
• Game_Exp — exploration RPG, free XY click-move; tilemap world, отдельный пошаговый бой, EXP, codex, tasks, battle log. Использовать только после устранения 22 Missing Scripts у Freeform Light 2D в environment prefab.

Не использовать как базу: Game_Dung — системный root _ALL_ выключен; Game_Dark — пустой grid и movement не подключён, wiring выглядит незавершённым.

Unity NavMesh в этих базах не найден: grid-сцены используют PositionSetter + Pathfinding2D.

6.2. ПОДКЛЮЧЕНИЕ СУЩЕСТВУЮЩЕЙ ИГРЫ

Подключать существующую игру к системе партнёра слоями, не перенося старую архитектуру целиком:

1. Scene bootstrap — выбрать ближайшую по gameplay/movement/UI wiring demo-сцену, скопировать её в BratAndronikProjects/<ProjectName>/ и сохранить рабочий system layer/Inspector links.
2. Project assets — переносить assets/prefabs/art/audio/materials/animations и рабочий локальный View/GUI-код. Старые gameplay Manager/Controller/State/DTO/save/framework-классы не переносить автоматически: сначала сопоставить их ответственность с системой партнёра и оставить только уникальный project-local код.
3. Import — переносить project assets вместе с .meta, чтобы сохранить GUID и ссылки. Всё game-specific хранить под BratAndronikProjects/<ProjectName>/; системные assets из Assets/System не копировать.
4. Compile-clean — до интеграции gameplay устранить Missing Script, отсутствующие package dependencies и compile errors. Не смешивать исправление импорта с переписыванием архитектуры.
5. Config/bootstrap — создать/назначить Config_*.xlsx и CONFIG_NAME, добавить минимальный MainCycle<ProjectName>, подключить PARSE_ENDED и mainPlayer.
6. Player/data — сопоставить старый gameplay-state с mainPlayer (RObj), PlayerData/ModelStatistics и существующими параметрами/реестрами. Собственный project-state оставлять только для данных, которых в системе нет.
7. Entities — gameplay/stateful/interactable сущности переносить на Obj/RObj, настроить ResourceHolder mappings и XD через AddViz. Чисто визуальные/decorative GameObject оставлять обычными project assets. Нестандартный world object — через Heroes; заранее стоящий — AddedObject → CreateAny(id) + addedVis.
8. World — переносить World/environment/visual blocks из старой игры project prefabs и устанавливать их в новую project-сцену, не заменяя системный слой сцены.
9. UI — сохранять уже рабочие визуальные prefab/View-компоненты; подключать данные системы reverse-сборкой через UIfiller → ObjHolder → UnoAll/GBind и готовые actions/events.
10. Unique gameplay — после подключения готовых систем дополнять MainCycle<ProjectName> только уникальными правилами и добавлять минимальные project-local scripts лишь для реально отсутствующей функциональности.
11. Save — подключать последним слоем после стабилизации runtime-state: project-specific snapshot из mainPlayer/PlayerData/нужных runtime dictionary entries по правилам раздела 4.
12. Verification — в Play Mode проверить PARSE_ENDED, mainPlayer, создание Obj/RObj, ResourceHolder, XD/AddedObject, movement, UI bindings, уникальный MainCycle и затем save/load.

7. ЧТО МОЖНО СОБРАТЬ БЕЗ НОВОГО GAMEPLAY-КОДА

• Entities/combat/movement — heroes/enemies, skills, buildings, battle/waves и готовые movement modes через таблицы, ResourceHolder, XD и системные movement-компоненты.
• Inventory/economy/progression — items/equipment/currency, loot/rewards, buy/sell/upgrade, tasks/conditions, shop/mail/skill tree/dynamic unlock.
• Dialogue/events — dialogues, table-driven cutscenes, EventTrigger/EventSubscribe и rewards/unlocks/stat changes через DYNAMIC_ID.
• UI/resources — lists/cards через UIfiller/ObjHolder/UnoAll/GBind; visual/icon/projectile/hit effect/sound через ResourceHolder.

8. ТИПОВЫЕ РЕЦЕПТЫ

Новый hero/enemy → Heroes + SKILLS2 + ResourceHolder.monsters → AddedObject/WaveSpawner/DatabaseAll.CreateMonster → нужные AddViz.

Scene-object → AddedObject → existing ID → addedVis/addedPars/addedMeta/loot → запуск после PARSE_ENDED.

Добавить готовое behavior → найти ключ ResourceHolder.XD → проверить prefab и AfterSet → подключить module#key:value → проверить обязательные соседние modules.

Reverse-сборка UI-list/card без нового кода:
1. Готовый visual slot/card prefab оставить как есть; на root добавить ObjHolder с контекстом RObj.
2. UnoAll на Image/TMP/Button читает param или вызывает action; GBind — для сложных именованных ссылок.
3. На контейнер поставить UIfiller: command/param → GetCommandResult → список RObj → slot prefab.
4. Для действий использовать готовые ClickedSome/EventTrigger/DYNAMIC_ID/events; новый UI-script — только если готового action нет.
Итог: visual prefab → UnoAll/GBind → ObjHolder → UIfiller. Рабочую локальную View-часть не переписывать.

Нестандартный world object → Heroes (NAME/ORIGIN/CLASS/SKILLBASIC; пустое заполнять x) → scene placement через AddedObject → CreateAny(id) → addedVis → AddViz → ResourceHolder.XD. Новый project-local ComponentBehavior нужен только если требуемого поведения нет среди готовых XD.

Button → найти существующее событие по EventManager.SUB/INV → EventTrigger → evtName + ArgPass.

Reward/unlock → DYNAMIC_ID → price/conditions/result + event/reward/dialog/cutscene → запуск через существующий Buyable/event/UI action.

Movement mode → если setup базовой demo-сцены подходит — сохранить его; иначе взять demo с нужным движением как reference, аналогично настроить METACONF + PositionSetter/grid-map в project-сцене → MainStates.MovePath. Отдельный movement-controller не писать до проверки готового пути.

9. КОГДА НУЖЕН СОБСТВЕННЫЙ СКРИПТ

• Новое behavior сущности, которого нет в XD → собственный небольшой ComponentBehavior + prefab → регистрация в ResourceHolder.XD → AddViz.
• Новый способ выбора цели → собственный наследник Targeter; системный Targeter не менять.
• Новое world-поведение способности → собственный SkillBehavior/компонент; системный код не менять.
• Уникальный game loop → собственный MainCycle.
• UI-операция, которую нельзя выразить GetCommandResult/ClickedSome/EventTrigger/DYNAMIC_ID/GBind → собственный UI-компонент.
• Новая отсутствующая механика → отдельный adapter/module/service в папке проекта, использующий публичный API системы.
• Project-specific save → MainCycle конкретной игры + mainPlayer/PlayerData/нужные runtime dictionary entries; системное ядро не менять.

10. СТРУКТУРА RUNTIME-ОБЪЕКТОВ

Типичная world-сущность:
EntityRoot
├── ObjHolder
├── vis_main
├── combat_z
├── coll_z
├── hp_z
├── death_z
└── animator_z

vis_main — основной visual; остальные модули создаются из ResourceHolder.XD.

Типичный UI-slot:
Slot
├── ObjHolder
├── GBind
├── Icon + UnoAll
├── Value + UnoAll
└── Button + UnoAll

11. КАК БЫСТРО НАЙТИ НУЖНУЮ РЕАЛИЗАЦИЮ

• Parameter → GetPar("key") + строка таблицы.
• Create entity → new RObj / DatabaseAll.Create*.
• Behavior → AddViz("key") + ResourceHolder.XD.
• Global mode → GetMetaParamValue("key").
• Event → искать вместе EventManager.INV и EventManager.SUB.
• UI source → MainStates.GetCommandResult.
• UI action → MainStates.ClickedSome.
• Table action → MainStates.ExecuteDone + FormatDynamic.
• Skill → SkillExecutor + SKILLS2.
• Task/condition → ModelStatistics + TASKS + CONDITIONS.
• Movement → MainStates.MovePath + PositionSetter + PathfindingMovement.
• Game-specific rules → MainCycle*.

12. ОГРАНИЧЕНИЯ И РИСКИ

• Большая часть API stringly typed: ключи задаются строками, например GetPar("attack"), AddViz("combat"), EventManager.INV("battle_start"); опечатка компилируется и проявляется только в runtime. Перед новым ключом искать точное существующее написание по Assets/System.
• В runtime-сборке есть импорты UnityEditor, NUnit и PlasticGui. Player build с ними не проверен; при первом standalone build проверить, не создают ли они build errors.
• Общей production save/load системы пока нет. До её появления каждый проект использует project-specific save composition из mainPlayer/PlayerData/нужных runtime dictionary entries, не меняя Assets/System.
• Комментарии и старые примеры могут описывать устаревший API — проверять актуальную реализацию в коде.
• Демо сцены вычищены не все. Структурно исправных баз без Missing Scripts — 8; Game_Exp использовать условно после ремонта environment lighting.
