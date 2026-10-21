#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;
public static class PrefabBuildSizeAudit
{
    private const string Work = "C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/work";
    private const string Target = "Assets/GameDevMarket/FF Heroes/Animation Source/FF Heroes/CAT_KNIGHT.prefab";
    [InitializeOnLoadMethod]
    private static void Requested()
    {
        if (!File.Exists(Work + "/cat-build-audit.request")) return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Work + "/cat-build-audit.request")) return;
            File.Delete(Work + "/cat-build-audit.request");
            try { Audit(); } catch (Exception e) { File.WriteAllText(Work + "/cat-build-audit.json", "FAIL: " + e); }
        };
    }
    [MenuItem("Tools/Diagnostics/Cat Knight build size")]
    public static void Audit()
    {
        var result = new Dictionary<string, object>();
        result["clips"] = AssetDatabase.LoadAllAssetsAtPath(Target).OfType<AnimationClip>().Select(c => new { c.name, c.length, c.frameRate, runtimeBytes = Profiler.GetRuntimeMemorySizeLong(c), curves = AnimationUtility.GetCurveBindings(c).Length }).ToArray();
        var report = BuildReport.GetLatestReport();
        result["reportAvailable"] = report != null;
        var matches = new List<Dictionary<string, string>>();
        if (report != null)
        {
            result["buildTime"] = report.summary.buildStartedAt;
            var packs = Read(report, "packedAssets") ?? report.GetType().GetMethod("GetPackedAssets", Type.EmptyTypes)?.Invoke(report, null);
            var guid = AssetDatabase.AssetPathToGUID(Target);
            if (packs is IEnumerable packages)
                foreach (var pack in packages)
                    if (Read(pack, "contents") is IEnumerable contents)
                        foreach (var item in contents)
                        {
                            if (!string.Equals(Read(item, "sourceAssetPath")?.ToString(), Target, StringComparison.OrdinalIgnoreCase) && Read(item, "sourceAssetGUID")?.ToString() != guid) continue;
                            var row = new Dictionary<string, string>(); row["container"] = Read(pack, "shortPath")?.ToString();
                            foreach (var field in item.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)) row[field.Name] = field.GetValue(item)?.ToString();
                            foreach (var property in item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                                if (property.GetIndexParameters().Length == 0) row[property.Name] = property.GetValue(item)?.ToString();
                            matches.Add(row);
                        }
            result["packedAssetsApiFound"] = packs != null;
        }
        result["packedObjects"] = matches;
        File.WriteAllText(Work + "/cat-build-audit.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
    }
    private static object Read(object instance, string name) => instance.GetType().GetField(name)?.GetValue(instance) ?? instance.GetType().GetProperty(name)?.GetValue(instance);
}
#endif
