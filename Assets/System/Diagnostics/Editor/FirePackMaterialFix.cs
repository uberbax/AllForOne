#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FirePackMaterialFix
{
    private const string Root="Assets/Suriyun/Monster Pack Fire";
    private const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\fire-materials";
    private static void Init()
    {if(File.Exists(Work+"/fix.request"))EditorApplication.delayCall+=Run;}
    [MenuItem("Tools/Diagnostics/Fix Monster Pack Fire Built-in Materials")]
    public static void Run()
    {
        bool asynchronous=ShaderUtil.allowAsyncCompilation;
        try
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit Mode");
            if(GraphicsSettings.currentRenderPipeline!=null)throw new Exception("Expected Built-in pipeline");
            var materials=AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).Where(m=>m!=null).ToArray();
            if(materials.Length==0||materials.Any(m=>m.shader==null||m.shader.name!="Toon/Toon"))throw new Exception("Unexpected material/shader in Fire pack");
            var textureBefore=materials.Select(m=>m.GetTexture("_MainTex")).ToArray();
            var guids=materials.Select(m=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m))).ToArray();
            Undo.RecordObjects(materials,"Enable Fire pack UTS Built-in rendering");
            for(int i=0;i<materials.Length;i++)
            {
                materials[i].EnableKeyword("UTS_RP_BUILTIN");
                if(!materials[i].IsKeywordEnabled("UTS_RP_BUILTIN")||!materials[i].shader.isSupported||materials[i].GetTexture("_MainTex")!=textureBefore[i])throw new Exception("Material validation failed: "+materials[i].name);
                EditorUtility.SetDirty(materials[i]);AssetDatabase.SaveAssetIfDirty(materials[i]);
            }
            ShaderUtil.allowAsyncCompilation=false;
            string[] models={"Tampora/Tampora_A","Minotos/Minotos","Golem/Golem_A","Fire Pig/Fire_Pig_A","Fire Gazer/Fire_Gazer_A","Cute Phoenix/Cute Phoenix_A"};
            var renders=new List<object>();
            foreach(var model in models)
            {
                string name=Path.GetFileName(model).Replace(' ','_');
                int before=Render(model,name+"-before.png",true),after=Render(model,name+"-after.png",false);
                if(before<100||after>before*.05f+2)throw new Exception("Magenta remains on "+model+": "+before+" -> "+after);
                renders.Add(new{model,before,after});
            }
            for(int i=0;i<materials.Length;i++)if(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(materials[i]))!=guids[i])throw new Exception("Material GUID changed");
            File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",count=materials.Length,materials=materials.Select(AssetDatabase.GetAssetPath).ToArray(),shader="Toon/Toon",pipeline="Built-in",renders},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/fix.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=asynchronous;}
    }
    private static int Render(string modelPath,string filename,bool broken)
    {
        var preview=new PreviewRenderUtility();Texture2D texture=null;var copies=new List<Material>();
        try
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefab/"+modelPath+".prefab");
            if(source==null)throw new Exception("Missing model "+modelPath);
            var model=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(model);model.SetActive(true);
            foreach(var ps in model.GetComponentsInChildren<ParticleSystem>(true))ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderers=model.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();
            if(renderers.Length==0)throw new Exception("No renderer on "+modelPath);
            foreach(var renderer in renderers)
            {
                var values=renderer.sharedMaterials;
                for(int i=0;i<values.Length;i++)if(values[i]!=null&&values[i].shader!=null&&values[i].shader.name=="Toon/Toon")
                {
                    var copy=new Material(values[i]);if(broken)copy.DisableKeyword("UTS_RP_BUILTIN");else copy.EnableKeyword("UTS_RP_BUILTIN");
                    copies.Add(copy);values[i]=copy;
                }
                renderer.sharedMaterials=values;
            }
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
            preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(.01f,size*.65f);
            preview.camera.transform.position=bounds.center+new Vector3(2,.7f,4).normalized*size*3;preview.camera.transform.LookAt(bounds.center);
            preview.camera.nearClipPlane=.001f;preview.camera.farClipPlane=size*10+10;preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.07f,.08f,.1f);
            preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);preview.lights[1].intensity=.4f;preview.ambientColor=new Color(.25f,.25f,.25f);
            preview.BeginStaticPreview(new Rect(0,0,512,512));preview.Render();texture=preview.EndStaticPreview();
            File.WriteAllBytes(Work+"/"+filename,texture.EncodeToPNG());
            return texture.GetPixels32().Count(c=>c.r>=250&&c.b>=250&&c.g<=5);
        }
        finally{preview.Cleanup();if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);foreach(var material in copies)UnityEngine.Object.DestroyImmediate(material);}
    }
}
#endif
