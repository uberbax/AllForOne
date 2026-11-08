#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;

public static class ButcherNavMeshCheck
{
    const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\butcher-navmesh";
    static void Init(){if(File.Exists(Work+"/inspect.request"))EditorApplication.delayCall+=Inspect;if(File.Exists(Work+"/fix.request"))EditorApplication.delayCall+=Fix;}
    static string PathOf(Transform t){return t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;}
    static List<NavMeshBuildSource> Sources(NavMeshSurface s){using(var state=new NavMeshBuilderState())return (List<NavMeshBuildSource>)typeof(NavMeshSurface).GetMethod("CollectSources",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(s,new object[]{state});}
    [MenuItem("Tools/Diagnostics/Fix Butcher NavMesh")]
    public static void Fix()
    {
        NavMeshSurface surface=null; CollectObjects oldMode=CollectObjects.Children;Vector3 oldCenter=default,oldSize=default;
        try
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="Game_Exp_butcher_3d")throw new Exception("Expected Butcher scene in Edit Mode");
            var land=scene.GetRootGameObjects().Single(g=>g.name=="Basic3Dland");
            surface=scene.GetRootGameObjects().Single(g=>g.name=="NavMesh_butcher_3d").GetComponent<NavMeshSurface>();
            var colliders=land.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
            if(colliders.Length==0||surface==null)throw new Exception("Missing land colliders/surface");
            if(Quaternion.Angle(surface.transform.rotation,Quaternion.identity)>.01f)throw new Exception("Expected horizontal 3D surface");
            var bounds=colliders[0].bounds;foreach(var c in colliders)bounds.Encapsulate(c.bounds);bounds.Expand(4);
            oldMode=surface.collectObjects;oldCenter=surface.center;oldSize=surface.size;
            Undo.RecordObject(surface,"Fix Basic3Dland NavMesh collection");
            surface.collectObjects=CollectObjects.Volume;surface.center=bounds.center-surface.transform.position;surface.size=bounds.size;
            var sources=Sources(surface);int landCount=sources.Count(x=>x.component!=null&&x.component.transform.IsChildOf(land.transform));
            if(landCount==0)throw new Exception("Land is still excluded");
            var data=new NavMeshData(surface.agentTypeID){name="Basic3Dland",position=surface.transform.position,rotation=surface.transform.rotation};
            var operation=surface.UpdateNavMesh(data);var target=surface;var previous=surface.navMeshData;var started=EditorApplication.timeSinceStartup;
            File.Delete(Work+"/fix.request");
            EditorApplication.CallbackFunction poll=null;
            poll=()=>
            {
                if(!operation.isDone)return;
                EditorApplication.update-=poll;
                try
                {
                    target.RemoveData();target.navMeshData=data;target.AddData();
                    var tri=NavMesh.CalculateTriangulation();
                    if(tri.indices.Length==0)throw new Exception("Bake produced no triangles");
                    var navBounds=new Bounds(tri.vertices[0],Vector3.zero);foreach(var v in tri.vertices)navBounds.Encapsulate(v);
                    if(!navBounds.Intersects(bounds))throw new Exception("NavMesh is outside Basic3Dland");
                    string asset=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Game_Exp_butcher_3d/NavMesh-Basic3Dland.asset");
                    AssetDatabase.CreateAsset(data,asset);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);EditorUtility.SetDirty(target);EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save scene");
                    File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",collectBefore=oldMode.ToString(),collectAfter=target.collectObjects.ToString(),sources=sources.Count,landSources=landCount,triangles=tri.indices.Length/3,bounds=navBounds.ToString(),asset,seconds=EditorApplication.timeSinceStartup-started},Newtonsoft.Json.Formatting.Indented));
                }
                catch(Exception e){target.RemoveData();target.navMeshData=previous;target.collectObjects=oldMode;target.center=oldCenter;target.size=oldSize;target.AddData();File.WriteAllText(Work+"/fix-error.txt",e.ToString());Debug.LogException(e);}
            };
            EditorApplication.update+=poll;
        }
        catch(Exception e){if(surface!=null){surface.collectObjects=oldMode;surface.center=oldCenter;surface.size=oldSize;}File.WriteAllText(Work+"/fix-error.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Diagnostics/Inspect Butcher NavMesh")]
    public static void Inspect()
    {
        try
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="Game_Exp_butcher_3d")throw new Exception("Expected Butcher scene in Edit Mode");
            var roots=scene.GetRootGameObjects();
            var land=roots.Single(g=>g.name=="Basic3Dland");
            var surfaces=roots.SelectMany(g=>g.GetComponentsInChildren<NavMeshSurface>(true)).ToArray();
            var info=surfaces.Select(s=>new{path=PathOf(s.transform),active=s.isActiveAndEnabled,collect=s.collectObjects.ToString(),geometry=s.useGeometry.ToString(),mask=s.layerMask.value,position=s.transform.position.ToString(),rotation=s.transform.eulerAngles.ToString(),size=s.size.ToString(),center=s.center.ToString(),sources=Sources(s).Count,landSources=Sources(s).Count(x=>x.component!=null&&x.component.transform.IsChildOf(land.transform)),data=AssetDatabase.GetAssetPath(s.navMeshData),extensions=s.GetComponents<MonoBehaviour>().Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray()}).ToArray();
            var colliders=land.GetComponentsInChildren<Collider>(true).Select(c=>new{path=PathOf(c.transform),type=c.GetType().Name,active=c.enabled&&c.gameObject.activeInHierarchy,trigger=c.isTrigger,layer=c.gameObject.layer,bounds=c.bounds.ToString()}).ToArray();
            var renderers=land.GetComponentsInChildren<MeshRenderer>(true).Select(c=>new{path=PathOf(c.transform),active=c.enabled&&c.gameObject.activeInHierarchy,layer=c.gameObject.layer,bounds=c.bounds.ToString()}).ToArray();
            File.WriteAllText(Work+"/inspect.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,dirty=scene.isDirty,surfaces=info,colliders,renderers,triangles=NavMesh.CalculateTriangulation().indices.Length/3},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/inspect.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
    }
}
#endif
