#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ReaperAnimationEventFix
{
    const string Work="C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/work";
    const string Prefab="Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 3/Prefabs/Reaper/Reaper_Death.prefab";
    const string Attack="Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 3/Animations/Reaper/Attack.anim";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.delayCall+=Requested;
        EditorApplication.playModeStateChanged+=s=> { if(s==PlayModeStateChange.EnteredEditMode) Requested(); };
    }
    static void Requested()
    {
        if(EditorApplication.isPlaying || !File.Exists(Work+"/reaper-animation-events.request")) return;
        File.Delete(Work+"/reaper-animation-events.request");
        try { Fix(); }
        catch(Exception e) { File.WriteAllText(Work+"/reaper-animation-events.json","FAIL: "+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Diagnostics/Fix Reaper empty animation events")]
    public static void Fix()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        var clips=prefab.GetComponentsInChildren<Animator>(true).Where(a=>a.runtimeAnimatorController!=null)
            .SelectMany(a=>a.runtimeAnimatorController.animationClips).Distinct().ToArray();
        int removed=0;
        foreach(var clip in clips)
        {
            var events=AnimationUtility.GetAnimationEvents(clip);
            var valid=events.Where(e=>!string.IsNullOrWhiteSpace(e.functionName)).ToArray();
            if(valid.Length==events.Length) continue;
            var path=AssetDatabase.GetAssetPath(clip);
            if(path!=Attack) throw new Exception("Unexpected affected clip: "+path);
            var backup=Work+"/Reaper-Attack.before-empty-event-fix.anim";
            if(!File.Exists(backup)) File.Copy(path,backup);
            float length=clip.length,rate=clip.frameRate;
            int curves=AnimationUtility.GetCurveBindings(clip).Length+AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            Undo.RecordObject(clip,"Remove Reaper empty animation event");
            AnimationUtility.SetAnimationEvents(clip,valid);
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
            if(clip.length!=length || clip.frameRate!=rate || curves!=AnimationUtility.GetCurveBindings(clip).Length+AnimationUtility.GetObjectReferenceCurveBindings(clip).Length)
                throw new Exception("Animation integrity changed");
            removed+=events.Length-valid.Length;
        }
        int remaining=clips.Sum(c=>AnimationUtility.GetAnimationEvents(c).Count(e=>string.IsNullOrWhiteSpace(e.functionName)));
        if(remaining!=0) throw new Exception("Empty events remain");
        File.WriteAllText(Work+"/reaper-animation-events.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            prefab=Prefab,checkedClips=clips.Length,removed,remainingEmpty=remaining,
            clips=clips.Select(c=>new{path=AssetDatabase.GetAssetPath(c),events=AnimationUtility.GetAnimationEvents(c).Length}).ToArray(),
            integrity="Duration, frame rate and curve counts preserved"
        },Newtonsoft.Json.Formatting.Indented));
    }
}
#endif
