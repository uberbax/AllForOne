#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ButcherShadowTuning
{
    const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\butcher-shadows";
    [MenuItem("Tools/Diagnostics/Manual Requests/Butcher shadows")]
    static void Init(){if(File.Exists(Work+"/audit.request"))EditorApplication.delayCall+=Audit;if(File.Exists(Work+"/compare.request"))EditorApplication.delayCall+=Compare;if(File.Exists(Work+"/apply.request"))EditorApplication.delayCall+=Apply;}
    static string PathOf(Transform t){return t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;}
    static void Render(Camera camera,string filename)
    {
        var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(720,1600,24);Texture2D texture=null;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(Work+"/"+filename,texture.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);}
    }
    static void Compare()
    {
        Camera camera=null;Light sun=null;float far=0,bias=0,normal=0;int resolution=0;LightShadowResolution quality=default;bool async=ShaderUtil.allowAsyncCompilation;
        try
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="Game_Exp_butcher_3d"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected Butcher in Edit Mode");
            camera=scene.GetRootGameObjects().Single(g=>g.name=="Main Camera").GetComponent<Camera>();sun=scene.GetRootGameObjects().Single(g=>g.name=="Sun - late afternoon").GetComponent<Light>();
            far=camera.farClipPlane;bias=sun.shadowBias;normal=sun.shadowNormalBias;resolution=sun.shadowCustomResolution;quality=sun.shadowResolution;
            ShaderUtil.allowAsyncCompilation=false;Render(camera,"before.png");
            camera.farClipPlane=60;sun.shadowBias=.03f;sun.shadowNormalBias=.2f;sun.shadowCustomResolution=2048;Render(camera,"after-2048.png");
            sun.shadowCustomResolution=4096;Render(camera,"after-4096.png");
            File.Delete(Work+"/compare.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/compare-error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=async;if(camera!=null)camera.farClipPlane=far;if(sun!=null){sun.shadowBias=bias;sun.shadowNormalBias=normal;sun.shadowCustomResolution=resolution;sun.shadowResolution=quality;}}
    }
    static void Apply()
    {
        bool async=ShaderUtil.allowAsyncCompilation;
        try
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="Game_Exp_butcher_3d"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected Butcher in Edit Mode");
            var camera=scene.GetRootGameObjects().Single(g=>g.name=="Main Camera").GetComponent<Camera>();var sun=scene.GetRootGameObjects().Single(g=>g.name=="Sun - late afternoon").GetComponent<Light>();
            int quality=QualitySettings.GetQualityLevel();float shadowDistance=QualitySettings.shadowDistance;var shadowResolution=QualitySettings.shadowResolution;
            Undo.RecordObjects(new UnityEngine.Object[]{camera,sun},"Improve Butcher scene shadows");
            camera.farClipPlane=60;sun.shadows=LightShadows.Soft;sun.shadowCustomResolution=4096;sun.shadowResolution=LightShadowResolution.VeryHigh;sun.shadowBias=.03f;sun.shadowNormalBias=.2f;sun.shadowStrength=.82f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);PrefabUtility.RecordPrefabInstancePropertyModifications(sun);EditorUtility.SetDirty(camera);EditorUtility.SetDirty(sun);EditorSceneManager.MarkSceneDirty(scene);
            ShaderUtil.allowAsyncCompilation=false;Render(camera,"final.png");
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
            if(QualitySettings.GetQualityLevel()!=quality||QualitySettings.shadowDistance!=shadowDistance||QualitySettings.shadowResolution!=shadowResolution)throw new Exception("Global Quality changed unexpectedly");
            File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",scene=scene.path,farClip=camera.farClipPlane,shadowResolution=sun.shadowCustomResolution,bias=sun.shadowBias,normalBias=sun.shadowNormalBias,strength=sun.shadowStrength,globalQualityUnchanged=true},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/apply.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/apply-error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=async;}
    }
    static void Audit()
    {
        try
        {
            var scene=EditorSceneManager.GetActiveScene();var roots=scene.GetRootGameObjects();
            var cams=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).ToArray();var lights=roots.SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
            File.WriteAllText(Work+"/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,playing=EditorApplication.isPlaying,dirty=scene.isDirty,quality=QualitySettings.names[QualitySettings.GetQualityLevel()],shadows=QualitySettings.shadows.ToString(),shadowResolution=QualitySettings.shadowResolution.ToString(),cascades=QualitySettings.shadowCascades,distance=QualitySettings.shadowDistance,projection=QualitySettings.shadowProjection.ToString(),cameras=cams.Select(c=>new{path=PathOf(c.transform),active=c.isActiveAndEnabled,position=c.transform.position.ToString(),rotation=c.transform.eulerAngles.ToString(),ortho=c.orthographic,size=c.orthographicSize,near=c.nearClipPlane,far=c.farClipPlane,mask=c.cullingMask,target=c.targetTexture==null?"screen":c.targetTexture.name}),lights=lights.Select(l=>new{path=PathOf(l.transform),active=l.isActiveAndEnabled,type=l.type.ToString(),shadows=l.shadows.ToString(),resolution=l.shadowResolution.ToString(),custom=l.shadowCustomResolution,strength=l.shadowStrength,bias=l.shadowBias,normalBias=l.shadowNormalBias,intensity=l.intensity,rotation=l.transform.eulerAngles.ToString()}),sceneView=SceneView.lastActiveSceneView==null?null:new{pivot=SceneView.lastActiveSceneView.pivot.ToString(),rotation=SceneView.lastActiveSceneView.rotation.eulerAngles.ToString(),size=SceneView.lastActiveSceneView.size},land=roots.Where(g=>g.name=="Basic3Dland").Select(g=>new{position=g.transform.position.ToString(),scale=g.transform.lossyScale.ToString()})},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/audit.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
    }
}
#endif
