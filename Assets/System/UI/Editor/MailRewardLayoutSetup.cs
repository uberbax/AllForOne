#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MailRewardLayoutSetup
{
    private const string PrefabPath = "Assets/System/UI/Prefabs/MailItem.prefab";
    private const string Work = "C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/work";

    [InitializeOnLoadMethod]
    private static void RunRequested()
    {
        if (!File.Exists(Work + "/mail-layout.request")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying || !File.Exists(Work + "/mail-layout.request")) return;
            File.Delete(Work + "/mail-layout.request");
            try { Configure(); File.WriteAllText(Work + "/mail-layout-result.txt", "PASS: prefab saved; reward pairs and rebinding verified."); }
            catch (Exception e) { File.WriteAllText(Work + "/mail-layout-result.txt", "FAIL: " + e); Debug.LogException(e); }
        };
    }

    [MenuItem("Tools/Mail/Configure reward icons")]
    public static void Configure()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var view = root.GetComponent<MailItemView>();
            if (view == null) throw new InvalidOperationException("MailItemView missing");
            typeof(MailItemView).GetMethod("EnsureRewardLayout", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            var data = new SerializedObject(view);
            var rewards = (RectTransform)data.FindProperty("rewardItemsRoot").objectReferenceValue;
            if (rewards == null || rewards.childCount != 1) throw new InvalidOperationException("Reward item layout missing");
            var amount = (TMP_Text)data.FindProperty("rewardAmountText").objectReferenceValue;
            var icon = (Image)data.FindProperty("rewardIcon").objectReferenceValue;
            if (amount.transform.parent != icon.transform.parent) throw new InvalidOperationException("Icon and amount must share a reward item");
            amount.text = "10000";
            icon.enabled = true;
            icon.preserveAspect = true;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
            if (!saved) throw new InvalidOperationException("MailItem prefab save failed");

            view.Bind(null, new FormatMail { rewards = new System.Collections.Generic.List<Bon>
            {
                new Bon { Key = "gold", Value = 100 }, new Bon { Key = "res1", Value = 25 }, new Bon { Key = "gold", Value = 3 }
            }});
            if (rewards.childCount != 3) throw new InvalidOperationException("Each reward needs its own item");
            string[] expected = { "100", "25", "3" };
            for (int i = 0; i < 3; i++)
            {
                var item = rewards.GetChild(i);
                if (item.GetComponentInChildren<Image>(true) == null || item.GetComponentInChildren<TMP_Text>(true).text != expected[i])
                    throw new InvalidOperationException("Incorrect reward pair " + i);
            }
            view.Bind(null, new FormatMail { rewards = new System.Collections.Generic.List<Bon> { new Bon { Key = "gold", Value = 7 } } });
            if (rewards.GetChild(1).gameObject.activeSelf || rewards.GetChild(2).gameObject.activeSelf)
                throw new InvalidOperationException("Rebinding must hide unused reward items");
            view.Bind(null, new FormatMail { rewards = new System.Collections.Generic.List<Bon>() });
            if (rewards.GetChild(0).gameObject.activeSelf) throw new InvalidOperationException("Empty rewards must hide all items");
            Debug.Log("MAIL_REWARD_LAYOUT_PASS: prefab saved; individual icon/amount pairs, rebinding and empty rewards verified.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
