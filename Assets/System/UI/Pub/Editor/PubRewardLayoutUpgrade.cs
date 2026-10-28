#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class PubRewardLayoutUpgrade
{
    private const string Work = @"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\pub-shards";
    [InitializeOnLoadMethod] private static void Init()
    {
        if (File.Exists(Work + "/layout.request")) EditorApplication.delayCall += Run;
    }
    [MenuItem("Tools/UI/Pub/Update Reward Layout")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(PubBuilder.PrefabPath);
            var holders = UnityEngine.Object.FindObjectsByType<ResourceHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Sprite icon = holders.Length > 0 ? holders[0].GetIcon("mileage_token") : null;
            var cards = root.GetComponentsInChildren<PubHeroCard>(true);
            foreach (var card in cards)
            {
                card.EnsureMileageView();
                card.EnsureOwnedView();
                var row = card.transform.Find("MileageReward");
                row.Find("Icon").GetComponent<Image>().sprite = icon;
                row.Find("Amount").GetComponent<TMP_Text>().text = "5";
                card.quantity.text = "×25";
            }
            PrefabUtility.SaveAsPrefabAsset(root, PubBuilder.PrefabPath, out bool success);
            if (!success) throw new InvalidOperationException("Cannot save Pub reward layout");
            ValidateOwnedView(cards[0]);
            File.WriteAllText(Work + "/layout.txt", "PASS: mileage icon/amount below " + cards.Length + " cards; owned level/shards panel added; prefab saved through Unity API.");
            File.Delete(Work + "/layout.request");
        }
        catch (Exception e) { File.WriteAllText(Work + "/layout.txt", "FAIL: " + e); Debug.LogException(e); }
        finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void ValidateOwnedView(PubHeroCard card)
    {
        var definition = new Obj { ID = "pub_ui_probe" };
        var hero = new PubHero { Id = definition.ID, Name = "Probe", Definition = definition };
        var player = new RObj();
        var shardDefinition = new Obj { ID = "shard" };
        player.inventory.Add(new RObj { dbObj = shardDefinition, shardID = hero.Id,
            curPars = new System.Collections.Generic.Dictionary<string, float> { { "amount", 12 } } });
        player.inventory.Add(new RObj { dbObj = shardDefinition, shardID = hero.Id,
            curPars = new System.Collections.Generic.Dictionary<string, float> { { "amount", 8 } } });
        player.inventory.Add(new RObj { dbObj = shardDefinition, shardID = "other_hero",
            curPars = new System.Collections.Generic.Dictionary<string, float> { { "amount", 999 } } });
        card.Bind(hero, player, 3);
        var row = card.transform.Find("OwnedProgress");
        if (!card.newBadge.activeSelf || row.gameObject.activeSelf)
            throw new InvalidOperationException("Unowned hero must show NEW, even when shards exist");
        player.inventory.Add(new RObj { dbObj = definition, it = ItemType.monster,
            curPars = new System.Collections.Generic.Dictionary<string, float> { { "level", 3 } } });
        card.Bind(hero, player, 3, 3);
        if (card.newBadge.activeSelf || !row.gameObject.activeSelf || card.owned.text != "20" ||
            row.Find("LevelBadge/Level").GetComponent<TMP_Text>().text != "3")
            throw new InvalidOperationException("Owned hero level/shard display is incorrect");
    }

}
#endif
