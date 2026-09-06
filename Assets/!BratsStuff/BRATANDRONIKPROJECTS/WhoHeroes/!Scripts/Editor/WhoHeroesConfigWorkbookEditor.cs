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
    private sealed class DemoBattleEnemy
    {
        public readonly string id;
        public readonly int amount;
        public readonly float time;
        public readonly int side;

        public DemoBattleEnemy(string id, int amount, float time, int side)
        {
            this.id = id;
            this.amount = amount;
            this.time = time;
            this.side = side;
        }
    }

    private const string GoldResourceId = "gold";
    private const string WoodResourceId = "wood";
    private const string StoneResourceId = "stone";
    private const int DemoDayDurationSeconds = 360;
    private const int DemoInitialGold = 120;
    private const int DemoInitialWood = 0;
    private const int DemoInitialStone = 0;
    private const int DemoStartingPikemen = 2;
    private const string PikeSweepSkillId = "whoheroes_pike_sweep";
    private const string RiderPushSkillId = "whoheroes_rider_push";
    private const string CyclopRockfallSkillId = "whoheroes_cyclop_rockfall";
    private const string AngelMassHealSkillId = "whoheroes_angel_mass_heal";
    private const string SwordGuardSkillId = "whoheroes_sword_guard";
    private const string SatyrCleaveSkillId = "whoheroes_satyr_cleave";
    private const string DevilInfernoSkillId = "whoheroes_devil_inferno";
    private const string EfreedFireRainSkillId = "whoheroes_efreed_fire_rain";
    private const string WorkbookAssetPath = "Assets/StreamingAssets/WhoHeroes/Config_whoheroes.xlsx";
    private const string SceneAssetPath = "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Scenes/WhoHeroes_System.unity";
    private const string CarrierPrefabAssetPath =
        "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Prefabs/Chars/keeper.prefab";
    private const string TraderPrefabAssetPath =
        "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Prefabs/Chars/king.prefab";
    private const string DemonStatePrefabAssetPath =
        "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Prefabs/Behaviors/whoheroes_demon_state.prefab";
    private const string CombatUnitPrefabAssetPath =
        "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Prefabs/Scene/WhoHeroesCombatUnit.prefab";
    private const string SatyrSpriteAssetPath =
        "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Chars/great elf/satyr/sprite_sheet_satyr_0_16x16.png";
    private static readonly IReadOnlyDictionary<string, string> DemoEnemySpritePaths =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "imp", "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Chars/dark bastion/imp/sprite_sheet_imp_0_16x16.png" },
            { "hellhound", "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Chars/dark bastion/hell hound/sprite_sheet_hell_hound_1_16x16.png" },
            { "efreed", "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Chars/dark bastion/efreet/sprite_sheet_efreet_0_16x16.png" },
            { "devil", "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Chars/dark bastion/devil/sprite_sheet_devil_0_16x16.png" },
            { "gog", "Assets/!BratsStuff/BRATANDRONIKPROJECTS/WhoHeroes/!Fantacy/!Chars/dark bastion/gog/sprite_sheet_gog_0_16x16.png" }
        };
    private static readonly IReadOnlyDictionary<string, string> DemoEnemyDisplayNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "imp", "Sparksnout the Imp" },
            { "hellhound", "Ashfang the Hellhound" },
            { "gog", "Cinder-Eye the Gog" },
            { "efreed", "Azhar the Efreet" },
            { "devil", "Malphas the Devil" },
            { "treant", "Oldroot the Treant" },
            { "gost", "Hollowshade the Ghost" },
            { "monkup", "Brother Granite" },
            { "naga", "Ssilara the Naga" },
            { "magicel", "Elowen the Arcane Elf" },
            { "centaur", "Swiftmane the Centaur" },
            { "harpybrown", "Dustwing the Harpy" },
            { "vampire", "Lord Vesper the Vampire" },
            { "shaman", "Grimtotem the Shaman" },
            { "monk", "Brother Rowan" },
            { "windel", "Zephyra the Wind Elf" },
            { "djinn", "Zahir the Djinn" },
            { "gostfem", "Lady Wisp the Ghost" },
            { "gargoyle", "Stoneclaw the Gargoyle" },
            { "stormel", "Tempestra the Storm Elf" },
            { "harpywhite", "Frostwing the Harpy" },
            { "boss", "The Dread Warden" },
            { "riderevil", "Blackspur the Dark Rider" },
            { "magmael", "Cindara the Magma Elf" },
            { "fireel", "Pyralis the Fire Elf" },
            { "lion", "Sunmane the Lion" },
            { "cyclop", "Boulder-Eye the Cyclops" },
            { "lich", "Morcant the Lich" }
        };
    private const string SteamUrl =
        "https://store.steampowered.com/app/4197340/WHO_THE_HELL_OPENED_THE_PORTAL/";
    private const string WoodQuestId = "whoheroes_quest_wood";
    private const string WoodQuest20Id = "whoheroes_quest_wood_20";
    private const string WoodQuest50Id = "whoheroes_quest_wood_50";
    private const string StoneQuestId = "whoheroes_quest_stone";
    private const string StoneQuest20Id = "whoheroes_quest_stone_20";
    private const string StoneQuest50Id = "whoheroes_quest_stone_50";
    private const string OnboardingTasks =
        WoodQuestId + "#" + WoodQuest20Id + "#" + WoodQuest50Id + "#" +
        StoneQuestId + "#" + StoneQuest20Id + "#" + StoneQuest50Id;
    private static readonly KeyValuePair<string, string>[] LocationValues =
    {
        new KeyValuePair<string, string>("grass0", "Sniffleaf Grove"),
        new KeyValuePair<string, string>("grass1", "Whisperlog Holt"),
        new KeyValuePair<string, string>("swamp0", "Rotroot Mire"),
        new KeyValuePair<string, string>("ice0", "Snowgrin Pass"),
        new KeyValuePair<string, string>("ice1", "Icetooth Point"),
        new KeyValuePair<string, string>("dirt0", "Dustbeard Bluff"),
        new KeyValuePair<string, string>("lava0", "Burnjaw Pit"),
        new KeyValuePair<string, string>("lava1", "Ashsnort Crater"),
        new KeyValuePair<string, string>("sand0", "Hotstep Barrens"),
        new KeyValuePair<string, string>("sand1", "Sunburn Flats"),
        new KeyValuePair<string, string>("under0", "Grumblepeak")
    };
    private static readonly KeyValuePair<string, string>[] MapBuildingNames =
    {
        new KeyValuePair<string, string>("castle", "Castle"),
        new KeyValuePair<string, string>("tower", "Tower"),
        new KeyValuePair<string, string>("tavern", "Tavern"),
        new KeyValuePair<string, string>("expedition", "Expedition Hub"),
        new KeyValuePair<string, string>("wood0", "Sawmill"),
        new KeyValuePair<string, string>("stone0", "Stone Forge"),
        new KeyValuePair<string, string>("portalingrass0", "Sniffleaf Grove Portal"),
        new KeyValuePair<string, string>("portalinswamp0", "Rotroot Mire Portal"),
        new KeyValuePair<string, string>("portalinice0", "Snowgrin Pass Portal"),
        new KeyValuePair<string, string>("portalindirt0", "Dustbeard Bluff Portal"),
        new KeyValuePair<string, string>("portalinlava0", "Burnjaw Pit Portal"),
        new KeyValuePair<string, string>("portalinsand0", "Hotstep Barrens Portal"),
        new KeyValuePair<string, string>("portalinunder0", "Grumblepeak Portal"),
        new KeyValuePair<string, string>("wood1", "Sawmill"),
        new KeyValuePair<string, string>("portalingrass1", "Whisperlog Holt Portal"),
        new KeyValuePair<string, string>("stone1", "Stone Forge"),
        new KeyValuePair<string, string>("obelisk0", "Blooming Obelisk"),
        new KeyValuePair<string, string>("market", "Market"),
        new KeyValuePair<string, string>("portalingrass2", "Mossy Mumble Portal"),
        new KeyValuePair<string, string>("ore0", "Ore Pit"),
        new KeyValuePair<string, string>("random0", "Wonder Vein"),
        new KeyValuePair<string, string>("portalinswamp1", "Croakfen Portal"),
        new KeyValuePair<string, string>("gem0", "Gem Mine"),
        new KeyValuePair<string, string>("fountain", "Fontain of Benefits"),
        new KeyValuePair<string, string>("obelisk1", "Frozen Obelisk"),
        new KeyValuePair<string, string>("portalinice1", "Icetooth Point Portal"),
        new KeyValuePair<string, string>("chapel", "Helpful Shrine"),
        new KeyValuePair<string, string>("portalinice2", "Frostmirth Reach Portal"),
        new KeyValuePair<string, string>("university", "University"),
        new KeyValuePair<string, string>("library", "Skill Tower"),
        new KeyValuePair<string, string>("kitchen", "Furnace"),
        new KeyValuePair<string, string>("obelisk2", "Dusty Obelisk"),
        new KeyValuePair<string, string>("portalindirt1", "Pebble Crown Portal"),
        new KeyValuePair<string, string>("mask", "Cave of Aggression"),
        new KeyValuePair<string, string>("portalinlava1", "Ashsnort Crater Portal"),
        new KeyValuePair<string, string>("ore1", "Ore Pit"),
        new KeyValuePair<string, string>("prison", "Prison"),
        new KeyValuePair<string, string>("sulfur0", "Sulfur Mine"),
        new KeyValuePair<string, string>("obelisk3", "Sand Obelisk"),
        new KeyValuePair<string, string>("portalinsand1", "Sunburn Flats Portal"),
        new KeyValuePair<string, string>("sulfur1", "Sulfur Mine"),
        new KeyValuePair<string, string>("sphinx", "Sphinx")
    };
    private static readonly KeyValuePair<string, object>[] MetaValues =
    {
        new KeyValuePair<string, object>("mode_manhattan", -1),
        new KeyValuePair<string, object>("mode_isometric", 0),
        new KeyValuePair<string, object>("mode_hex", 0),
        new KeyValuePair<string, object>("use_2d_navmesh", 0),
        new KeyValuePair<string, object>("blood_death", 0),
        new KeyValuePair<string, object>("sim_time_cont", 1),
        new KeyValuePair<string, object>("whoheroes_day_duration", DemoDayDurationSeconds),
        new KeyValuePair<string, object>("whoheroes_daily_gold", 10),
        new KeyValuePair<string, object>("whoheroes_territory_gold", 5),
        new KeyValuePair<string, object>("whoheroes_start_active_portals", 1),
        new KeyValuePair<string, object>("whoheroes_mine_max_level", 5),
        new KeyValuePair<string, object>("whoheroes_wood_production_interval", 60),
        new KeyValuePair<string, object>("whoheroes_stone_production_interval", 120),
        new KeyValuePair<string, object>("whoheroes_resource_gold", "gold"),
        new KeyValuePair<string, object>("whoheroes_resource_wood", "wood"),
        new KeyValuePair<string, object>("whoheroes_resource_stone", "stone"),
        new KeyValuePair<string, object>("whoheroes_start_unit_amount", DemoStartingPikemen),
        new KeyValuePair<string, object>("whoheroes_boost_percent", 10),
        new KeyValuePair<string, object>("whoheroes_expedition_max_stacks", 3),
        new KeyValuePair<string, object>("whoheroes_trader_start_night", 2),
        new KeyValuePair<string, object>("whoheroes_tavern_offer_count", 2),
        new KeyValuePair<string, object>("whoheroes_trader_gold_surcharge", 25),
        new KeyValuePair<string, object>("whoheroes_trader_power_multiplier", 115),
        new KeyValuePair<string, object>("whoheroes_trader_travel_seconds", 3),
        new KeyValuePair<string, object>("whoheroes_steam_url", SteamUrl)
    };
    private static readonly KeyValuePair<string, string>[] UiTextValues =
    {
        new KeyValuePair<string, string>("castle", "Castle"),
        new KeyValuePair<string, string>("castle_descr", ""),
        new KeyValuePair<string, string>("tavern", "Tavern"),
        new KeyValuePair<string, string>("tavern_descr", "The place for random defenders for relax. You can hire them while they are here!"),
        new KeyValuePair<string, string>("tower", "Guard Tower"),
        new KeyValuePair<string, string>("tower_descr", "Here you can choose defenders to protect your Castle from guests from the Hell during night attacks."),
        new KeyValuePair<string, string>("expedition", "Expedition Hall"),
        new KeyValuePair<string, string>("expedition_descr", "Here you can choose expeditors to explore new lands and expand your Kindom."),
        new KeyValuePair<string, string>("market", "Market"),
        new KeyValuePair<string, string>("market_descr", "Allows to sell build resourses to receive gold coins."),
        new KeyValuePair<string, string>("factorywood", "Lumber Mill"),
        new KeyValuePair<string, string>("factorystone", "Stone Quarry"),
        new KeyValuePair<string, string>("factory", "Produces resourses nessesary for restore, upgrade and hire "),
        new KeyValuePair<string, string>("portal", "Portal"),
        new KeyValuePair<string, string>("portal_descr", "Gives busts in battles during expantion and defence."),
        new KeyValuePair<string, string>("bust", "Gives busts in battles during expantion and defence."),
        new KeyValuePair<string, string>("dbuildingstory", "Reveals secrets, helps complete tasks and receive additional rewards."),
        new KeyValuePair<string, string>("generic_building_descr", "Gives the gold coins as passive tax income each day."),
        new KeyValuePair<string, string>("tent", "Pikeman Camp"),
        new KeyValuePair<string, string>("tent_descr", "Here is the habitant for defenders. You can hire them into your army!"),
        new KeyValuePair<string, string>("barracks", "Barracks"),
        new KeyValuePair<string, string>("barracks_descr", "Here is the habitant for defenders. You can hire them into your army!"),
        new KeyValuePair<string, string>("stables", "Stables"),
        new KeyValuePair<string, string>("stables_descr", "Here is the habitant for defenders. You can hire them into your army!"),
        new KeyValuePair<string, string>("angelfort", "Angel Fort"),
        new KeyValuePair<string, string>("angelfort_descr", "Here is the habitant for defenders. You can hire them into your army!"),
        new KeyValuePair<string, string>("obelisk0", "Obelisk"),
        new KeyValuePair<string, string>("obelisk1", "Obelisk"),
        new KeyValuePair<string, string>("obelisk2", "Obelisk"),
        new KeyValuePair<string, string>("obelisk3", "Obelisk"),
        new KeyValuePair<string, string>("university", "University"),
        new KeyValuePair<string, string>("library", "Library"),
        new KeyValuePair<string, string>("kitchen", "Kitchen"),
        new KeyValuePair<string, string>("fountain", "Fountain"),
        new KeyValuePair<string, string>("chapel", "Chapel"),
        new KeyValuePair<string, string>("mask", "War Mask"),
        new KeyValuePair<string, string>("prison", "Prison"),
        new KeyValuePair<string, string>("sphinx", "Sphinx"),
        new KeyValuePair<string, string>("random0", "Event Site"),
        new KeyValuePair<string, string>("gem0", "Gem Mine"),
        new KeyValuePair<string, string>("ore0", "Ore Mine"),
        new KeyValuePair<string, string>("ore1", "Ore Mine"),
        new KeyValuePair<string, string>("sulfur0", "Sulfur Mine"),
        new KeyValuePair<string, string>("sulfur1", "Sulfur Mine")
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
            SetMetaValue(meta, "whoheroes_day_duration", DemoDayDurationSeconds);
            SetMetaValue(meta, "whoheroes_trader_travel_seconds", 3);
            SetMetaValue(meta, "whoheroes_tavern_offer_count", 2);
            SetMetaValue(meta, "whoheroes_steam_url", SteamUrl);
            SetMetaValue(meta, "whoheroes_resource_gold", "gold");
            SetMetaValue(meta, "whoheroes_resource_wood", "wood");
            SetMetaValue(meta, "whoheroes_resource_stone", "stone");
            SetMetaValue(meta, "whoheroes_initial_gold", DemoInitialGold);
            SetMetaValue(meta, "whoheroes_initial_wood", DemoInitialWood);
            SetMetaValue(meta, "whoheroes_initial_stone", DemoInitialStone);
            SetMetaValue(meta, "whoheroes_start_unit_amount", DemoStartingPikemen);
            foreach (var pair in UiTextValues)
                SetMetaValue(meta, "whoheroes_text_" + pair.Key, pair.Value);
            foreach (var pair in MapBuildingNames)
                SetMetaValue(meta, "whoheroes_text_" + pair.Key, pair.Value);
            SetPlayerStartPack(RequireSheet(package, "PLAYER"));
            package.Save();
        }

        AssetDatabase.ImportAsset(WorkbookAssetPath, ImportAssetOptions.ForceUpdate);
        ValidateWorkbook(fullPath);
        Debug.Log("WhoHeroes static metadata updated and validated: " + WorkbookAssetPath);
    }

    [MenuItem("Tools/WhoHeroes/Ensure Demo Resource Tasks")]
    public static void EnsureDemoResourceTasks()
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
                var tasks = RequireSheet(package, "TASKS");
                EnsureResourceTask(tasks, WoodQuestId, WoodResourceId, 5, "Collect 5 wood");
                EnsureResourceTask(tasks, WoodQuest20Id, WoodResourceId, 20, "Collect 20 wood", WoodQuestId);
                EnsureResourceTask(tasks, WoodQuest50Id, WoodResourceId, 59, "Collect 59 wood", WoodQuest20Id);
                EnsureResourceTask(tasks, StoneQuestId, StoneResourceId, 5, "Collect 5 stone");
                EnsureResourceTask(tasks, StoneQuest20Id, StoneResourceId, 20, "Collect 20 stone", StoneQuestId);
                EnsureResourceTask(tasks, StoneQuest50Id, StoneResourceId, 50, "Collect 50 stone", StoneQuest20Id);

                var onboarding = OnboardingTasks
                    .Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => (item: value, amount1: 1, amount2: 1))
                    .ToList();
                ReplaceLootSet(RequireSheet(package, "LOOTSET"),
                    MainCycle_WhoHeroes.OnboardingTaskSetId, onboarding);
                var meta = RequireSheet(package, "METACONF");
                SetMetaValue(meta, "whoheroes_initial_gold", DemoInitialGold);
                SetMetaValue(meta, "whoheroes_initial_wood", DemoInitialWood);
                SetMetaValue(meta, "whoheroes_initial_stone", DemoInitialStone);
                SetMetaValue(meta, "whoheroes_start_unit_amount", DemoStartingPikemen);
                SetPlayerStartPack(RequireSheet(package, "PLAYER"));
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
        Debug.Log("WhoHeroes wood and stone tasks updated and validated: " + WorkbookAssetPath);
    }

    [MenuItem("Tools/WhoHeroes/Ensure Location Config")]
    public static void EnsureLocationConfig()
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
                var stages = package.Workbook.Worksheets["STAGES"] ??
                             package.Workbook.Worksheets.Add("STAGES");
                if (stages.Dimension == null)
                {
                    stages.Cells[1, 1].Value = "NAME";
                    stages.Cells[1, 2].Value = "DISPLAY_NAME";
                    stages.Cells[1, 3].Value = "NORMAL_BATTLES";
                    stages.Cells[1, 4].Value = "ELITE_BATTLES";
                    stages.Cells[1, 5].Value = "BOSS_BATTLES";
                    stages.Cells[1, 6].Value = "REWARDS";
                }
                var idColumn = EnsureColumn(stages, "NAME");
                var displayNameColumn = EnsureColumn(stages, "DISPLAY_NAME");
                var normalColumn = EnsureColumn(stages, "NORMAL_BATTLES");
                var eliteColumn = EnsureColumn(stages, "ELITE_BATTLES");
                var bossColumn = EnsureColumn(stages, "BOSS_BATTLES");
                var rewardsColumn = EnsureColumn(stages, "REWARDS");
                foreach (var location in LocationValues)
                {
                    var row = FindRow(stages, idColumn, location.Key);
                    if (row <= 0)
                        row = Mathf.Max(2, (stages.Dimension?.End.Row ?? 1) + 1);
                    stages.Cells[row, idColumn].Value = location.Key;
                    stages.Cells[row, displayNameColumn].Value = location.Value;
                    stages.Cells[row, normalColumn].Value = "x";
                    stages.Cells[row, eliteColumn].Value = "x";
                    stages.Cells[row, bossColumn].Value = "x";
                    stages.Cells[row, rewardsColumn].Value = GoldResourceId + ",0";
                }
                package.Save();
            }
            ValidateLocationConfig(fullPath);
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
        Debug.Log("WhoHeroes location IDs and map names updated and validated: " + WorkbookAssetPath);
    }

    [MenuItem("Tools/WhoHeroes/Ensure Map Building Names")]
    public static void EnsureMapBuildingNames()
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
                foreach (var pair in MapBuildingNames)
                    SetMetaValue(meta, "whoheroes_text_" + pair.Key, pair.Value);
                package.Save();
            }
            ValidateMapBuildingNames(fullPath);
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
        Debug.Log("WhoHeroes building names copied from Map and validated: " + WorkbookAssetPath);
    }

    [MenuItem("Tools/WhoHeroes/Ensure Demo Defender Names")]
    public static void EnsureDemoDefenderNames()
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
                foreach (var pair in DemoEnemyDisplayNames)
                    SetMetaValue(meta, "whoheroes_text_" + pair.Key, pair.Value);
                package.Save();
            }
            ValidateDemoDefenderNames(fullPath);
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
        Debug.Log("WhoHeroes demo defender names updated and validated: " + WorkbookAssetPath);
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

    [MenuItem("Tools/WhoHeroes/Configure Demo Content")]
    public static void ConfigureDemoContent()
    {
        var scene = RequireWhoHeroesScene();
        var fullPath = Path.GetFullPath(WorkbookAssetPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("WhoHeroes config workbook was not found.", fullPath);

        var maxStacks = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "peak", 20 },
            { "sword", 15 },
            { "rider", 5 },
            { "angel", 2 },
            { "satyr", 10 },
            { "cyclop", 3 }
        };
        var castleOffers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "tent", "peak" },
            { "barracks", "sword" },
            { "stables", "rider" },
            { "angelfort", "angel" }
        };
        var tavernUnits = new[] { "peak", "sword", "rider", "angel", "satyr", "cyclop" };
        var tavernAmounts = tavernUnits.ToDictionary(
            id => id,
            id => string.Equals(id, "satyr", StringComparison.Ordinal) ? 3 : 1,
            StringComparer.Ordinal);
        var portalEncounters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "portalingrass0", "1|battle6" },
            { "portalingrass1", "2|battle8" },
            { "portalinswamp0", "3|battle9" }
        };

        var backupPath = Path.GetTempFileName();
        File.Copy(fullPath, backupPath, true);
        try
        {
            using (var package = new ExcelPackage(new FileInfo(fullPath)))
            {
                var heroes = RequireSheet(package, "Heroes");
                var maxStackColumn = EnsureColumn(heroes, "MAX_STACK");
                EnsureSatyrHero(heroes);
                foreach (var pair in maxStacks)
                {
                    var row = FindRow(heroes, 1, pair.Key);
                    if (row <= 0)
                        throw new InvalidDataException("Demo hero is missing from Heroes: " + pair.Key);
                    heroes.Cells[row, maxStackColumn].Value = pair.Value;
                }

                var encounterColumn = FindColumn(heroes, "ENCOUNTER");
                if (encounterColumn <= 0)
                    throw new InvalidDataException("Heroes ENCOUNTER column is missing.");
                for (var row = 2; row <= heroes.Dimension.End.Row; row++)
                {
                    var id = heroes.Cells[row, 1].Text.Trim();
                    if (!id.StartsWith("portalin", StringComparison.Ordinal))
                        continue;
                    heroes.Cells[row, encounterColumn].Value = portalEncounters.TryGetValue(id, out var encounter)
                        ? encounter
                        : "x";
                }

                var lootSets = RequireSheet(package, "LOOTSET");
                var allowedCastleSets = new HashSet<string>(castleOffers.Keys.Select(
                    id => MainCycle_WhoHeroes.CastleOfferSetPrefix + id), StringComparer.Ordinal);
                foreach (var setId in LootSetIds(lootSets)
                             .Where(id => id.StartsWith(MainCycle_WhoHeroes.CastleOfferSetPrefix,
                                 StringComparison.Ordinal) && !allowedCastleSets.Contains(id)).ToList())
                    RemoveLootSet(lootSets, setId);
                foreach (var pair in castleOffers)
                    ReplaceLootSet(lootSets, MainCycle_WhoHeroes.CastleOfferSetPrefix + pair.Key,
                        new List<(string item, int amount1, int amount2)> { (pair.Value, 1, 1) });

                var tavernPool = tavernUnits
                    .Select(id => (item: id, amount1: tavernAmounts[id], amount2: tavernAmounts[id]))
                    .ToList();
                var traderPool = tavernUnits.Select(id => (item: id, amount1: 1, amount2: 1)).ToList();
                ReplaceLootSet(lootSets, MainCycle_WhoHeroes.TavernSetId, tavernPool);
                ReplaceLootSet(lootSets, MainCycle_WhoHeroes.TraderSetId, traderPool);
                package.Save();
            }

            ValidateWorkbook(fullPath);
            ValidateDemoContent(fullPath, maxStacks, castleOffers, tavernUnits, tavernAmounts, portalEncounters);
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

        WireSatyrAssets(scene);
        AssetDatabase.ImportAsset(WorkbookAssetPath, ImportAssetOptions.ForceUpdate);
        Debug.Log("WhoHeroes demo roster, max stacks and three-island progression configured and validated.");
    }

    [MenuItem("Tools/WhoHeroes/Configure Demo Skills")]
    public static void ConfigureDemoSkills()
    {
        var scene = RequireWhoHeroesScene();
        var fullPath = Path.GetFullPath(WorkbookAssetPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("WhoHeroes config workbook was not found.", fullPath);

        var heroSkills = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "peak", PikeSweepSkillId },
            { "rider", RiderPushSkillId },
            { "cyclop", CyclopRockfallSkillId },
            { "angel", AngelMassHealSkillId },
            { "sword", SwordGuardSkillId },
            { "satyr", SatyrCleaveSkillId },
            { "devil", DevilInfernoSkillId },
            { "efreed", EfreedFireRainSkillId }
        };
        var backupPath = Path.GetTempFileName();
        File.Copy(fullPath, backupPath, true);
        try
        {
            using (var package = new ExcelPackage(new FileInfo(fullPath)))
            {
                var heroes = RequireSheet(package, "Heroes");
                EnsureDemoEnemy(heroes, "imp", "peak", 3, 10, 1, "basic_melee", "x");
                EnsureDemoEnemy(heroes, "hellhound", "sword", 4, 15, 2, "basic_melee", "x");
                EnsureDemoEnemy(heroes, "gog", "shaman", 5, 15, 0, "basic_range", "x");
                EnsureDemoEnemy(heroes, "efreed", "rider", 20, 50, 8, "basic_range", EfreedFireRainSkillId);
                EnsureDemoEnemy(heroes, "devil", "angel", 50, 100, 10, "basic_melee", DevilInfernoSkillId);

                var skills = RequireSheet(package, "SKILLS2");
                ConfigureDemoSkill(skills, PikeSweepSkillId, 0.25f, 0f, 1f, 0f, 5f, 2, "enemy");
                ConfigureDemoSkill(skills, RiderPushSkillId, 2f, 0f, 2f, 0f, 5f, 1, "enemy");
                ConfigureDemoSkill(skills, CyclopRockfallSkillId, 1f, 0f, 4f, 2f, 8f, 1, "enemy");
                ConfigureDemoSkill(skills, AngelMassHealSkillId, 0f, 10f, 999f, 0f, 10f, 999, "player");
                ConfigureDemoSkill(skills, SwordGuardSkillId, 0f, 0f, 0f, 0f, 10f, 1, "player", 3f);
                ConfigureDemoSkill(skills, SatyrCleaveSkillId, 0.75f, 0f, 1f, 0f, 6f, 999, "enemy");
                ConfigureDemoSkill(skills, DevilInfernoSkillId, 1.25f, 0f, 2f, 0f, 8f, 999, "enemy");
                ConfigureDemoSkill(skills, EfreedFireRainSkillId, 0.25f, 0f, 5f, 0f, 6f, 3, "enemy");

                var skillOtherColumn = FindColumn(heroes, "SKILLOTHER");
                if (skillOtherColumn <= 0)
                    throw new InvalidDataException("Heroes SKILLOTHER column is missing.");
                foreach (var pair in heroSkills)
                {
                    var row = FindRow(heroes, 1, pair.Key);
                    if (row <= 0)
                        throw new InvalidDataException("Demo hero is missing from Heroes: " + pair.Key);
                    heroes.Cells[row, skillOtherColumn].Value = pair.Value;
                }

                var battles = RequireSheet(package, "BATTLES");
                ReplaceBattleEnemies(battles, "battle6", new[]
                {
                    new DemoBattleEnemy("imp", 4, 0f, 0),
                    new DemoBattleEnemy("hellhound", 2, 3f, 1),
                    new DemoBattleEnemy("gog", 2, 6f, 0)
                });
                ReplaceBattleEnemies(battles, "battle8", new[]
                {
                    new DemoBattleEnemy("efreed", 2, 0f, 0)
                });
                ReplaceBattleEnemies(battles, "battle9", new[]
                {
                    new DemoBattleEnemy("devil", 1, 0f, 0)
                });
                FillBlankBattleRows(battles, "battle6", "battle9");

                package.Save();
            }

            ValidateWorkbook(fullPath);
            ValidateDemoSkills(fullPath, heroSkills);
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
        WireDemoEnemyAssets(scene);
        WireSatyrCombatAnimation();
        Debug.Log("WhoHeroes demo combat skills and enemy roster configured and validated.");
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
        var mainScreen = RequireSingle(GetSceneComponents<GUIMainScreen>(scene), nameof(GUIMainScreen));
        var mainCanvas = mainScreen.GetComponentInParent<Canvas>();
        if (mainCanvas == null)
            throw new InvalidDataException("WhoHeroes main screen has no parent Canvas.");
        SetReference(router, "mainCanvas", mainCanvas);
        SetReference(router, "mainScreen", mainScreen);
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

    private static int EnsureColumn(ExcelWorksheet sheet, string header)
    {
        var column = FindColumn(sheet, header);
        if (column > 0)
            return column;

        column = sheet.Dimension.End.Column + 1;
        var source = column - 1;
        sheet.Cells[1, column].Value = header;
        sheet.Cells[1, column].StyleID = sheet.Cells[1, source].StyleID;
        sheet.Column(column).Width = sheet.Column(source).Width;
        return column;
    }

    private static void ConfigureDemoSkill(
        ExcelWorksheet skills, string id, float attackPercent, float health, float range, float aoe, float cooldown,
        int targets, string targetTag, float shield = 0f)
    {
        var templateRow = FindRow(skills, 1, "basic_melee");
        if (templateRow <= 0)
            throw new InvalidDataException("SKILLS2 basic_melee template is missing.");

        var row = FindRow(skills, 1, id);
        if (row <= 0)
        {
            row = templateRow + 1;
            skills.InsertRow(row, 1, templateRow);
        }
        for (var column = 1; column <= skills.Dimension.End.Column; column++)
            skills.Cells[row, column].Value = skills.Cells[templateRow, column].Value;

        SetCell(skills, row, "NAME", id);
        SetCell(skills, row, "ATTACK_PRC", attackPercent.ToString(CultureInfo.InvariantCulture));
        SetCell(skills, row, "ATTACK", 0);
        SetCell(skills, row, "HEALTH", health.ToString(CultureInfo.InvariantCulture));
        SetCell(skills, row, "SHIELD", shield.ToString(CultureInfo.InvariantCulture));
        SetCell(skills, row, "RANGE", range);
        SetCell(skills, row, "AOE", aoe);
        SetCell(skills, row, "COOLDOWN", cooldown);
        SetCell(skills, row, "TARGETS", targets);
        SetCell(skills, row, "TAG_APPLY", targetTag);
        SetCell(skills, row, "FILTER_RANGE", "lowest");
        SetCell(skills, row, "REQ_ACTION", 0);
        SetCell(skills, row, "BUFF_APPLY", "x");
    }

    private static void SetCell(ExcelWorksheet sheet, int row, string header, object value)
    {
        var column = FindColumn(sheet, header);
        if (column <= 0)
            throw new InvalidDataException(sheet.Name + " column is missing: " + header);
        sheet.Cells[row, column].Value = value;
    }

    private static void EnsureResourceTask(
        ExcelWorksheet tasks, string taskId, string resourceId, int amount, string description,
        string previousTaskId = null)
    {
        var templateRow = FindRow(tasks, 1, "whoheroes_quest_hire");
        if (templateRow <= 0)
            throw new InvalidDataException("WhoHeroes task template is missing: whoheroes_quest_hire");

        var row = FindRow(tasks, 1, taskId);
        if (row <= 0)
        {
            row = tasks.Dimension.End.Row + 1;
            tasks.InsertRow(row, 1, templateRow);
        }
        for (var column = 1; column <= tasks.Dimension.End.Column; column++)
            tasks.Cells[row, column].Value = tasks.Cells[templateRow, column].Value;

        SetCell(tasks, row, "TASKID", taskId);
        SetCell(tasks, row, "CATEGORY", "common");
        SetCell(tasks, row, "REWARDS", GoldResourceId + ",10");
        SetCell(tasks, row, "REQSTART", string.IsNullOrEmpty(previousTaskId)
            ? "x"
            : "complete_other," + previousTaskId + ",1,>=");
        SetCell(tasks, row, "REQFINISH", "gather," + resourceId + "," + amount + ",>=");
        SetCell(tasks, row, "REQITEMS", "x");
        SetCell(tasks, row, "EXPIRE", "x");
        SetCell(tasks, row, "DESCRIPTION", description);
        SetCell(tasks, row, "ICON", "x");
        SetCell(tasks, row, "MARKETID", "x");
        SetCell(tasks, row, "MARKETPRICE", "x");
        SetCell(tasks, row, "LIMIT", 1);
        SetCell(tasks, row, "FREE_EVERY", "x");
        SetCell(tasks, row, "STAT_NEW", false);
    }

    private static void SetPlayerStartPack(ExcelWorksheet player)
    {
        var row = FindRow(player, 1, "items");
        if (row <= 0)
            throw new InvalidDataException("PLAYER start pack row is missing.");
        player.Cells[row, 2].Value =
            $"{GoldResourceId},{DemoInitialGold}#{WoodResourceId},{DemoInitialWood}#" +
            $"{StoneResourceId},{DemoInitialStone}";
    }

    private static void ValidateDemoSkills(string fullPath, IReadOnlyDictionary<string, string> heroSkills)
    {
        using (var package = new ExcelPackage(new FileInfo(fullPath)))
        {
            var heroes = RequireSheet(package, "Heroes");
            var skillOtherColumn = FindColumn(heroes, "SKILLOTHER");
            foreach (var pair in heroSkills)
            {
                var row = FindRow(heroes, 1, pair.Key);
                if (row <= 0 || !string.Equals(heroes.Cells[row, skillOtherColumn].Text.Trim(), pair.Value,
                        StringComparison.Ordinal))
                    throw new InvalidDataException("Invalid demo skill assignment for hero: " + pair.Key);
            }

            var skills = RequireSheet(package, "SKILLS2");
            ValidateDemoSkill(skills, PikeSweepSkillId, 0.25f, 0f, 1f, 0f, 5f, 2, "enemy");
            ValidateDemoSkill(skills, RiderPushSkillId, 2f, 0f, 2f, 0f, 5f, 1, "enemy");
            ValidateDemoSkill(skills, CyclopRockfallSkillId, 1f, 0f, 4f, 2f, 8f, 1, "enemy");
            ValidateDemoSkill(skills, AngelMassHealSkillId, 0f, 10f, 999f, 0f, 10f, 999, "player");
            ValidateDemoSkill(skills, SwordGuardSkillId, 0f, 0f, 0f, 0f, 10f, 1, "player", 3f);
            ValidateDemoSkill(skills, SatyrCleaveSkillId, 0.75f, 0f, 1f, 0f, 6f, 999, "enemy");
            ValidateDemoSkill(skills, DevilInfernoSkillId, 1.25f, 0f, 2f, 0f, 8f, 999, "enemy");
            ValidateDemoSkill(skills, EfreedFireRainSkillId, 0.25f, 0f, 5f, 0f, 6f, 3, "enemy");
            ValidateDemoEnemies(package);
        }
    }

    private static void EnsureDemoEnemy(
        ExcelWorksheet heroes, string id, string templateId, int attack, int health, int armor,
        string basicSkill, string otherSkill)
    {
        var row = FindRow(heroes, 1, id);
        if (row <= 0)
        {
            var templateRow = FindRow(heroes, 1, templateId);
            if (templateRow <= 0)
                throw new InvalidDataException("Demo enemy template is missing: " + templateId);

            row = heroes.Dimension.End.Row + 1;
            heroes.InsertRow(row, 1, templateRow);
            for (var column = 1; column <= heroes.Dimension.End.Column; column++)
                heroes.Cells[row, column].Value = heroes.Cells[templateRow, column].Value;
        }

        SetCell(heroes, row, "NAME", id);
        SetCell(heroes, row, "ATTACK", attack);
        SetCell(heroes, row, "HEALTH", health);
        SetCell(heroes, row, "ARMOR", armor);
        SetCell(heroes, row, "SKILLBASIC", basicSkill);
        SetCell(heroes, row, "SKILLOTHER", otherSkill);
    }

    private static void ReplaceBattleEnemies(
        ExcelWorksheet battles, string battleId, IReadOnlyList<DemoBattleEnemy> enemies)
    {
        var nameColumn = FindColumn(battles, "NAME");
        var enemyColumn = FindColumn(battles, "ENEMY-LEVEL-POSITION");
        var amountColumn = FindColumn(battles, "AMOUNT");
        var timeColumn = FindColumn(battles, "TIME");
        var sideColumn = FindColumn(battles, "SIDE");
        if (nameColumn <= 0 || enemyColumn <= 0 || amountColumn <= 0 || timeColumn <= 0 || sideColumn <= 0)
            throw new InvalidDataException("BATTLES enemy schema is incomplete.");

        var startRow = FindRow(battles, nameColumn, battleId);
        if (startRow <= 0)
            throw new InvalidDataException("Demo battle is missing: " + battleId);

        var endRow = startRow + 1;
        while (endRow <= battles.Dimension.End.Row)
        {
            var value = battles.Cells[endRow, nameColumn].Text.Trim();
            if (!string.IsNullOrEmpty(value) && !string.Equals(value, "x", StringComparison.OrdinalIgnoreCase))
                break;
            endRow++;
        }

        var capacity = endRow - startRow;
        if (capacity < enemies.Count)
        {
            var addedRows = enemies.Count - capacity;
            battles.InsertRow(endRow, addedRows, startRow);
            for (var row = endRow; row < endRow + addedRows; row++)
                for (var column = 1; column <= battles.Dimension.End.Column; column++)
                    battles.Cells[row, column].Value = "x";
            capacity = enemies.Count;
        }
        var optionalEnemyColumns = new[] { "ENEMY-ARTEFACT", "ENEMY-KINGDOM", "ENEMY-PERKS" }
            .Select(header => FindColumn(battles, header)).Where(column => column > 0).ToArray();
        for (var index = 0; index < capacity; index++)
        {
            var row = startRow + index;
            if (index > 0)
                battles.Cells[row, nameColumn].Value = "x";
            battles.Cells[row, enemyColumn].Value = "x";
            battles.Cells[row, amountColumn].Value = "x";
            battles.Cells[row, timeColumn].Value = "x";
            battles.Cells[row, sideColumn].Value = "x";
            foreach (var column in optionalEnemyColumns)
                battles.Cells[row, column].Value = "x";
        }

        for (var index = 0; index < enemies.Count; index++)
        {
            var enemy = enemies[index];
            var row = startRow + index;
            battles.Cells[row, enemyColumn].Value = enemy.id + ",1,0,0";
            battles.Cells[row, amountColumn].Value = enemy.amount;
            battles.Cells[row, timeColumn].Value = enemy.time;
            battles.Cells[row, sideColumn].Value = enemy.side;
        }
    }

    private static void FillBlankBattleRows(ExcelWorksheet battles, string firstBattleId, string lastBattleId)
    {
        var nameColumn = FindColumn(battles, "NAME");
        var firstRow = FindRow(battles, nameColumn, firstBattleId);
        var lastRow = FindRow(battles, nameColumn, lastBattleId);
        if (firstRow <= 0 || lastRow <= firstRow)
            throw new InvalidDataException("Cannot determine demo battle range.");

        for (var row = firstRow; row <= lastRow; row++)
        {
            var isBlank = true;
            for (var column = 1; column <= battles.Dimension.End.Column; column++)
            {
                if (!string.IsNullOrWhiteSpace(battles.Cells[row, column].Text))
                {
                    isBlank = false;
                    break;
                }
            }

            if (!isBlank)
                continue;

            for (var column = 1; column <= battles.Dimension.End.Column; column++)
                battles.Cells[row, column].Value = "x";
        }
    }

    private static void ValidateDemoEnemies(ExcelPackage package)
    {
        var heroes = RequireSheet(package, "Heroes");
        var expectedHeroes = new Dictionary<string, (int attack, int health, int armor, string basic, string other)>(
            StringComparer.Ordinal)
        {
            { "imp", (3, 10, 1, "basic_melee", "x") },
            { "hellhound", (4, 15, 2, "basic_melee", "x") },
            { "gog", (5, 15, 0, "basic_range", "x") },
            { "efreed", (20, 50, 8, "basic_range", EfreedFireRainSkillId) },
            { "devil", (50, 100, 10, "basic_melee", DevilInfernoSkillId) }
        };
        foreach (var pair in expectedHeroes)
        {
            var row = FindRow(heroes, 1, pair.Key);
            if (row <= 0 || !CellEquals(heroes, row, "ATTACK", pair.Value.attack) ||
                !CellEquals(heroes, row, "HEALTH", pair.Value.health) ||
                !CellEquals(heroes, row, "ARMOR", pair.Value.armor) ||
                !string.Equals(CellText(heroes, row, "SKILLBASIC"), pair.Value.basic, StringComparison.Ordinal) ||
                !string.Equals(CellText(heroes, row, "SKILLOTHER"), pair.Value.other, StringComparison.Ordinal))
                throw new InvalidDataException("Invalid demo enemy: " + pair.Key);
        }

        var battles = RequireSheet(package, "BATTLES");
        ValidateBattleEnemies(battles, "battle6", new[] { "imp:4", "hellhound:2", "gog:2" });
        ValidateBattleEnemies(battles, "battle8", new[] { "efreed:2" });
        ValidateBattleEnemies(battles, "battle9", new[] { "devil:1" });
    }

    private static void ValidateBattleEnemies(ExcelWorksheet battles, string battleId, IReadOnlyList<string> expected)
    {
        var nameColumn = FindColumn(battles, "NAME");
        var enemyColumn = FindColumn(battles, "ENEMY-LEVEL-POSITION");
        var amountColumn = FindColumn(battles, "AMOUNT");
        var startRow = FindRow(battles, nameColumn, battleId);
        var actual = new List<string>();
        for (var row = startRow; row <= battles.Dimension.End.Row; row++)
        {
            if (row > startRow)
            {
                var name = battles.Cells[row, nameColumn].Text.Trim();
                if (!string.IsNullOrEmpty(name) && !string.Equals(name, "x", StringComparison.OrdinalIgnoreCase))
                    break;
            }

            var enemy = battles.Cells[row, enemyColumn].Text.Trim();
            if (string.IsNullOrEmpty(enemy) || string.Equals(enemy, "x", StringComparison.OrdinalIgnoreCase))
                continue;
            actual.Add(enemy.Split(',')[0] + ":" + battles.Cells[row, amountColumn].Text.Trim());
        }

        if (!actual.SequenceEqual(expected))
            throw new InvalidDataException("Invalid demo battle enemies: " + battleId);
    }

    private static void WireDemoEnemyAssets(Scene scene)
    {
        var holder = RequireSingle(GetSceneComponents<ResourceHolder>(scene), nameof(ResourceHolder));
        var combatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatUnitPrefabAssetPath);
        if (combatPrefab == null)
            throw new FileNotFoundException("WhoHeroes combat unit prefab is missing.", CombatUnitPrefabAssetPath);

        Undo.RecordObject(holder, "Wire WhoHeroes demo enemy assets");
        holder.monsters ??= new StringObjectDictionary();
        holder.avas ??= new StringSpriteDictionary();
        foreach (var pair in DemoEnemySpritePaths)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(pair.Value).OfType<Sprite>()
                .FirstOrDefault(value => value.name.EndsWith("_0", StringComparison.Ordinal));
            if (sprite == null)
                throw new FileNotFoundException("WhoHeroes demo enemy sprite is missing: " + pair.Key, pair.Value);
            holder.monsters[pair.Key] = combatPrefab;
            holder.avas[pair.Key] = sprite;
        }

        EditorUtility.SetDirty(holder);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("WhoHeroes scene could not be saved after wiring demo enemy assets.");
    }

    private static void WireSatyrCombatAnimation()
    {
        var frames = AssetDatabase.LoadAllAssetsAtPath(SatyrSpriteAssetPath).OfType<Sprite>()
            .Select(sprite => new { sprite, index = SpriteFrameIndex(sprite.name) })
            .Where(value => value.index >= 8 && value.index <= 11)
            .OrderBy(value => value.index)
            .Select(value => value.sprite)
            .ToArray();
        if (frames.Length != 4)
            throw new InvalidDataException("Satyr attack animation requires atlas frames 8-11.");

        var prefabRoot = PrefabUtility.LoadPrefabContents(CombatUnitPrefabAssetPath);
        try
        {
            var view = prefabRoot.GetComponent<WhoHeroesUnitView>();
            if (view == null)
                throw new MissingComponentException("WhoHeroesCombatUnit is missing WhoHeroesUnitView.");

            var serializedView = new SerializedObject(view);
            var framesProperty = serializedView.FindProperty("satyrAttackFrames");
            var durationProperty = serializedView.FindProperty("satyrAttackFrameSeconds");
            if (framesProperty == null || durationProperty == null)
                throw new MissingFieldException("WhoHeroesUnitView satyr animation fields are missing.");

            framesProperty.arraySize = frames.Length;
            for (var index = 0; index < frames.Length; index++)
                framesProperty.GetArrayElementAtIndex(index).objectReferenceValue = frames[index];
            durationProperty.floatValue = 0.1f;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.SaveAsPrefabAsset(prefabRoot, CombatUnitPrefabAssetPath) == null)
                throw new IOException("WhoHeroesCombatUnit prefab could not be saved.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static int SpriteFrameIndex(string spriteName)
    {
        var separator = spriteName.LastIndexOf('_');
        return separator >= 0 && int.TryParse(spriteName.Substring(separator + 1), out var index) ? index : -1;
    }

    private static void ValidateDemoSkill(
        ExcelWorksheet sheet, string id, float attackPercent, float health, float range, float aoe, float cooldown,
        int targets, string targetTag, float shield = 0f)
    {
        var row = FindRow(sheet, 1, id);
        if (row <= 0 ||
            !CellEquals(sheet, row, "ATTACK_PRC", attackPercent) ||
            !CellEquals(sheet, row, "HEALTH", health) ||
            !CellEquals(sheet, row, "SHIELD", shield) ||
            !CellEquals(sheet, row, "RANGE", range) ||
            !CellEquals(sheet, row, "AOE", aoe) ||
            !CellEquals(sheet, row, "COOLDOWN", cooldown) ||
            !CellEquals(sheet, row, "TARGETS", targets) ||
            !string.Equals(CellText(sheet, row, "TAG_APPLY"), targetTag, StringComparison.Ordinal) ||
            !string.Equals(CellText(sheet, row, "FILTER_RANGE"), "lowest", StringComparison.Ordinal))
            throw new InvalidDataException("Invalid demo combat skill: " + id);
    }

    private static bool CellEquals(ExcelWorksheet sheet, int row, string header, float expected)
    {
        var column = FindColumn(sheet, header);
        if (column <= 0 || sheet.Cells[row, column].Value == null)
            return false;
        try
        {
            return Mathf.Approximately(
                Convert.ToSingle(sheet.Cells[row, column].Value, CultureInfo.InvariantCulture), expected);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
    }

    private static void EnsureSatyrHero(ExcelWorksheet heroes)
    {
        if (FindRow(heroes, 1, "satyr") > 0)
            return;

        var sourceRow = FindRow(heroes, 1, "treant");
        var insertRow = FindRow(heroes, 1, "cyclop");
        if (sourceRow <= 0 || insertRow <= 0)
            throw new InvalidDataException("Cannot create the demo satyr from the verified treant baseline.");

        heroes.InsertRow(insertRow, 1, sourceRow);
        for (var column = 1; column <= heroes.Dimension.End.Column; column++)
            heroes.Cells[insertRow, column].Value = heroes.Cells[sourceRow, column].Value;
        heroes.Cells[insertRow, 1].Value = "satyr";
    }

    private static void WireSatyrAssets(Scene scene)
    {
        var holder = RequireSingle(GetSceneComponents<ResourceHolder>(scene), nameof(ResourceHolder));
        var combatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatUnitPrefabAssetPath);
        var satyrSprite = AssetDatabase.LoadAllAssetsAtPath(SatyrSpriteAssetPath).OfType<Sprite>()
            .FirstOrDefault(value => value.name.EndsWith("_0", StringComparison.Ordinal));
        if (combatPrefab == null || satyrSprite == null)
            throw new FileNotFoundException("WhoHeroes satyr combat prefab or sprite is missing.");

        Undo.RecordObject(holder, "Wire WhoHeroes satyr assets");
        holder.monsters ??= new StringObjectDictionary();
        holder.avas ??= new StringSpriteDictionary();
        holder.monsters["satyr"] = combatPrefab;
        holder.avas["satyr"] = satyrSprite;
        EditorUtility.SetDirty(holder);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("WhoHeroes scene could not be saved after wiring satyr assets.");
    }

    private static void ValidateLocationConfig(string fullPath)
    {
        using (var package = new ExcelPackage(new FileInfo(fullPath)))
        {
            var stages = RequireSheet(package, "STAGES");
            var idColumn = FindColumn(stages, "NAME");
            var displayNameColumn = FindColumn(stages, "DISPLAY_NAME");
            if (idColumn <= 0 || displayNameColumn <= 0)
                throw new InvalidDataException("WhoHeroes STAGES schema is incomplete.");
            foreach (var location in LocationValues)
            {
                var row = FindRow(stages, idColumn, location.Key);
                if (row <= 0 || !string.Equals(stages.Cells[row, displayNameColumn].Text.Trim(),
                        location.Value, StringComparison.Ordinal))
                    throw new InvalidDataException("WhoHeroes location is missing or invalid: " + location.Key);
            }
        }
    }

    private static void ValidateMapBuildingNames(string fullPath)
    {
        using (var package = new ExcelPackage(new FileInfo(fullPath)))
        {
            var meta = RequireSheet(package, "METACONF");
            foreach (var pair in MapBuildingNames)
            {
                var row = FindRow(meta, 1, "whoheroes_text_" + pair.Key);
                if (row <= 0 || !string.Equals(meta.Cells[row, 3].Text.Trim(), pair.Value,
                        StringComparison.Ordinal))
                    throw new InvalidDataException("WhoHeroes Map building name is missing or invalid: " + pair.Key);
            }
        }
    }

    private static void ValidateDemoDefenderNames(string fullPath)
    {
        using (var package = new ExcelPackage(new FileInfo(fullPath)))
        {
            var meta = RequireSheet(package, "METACONF");
            foreach (var pair in DemoEnemyDisplayNames)
            {
                var row = FindRow(meta, 1, "whoheroes_text_" + pair.Key);
                if (row <= 0 || !string.Equals(meta.Cells[row, 3].Text.Trim(), pair.Value,
                        StringComparison.Ordinal))
                    throw new InvalidDataException("WhoHeroes defender name is missing or invalid: " + pair.Key);
            }
        }
    }

    private static void ValidateDemoContent(
        string fullPath,
        IReadOnlyDictionary<string, int> maxStacks,
        IReadOnlyDictionary<string, string> castleOffers,
        IReadOnlyCollection<string> tavernUnits,
        IReadOnlyDictionary<string, int> tavernAmounts,
        IReadOnlyDictionary<string, string> portalEncounters)
    {
        using (var package = new ExcelPackage(new FileInfo(fullPath)))
        {
            var heroes = RequireSheet(package, "Heroes");
            var maxStackColumn = FindColumn(heroes, "MAX_STACK");
            var encounterColumn = FindColumn(heroes, "ENCOUNTER");
            if (maxStackColumn <= 0 || encounterColumn <= 0)
                throw new InvalidDataException("Demo Heroes schema is incomplete.");
            foreach (var pair in maxStacks)
            {
                var row = FindRow(heroes, 1, pair.Key);
                if (row <= 0 || heroes.Cells[row, maxStackColumn].Text != pair.Value.ToString())
                    throw new InvalidDataException("Invalid demo MAX_STACK for hero: " + pair.Key);
            }

            var configuredPortals = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var row = 2; row <= heroes.Dimension.End.Row; row++)
            {
                var id = heroes.Cells[row, 1].Text.Trim();
                var encounter = heroes.Cells[row, encounterColumn].Text.Trim();
                if (id.StartsWith("portalin", StringComparison.Ordinal) &&
                    !string.IsNullOrEmpty(encounter) && !string.Equals(encounter, "x", StringComparison.OrdinalIgnoreCase))
                    configuredPortals[id] = encounter;
            }
            if (configuredPortals.Count != portalEncounters.Count ||
                portalEncounters.Any(pair => !configuredPortals.TryGetValue(pair.Key, out var value) || value != pair.Value))
                throw new InvalidDataException("Demo portal progression differs from the three-island whitelist.");

            var lootSets = RequireSheet(package, "LOOTSET");
            var actualCastleSets = LootSetIds(lootSets)
                .Where(id => id.StartsWith(MainCycle_WhoHeroes.CastleOfferSetPrefix, StringComparison.Ordinal))
                .ToArray();
            if (actualCastleSets.Length != castleOffers.Count)
                throw new InvalidDataException("Demo Castle contains unexpected unit buildings.");
            foreach (var pair in castleOffers)
            {
                var items = LootSetItems(lootSets, MainCycle_WhoHeroes.CastleOfferSetPrefix + pair.Key);
                if (items.Count != 1 || items[0] != pair.Value)
                    throw new InvalidDataException("Invalid demo Castle offer: " + pair.Key);
            }

            foreach (var setId in new[] { MainCycle_WhoHeroes.TavernSetId, MainCycle_WhoHeroes.TraderSetId })
            {
                var items = LootSetItems(lootSets, setId);
                if (!items.SequenceEqual(tavernUnits))
                    throw new InvalidDataException("Invalid demo unit pool: " + setId);
            }

            foreach (var pair in tavernAmounts)
            {
                var amount = LootSetAmount(lootSets, MainCycle_WhoHeroes.TavernSetId, pair.Key);
                if (amount != pair.Value)
                    throw new InvalidDataException("Invalid demo Tavern amount for hero: " + pair.Key);
            }
        }
    }

    private static int LootSetAmount(ExcelWorksheet sheet, string setId, string itemId)
    {
        var nameColumn = FindColumn(sheet, "NAME");
        var itemColumn = FindColumn(sheet, "ITEM");
        var amountColumn = FindColumn(sheet, "AMOUNT1");
        var currentSet = string.Empty;
        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var name = sheet.Cells[row, nameColumn].Text.Trim();
            if (!string.IsNullOrEmpty(name) && !string.Equals(name, "x", StringComparison.OrdinalIgnoreCase))
                currentSet = name;
            if (string.Equals(currentSet, setId, StringComparison.Ordinal) &&
                string.Equals(sheet.Cells[row, itemColumn].Text.Trim(), itemId, StringComparison.Ordinal))
                return Mathf.Max(0, Mathf.RoundToInt((float)sheet.Cells[row, amountColumn].GetValue<double>()));
        }
        return 0;
    }

    private static List<string> LootSetItems(ExcelWorksheet sheet, string setId)
    {
        var result = new List<string>();
        var nameColumn = FindColumn(sheet, "NAME");
        var itemColumn = FindColumn(sheet, "ITEM");
        var currentSet = string.Empty;
        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var name = sheet.Cells[row, nameColumn].Text.Trim();
            if (!string.IsNullOrEmpty(name) && !string.Equals(name, "x", StringComparison.OrdinalIgnoreCase))
                currentSet = name;
            if (currentSet == setId)
                result.Add(sheet.Cells[row, itemColumn].Text.Trim());
        }
        return result;
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
            var expectedStartPack =
                $"{GoldResourceId},{DemoInitialGold}#{WoodResourceId},{DemoInitialWood}#" +
                $"{StoneResourceId},{DemoInitialStone}";
            if (itemsRow <= 0 || !string.Equals(player.Cells[itemsRow, 2].Text.Trim(), expectedStartPack,
                    StringComparison.Ordinal))
                throw new InvalidDataException("PLAYER start pack is missing or invalid.");

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
            foreach (var pair in UiTextValues)
            {
                var key = "whoheroes_text_" + pair.Key;
                var row = FindRow(meta, 1, key);
                if (row <= 0 || !string.IsNullOrEmpty(pair.Value) &&
                    string.IsNullOrWhiteSpace(meta.Cells[row, 3].Text))
                    throw new InvalidDataException("WhoHeroes UI text is missing after save: " + key);
            }
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
            var tasks = RequireSheet(package, "TASKS");
            foreach (var task in new[]
                     {
                         (id: WoodQuestId, resource: WoodResourceId, amount: 5, previous: (string)null),
                         (id: WoodQuest20Id, resource: WoodResourceId, amount: 20, previous: WoodQuestId),
                         (id: WoodQuest50Id, resource: WoodResourceId, amount: 59, previous: WoodQuest20Id),
                         (id: StoneQuestId, resource: StoneResourceId, amount: 5, previous: (string)null),
                         (id: StoneQuest20Id, resource: StoneResourceId, amount: 20, previous: StoneQuestId),
                         (id: StoneQuest50Id, resource: StoneResourceId, amount: 50, previous: StoneQuest20Id)
                     })
            {
                var row = FindRow(tasks, 1, task.id);
                var expectedStart = string.IsNullOrEmpty(task.previous)
                    ? "x"
                    : "complete_other," + task.previous + ",1,>=";
                if (row <= 0 || CellText(tasks, row, "REWARDS") != GoldResourceId + ",10" ||
                    CellText(tasks, row, "REQSTART") != expectedStart ||
                    CellText(tasks, row, "REQFINISH") !=
                    "gather," + task.resource + "," + task.amount + ",>=")
                    throw new InvalidDataException("WhoHeroes resource task is missing or invalid: " + task.id);
            }
            var onboardingItems = LootSetItems(lootSets, MainCycle_WhoHeroes.OnboardingTaskSetId);
            var expectedOnboarding = OnboardingTasks.Split('#');
            if (!onboardingItems.SequenceEqual(expectedOnboarding))
                throw new InvalidDataException("WhoHeroes onboarding LOOTSET differs from the active demo quests.");
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
