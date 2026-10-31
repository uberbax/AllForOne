#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class RumbleAnimationEventFix
{
    private const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\rumble-animation-events";
    private static readonly string[] Ids={"toad_horn","worm_poison","orc_smasher_berserker","winter_ice","carapace_castle","wing_demon_bee","flower_candy","raptor_mechanic","horse_unicorn","flower_daisy"};
    [InitializeOnLoadMethod]private static void Init()
    {
        EditorApplication.delayCall+=Requested;
        EditorApplication.playModeStateChanged-=ModeChanged;
        EditorApplication.playModeStateChanged+=ModeChanged;
    }
    private static void ModeChanged(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode)Requested();}
    private static void Requested(){if(File.Exists(Work+"/fix.request")&&!EditorApplication.isPlayingOrWillChangePlaymode)Fix();}
    private static string Curves(AnimationClip clip)
    {
        return JsonConvert.SerializeObject(new {
            clip.length,clip.frameRate,clip.wrapMode,clip.legacy,
            settings=JsonUtility.ToJson(AnimationUtility.GetAnimationClipSettings(clip)),
            curves=AnimationUtility.GetCurveBindings(clip).Select(b=>new{b.path,b.propertyName,type=b.type.FullName,curve=JsonUtility.ToJson(AnimationUtility.GetEditorCurve(clip,b))}).ToArray(),
            sprites=AnimationUtility.GetObjectReferenceCurveBindings(clip).Select(b=>new{b.path,b.propertyName,type=b.type.FullName,keys=AnimationUtility.GetObjectReferenceCurve(clip,b).Select(k=>new{k.time,id=k.value!=null?GlobalObjectId.GetGlobalObjectIdSlow(k.value).ToString():"null"}).ToArray()}).ToArray()
        });
    }
    private static bool HasReceiver(GameObject gameObject,string name)
    {
        return gameObject.GetComponents<MonoBehaviour>().Where(b=>b!=null).Any(b=>
            b.GetType().GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Any(m=>
            {
                if(m.Name!=name||m.IsGenericMethod||m.ReturnType!=typeof(void))return false;
                var p=m.GetParameters();return p.Length==0||(p.Length==1&&
                    (p[0].ParameterType==typeof(float)||p[0].ParameterType==typeof(int)||p[0].ParameterType==typeof(string)||
                     p[0].ParameterType==typeof(AnimationEvent)||typeof(UnityEngine.Object).IsAssignableFrom(p[0].ParameterType)));
            }));
    }
    [MenuItem("Tools/Skills/Fix Rumble Monster Animation Events")]
    public static void Fix()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        try
        {
            Directory.CreateDirectory(Work+"/before");
            var resources=UnityEngine.Object.FindFirstObjectByType<ResourceHolder>(FindObjectsInactive.Include);
            if(resources==null)throw new InvalidOperationException("ResourceHolder not found");
            var animators=new List<Animator>();
            foreach(var id in Ids)
            {
                if(!resources.monsters.TryGetValue(id,out var prefab)||prefab==null)throw new InvalidOperationException("Missing monster "+id);
                animators.AddRange(prefab.GetComponentsInChildren<Animator>(true));
            }
            var clips=animators.Where(a=>a.runtimeAnimatorController!=null).SelectMany(a=>a.runtimeAnimatorController.animationClips).Distinct().ToArray();
            var changed=new List<object>();int removed=0;
            foreach(var clip in clips)
            {
                var events=AnimationUtility.GetAnimationEvents(clip);var valid=events.Where(e=>!string.IsNullOrWhiteSpace(e.functionName)).ToArray();
                if(valid.Length==events.Length)continue;
                string path=AssetDatabase.GetAssetPath(clip);
                if(!path.StartsWith("Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 3/Animations/",StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected clip outside selected pack: "+path);
                string guid=AssetDatabase.AssetPathToGUID(path),before=Curves(clip);
                string backup=Work+"/before/"+Path.GetFileName(Path.GetDirectoryName(path))+"_"+Path.GetFileName(path);
                if(!File.Exists(backup))File.Copy(path,backup);
                Undo.RecordObject(clip,"Remove empty Rumble Animation Events");
                AnimationUtility.SetAnimationEvents(clip,valid);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
                if(before!=Curves(clip)||guid!=AssetDatabase.AssetPathToGUID(path))throw new InvalidOperationException("Animation curves/settings/GUID changed: "+path);
                removed+=events.Length-valid.Length;changed.Add(new{path,removed=events.Length-valid.Length,curvesAndSettingsPreserved=true});
            }
            int empty=clips.Sum(c=>AnimationUtility.GetAnimationEvents(c).Count(e=>string.IsNullOrWhiteSpace(e.functionName)));
            var missing=new List<object>();
            foreach(var animator in animators.Where(a=>a.runtimeAnimatorController!=null))
                foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                    foreach(var evt in AnimationUtility.GetAnimationEvents(clip))
                        if(!string.IsNullOrWhiteSpace(evt.functionName)&&evt.messageOptions==SendMessageOptions.RequireReceiver&&!HasReceiver(animator.gameObject,evt.functionName))
                            missing.Add(new{prefab=AssetDatabase.GetAssetPath(animator.gameObject),clip=AssetDatabase.GetAssetPath(clip),evt.functionName});
            File.WriteAllText(Work+"/result.json",JsonConvert.SerializeObject(new{result=empty==0&&missing.Count==0?"PASS":"FAIL",monsters=Ids.Length,checkedClips=clips.Length,removed,remainingEmpty=empty,missingReceivers=missing,changed},Formatting.Indented));
            if(empty!=0||missing.Count!=0)throw new InvalidOperationException("Animation event audit found unresolved events");
            File.Delete(Work+"/fix.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
    }
}
#endif

