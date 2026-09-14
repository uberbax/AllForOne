# Unity CLI: настройка и проверка

Проверено 13 сентября 2026 в `C:\UnityProjects\AllForOne`.

## Установка

- Unity CLI `1.0.0-beta.8`: `C:\Users\ASUS\AppData\Local\Unity\bin\unity.exe`. Уже был установлен Hub; каталог добавлен в пользовательский PATH. Открытые терминалы сохраняют старый PATH до перезапуска.
- Установлен официальный `com.unity.pipeline` `0.7.0-exp.1`; зависимости разрешены Package Manager. Версии остальных прямых зависимостей не изменены.
- Установлены официальные проектные skills `.agents/skills/unity-cli` и `.agents/skills/unity-pipeline`.
- Открытый Editor: `6000.5.2f1`. `ProjectVersion.txt` содержит `6000.5.0f1`; файл версии и сам Editor в рамках настройки не менялись.
- Сервер отвечает на localhost:7800, состояние `ready`; autotick включён.
- По последнему указанию Инги основные каналы — CLI и встроенный в него MCP (`unity mcp`); прежний Unity MCP использовать только при сбое или ограничениях основных инструментов. Его пакет и конфигурация сохранены.

## Что проверено

| Возможность | Результат |
|---|---|
| Обнаружение Editor, каталог | 151 команда; нет повторяющихся имён или отсутствующих схем |
| Состояние Editor, консоль | Ответы получены, ошибки компиляции проекта отсутствуют |
| Сцены, иерархия, выбор, Inspector | Чтение успешно |
| Настройки сборки, Player, графики, runtime Pipeline | Чтение успешно |
| Поиск prefab | `UpgradeItem.prefab` найден; тип для поиска — `GameObject`, не `Prefab` |
| `eval`, `run_script` | Выполняются; проверены результат и диагностика |
| Сериализация, Undo, экземпляр prefab | Проверены в изолированной preview-сцене; активная сцена и её dirty-состояние сохранены |
| Перекомпиляция | Установка пакета вызвала компиляцию; затем `recompile`/`recompile_status` вернули `up_to_date`, `failed=false` |
| Тестовый интерфейс | `list_tests` и `test_status` отвечают; обнаружено 0 тестов, NUnit-прогон не выполнялся |
| Захват камеры | PNG получен и визуально проверен |
| Захват Scene View | PNG получен; текущий ракурс дал однотонный фон |

При проверке один вызов `find_assets --type Prefab` вернул ошибку типа; исправленный вызов с `GameObject` успешен. Эта диагностическая запись осталась в Console вместе с существующими предупреждениями; Console не очищалась.

## Границы проверки

Регистрация всех 151 команд проверена, но каждая изменяющая команда отдельно не запускалась. Полная сборка, Play Mode, действия над лицензиями/облаком, установка других Editor и runtime hot reload не проверялись. Runtime-сервер в Player отключён (`enableInBuilds=false`).

`capture_game_view --source screen` с Overlay UI требует Play Mode. Захват отдельных UI Toolkit элементов требует Unity 6000.7+ и в текущем каталоге отсутствует. Это ограничения пакета/версии, а не недостающая установка.

В песочнице Codex localhost блокируется: `status` может ошибочно выглядеть как отсутствие Editor. Те же вызовы с обычным расширением доступа работают. Не менять серверные токены или защиту для обхода этого ограничения.

Windows PowerShell 5 может терять внутренние кавычки при передаче C# в native CLI: для такого кода использовать файл и `run_script`. `capture_* --save_path Temp/...` в этой версии фактически сохраняет под `Assets/Temp/...`; проверочные PNG перенесены в `Temp/pipeline-verification`, созданные в Assets файлы удалены через Editor API.

## Повторная проверка

```powershell
$cli = 'C:\Users\ASUS\AppData\Local\Unity\bin\unity.exe'
& $cli status --json
& $cli command editor_status --project-path 'C:\UnityProjects\AllForOne' --json
& $cli command run_script --file AgentScripts/VerifyUnityPipeline.cs --entry VerifyUnityPipeline.Run --project-path 'C:\UnityProjects\AllForOne' --json
```

Последняя проверка использует существующий `Assets/!Prefabs/UpgradeItem.prefab`. Скрипт расположен вне Assets и не вызывает перекомпиляцию проекта.

Официальная документация: [установка CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli), [Pipeline](https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package).
