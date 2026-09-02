# NIKA_CODEX_BRIDGE

Этот файл — мост между Никой в ChatGPT и Codex в проекте.

## Как использовать

- Ника пишет сюда задачи, решения, ограничения и вопросы для Codex.
- Codex перед началом работы читает этот файл.
- После выполнения задачи Codex обновляет раздел **CODEX → NIKA**.
- Не копировать сюда длинные логи, весь diff или содержимое `AGENTS.md`.
- Старые обмены можно удалять после переноса важной информации в постоянную документацию или фиксации в Git.

---

## ОБЩИЕ ПРАВИЛА

- Соблюдать `AGENTS.md` и локальные инструкции проекта.
- Делать минимальный рабочий diff.
- Не менять архитектуру и публичные контракты без необходимости.
- Не править Unity scene/prefab YAML вручную без явного разрешения.
- Не коммитить секреты, ключи, credentials и локальные конфиги.
- После изменений запускать доступные релевантные проверки/тесты.
- Если задача неоднозначна и решение может заметно повлиять на проект — сначала записать вопрос в **CODEX → NIKA**, а не угадывать.

---

## NIKA → CODEX

### Задача
Пока задач нет.

### Ограничения
Пока дополнительных ограничений нет.

### Критерии готовности
Пока не заданы.

---

## CODEX → NIKA

### Текущий статус
Этап 6 WhoHeroes — Tavern: отдельный пул специальных юнитов, случайные предложения, покупка с немедленной заменой и платный reroll — реализован и проверен в рабочей сцене `WhoHeroes_System`.

### Что сделано на этапе 6
- Максимально переиспользована система Minimus: предложения хранятся в `RObj.inventory` таверны, цена берётся через `UpgradeSystem.GetPrice`, проверка и списание ресурсов выполняются `HaveAmount`/`Buy`, покупка — штатным действием `buy`, добавление в roster и объединение stack — `MainStates.AddItem`.
- Игра-специфичной оставлена только политика Tavern: случайный выбор ID из отдельного пула, поддержание пяти активных предложений и замена предложений после покупки/reroll. `Assets/System` не менялся.
- Пул Tavern не пересекается с Castle: `treant`, `shaman`, `magicel`, `naga`, `cyclop`. Характеристики и цены перенесены из проверенного исходного WhoHeroes:
  - `treant`: Gold 10, HP 15, Damage 2, Armor 3, rare;
  - `shaman`: Gold 10, HP 15, Damage 5, Armor 0, rare;
  - `magicel`: Gold 40, HP 30, Damage 15, Armor 20, legend, `activearmor`;
  - `naga`: Gold 45, HP 50, Damage 17, Armor 10, legend, `activehp`;
  - `cyclop`: Gold 100, HP 100, Damage 45, Armor 20, mystic, `activestone`.
- В таверне одновременно пять случайных предложений; повторы допустимы. Каждому runtime-офферу назначается уникальный `shardID`, поэтому одинаковые офферы не схлопываются внутри магазина. Перед покупкой `shardID` очищается, и купленный юнит штатно объединяется с одноимённым stack игрока.
- После успешной покупки удаляется использованный оффер и сразу создаётся ровно один новый. При нехватке Gold предложение и состав магазина не меняются.
- Reroll стоит 50 Gold, использует штатную Minimus-транзакцию и при успехе заменяет все пять предложений. Ночью покупка и reroll блокируются общей блокировкой management actions.
- `GUITavernWindow` теперь показывает `tavern.inventory`, а не roster игрока. Пять существующих GUI-слотов заполняются офферами, шестой скрывается. Доступность каждой кнопки учитывает цену конкретного оффера.
- Существующая кнопка upgrade переиспользована как reroll. Обработчики back/reroll и подписка на refresh подключаются в `Awake`, поэтому изначально выключенное окно полностью готово сразу после активации.
- В `ResourceHolder.avas` назначены все пять портретов Tavern. Недостающие PNG для `naga`, `shaman`, `magicel` перенесены из проверенного старого WhoHeroes с сохранением их `.meta`/GUID; существующие `treant` и `cyclop` подключены к нужным ID.
- Tilemap не добавлялся; Unity scene YAML вручную не редактировался.

