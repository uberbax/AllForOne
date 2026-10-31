#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RumbleVfxBuilder
{
    public const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\rumble-vfx";
    public const string Root="Assets/System/Effects/RumbleSkills";
    public const string LibraryPath=Root+"/Resources/RumbleSkills/VfxLibrary.asset";
    [Serializable]public class Part { public string path;public float[] rotation,tint;public float scale=1; }
    [Serializable]public class Recipe { public string id;public Part[] parts;public bool loop,beam; }
    [Serializable]public class Style { public string id,projectile,impact,area,cast,beam,heal,shield; }
    [Serializable]public class Selection { public Recipe[] recipes;public Style[] styles; }
    [InitializeOnLoadMethod]private static void Init(){if(File.Exists(Work+"/build.request"))EditorApplication.delayCall+=Build;}
    private static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    private static ParticleSystem[] Roots(GameObject root)=>root.GetComponentsInChildren<ParticleSystem>(true)
        .Where(p=>!p.GetComponentsInParent<ParticleSystem>(true).Any(a=>a!=p)).ToArray();
    private static void Simulate(GameObject root,float time)
    {foreach(var system in Roots(root))system.Simulate(time,true,true,false);}
    private static Bounds Bounds(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r is ParticleSystemRenderer||r is LineRenderer)&&r.bounds.size.sqrMagnitude>.00001f).ToArray();
        if(renderers.Length==0)return new Bounds(Vector3.zero,Vector3.one*2);
        var result=renderers[0].bounds;foreach(var renderer in renderers)result.Encapsulate(renderer.bounds);return result;
    }
    [MenuItem("Tools/Skills/Build Pack VFX for Rumble")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        try
        {
            Folder(Root+"/Prefabs");Folder(Root+"/Resources/RumbleSkills");Directory.CreateDirectory(Work+"/previews");
            var selection=JsonUtility.FromJson<Selection>(File.ReadAllText("Assets/System/SkillS/Editor/RumbleVfxSelection.json"));
            var assets=new Dictionary<string,GameObject>();var diagnostics=new List<object>();
            foreach(var recipe in selection.recipes)
            {
                var scene=EditorSceneManager.NewPreviewScene();GameObject wrapper=null;
                try
                {
                    wrapper=new GameObject(recipe.id);SceneManager.MoveGameObjectToScene(wrapper,scene);
                    bool directional=recipe.beam||recipe.id.EndsWith("projectile")||recipe.id=="bullet_cast"||recipe.id=="sniper_cast";
                    float sample=recipe.beam?.2f:recipe.id.EndsWith("cast")?.06f:recipe.loop?.6f:.1f;
                    foreach(var part in recipe.parts)
                    {
                        var source=AssetDatabase.LoadAssetAtPath<GameObject>(part.path);if(source==null)throw new InvalidOperationException("Missing source "+part.path);
                        var child=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);child.transform.SetParent(wrapper.transform,false);
                        child.transform.localPosition=Vector3.zero;child.transform.localRotation=Quaternion.Euler(part.rotation[0],part.rotation[1],part.rotation[2]);
                        // VFX-only copies: movement, collision callbacks, demo spawners and auto-destroy belong to the game, not the asset pack.
                        foreach(var script in child.GetComponentsInChildren<MonoBehaviour>(true))if(script!=null)UnityEngine.Object.DestroyImmediate(script);
                        foreach(var collider in child.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                        foreach(var collider in child.GetComponentsInChildren<Collider2D>(true))UnityEngine.Object.DestroyImmediate(collider);
                        foreach(var body in child.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                        foreach(var body in child.GetComponentsInChildren<Rigidbody2D>(true))UnityEngine.Object.DestroyImmediate(body);
                        foreach(var light in child.GetComponentsInChildren<Light>(true))light.enabled=false;
                        foreach(var audio in child.GetComponentsInChildren<AudioSource>(true))audio.enabled=false;
                        if(recipe.beam)
                            foreach(var line in child.GetComponentsInChildren<LineRenderer>(true))
                            {line.useWorldSpace=false;line.positionCount=2;line.SetPosition(0,Vector3.zero);line.SetPosition(1,Vector3.forward);line.widthMultiplier=.9f;}
                        foreach(var system in child.GetComponentsInChildren<ParticleSystem>(true))
                        {
                            system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                            var main=system.main;main.loop=recipe.loop;main.stopAction=ParticleSystemStopAction.None;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.useUnscaledTime=false;
                            if(recipe.id=="iron_area")main.duration=.5f;
                            var collision=system.collision;collision.enabled=false;var trigger=system.trigger;trigger.enabled=false;var lights=system.lights;lights.enabled=false;
                            if(recipe.id=="heal_beam")
                            {
                                // Reuse the authored healing-cross/spark textures as a directional stream.
                                system.transform.localPosition=Vector3.zero;system.transform.localRotation=Quaternion.identity;
                                main.startDelay=0;main.startSpeed=0;main.startLifetime=.4f;main.startSize=1;main.simulationSpace=ParticleSystemSimulationSpace.Local;
                                var emission=system.emission;emission.enabled=true;emission.rateOverTime=30;emission.SetBursts(new[]{new ParticleSystem.Burst(0,8)});
                                var shape=system.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.position=new Vector3(0,0,.5f);shape.scale=new Vector3(.1f,.1f,1);
                            }
                            if(part.tint!=null&&(part.tint[0]!=1||part.tint[1]!=1||part.tint[2]!=1))
                            {
                                var color=main.startColor;var tint=new Color(part.tint[0],part.tint[1],part.tint[2],part.tint[3]);
                                if(color.mode==ParticleSystemGradientMode.Color)main.startColor=color.color*tint;
                                else if(color.mode==ParticleSystemGradientMode.TwoColors)main.startColor=new ParticleSystem.MinMaxGradient(color.colorMin*tint,color.colorMax*tint);
                            }
                        }
                        foreach(var renderer in child.GetComponentsInChildren<Renderer>(true)){renderer.sortingOrder=100;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
                        Simulate(child,sample);var bounds=Bounds(child);
                        float diameter=directional?Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z):Mathf.Max(bounds.size.x,bounds.size.y);
                        float scale=2/Mathf.Max(.1f,diameter)*part.scale;
                        child.transform.localScale*=scale;
                        if(!directional)child.transform.localPosition-=bounds.center*scale;
                        if(recipe.beam)
                        {
                            // Standardize the longitudinal emitter to one metre; runtime sets only its length.
                            var scaled=child.transform.localScale;scaled.z/=Mathf.Max(.01f,bounds.size.z*scale);child.transform.localScale=scaled;
                        }
                    }
                    foreach(var transform in wrapper.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=0;
                    var materials=wrapper.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
                    var unsupported=materials.Where(m=>m.shader==null||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader").Select(AssetDatabase.GetAssetPath).ToArray();
                    if(unsupported.Length>0)throw new InvalidOperationException(recipe.id+" has unsupported materials: "+string.Join(", ",unsupported));
                    foreach(var system in wrapper.GetComponentsInChildren<ParticleSystem>(true))system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                    wrapper.SetActive(false);
                    var asset=PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/Prefabs/"+recipe.id+".prefab");assets[recipe.id]=asset;
                    Preview(asset,recipe.id,directional,sample);
                    diagnostics.Add(new{recipe.id,recipe.loop,recipe.beam,parts=recipe.parts.Select(p=>p.path).ToArray(),particleSystems=wrapper.GetComponentsInChildren<ParticleSystem>(true).Length,materials=materials.Select(m=>new{name=m.name,shader=m.shader.name}).ToArray()});
                }
                finally{if(wrapper!=null)UnityEngine.Object.DestroyImmediate(wrapper);EditorSceneManager.ClosePreviewScene(scene);}
            }
            var library=AssetDatabase.LoadAssetAtPath<RumbleVfxLibrary>(LibraryPath);
            if(library==null){library=ScriptableObject.CreateInstance<RumbleVfxLibrary>();AssetDatabase.CreateAsset(library,LibraryPath);}
            GameObject Asset(string key)=>string.IsNullOrEmpty(key)?null:assets[key];
            library.styles=selection.styles.Select(s=>new RumbleVfxLibrary.Style{id=s.id,projectile=Asset(s.projectile),impact=Asset(s.impact),area=Asset(s.area),cast=Asset(s.cast),beam=Asset(s.beam),heal=Asset(s.heal),shield=Asset(s.shield)}).ToArray();
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);
            if(RumbleSkills.Ids.Any(id=>!library.styles.Any(s=>s.id==id)))throw new InvalidOperationException("Incomplete skill coverage");
            File.WriteAllText(Work+"/build.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",recipes=assets.Count,styles=library.styles.Length,diagnostics},Newtonsoft.Json.Formatting.Indented));File.Delete(Work+"/build.request");
            if(File.Exists(Work+"/validate.request"))RumbleVfxValidation.Run();
        }
        catch(Exception e){File.WriteAllText(Work+"/build-error.txt",e.ToString());Debug.LogException(e);}
    }
    private static void Preview(GameObject prefab,string id,bool directional,float sample)
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject root=null,cameraGo=null;RenderTexture target=null;Texture2D texture=null;var previous=RenderTexture.active;
        try
        {
            root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);root.SetActive(true);
            foreach(var part in root.GetComponentsInChildren<Transform>(true))part.gameObject.layer=2;
            if(directional)root.transform.rotation=Quaternion.LookRotation(Vector3.right,Vector3.forward);
            Simulate(root,sample);var bounds=Bounds(root);
            cameraGo=new GameObject("VFX preview camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGo,scene);
            var camera=cameraGo.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.4f;
            camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-30);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.06f,.08f,1);camera.cullingMask=1<<2;
            camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.Render();
            RenderTexture.active=target;texture=new Texture2D(256,256,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();File.WriteAllBytes(Work+"/previews/"+id+".png",texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;if(cameraGo!=null){cameraGo.GetComponent<Camera>().targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraGo);}
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif
