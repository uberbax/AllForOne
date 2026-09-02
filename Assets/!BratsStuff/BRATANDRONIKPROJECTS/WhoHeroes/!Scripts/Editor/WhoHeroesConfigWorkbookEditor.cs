using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WhoHeroesConfigWorkbookEditor
{
    private const string GoldResourceId = "gold";
    private const string WoodResourceId = "wood";
    private const string StoneResourceId = "stone";
    private const string WorkbookAssetPath = "Assets/StreamingAssets/WhoHeroes/Config_whoheroes.xlsx";
    private const string SceneAssetPath = "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scenes/WhoHeroes_System.unity";
    private const string CarrierPrefabAssetPath =
        "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Prefabs/Chars/keeper.prefab";
    private const string TraderPrefabAssetPath =
        "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Prefabs/Chars/king.prefab";
    private const string DemonStatePrefabAssetPath =
        "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Prefabs/Behaviors/whoheroes_demon_state.prefab";
    private const string SteamUrl =
        "https://store.steampowered.com/app/4197340/WHO_THE_HELL_OPENED_THE_PORTAL/";
    private const string OnboardingTasks =
        "whoheroes_quest_hire#whoheroes_quest_restore#whoheroes_quest_expedition#whoheroes_quest_night";
    private static readonly KeyValuePair<string, object>[] MetaValues =
    {
        new KeyValuePair<string, object>("mode_manhattan", -1),
        new KeyValuePair<string, object>("mode_isometric", 0),
        new KeyValuePair<string, object>("mode_hex", 0),
        new KeyValuePair<string, object>("use_2d_navmesh", 0),
        new KeyValuePair<string, object>("blood_death", 0),
        new KeyValuePair<string, object>("sim_time_cont", 1),
        new KeyValuePair<string, object>("whoheroes_day_duration", 120),
        new KeyValuePair<string, object>("whoheroes_daily_gold", 10),
        new KeyValuePair<string, object>("whoheroes_territory_gold", 5),
        new KeyValuePair<string, object>("whoheroes_start_active_portals", 1),
        new KeyValuePair<string, object>("whoheroes_mine_max_level", 5),
        new KeyValuePair<string, object>("whoheroes_wood_production_interval", 60),
        new KeyValuePair<string, object>("whoheroes_stone_production_interval", 120),
        new KeyValuePair<string, object>("whoheroes_resource_gold", "gold"),
        new KeyValuePair<string, object>("whoheroes_resource_wood", "wood"),
        new KeyValuePair<string, object>("whoheroes_resource_stone", "stone"),
        new KeyValuePair<string, object>("whoheroes_start_unit_amount", 5),
        new KeyValuePair<string, object>("whoheroes_boost_percent", 10),
        new KeyValuePair<string, object>("whoheroes_expedition_max_stacks", 3),
        new KeyValuePair<string, object>("whoheroes_trader_start_night", 2),
        new KeyValuePair<string, object>("whoheroes_trader_gold_surcharge", 25),
        new KeyValuePair<string, object>("whoheroes_trader_power_multiplier", 115),
        new KeyValuePair<string, object>("whoheroes_trader_travel_seconds", 3),
        new KeyValuePair<string, object>("whoheroes_steam_url", SteamUrl)
    };

    [MenuItem("Tools/WhoHeroes/Ensure Static Metadata")]
    public static void EnsureStaticMetadata()
    {
        RequireWhoHeroesScene();
        var fullPath = Path.GetFullPath(WorkbookAssetPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("WhoHeroes config workbook was not found.", fullPath);

        using (var package = new ExcelPackage(new FileInfo(fullPath)))
        {
            var meta = RequireSheet(package, "METACONF");
            SetMetaValue(meta, "whoheroes_trader_travel_seconds", 3);
            SetMetaValue(meta, "whoheroes_steam_url", SteamUrl);
            SetMetaValue(meta, "whoheroes_resource_gold", "gold");
            SetMetaValue(meta, "whoheroes_resource_wood", "wood");
            SetMetaValue(meta, "whoheroes_resource_stone", "stone");
            package.Save();
        }

        AssetDatabase.ImportAsset(WorkbookAssetPath, ImportAssetOptions.ForceUpdate);
        ValidateWorkbook(fullPath);
        Debug.Log("WhoHeroes static metadata updated and validated: " + WorkbookAssetPath);
    }

    [MenuItem("Tools/WhoHeroes/Migrate Composite Config To Minimus Sets")]
    public static void MigrateCompositeConfigToMinimusSets()
    {
        RequireWhoHeroesScene();
        var fullPath = Path.GetFullPath(WorkbookAssetPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("WhoHeroes config workbook was not found.", fullPath);

        var backupPath = Path.GetTempFileName();
        File.Copy(fullPath, backupPath, true);
        try
        {
            using (var package = new ExcelPackage(new FileInfo(fullPath)))
            {
                var meta = RequireSheet(package, "METACONF");
                MigrateCompositeMetaToLootSets(package, meta);
                EnsureExpeditionCaptureDynamic(package);
                package.Save();
            }
            ValidateWorkbook(fullPath);
        }
        catch
        {
            File.Copy(backupPath, fullPath, true);
            throw;
        }
        finally
        {
            File.Delete(backupPath);
        }

        AssetDatabase.ImportAsset(WorkbookAssetPath, ImportAssetOptions.ForceUpdate);
        Debug.Log("WhoHeroes composite config migrated from METACONF strings to Minimus LOOTSET.");
    }

    [MenuItem("Tools/WhoHeroes/Validate Config Workbook")]
    public static void ValidateWorkbookCommand()
    {
        RequireWhoHeroesScene();
        var fullPath = Path.GetFullPath(WorkbookAssetPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("WhoHeroes config workbook was not found.", fullPath);
        ValidateWorkbook(fullPath);
        Debug.Log("WhoHeroes static config workbook validated without modification: " + WorkbookAssetPath);
    }

    [MenuItem("Tools/WhoHeroes/Wire Resource Icons")]
    public static void WireResourceIcons()
    {
        WireResourceIcons(RequireWhoHeroesScene());
    }

    [MenuItem("Tools/WhoHeroes/Wire Scene References")]
    public static void WireSceneReferences()
    {
        var scene = RequireWhoHeroesScene();
        var roots = scene.GetRootGameObjects();
        var cycle = RequireSingle(GetSceneComponents<MainCycle_WhoHeroes>(scene), nameof(MainCycle_WhoHeroes));
        SetReference(cycle, "startScreen", GetSceneComponents<GUIStartScreen>(scene).FirstOrDefault());
        SetReferences(cycle, "deliveryPoints", GetSceneComponents<SpaumPoint>(scene)
            .Where(value => string.Equals(value.type, "delivery", StringComparison.OrdinalIgnoreCase)));
        var carrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarrierPrefabAssetPath);
        if (carrierPrefab == null || carrierPrefab.GetComponent<WhoHeroesCarrierStateMachine>() == null)
            throw new FileNotFoundException(
                "WhoHeroes keeper prefab or carrier state machine was not found.", CarrierPrefabAssetPath);
        SetReference(cycle, "deliveryCarrierPrefab", carrierPrefab);
        SetMineWorkers(cycle, GetSceneComponents<BuildingPref>(scene));

        var demonStatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DemonStatePrefabAssetPath);
        if (demonStatePrefab == null || demonStatePrefab.GetComponent<WhoHeroesDemonStateMachine>() == null)
            throw new FileNotFoundException(
                "WhoHeroes demon state prefab or component was not found.", DemonStatePrefabAssetPath);
        var resourceHolder = RequireSingle(GetSceneComponents<ResourceHolder>(scene), nameof(ResourceHolder));
        Undo.RecordObject(resourceHolder, "Wire WhoHeroes demon state module");
        resourceHolder.XD ??= new StringObjectDictionary();
        resourceHolder.XD["whoheroes_demon_state"] = demonStatePrefab;
        EditorUtility.SetDirty(resourceHolder);

        var router = RequireSingle(GetSceneComponents<WhoHeroesUIRouter>(scene), nameof(WhoHeroesUIRouter));
        SetReference(router, "castle", GetSceneComponents<GUICastleWindow>(scene).FirstOrDefault());
        SetReference(router, "hire", GetSceneComponents<GUIHireBuildingWindow>(scene).FirstOrDefault());
        SetReference(router, "factory", GetSceneComponents<GUIFactotyWindow>(scene).FirstOrDefault());
        SetReference(router, "portal", GetSceneComponents<GUIPortalWindow>(scene).FirstOrDefault());
        SetReference(router, "tavern", GetSceneComponents<GUITavernWindow>(scene).FirstOrDefault());
        SetReference(router, "market", GetSceneComponents<GUIMarketWindow>(scene).FirstOrDefault());
        SetReference(router, "enemy", GetSceneComponents<GUIEnemyBuilding>(scene).FirstOrDefault());
        SetReference(router, "warBuilding", GetSceneComponents<GUIWarBuildWindow>(scene).FirstOrDefault());
        SetReference(router, "taskBuilding", GetSceneComponents<GUITaskBuilWindow>(scene).FirstOrDefault());
        SetReference(router, "genericBuilding", GetSceneComponents<GUIBuildingInfo>(scene).FirstOrDefault());
        SetReference(router, "tower", GetSceneComponents<GUIArmyWindow>(scene)
            .FirstOrDefault(value => value.building?.id == "tower"));
        SetReference(router, "expedition", GetSceneComponents<GUIArmyWindow>(scene)
            .FirstOrDefault(value => value.building?.id == "expedition"));
        SetReference(router, "tasks", GetSceneComponents<GUITasksWindow>(scene).FirstOrDefault());
        SetReference(router, "trader", GetSceneComponents<GUIPerkWindow>(scene)
            .FirstOrDefault(value => value.winType == "trade"));
        SetReference(router, "permanentPerks", GetSceneComponents<GUIPerkWindow>(scene)
            .FirstOrDefault(value => value.winType == "perk"));
        SetReference(router, "castleInterior", roots.FirstOrDefault(value => value.name == "Castle"));
        SetReference(router, "tavernInterior", roots.FirstOrDefault(value => value.name == "Tavern"));
        SetReference(router, "towerInterior", roots.FirstOrDefault(value => value.name == "Tower"));
        SetReferences(router, "worldRoots", roots.Where(value => value.name == "MainLocals" || value.name == "Transforms"));
        var traderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TraderPrefabAssetPath);
        if (traderPrefab == null || traderPrefab.GetComponent<WhoHeroesTraderStateMachine>() == null)
            throw new FileNotFoundException(
                "WhoHeroes trader visual prefab or state machine was not found.", TraderPrefabAssetPath);
        SetReference(router, "traderVisualPrefab", traderPrefab);
        var runtimeRoot = roots.FirstOrDefault(value => value.name == "WHO_HEROES_RUNTIME");
        if (runtimeRoot == null)
            throw new InvalidDataException("WHO_HEROES_RUNTIME root is missing from the scene.");
        var actionHolder = runtimeRoot.GetComponent<ObjHolder>();
        if (actionHolder == null)
            throw new InvalidDataException("WHO_HEROES_RUNTIME Minimus action holder is missing from the scene.");
        SetReference(router, "actionHolder", actionHolder);
        Undo.RecordObject(actionHolder, "Delay WhoHeroes action holder initialization");
        actionHolder.enabled = false;
        EditorUtility.SetDirty(actionHolder);
        SetReference(router, "traderSpawnPoint", RequireChild(runtimeRoot.transform, "NightBattle/CastleGate"));
        var castleBuilding = GetSceneComponents<BuildingPref>(scene)
            .FirstOrDefault(value => string.Equals(value.build?.id, "castle", StringComparison.Ordinal));
        if (castleBuilding == null)
            throw new InvalidDataException("WhoHeroes castle BuildingPref is missing from the scene.");
        SetReference(router, "traderDestination", castleBuilding.transform);

        foreach (var portal in GetSceneComponents<GUIPortalWindow>(scene))
        {
            var startLabel = portal.start?.name?.transform.parent?.parent?.GetComponent<TextMeshProUGUI>();
            var endLabel = portal.end?.name?.transform.parent?.parent?.GetComponent<TextMeshProUGUI>();
            SetReference(portal, "startLabel", startLabel);
            SetReference(portal, "endLabel", endLabel);
        }

        foreach (var army in GetSceneComponents<GUIArmyWindow>(scene))
        {
            var header = army.busy?.onFalse.Where(value => value != null)
                .Select(value => value.GetComponentInChildren<TextMeshProUGUI>(true))
                .FirstOrDefault(value => value != null);
            SetReference(army, "busyHeader", header);
        }

        foreach (var window in GetSceneComponents<GUIPerkWindow>(scene).Where(value => value.winType == "trade"))
        {
            var cost = window.afterAppear == null
                ? null
                : window.afterAppear.transform.Find("Text_Value (1)")?.GetComponent<TextMeshProUGUI>();
            SetReference(window, "tradeCostText", cost);
        }

        var hud = RequireSingle(GetSceneComponents<GUIWhoHeroesNightHUD>(scene), nameof(GUIWhoHeroesNightHUD));
        var loseScreen = RequireSingle(roots
            .SelectMany(value => value.GetComponentsInChildren<Transform>(true))
            .Where(value => value.name == "UI_LoseScreen")
            .Select(value => value.gameObject), "UI_LoseScreen");
        var loseRoot = loseScreen.transform;
        var restartButton = RequireChildComponent<Button>(loseRoot, "OK");
        SetReference(hud, "loseScreen", loseScreen);
        SetReference(hud, "loseTitle", RequireChildComponent<TextMeshProUGUI>(loseRoot, "Image/Description"));
        SetReference(hud, "nightReachedLabel",
            RequireChildComponent<TextMeshProUGUI>(loseRoot, "stat (1)/Text (TMP) (1)"));
        SetReference(hud, "nightReachedValue",
            RequireChildComponent<TextMeshProUGUI>(loseRoot, "stat (1)/Text (TMP) (1)/lost"));
        SetReference(hud, "bestNightLabel",
            RequireChildComponent<TextMeshProUGUI>(loseRoot, "stat (1)/Text (TMP)"));
        SetReference(hud, "bestNightValue",
            RequireChildComponent<TextMeshProUGUI>(loseRoot, "stat (1)/Text (TMP)/killed"));
        SetReference(hud, "permanentPerksText",
            RequireChildComponent<TextMeshProUGUI>(loseRoot, "UI_reward (2)/header"));
        SetReference(hud, "rewardSlots", RequireChild(loseRoot, "UI_reward (2)/frame").gameObject);
        SetReference(hud, "restartButton", restartButton);
        SetReference(hud, "restartText", RequireChildComponent<TextMeshProUGUI>(loseRoot, "OK/Text (TMP)"));

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("WhoHeroes scene could not be saved after wiring Inspector references.");
        Debug.Log("WhoHeroes Inspector references wired and scene saved.");
    }

    [MenuItem("Tools/WhoHeroes/Clear Legacy PlayerPrefs")]
    public static void ClearLegacyPlayerPrefs()
    {
        PlayerPrefs.DeleteKey("restart");
        PlayerPrefs.DeleteKey("WhoHeroes.RunSnapshot.v1");
        PlayerPrefs.DeleteKey("WhoHeroes.Meta.whoheroes_best_night");
        foreach (var id in new[]
                 {
                     MainCycle_WhoHeroes.UnitDamagePerkStat,
                     MainCycle_WhoHeroes.UnitHealthPerkStat,
                     MainCycle_WhoHeroes.UnitArmorPerkStat,
                     MainCycle_WhoHeroes.PrinceDamagePerkStat,
                     MainCycle_WhoHeroes.PrinceHealthPerkStat,
                     MainCycle_WhoHeroes.PrinceArmorPerkStat,
                     MainCycle_WhoHeroes.UnitCostPerkStat,
                     MainCycle_WhoHeroes.BuildCostPerkStat
                 })
            PlayerPrefs.DeleteKey("WhoHeroes.Meta." + id);
        PlayerPrefs.Save();
        Debug.Log("WhoHeroes legacy PlayerPrefs cleared.");
    }

    private static void WireResourceIcons(Scene scene)
    {
        var holder = GetSceneComponents<ResourceHolder>(scene).FirstOrDefault();
        var wallet = GetSceneComponents<GUIPlayerWallet>(scene).FirstOrDefault();
        if (holder == null || wallet?.wallet == null)
            throw new InvalidDataException("WhoHeroes ResourceHolder or GUIPlayerWallet is missing in the scene.");

        var ids = new[] { GoldResourceId, WoodResourceId, StoneResourceId };
        Undo.RecordObject(holder, "Wire WhoHeroes resource icons");
        holder.items ??= new StringSpriteDictionary();
        for (var index = 0; index < ids.Length; index++)
        {
            if (wallet.wallet.objs.Count <= index || wallet.wallet.objs[index] == null)
                throw new InvalidDataException("WhoHeroes wallet resource slot is missing: " + ids[index]);
            var sprite = wallet.wallet.objs[index].GetComponentsInChildren<Image>(true)
                .Where(value => value != null && value.sprite != null)
                .OrderByDescending(value => value.name == "Icon_Coin")
                .Select(value => value.sprite)
                .FirstOrDefault();
            if (sprite == null)
                throw new InvalidDataException("WhoHeroes wallet resource icon is missing: " + ids[index]);
            holder.items[ids[index]] = sprite;
        }

        EditorUtility.SetDirty(holder);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("WhoHeroes scene could not be saved after wiring resource icons.");
        Debug.Log("WhoHeroes resource icons wired into ResourceHolder and scene saved.");
    }

    private static Scene RequireWhoHeroesScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !string.Equals(scene.path, SceneAssetPath, StringComparison.Ordinal))
            throw new InvalidDataException("Open WhoHeroes_System before running a WhoHeroes editor command.");
        return scene;
    }

    private static ExcelWorksheet RequireSheet(ExcelPackage package, string name)
    {
        var sheet = package.Workbook.Worksheets[name];
        if (sheet == null)
            throw new InvalidDataException("Required worksheet is missing: " + name);
        return sheet;
    }

    private static bool HasBuilding(ExcelWorksheet sheet, string baseId, int level)
    {
        var currentBaseId = string.Empty;
        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var name = sheet.Cells[row, 1].Text.Trim();
            if (!string.IsNullOrEmpty(name) && !string.Equals(name, "x", StringComparison.OrdinalIgnoreCase))
                currentBaseId = name;

            if (!string.Equals(currentBaseId, baseId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (int.TryParse(sheet.Cells[row, 2].Text, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var parsedLevel) && parsedLevel == level)
                return true;
        }
        return false;
    }

    private static List<string> CollectSceneIds()
    {
        var scene = RequireWhoHeroesScene();

        var configuredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var building in GetSceneComponents<BuildingPref>(scene))
        {
            if (building == null || building.build == null || string.IsNullOrWhiteSpace(building.build.id))
                continue;

            var id = building.build.id.Trim();
            if (!configuredIds.Add(id))
                throw new InvalidDataException("Duplicate BuildingPref id in loaded scenes: " + id);
            result.Add(id);
        }

        if (configuredIds.Count == 0)
            throw new InvalidDataException("No BuildingPref objects were found in loaded Unity scenes.");
        return result;
    }

    private static IEnumerable<T> GetSceneComponents<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    private static T RequireSingle<T>(IEnumerable<T> values, string label) where T : UnityEngine.Object
    {
        var result = values.Where(value => value != null).ToList();
        if (result.Count != 1)
            throw new InvalidDataException($"WhoHeroes scene requires exactly one {label}; found {result.Count}.");
        return result[0];
    }

    private static Transform RequireChild(Transform root, string path)
    {
        var child = root.Find(path);
        if (child == null)
            throw new InvalidDataException($"WhoHeroes scene object is missing: {root.name}/{path}.");
        return child;
    }

    private static T RequireChildComponent<T>(Transform root, string path) where T : Component
    {
        var child = RequireChild(root, path);
        var component = child.GetComponent<T>();
        if (component == null)
            throw new InvalidDataException(
                $"WhoHeroes scene component {typeof(T).Name} is missing: {root.name}/{path}.");
        return component;
    }

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        if (target == null)
            return;
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName) ??
                       throw new MissingFieldException(target.GetType().Name, propertyName);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetReferences<T>(UnityEngine.Object target, string propertyName, IEnumerable<T> values)
        where T : UnityEngine.Object
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName) ??
                       throw new MissingFieldException(target.GetType().Name, propertyName);
        var items = values.Where(value => value != null).ToList();
        property.arraySize = items.Count;
        for (var index = 0; index < items.Count; index++)
            property.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetMineWorkers(MainCycle_WhoHeroes cycle, IEnumerable<BuildingPref> buildings)
    {
        var bindings = new List<(string id, List<GameObject> workers)>();
        foreach (var building in buildings)
        {
            var id = building?.build?.id?.Trim() ?? string.Empty;
            var workerPrefix = id.StartsWith("wood", StringComparison.OrdinalIgnoreCase)
                ? "zombiewood"
                : id.StartsWith("stone", StringComparison.OrdinalIgnoreCase) ? "zombiestone" : string.Empty;
            if (string.IsNullOrEmpty(workerPrefix))
                continue;
            var workers = building.GetComponentsInChildren<Transform>(true)
                .Where(value => value != building.transform &&
                                value.name.StartsWith(workerPrefix, StringComparison.OrdinalIgnoreCase) &&
                                value.GetComponent<SpriteRenderer>() != null)
                .OrderBy(value => value.name, StringComparer.OrdinalIgnoreCase)
                .Select(value => value.gameObject)
                .ToList();
            if (workers.Count == 0)
                throw new InvalidDataException($"Mine '{id}' has no stationary workers '{workerPrefix}'.");
            bindings.Add((id, workers));
        }

        var serialized = new SerializedObject(cycle);
        var property = serialized.FindProperty("mineWorkers") ??
                       throw new MissingFieldException(nameof(MainCycle_WhoHeroes), "mineWorkers");
        property.arraySize = bindings.Count;
        for (var index = 0; index < bindings.Count; index++)
        {
            var element = property.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("mineId").stringValue = bindings[index].id;
            var workers = element.FindPropertyRelative("workers");
            workers.arraySize = bindings[index].workers.Count;
            for (var workerIndex = 0; workerIndex < bindings[index].workers.Count; workerIndex++)
                workers.GetArrayElementAtIndex(workerIndex).objectReferenceValue = bindings[index].workers[workerIndex];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cycle);
    }

    private static void ValidateRequiredHeroes(ExcelWorksheet sheet, IEnumerable<string> requiredHeroIds)
    {
        foreach (var id in requiredHeroIds)
        {
            var row = FindRow(sheet, 1, id);
            if (row <= 0)
                throw new InvalidDataException("Required Heroes entry is missing: " + id);
            foreach (var header in new[] { "ORIGIN", "CLASS", "SKILLBASIC" })
                if (string.IsNullOrWhiteSpace(CellText(sheet, row, header)) ||
                    string.Equals(CellText(sheet, row, header), "x", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Heroes entry '{id}' is missing required {header}.");
        }
    }

    private static void ValidateCombatHeroes(ExcelWorksheet sheet, IEnumerable<string> combatHeroIds)
    {
        foreach (var id in combatHeroIds)
        {
            var row = FindRow(sheet, 1, id);
            if (row <= 0)
                throw new InvalidDataException("Required combat Heroes entry is missing: " + id);
            if (CellText(sheet, row, "BUILDING") == "1" || CellText(sheet, row, "COUNTASUNIT") == "0")
                throw new InvalidDataException($"Combat Heroes entry '{id}' was converted to a scene building.");
        }
    }

    private static void SplitBuildingId(string id, out string baseId, out int level)
    {
        var split = id.Length;
        while (split > 0 && char.IsDigit(id[split - 1]))
            split--;

        if (split < id.Length && int.TryParse(id.Substring(split), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out level))
        {
            baseId = id.Substring(0, split);
            return;
        }

        baseId = id;
        level = 0;
    }

    private static int FindRow(ExcelWorksheet sheet, int column, string value)
    {
        for (var row = 1; row <= sheet.Dimension.End.Row; row++)
            if (string.Equals(sheet.Cells[row, column].Text.Trim(), value, StringComparison.OrdinalIgnoreCase))
                return row;
        return -1;
    }

    private static int FindColumn(ExcelWorksheet sheet, string value)
    {
        for (var column = 1; column <= sheet.Dimension.End.Column; column++)
            if (string.Equals(sheet.Cells[1, column].Text.Trim(), value, StringComparison.OrdinalIgnoreCase))
                return column;
        return -1;
    }

    private static void SetMetaValue(ExcelWorksheet sheet, string key, object value)
    {
        var row = FindRow(sheet, 1, key);
        if (row <= 0)
        {
            row = sheet.Dimension.End.Row + 1;
            sheet.InsertRow(row, 1, row - 1);
        }

        sheet.Cells[row, 1].Value = key;
        if (value is string text)
        {
            sheet.Cells[row, 2].Value = 0;
            sheet.Cells[row, 3].Value = text;
        }
        else
        {
            sheet.Cells[row, 2].Value = value;
            sheet.Cells[row, 3].Value = null;
        }
    }

    private static void ValidateWorkbook(string fullPath)
    {
        using (var package = new ExcelPackage(new FileInfo(fullPath)))
        {
            var items = RequireSheet(package, "ITEMS");
            foreach (var id in new[] { GoldResourceId, WoodResourceId, StoneResourceId })
                if (FindRow(items, 1, id) <= 0)
                    throw new InvalidDataException("ITEMS resource is missing after save: " + id);

            var player = RequireSheet(package, "PLAYER");
            var itemsRow = FindRow(player, 1, "items");
            if (itemsRow <= 0 || string.IsNullOrWhiteSpace(player.Cells[itemsRow, 2].Text))
                throw new InvalidDataException("PLAYER start pack is missing.");

            var heroes = RequireSheet(package, "Heroes");
            foreach (var header in new[] { "FOUND_IN", "ENCOUNTER" })
                if (FindColumn(heroes, header) <= 0)
                    throw new InvalidDataException("Heroes column is missing after save: " + header);

            var sceneIds = CollectSceneIds();
            var combatHeroIds = CollectCombatHeroIds(package);
            var collisions = sceneIds.Intersect(combatHeroIds, StringComparer.OrdinalIgnoreCase).ToArray();
            if (collisions.Length > 0)
                throw new InvalidDataException("Scene IDs overlap combat Heroes: " + string.Join(", ", collisions));
            var requiredHeroIds = CollectRequiredHeroIds(package, sceneIds);
            ValidateRequiredHeroes(heroes, requiredHeroIds);
            ValidateCombatHeroes(heroes, combatHeroIds);
            foreach (var id in sceneIds)
            {
                var row = FindRow(heroes, 1, id);
                if (row <= 0)
                    throw new InvalidDataException("Heroes scene entry is missing after save: " + id);
                if (CellText(heroes, row, "LEVEL") != "1")
                    throw new InvalidDataException("Heroes scene entry must load under its exact id at level 1: " + id);
            }

            var battles = RequireSheet(package, "BATTLES");
            if (FindColumn(battles, "REQSTART") <= 0)
                throw new InvalidDataException("BATTLES column is missing after save: REQSTART");

            var buildings = RequireSheet(package, "BUILDINGS");
            foreach (var id in sceneIds)
            {
                SplitBuildingId(id, out var baseId, out var level);
                if (HasBuilding(buildings, baseId, level))
                    throw new InvalidDataException("Scene state must not be stored in BUILDINGS: " + id);
            }

            var meta = RequireSheet(package, "METACONF");
            foreach (var pair in MetaValues)
                if (FindRow(meta, 1, pair.Key) <= 0)
                    throw new InvalidDataException("METACONF entry is missing after save: " + pair.Key);
            var upgradeCostRow = FindRow(meta, 1, "upgrade_cost");
            if (upgradeCostRow <= 0 || string.IsNullOrWhiteSpace(meta.Cells[upgradeCostRow, 3].Text))
                throw new InvalidDataException("WhoHeroes upgrade_cost is missing.");
            var forecastTextRow = FindRow(meta, 1, "whoheroes_text_night_forecast");
            if (forecastTextRow <= 0 || string.IsNullOrWhiteSpace(meta.Cells[forecastTextRow, 3].Text))
                throw new InvalidDataException("WhoHeroes night forecast text is missing.");
            var steamUrlRow = FindRow(meta, 1, "whoheroes_steam_url");
            if (steamUrlRow <= 0 || !string.Equals(meta.Cells[steamUrlRow, 3].Text.Trim(), SteamUrl,
                    StringComparison.Ordinal))
                throw new InvalidDataException("WhoHeroes Steam URL is missing or differs from GDD.");
            var traderTravelRow = FindRow(meta, 1, "whoheroes_trader_travel_seconds");
            if (traderTravelRow <= 0 || !float.TryParse(meta.Cells[traderTravelRow, 2].Text,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var traderTravelSeconds) ||
                traderTravelSeconds <= 0f)
                throw new InvalidDataException("WhoHeroes trader travel duration must be positive.");
            foreach (var key in new[]
                     {
                         "whoheroes_production_interval", "whoheroes_delivery_amount", "whoheroes_carrier_speed",
                         "whoheroes_expedition_max_active", "whoheroes_perk_offer_count",
                         "whoheroes_perk_pick_count", "whoheroes_castle_units",
                         "whoheroes_expedition_defenses", "whoheroes_boost_sources",
                         "whoheroes_run_boost_stats", "whoheroes_onboarding_tasks"
                     })
                if (FindRow(meta, 1, key) > 0)
                    throw new InvalidDataException("Obsolete local gameplay parameter is still present: " + key);

            var dynamics = RequireSheet(package, "DYNAMIC_ID");
            var multiColumn = FindColumn(dynamics, "MULTI");
            if (multiColumn <= 0)
                throw new InvalidDataException("DYNAMIC_ID column MULTI is missing.");
            for (var row = 2; row <= dynamics.Dimension.End.Row; row++)
            {
                var id = dynamics.Cells[row, 1].Text.Trim();
                if (id.StartsWith("whoheroes_buy_", StringComparison.Ordinal) &&
                    dynamics.Cells[row, multiColumn].Text != "1")
                    throw new InvalidDataException("WhoHeroes purchase must be repeatable in Minimus: " + id);
            }

            var lootSets = RequireSheet(package, "LOOTSET");
            if (!HasLootSet(lootSets, MainCycle_WhoHeroes.OnboardingTaskSetId))
                throw new InvalidDataException("WhoHeroes onboarding TASKS set is missing from LOOTSET.");
            if (!HasLootSetPrefix(lootSets, MainCycle_WhoHeroes.CastleOfferSetPrefix) ||
                !HasLootSetPrefix(lootSets, MainCycle_WhoHeroes.ExpeditionDefenseSetPrefix) ||
                !HasLootSetPrefix(lootSets, MainCycle_WhoHeroes.BoostStatSetPrefix))
                throw new InvalidDataException("WhoHeroes composite config was not migrated to LOOTSET.");

            var captureRow = FindRow(dynamics, 1, MainCycle_WhoHeroes.CaptureDynamicId);
            if (captureRow <= 0 ||
                CellText(dynamics, captureRow, "PAR_UPGRADE") != "level,1" ||
                CellText(dynamics, captureRow, "MULTI") != "1")
                throw new InvalidDataException("WhoHeroes capture must use a repeatable Minimus DYNAMIC_ID.");
            foreach (var setId in LootSetIds(lootSets)
                         .Where(value => value.StartsWith(MainCycle_WhoHeroes.ExpeditionDefenseSetPrefix,
                             StringComparison.Ordinal)))
            {
                var targetId = setId.Substring(MainCycle_WhoHeroes.ExpeditionDefenseSetPrefix.Length);
                var targetRow = FindRow(heroes, 1, targetId);
                if (targetRow <= 0 ||
                    CellText(heroes, targetRow, "DYNAMIC") != MainCycle_WhoHeroes.CaptureDynamicId)
                    throw new InvalidDataException("WhoHeroes expedition target has no capture DYNAMIC: " + targetId);
            }
        }
    }

    private static string CellText(ExcelWorksheet sheet, int row, string header)
    {
        var column = FindColumn(sheet, header);
        if (column <= 0)
            throw new InvalidDataException("Required worksheet column is missing: " + header);
        return sheet.Cells[row, column].Text.Trim();
    }

    private static IReadOnlyCollection<string> CollectRequiredHeroIds(
        ExcelPackage package, IEnumerable<string> sceneIds)
    {
        var result = new HashSet<string>(sceneIds.Where(value => !string.IsNullOrWhiteSpace(value)),
            StringComparer.OrdinalIgnoreCase);
        var meta = RequireSheet(package, "METACONF");
        AddId(result, MetaString(meta, "whoheroes_starting_castle"));
        AddId(result, MetaString(meta, "whoheroes_starting_unit"));

        var lootSets = RequireSheet(package, "LOOTSET");
        AddLootSetItemsByPrefix(result, lootSets, MainCycle_WhoHeroes.CastleOfferSetPrefix);
        AddLootSetItemsByPrefix(result, lootSets, MainCycle_WhoHeroes.ExpeditionDefenseSetPrefix);
        AddLootSetItems(result, lootSets, MainCycle_WhoHeroes.TavernSetId);
        AddLootSetItems(result, lootSets, MainCycle_WhoHeroes.TraderSetId);
        return result;
    }

    private static IReadOnlyCollection<string> CollectCombatHeroIds(ExcelPackage package)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var meta = RequireSheet(package, "METACONF");
        AddId(result, MetaString(meta, "whoheroes_starting_unit"));
        var lootSets = RequireSheet(package, "LOOTSET");
        AddLootSetItemsByPrefix(result, lootSets, MainCycle_WhoHeroes.CastleOfferSetPrefix);
        AddLootSetItemsByPrefix(result, lootSets, MainCycle_WhoHeroes.ExpeditionDefenseSetPrefix);
        AddLootSetItems(result, lootSets, MainCycle_WhoHeroes.TavernSetId);
        AddLootSetItems(result, lootSets, MainCycle_WhoHeroes.TraderSetId);
        return result;
    }

    private static string MetaString(ExcelWorksheet sheet, string key)
    {
        var row = FindRow(sheet, 1, key);
        return row <= 0 ? string.Empty : CellText(sheet, row, "STRINGVAL");
    }

    private static void MigrateCompositeMetaToLootSets(ExcelPackage package, ExcelWorksheet meta)
    {
        var lootSets = RequireSheet(package, "LOOTSET");
        MigrateMapToLootSets(meta, lootSets, "whoheroes_castle_units",
            MainCycle_WhoHeroes.CastleOfferSetPrefix, false);
        MigrateMapToLootSets(meta, lootSets, "whoheroes_boost_sources",
            MainCycle_WhoHeroes.BoostStatSetPrefix, false);
        MigrateMapToLootSets(meta, lootSets, "whoheroes_expedition_defenses",
            MainCycle_WhoHeroes.ExpeditionDefenseSetPrefix, true);

        var onboarding = MetaString(meta, "whoheroes_onboarding_tasks")
            .Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim()).Where(value => value.Length > 0)
            .Select(value => (item: value, amount1: 1, amount2: 1)).ToList();
        if (onboarding.Count > 0)
            ReplaceLootSet(lootSets, MainCycle_WhoHeroes.OnboardingTaskSetId, onboarding);

        foreach (var key in new[]
                 {
                     "whoheroes_castle_units", "whoheroes_boost_sources",
                     "whoheroes_expedition_defenses", "whoheroes_run_boost_stats",
                     "whoheroes_onboarding_tasks"
                 })
        {
            var row = FindRow(meta, 1, key);
            if (row > 0)
                meta.DeleteRow(row);
        }
    }

    private static void EnsureExpeditionCaptureDynamic(ExcelPackage package)
    {
        var dynamics = RequireSheet(package, "DYNAMIC_ID");
        var idColumn = FindColumn(dynamics, "ID");
        var parUpgradeColumn = FindColumn(dynamics, "PAR_UPGRADE");
        var multiColumn = FindColumn(dynamics, "MULTI");
        if (idColumn <= 0 || parUpgradeColumn <= 0 || multiColumn <= 0)
            throw new InvalidDataException("DYNAMIC_ID capture schema is incomplete.");

        var dynamicRow = FindRow(dynamics, idColumn, MainCycle_WhoHeroes.CaptureDynamicId);
        if (dynamicRow <= 0)
        {
            dynamicRow = dynamics.Dimension.End.Row + 1;
            dynamics.InsertRow(dynamicRow, 1, dynamicRow - 1);
        }
        for (var column = 1; column <= dynamics.Dimension.End.Column; column++)
            if (string.IsNullOrWhiteSpace(dynamics.Cells[dynamicRow, column].Text))
                dynamics.Cells[dynamicRow, column].Value = "x";
        dynamics.Cells[dynamicRow, idColumn].Value = MainCycle_WhoHeroes.CaptureDynamicId;
        dynamics.Cells[dynamicRow, parUpgradeColumn].Value = "level,1";
        dynamics.Cells[dynamicRow, multiColumn].Value = 1;

        var heroes = RequireSheet(package, "Heroes");
        var heroDynamicColumn = FindColumn(heroes, "DYNAMIC");
        if (heroDynamicColumn <= 0)
            throw new InvalidDataException("Heroes DYNAMIC column is missing.");

        var lootSets = RequireSheet(package, "LOOTSET");
        foreach (var setId in LootSetIds(lootSets)
                     .Where(value => value.StartsWith(MainCycle_WhoHeroes.ExpeditionDefenseSetPrefix,
                         StringComparison.Ordinal)))
        {
            var targetId = setId.Substring(MainCycle_WhoHeroes.ExpeditionDefenseSetPrefix.Length);
            var targetRow = FindRow(heroes, 1, targetId);
            if (targetRow <= 0)
                throw new InvalidDataException("Expedition target is missing from Heroes: " + targetId);
            heroes.Cells[targetRow, heroDynamicColumn].Value = MainCycle_WhoHeroes.CaptureDynamicId;
        }
    }

    private static void MigrateMapToLootSets(
        ExcelWorksheet meta, ExcelWorksheet lootSets, string metaKey, string setPrefix, bool defense)
    {
        var source = MetaString(meta, metaKey);
        if (string.IsNullOrWhiteSpace(source))
            return;

        foreach (var rawEntry in source.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = rawEntry.Split(',').Select(value => value.Trim()).ToArray();
            if (parts.Length < 2 || parts[0].Length == 0 || parts[1].Length == 0)
                throw new InvalidDataException("Invalid composite METACONF entry: " + metaKey + " = " + rawEntry);

            var count = defense && parts.Length > 3 && int.TryParse(parts[3], out var parsedCount)
                ? Mathf.Max(1, parsedCount)
                : 1;
            var level = defense && parts.Length > 2 && int.TryParse(parts[2], out var parsedLevel)
                ? Mathf.Max(1, parsedLevel)
                : 1;
            ReplaceLootSet(lootSets, setPrefix + parts[0],
                new List<(string item, int amount1, int amount2)> { (parts[1], count, level) });
        }
    }

    private static void ReplaceLootSet(
        ExcelWorksheet sheet, string setId, IReadOnlyList<(string item, int amount1, int amount2)> entries)
    {
        RemoveLootSet(sheet, setId);
        if (entries == null || entries.Count == 0)
            return;

        var nameColumn = FindColumn(sheet, "NAME");
        var groupColumn = FindColumn(sheet, "GROUP");
        var weightColumn = FindColumn(sheet, "WEIGHT");
        var amount1Column = FindColumn(sheet, "AMOUNT1");
        var amount2Column = FindColumn(sheet, "AMOUNT2");
        var itemColumn = FindColumn(sheet, "ITEM");
        var shardColumn = FindColumn(sheet, "SHARD");
        if (new[] { nameColumn, groupColumn, weightColumn, amount1Column, amount2Column, itemColumn, shardColumn }
            .Any(value => value <= 0))
            throw new InvalidDataException("LOOTSET schema is incomplete.");

        var firstRow = sheet.Dimension.End.Row + 1;
        sheet.InsertRow(firstRow, entries.Count, firstRow - 1);
        for (var index = 0; index < entries.Count; index++)
        {
            var row = firstRow + index;
            sheet.Cells[row, nameColumn].Value = index == 0 ? setId : "x";
            sheet.Cells[row, groupColumn].Value = 0;
            sheet.Cells[row, weightColumn].Value = 1;
            sheet.Cells[row, amount1Column].Value = Mathf.Max(1, entries[index].amount1);
            sheet.Cells[row, amount2Column].Value = Mathf.Max(1, entries[index].amount2);
            sheet.Cells[row, itemColumn].Value = entries[index].item;
            sheet.Cells[row, shardColumn].Value = 0;
        }
    }

    private static void RemoveLootSet(ExcelWorksheet sheet, string setId)
    {
        var nameColumn = FindColumn(sheet, "NAME");
        var start = -1;
        var end = -1;
        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var name = sheet.Cells[row, nameColumn].Text.Trim();
            if (start < 0)
            {
                if (string.Equals(name, setId, StringComparison.Ordinal))
                    start = end = row;
                continue;
            }
            if (!string.IsNullOrEmpty(name) && !string.Equals(name, "x", StringComparison.OrdinalIgnoreCase))
                break;
            end = row;
        }
        if (start > 0)
            sheet.DeleteRow(start, end - start + 1);
    }

    private static void AddMapIds(HashSet<string> result, string value, bool includeFirst, bool includeSecond)
    {
        foreach (var entry in value.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(',');
            if (includeFirst && parts.Length > 0)
                AddId(result, parts[0]);
            if (includeSecond && parts.Length > 1)
                AddId(result, parts[1]);
        }
    }

    private static void AddLootSetItems(HashSet<string> result, ExcelWorksheet sheet, string setId)
    {
        var nameColumn = FindColumn(sheet, "NAME");
        var itemColumn = FindColumn(sheet, "ITEM");
        if (nameColumn <= 0 || itemColumn <= 0)
            throw new InvalidDataException("LOOTSET requires NAME and ITEM columns.");

        var currentSet = string.Empty;
        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var name = sheet.Cells[row, nameColumn].Text.Trim();
            if (!string.IsNullOrEmpty(name) && !string.Equals(name, "x", StringComparison.OrdinalIgnoreCase))
                currentSet = name;
            if (!string.Equals(currentSet, setId, StringComparison.Ordinal))
                continue;
            AddId(result, sheet.Cells[row, itemColumn].Text);
        }
    }

    private static void AddLootSetItemsByPrefix(HashSet<string> result, ExcelWorksheet sheet, string prefix)
    {
        foreach (var setId in LootSetIds(sheet).Where(value => value.StartsWith(prefix, StringComparison.Ordinal)))
            AddLootSetItems(result, sheet, setId);
    }

    private static bool HasLootSet(ExcelWorksheet sheet, string setId)
    {
        return LootSetIds(sheet).Contains(setId, StringComparer.Ordinal);
    }

    private static bool HasLootSetPrefix(ExcelWorksheet sheet, string prefix)
    {
        return LootSetIds(sheet).Any(value => value.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static IEnumerable<string> LootSetIds(ExcelWorksheet sheet)
    {
        var nameColumn = FindColumn(sheet, "NAME");
        if (nameColumn <= 0)
            throw new InvalidDataException("LOOTSET requires NAME column.");
        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var value = sheet.Cells[row, nameColumn].Text.Trim();
            if (!string.IsNullOrEmpty(value) && !string.Equals(value, "x", StringComparison.OrdinalIgnoreCase))
                yield return value;
        }
    }

    private static void AddId(HashSet<string> result, string value)
    {
        var id = value?.Trim();
        if (!string.IsNullOrEmpty(id) && !string.Equals(id, "x", StringComparison.OrdinalIgnoreCase))
            result.Add(id);
    }
}
