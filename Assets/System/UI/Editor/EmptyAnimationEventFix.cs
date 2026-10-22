#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EmptyAnimationEventFix
{
    private const string Work="C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/work";
    private const string Folder="Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 1/Animations";
    private static IEnumerable<string> GetPaths()
    {
        return AssetDatabase.FindAssets("t:AnimationClip",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).Concat(new[]{
            "Assets/Layer Lab/2D Minimal-CharacterMaker/Common/Animations/Jump Attack.anim",
            "Assets/Layer Lab/2D Minimal World/Pack/Forest/ForestVillage/ResourcesData/Animation/Tower_Catapult_Attack.anim"}).Distinct();
    }
    [InitializeOnLoadMethod]
    private static void Requested()
    {
        if(File.Exists(Work+"/empty-animation-events.request")) EditorApplication.delayCall+=Fix;
    }
    [MenuItem("Tools/Diagnostics/Remove empty monster animation events")]
    public static void Fix()
    {
        if(EditorApplication.isPlaying) return;
        File.Delete(Work+"/empty-animation-events.request");
        try
        {
            var changed=new List<object>();
            int checkedClips=0, removed=0, remainingEmpty=0;
            foreach(var path in GetPaths())
            {
                string guid=AssetDatabase.AssetPathToGUID(path);
                foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                {
                    checkedClips++;
                    var before=AnimationUtility.GetAnimationEvents(clip);
                    var valid=before.Where(e=>!string.IsNullOrWhiteSpace(e.functionName)).ToArray();
                    if(valid.Length==before.Length) continue;
                    int curves=AnimationUtility.GetCurveBindings(clip).Length+AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
                    float length=clip.length, rate=clip.frameRate;
                    var referenceGuids=AssetDatabase.GetDependencies(path,false);
                    File.Copy(path,Work+"/animation-event-backup-"+guid+".anim",true);
                    Undo.RecordObject(clip,"Remove empty animation events");
                    AnimationUtility.SetAnimationEvents(clip,valid);
                    EditorUtility.SetDirty(clip);
                    AssetDatabase.SaveAssetIfDirty(clip);
                    var after=AnimationUtility.GetAnimationEvents(clip);
                    bool preserved=after.Length==valid.Length && length==clip.length && rate==clip.frameRate && curves==AnimationUtility.GetCurveBindings(clip).Length+AnimationUtility.GetObjectReferenceCurveBindings(clip).Length && referenceGuids.SequenceEqual(AssetDatabase.GetDependencies(path,false));
                    if(!preserved) throw new InvalidOperationException("Animation integrity check failed: "+path);
                    removed+=before.Length-valid.Length;
                    changed.Add(new {path,before=before.Length,after=after.Length,removedTimes=before.Where(e=>string.IsNullOrWhiteSpace(e.functionName)).Select(e=>e.time).ToArray(),preserved});
                }
            }
            foreach(var path in GetPaths())
                foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    remainingEmpty+=AnimationUtility.GetAnimationEvents(clip).Count(e=>string.IsNullOrWhiteSpace(e.functionName));
            File.WriteAllText(Work+"/empty-animation-events.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {checkedClips,removed,remainingEmpty,changed},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e) { File.WriteAllText(Work+"/empty-animation-events.json","FAIL: "+e); }
    }
}
#endif
