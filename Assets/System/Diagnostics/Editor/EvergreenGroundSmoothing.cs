#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class EvergreenGroundSmoothing
{
    const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\ground-smoothing";
    const string Root="Assets/Scenes/EvergreenRPG/";
    [InitializeOnLoadMethod] static void Init(){if(File.Exists(Work+"/smooth.request"))EditorApplication.delayCall+=Run;}
    static void Render(Camera c,string path)
    {
        var old=c.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(720,1600,24);Texture2D image=null;
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{c.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
    static void Run()
    {
        bool async=ShaderUtil.allowAsyncCompilation;
        try
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit Mode");
            var scene=EditorSceneManager.GetActiveScene();var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).FirstOrDefault(c=>c.name=="Main Camera");
            ShaderUtil.allowAsyncCompilation=false;if(camera!=null)Render(camera,Work+"/before.png");
            var generator=typeof(EvergreenRPGBuilder);var flags=BindingFlags.NonPublic|BindingFlags.Static;
            var height=(Func<float,float,float>)Delegate.CreateDelegate(typeof(Func<float,float,float>),generator.GetMethod("Height",flags));
            var road=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("RoadDistance",flags));
            var clearing=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("ClearingDistance",flags));
            var bank=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("BankDistance",flags));
            var meshes=new[]{AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Forest terrain.asset"),AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Stream bed - Not Walkable.asset")};
            if(meshes.Any(m=>m==null))throw new Exception("Missing generated ground meshes");
            Undo.RegisterCompleteObjectUndo(meshes,"Smooth forest ground colors and lighting");
            int count=0;var unique=new Dictionary<Vector3,Tuple<Color,Vector3>>();
            foreach(var mesh in meshes)
            {
                var vertices=mesh.vertices;var triangles=mesh.triangles;var guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh));
                var colors=new Color[vertices.Length];var normals=new Vector3[vertices.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    var v=vertices[i];
                    if(!unique.TryGetValue(v,out var value))
                    {
                        var p=new Vector2(v.x,v.z);float noise=Mathf.PerlinNoise(v.x*.16f+64,v.z*.16f+64);
                        var color=Color.Lerp(new Color(.19f,.32f,.105f),new Color(.31f,.43f,.16f),noise);
                        float path=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.25f,2.6f,road(p)+.3f*(noise-.5f)));
                        float open=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.8f,5.7f,clearing(p)+.5f*(noise-.5f)));
                        color=Color.Lerp(color,new Color(.34f,.32f,.19f),Mathf.Max(path,open*.82f));
                        float distance=bank(p);if(distance<3.1f)color=Color.Lerp(new Color(.26f,.28f,.16f),color,Mathf.InverseLerp(1.7f,3.1f,distance));
                        const float e=.04f;float dx=(height(v.x+e,v.z)-height(v.x-e,v.z))/(2*e),dz=(height(v.x,v.z+e)-height(v.x,v.z-e))/(2*e);
                        value=Tuple.Create(color,new Vector3(-dx,1,-dz).normalized);unique.Add(v,value);
                    }
                    colors[i]=value.Item1;normals[i]=value.Item2;
                }
                mesh.colors=colors;mesh.normals=normals;
                if(!mesh.vertices.SequenceEqual(vertices)||!mesh.triangles.SequenceEqual(triangles)||AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh))!=guid)throw new Exception("Ground geometry/reference changed");
                EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);count+=vertices.Length;
            }
            if(camera!=null)Render(camera,Work+"/after.png");
            File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",meshes=meshes.Length,vertices=count,uniquePositions=unique.Count,geometryUnchanged=true,colorsContinuous=true,normals="smooth analytical",scene=scene.path},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/smooth.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=async;}
    }
}
#endif
