#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class PubCardHolderUpgrade
{
    private const string Work = @"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\pub-card-holder";
    [InitializeOnLoadMethod] private static void Init()
    {
        if (File.Exists(Work + "/upgrade.request")) EditorApplication.delayCall += Run;
    }
    [MenuItem("Tools/UI/Pub/Add Card ObjHolders")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(PubBuilder.PrefabPath);
            var resources = UnityEngine.Object.FindObjectsByType<ResourceHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            var legacy = AssetDatabase.LoadAssetAtPath<PubPortraitLibrary>(PubBuilder.Root + "/PubPortraits.asset");
            var cards = root.GetComponentsInChildren<PubHeroCard>(true);
            var ids = new System.Collections.Generic.List<string>();
            foreach (var card in cards)
            {
                var holder = card.EnsureObjHolder();
                string id = resources != null ? resources.avas.FirstOrDefault(v => v.Value != null && v.Value == card.portrait.sprite).Key : null;
                if (string.IsNullOrEmpty(id) && legacy != null)
                    id = legacy.entries.FirstOrDefault(v => v.sprite != null && v.sprite == card.portrait.sprite).id;
                if (!string.IsNullOrEmpty(id)) holder.asDBObj = id;
                ids.Add(holder.asDBObj);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PubBuilder.PrefabPath, out bool success);
            if (!success) throw new InvalidOperationException("Cannot save UI_Pub");
            foreach (var card in cards)
            {
                var holder = card.GetComponent<ObjHolder>();
                card.Bind(new PubHero { Id = "holder_probe", Name = "Probe", Definition = new Obj { ID = "holder_probe" } }, null, 1);
                if (!holder.noTrack || holder.asDBObj != "holder_probe" || holder.obj.dbObj.ID != "holder_probe" || holder.obj.main != null)
                    throw new InvalidOperationException("Card holder binding failed");
                card.Bind(null, null, 0);
                if (holder.asDBObj != "" || holder.obj != null) throw new InvalidOperationException("Empty card retained its binding");
            }
            File.WriteAllText(Work + "/result.txt", "PASS: ObjHolder added to " + cards.Length + " cards, noTrack=true; preview IDs=" + string.Join(",", ids) + "; runtime ID change/empty binding verified, no world actor created.");
            File.Delete(Work + "/upgrade.request");
        }
        catch (Exception e) { File.WriteAllText(Work + "/result.txt", "FAIL: " + e); Debug.LogException(e); }
        finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