### Изменённые файлы этапа 6
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/WhoHeroesDemoConfig.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/WhoHeroesMainCycle.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/LIBS/GUILIB.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/GUIPrefs/GUIInventoryList.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/GUIPrefs/Windows/GUITavernWindow.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scenes/WhoHeroes_System.unity`
- PNG и `.meta` портретов `naga`, `shaman`, `magicel` в `WhoHeroes/!Fantacy/!Chars`.

### Проверки этапа 6
- Компиляция Unity прошла без ошибок.
- Начальный магазин: 5/5 активных офферов, каждый amount 1, owner `tavern`, уникальные `shardID`; дубликаты реально проверены; Castle ID в пул не попадают.
- Покупка двух одинаковых `naga`: Gold 120→75→30, в roster один stack `naga` amount 2, в таверне после каждой покупки снова пять офферов.
- Неуспешная покупка `cyclop` при Gold 30 не списала ресурс и не заменила ни одного оффера.
- Неуспешный reroll при Gold 30 сохранил все пять объектов; успешный reroll Gold 130→80 заменил 5/5 объектов.
- Ночью и покупка, и reroll заблокированы без изменения Gold и предложений.
- Реальный клик по GUI-кнопке покупки: `shaman`, Gold 290→280, roster 0→1, сохранены четыре старых оффера и создан один новый.
- Реальный клик по GUI-кнопке reroll: Gold 320→270, заменены все пять офферов.
- Финальная сцена: Play Mode выключен, dirty=false, Missing Scripts — 0, Unity Console — 0 ошибок и 0 предупреждений. Корневые `Castle`, `Tavern`, `Tower` выключены; `WhoHeroes_RuntimeCastle` содержит шесть якорей.

### Открытые ограничения
- `Config_whoheroes.xlsx` на этапе 6 не изменён. Количество предложений 5 и цена reroll 50 находятся в проектном адаптере `WhoHeroesDemoConfig`; сами экономика и транзакции остаются Minimus. Для переноса этих двух значений в XLSX нужен доступный безопасный инструмент редактирования книги и подтверждение схемы импорта.
- Общий Restart всей игры в проекте пока не реализован; отдельный Tavern-only reset не добавлялся, чтобы не создавать второй жизненный цикл состояния.

---

### Архив: этап 5

### Статус
Этап 5 WhoHeroes — замковые постройки, постоянные предложения базовых юнитов, roster и Defense Setup — реализован и проверен в рабочей сцене.

### Что сделано
- Переиспользованы штатные Minimus-действия `buy`, `upgrade`, `equip_exp`, `unequip_exp`, кошелёк и слоты Defense 20–23. `Assets/System` не менялся.
- Добавлены шесть проверенных связок замковая постройка → базовый юнит: `tent→peak`, `barracks→sword`, `snowhouse→dwarf`, `knighttower→knight`, `stables→rider`, `angelfort→angel`.
- Уровень 0 не даёт предложения. После восстановления до Level 1 появляется постоянное предложение одного соответствующего юнита за Gold; успешная покупка сразу восполняет предложение.
- Повторные покупки объединяются Minimus в один roster stack по ID юнита. Проектная конфигурация снимает ошибочный для этих юнитов импортный `max_stack=1`.
- Defense Setup использует существующие Minimus-слоты. После `equip_exp` новый отряд нормализуется в первый свободный слот 20–23; порядок выбранных отрядов в GUI соответствует слотам.
- Исправлен `GUIHireBuildingWindow`: кнопка покупки реально вызывает Minimus `buy`, корректно блокируется при отсутствии предложения и обновляет окно после покупки.
- `WhoHeroesUIRouter` теперь открывает Castle overview и Hire Building, а также перенесённые Tower/Tavern; при входе скрывает мир, при Back полностью возвращает `MainLocals` и `Transforms`.
- В сцену через Unity Editor API добавлен `WhoHeroes_RuntimeCastle` с шестью runtime-якорями построек. Исправлены ID окон Castle/Tower/Expedition/Tavern, подключены иконки шести юнитов. Tilemap не добавлялся, YAML вручную не редактировался.
- Точные цены и характеристики взяты из проверенного исходного WhoHeroes: peak 9/10/3/1, dwarf 10/15/2/3, sword 12/10/4/2, rider 50/50/20/8, knight 50/50/20/8, angel 100/100/50/10 (Gold/HP/Damage/Armor). Множитель grade — 2.
- `Config_whoheroes.xlsx` на этом этапе не менялся: его `Heroes`-импортёр не переносит цену в `Obj.price` и жёстко задаёт `max_stack=1`; корректные определения накладываются проектным адаптером после парсинга в `DatabaseAll`.

### Изменённые файлы
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/WhoHeroesDemoConfig.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/WhoHeroesMainCycle.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/WhoHeroesUIRouter.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/LIBS/GUILIB.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/GUIPrefs/Windows/GUIHireBuildingWindow.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/GUIPrefs/GUIInventoryList.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scripts/NEW/GUIPrefs/GUISmallBuildingPref.cs`
- `Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scenes/WhoHeroes_System.unity`
- Пять отсутствовавших unit icon PNG и их `.meta` скопированы из проверенного старого WhoHeroes с сохранением GUID.

### Проверки / тесты
- Интеграционный Play Mode: `tent` Level 0 без предложения; после `upgrade` — Level 1 и `peak:1`, списаны Wood 10 и Stone 5.
- Две последовательные покупки: Gold 120→111→102, один stack `peak` вырос 0→1→2, постоянное предложение осталось `peak:1`.
- `equip_exp` назначил stack в первый Defense slot 20.
- Castle/Tower/Tavern открывают только соответствующий интерьер. Castle скрывает `MainLocals`/`Transforms`; Back возвращает оба корня и закрывает интерьер.
- Покупка непосредственно кнопкой `GUIHireBuildingWindow` увеличила stack 2→3 и сохранила предложение.
- Сцена сохранена, dirty=false; Missing Scripts — 0; финальная Unity Console — 0 ошибок.

### Вопросы / риски
- GDD задаёт одну стартовую постройку Level 1 и бесплатный stack, но не указывает конкретную постройку и размер stack; эти значения не придуманы и не зашиты.
- В GDD есть параметр Range, но точных значений в GDD и проверенном source нет; численное поле не добавлено.
- Случайный Tavern pool, замена купленного героя и платный reroll относятся к следующему отдельному этапу Tavern, не к Castle shop.
