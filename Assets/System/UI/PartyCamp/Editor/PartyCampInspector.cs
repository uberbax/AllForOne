#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class PartyCampInspector
{
    public const string Work = @"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\party-camp";
    [InitializeOnLoadMethod] private static void Init(){if(File.Exists(Work+"/inspect.request"))EditorApplication.delayCall+=Run;}
    [MenuItem("Tools/UI/Party Camp/Inspect")]
    public static void Run()
    {
        try
        {
            var source=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name.Equals("UI_AssignPets",StringComparison.OrdinalIgnoreCase));
            string P(Transform t)=>AnimationUtility.CalculateTransformPath(t,source);
            var resources=UnityEngine.Object.FindFirstObjectByType<ResourceHolder>(FindObjectsInactive.Include);
            var scene=source.gameObject.scene;
            var data=new{
                scene=scene.path,dirty=scene.isDirty,
                hierarchy=source.GetComponentsInChildren<Transform>(true).Take(100).Select(t=>new{path=P(t),active=t.gameObject.activeSelf}).ToArray(),
                fillers=source.GetComponentsInChildren<UIfiller>(true).Select(f=>new{path=P(f.transform),f.nm,f.param,f.param2,f.command,f.clickFunc,f.fillFunc,f.compatibility,f.replaceClick,f.asGrid,root=f.root!=null?P(f.root):null,spawn=AssetDatabase.GetAssetPath(f.spawn)}).ToArray(),
                bindings=source.GetComponentsInChildren<UnoAll>(true).Select(u=>new{path=P(u.transform),u.param,u.param2,u.isButton,u.isStat,u.isPar}).ToArray(),
                holders=source.GetComponentsInChildren<ObjHolder>(true).Select(h=>new{path=P(h.transform),h.noTrack,h.asMain,h.asObj,h.asCurObj,h.asDBObj}).ToArray(),
                cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene).Select(c=>new{c.name,c.enabled,c.orthographic,c.orthographicSize,position=c.transform.position.ToString(),rotation=c.transform.eulerAngles.ToString(),c.cullingMask}).ToArray(),
                renderers=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene&&c.sprite!=null).Take(35).Select(c=>new{c.name,sprite=AssetDatabase.GetAssetPath(c.sprite),position=c.transform.position.ToString(),bounds=c.bounds.ToString()}).ToArray(),
                monsterVisuals=resources!=null?resources.monsters.Take(8).Select(v=>new{v.Key,path=AssetDatabase.GetAssetPath(v.Value),sprites=v.Value!=null?v.Value.GetComponentsInChildren<SpriteRenderer>(true).Length:0}).ToArray():null
            };
            File.WriteAllText(Work+"/inspection.json",JsonConvert.SerializeObject(data,Formatting.Indented));
            PubInspector.Render(source.gameObject,"assign-pets-reference.png");
            File.WriteAllText(Work+"/inspect.txt","PASS");File.Delete(Work+"/inspect.request");
        }catch(Exception e){File.WriteAllText(Work+"/inspect.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
#endif
