#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

internal static class BattlePassValidation
{
    internal static string Validate(GameObject prefab, ConfigLoader loader, List<ElTasko> tasks)
    {
        var saved = BattlePassPrefabSetup.SaveStatics();
        var root = new GameObject("BattlePassValidation") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        try
        {
            ConfigLoader.Instance = loader;
            var state = root.AddComponent<MainStates>(); MainStates.instance = state;
            state.playerData = new PlayerData(); MainStates.allVisuals = new List<Vizualo>();
            var database = root.AddComponent<DatabaseAll>(); DatabaseAll.instance = database;
            ModelStatistics.instance = root.AddComponent<ModelStatistics>(); ResourceHolder.instance = null;
            EventManager.dynActions = new Dictionary<string, Action<ArgPass>>(); EventManager.eventCount = new Dictionary<string, int>();
            loader.allTasks = tasks.ToDictionary(t => t.id);
            foreach (var id in tasks.SelectMany(t => t.rewards.Concat(t.rewardsPremium)).Select(r => r.Key).Append(BattlePassController.PointsStat).Distinct())
            {
                var item = new Obj { ID = id };
                item.pars["level"] = 1; item.pars["max_stack"] = 1000000;
                database.items.Add(id, item);
            }
            var hero = new Obj { ID = "battlepass_test_player" }; hero.pars["level"] = 1;
            var player = new RObj { dbObj = hero, RID = "main_player", it = ItemType.monster };
            player.upgradePars["level"] = 0; player.upgradePars["rarity"] = 0;
            player.RecalcPars(); state.all["main_player"] = player;
            var controller = prefab.GetComponent<BattlePassController>(); controller.RefreshTasks();
            Require(controller.Stages.Count == tasks.Count, "Real TASKS records bind to all stages");
            Require(tasks.Select(t => t.id).SequenceEqual(prefab.GetComponentsInChildren<BattlePassLevelView>(true).Select(v => v.TaskId)), "Prefab row order and IDs");
            state.AddItems(new List<Bon>{new Bon{Key=BattlePassController.PointsStat,Value=5000}});
            Require(controller.Points == 0 && !controller.Claim(tasks[0].id, false), "Inventory points must not unlock the statistic-based track");
            ModelStatistics.instance.SetStatValueForce(BattlePassController.PointsStat, BattlePassController.RequiredPoints(tasks[0]) - 1);
            Require(!controller.Claim(tasks[0].id, false), "Reward threshold boundary");
            ModelStatistics.instance.SetStatValueForce(BattlePassController.PointsStat, BattlePassController.RequiredPoints(tasks[0]));
            Require(controller.Claim(tasks[0].id, false), "Free reward grant");
            Require(!controller.Claim(tasks[0].id, false), "Duplicate free reward blocked");
            Require(!controller.Claim(tasks[0].id, true), "Premium blocked while unpurchased");
            ModelStatistics.instance.SetStatValueForce(BattlePassController.PremiumStat, 2);
            Require(!controller.HasPremium, "Premium requires exactly bought_battlepass = 1");
            ModelStatistics.instance.SetStatValueForce(BattlePassController.PremiumStat, 1);
            Require(controller.Claim(tasks[0].id, true), "Late premium purchase can claim an already reached stage");
            Require(!controller.Claim(tasks[0].id, true), "Duplicate premium reward blocked");
            Require(controller.Points == BattlePassController.RequiredPoints(tasks[0]) && state.GetItemsCount(BattlePassController.PointsStat) == 5000, "Claim consumes neither statistic nor inventory points");
            ModelStatistics.instance.SetStatValueForce(BattlePassController.PointsStat, tasks.Max(BattlePassController.RequiredPoints));
            controller.ClaimAll();
            foreach (var task in tasks)
                Require(controller.IsTaken(task.id, false) && controller.IsTaken(task.id, true), "Both tracks claimed: " + task.id);
            var expected = tasks.SelectMany(t => t.rewards.Concat(t.rewardsPremium)).GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.Sum(r => r.Value));
            foreach (var reward in expected) Require(state.GetItemsCount(reward.Key) == reward.Value, "Exact configured reward total: " + reward.Key);
            var persisted = JsonConvert.SerializeObject(state.playerData.playerStats);
            state.playerData.playerStats = JsonConvert.DeserializeObject<Dictionary<string, Bon>>(persisted);
            controller.RefreshTasks(); controller.ClaimAll();
            foreach (var reward in expected) Require(state.GetItemsCount(reward.Key) == reward.Value, "Saved claim markers prevent repeat grants");
            return "Verified statistic thresholds, exact premium flag, free/premium independence, late purchase, all configured rewards, duplicate prevention, saved markers and unchanged points.";
        }
        finally { UnityEngine.Object.DestroyImmediate(root); BattlePassPrefabSetup.RestoreStatics(saved); }
    }
    private static void Require(bool condition, string message) => BattlePassPrefabSetup.Require(condition, message);
}
#endif
