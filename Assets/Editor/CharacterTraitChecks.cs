using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>Checks use isolated data only: no scene player, singletons or PlayerPrefs writes.</summary>
public static class CharacterTraitChecks
{
    [MenuItem("Tools/Character Traits/Check progression")]
    public static void Run()
    {
        foreach (var trait in CharacterTraitDefinition.Defaults())
        {
            var data = Fixture(2000, 200);
            var player = data.mainPlayer;
            for (int level = 0; level < 10; level++)
            {
                Require(CharacterTraitProgression.TryUpgrade(data, player, trait, level), "Upgrade " + trait.Parameter);
                Require(!CharacterTraitProgression.TryUpgrade(data, player, trait, level), "Duplicate confirmation accepted");
            }
            Require(CharacterTraitProgression.GetLevel(data, trait) == 10, "Level cap");
            Require(!CharacterTraitProgression.TryUpgrade(data, player, trait, 10), "Level 11 accepted");
            Require(Mathf.Approximately(player.GetPar(trait.RuntimeParameter), trait.Increment * 10), "Wrong increment");
            Require(CharacterTraitProgression.GetAmount(player, "gold") == 1000, "Wrong gold cost");
            Require(CharacterTraitProgression.GetAmount(player, "res1") == 100, "Wrong res1 cost");
            // Round-trip the existing save fields without RObj's database-dependent OnDeserialized callback.
            var loaded = Fixture(1000, 100);
            loaded.playerStats = JsonConvert.DeserializeObject<Dictionary<string, Bon>>(JsonConvert.SerializeObject(data.playerStats));
            loaded.mainPlayer.upgradePars = JsonConvert.DeserializeObject<Dictionary<string, float>>(JsonConvert.SerializeObject(player.upgradePars));
            loaded.mainPlayer.RecalcPars();
            Require(CharacterTraitProgression.GetLevel(loaded, trait) == 10, "Saved level lost");
            Require(Mathf.Approximately(loaded.mainPlayer.GetPar(trait.RuntimeParameter), trait.Increment * 10), "Saved bonus changed");
            Require(!CharacterTraitProgression.TryUpgrade(loaded, loaded.mainPlayer, trait, 10), "Loaded cap ignored");
        }
        var attack = new CharacterTraitDefinition("attack", "Attack");
        foreach (var amounts in new[] { new[] { 99, 10 }, new[] { 100, 9 }, new[] { 0, 0 } })
        {
            var data = Fixture(amounts[0], amounts[1]);
            Require(!CharacterTraitProgression.TryUpgrade(data, data.mainPlayer, attack, 0), "Insufficient resources accepted");
            Require(data.playerStats.Count == 0 && data.mainPlayer.GetPar("attack") == 0, "Failed purchase changed progress");
            Require(CharacterTraitProgression.GetAmount(data.mainPlayer, "gold") == amounts[0] &&
                    CharacterTraitProgression.GetAmount(data.mainPlayer, "res1") == amounts[1], "Partial payment");
        }
        foreach (var trait in new[] { attack, new CharacterTraitDefinition("attack", "Attack", 100, 0),
                     new CharacterTraitDefinition("attack", "Attack", 0, 10) })
        {
            var data = Fixture(trait.GoldCost, trait.Res1Cost);
            Require(CharacterTraitProgression.TryUpgrade(data, data.mainPlayer, trait, 0), "Exact/one-currency payment failed");
            Require(CharacterTraitProgression.GetAmount(data.mainPlayer, "gold") == 0 &&
                    CharacterTraitProgression.GetAmount(data.mainPlayer, "res1") == 0, "Exact payment remainder");
        }
        var invalid = Fixture(100, 10);
        Require(!CharacterTraitProgression.TryUpgrade(invalid, invalid.mainPlayer, new CharacterTraitDefinition("attack", "Attack", -1, 10), 0), "Negative price");
        Require(!CharacterTraitProgression.TryUpgrade(invalid, invalid.mainPlayer, attack, -1), "Negative level");
        invalid.playerStats["add_trait_attack"] = new Bon { Key = "add_trait_attack", Value = 10 };
        Require(CharacterTraitProgression.GetLevel(invalid, attack) == 0, "Temporary stats affect permanent level");
        Require(new CharacterTraitDefinition("crit_dmg", "Critical").StatKey == "trait_crit_damage", "Critical alias");
        Directory.CreateDirectory("work");
        File.WriteAllText("work/trait-checks-result.txt", "PASS: ten parameters 0->10; cap; duplicate/stale confirmation; insufficient and exact resources; gold-only and res1-only; saved fields round-trip; critical units/alias; invalid input.\n");
        Debug.Log("Character trait progression checks passed (isolated data; player save untouched).");
    }

    private static PlayerData Fixture(int gold, int res1)
    {
        var data = new PlayerData();
        data.mainPlayer.RID = "main_player";
        data.mainPlayer.upgradePars["level"] = 0;
        foreach (var entry in new[] { new Bon { Key = "gold", Value = gold }, new Bon { Key = "res1", Value = res1 } })
        {
            if (entry.Value == 0) continue;
            var item = new RObj { dbObj = new Obj { ID = entry.Key } };
            item.dbObj.pars["level"] = 1;
            item.upgradePars["level"] = 0; item.upgradePars["amount"] = entry.Value;
            item.RecalcPars(); data.mainPlayer.inventory.Add(item);
        }
        data.mainPlayer.RecalcPars();
        return data;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Trait check failed: " + message); }
}
