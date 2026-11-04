#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuiltinPipelineVerification
{
    private const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\remove-srp";
    private const string MaterialPath="Assets/Suriyun/RedKnight/Materials/Characters/M05.mat";
    private static ListRequest request;
    [InitializeOnLoadMethod] private static void Init()
    {if(File.Exists(Work+"/verify.request")){request=Client.List(true,false);EditorApplication.update+=Wait;}}
    private static void Wait()
    {
        if(request==null||!request.IsCompleted||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        EditorApplication.update-=Wait;
        if(request.Status!=StatusCode.Success){File.WriteAllText(Work+"/error.txt",request.Error?.message);return;}
        var installed=request.Result.Select(p=>p.name).ToArray();
        File.WriteAllText(Work+"/packages.json",Newtonsoft.Json.JsonConvert.SerializeObject(installed,Newtonsoft.Json.Formatting.Indented));
        if(installed.Any(p=>p=="com.unity.render-pipelines.universal"||p=="com.unity.render-pipelines.high-definition"||p=="com.unity.render-pipelines.high-definition-config"))
        {File.WriteAllText(Work+"/error.txt","SRP packages still installed");return;}
        Verify();
    }
    [MenuItem("Tools/Diagnostics/Verify Built-in and RedKnight")]
    public static void Verify()
    {
        Material before=null;bool asyncCompilation=ShaderUtil.allowAsyncCompilation;
        try
        {
            if(GraphicsSettings.currentRenderPipeline!=null)throw new Exception("Active render pipeline is not Built-in");
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material==null||material.shader==null||material.shader.name!="Toon/Toon")throw new Exception("Original Unity Toon Shader is missing");
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(material.shader),ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var toonMaterials=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Suriyun/RedKnight/Materials"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).Where(m=>m!=null&&m.shader!=null&&m.shader.name=="Toon/Toon").ToArray();
            var texture=material.GetTexture("_MainTex");var textureScale=material.GetTextureScale("_MainTex");var textureOffset=material.GetTextureOffset("_MainTex");
            bool initialKeyword=material.IsKeywordEnabled("UTS_RP_BUILTIN");
            ShaderUtil.allowAsyncCompilation=false;
            before=new Material(material);before.DisableKeyword("UTS_RP_BUILTIN");
            var oldPixels=Render(before,"before.png");
            Undo.RecordObjects(toonMaterials,"Enable Unity Toon Built-in rendering");
            foreach(var toon in toonMaterials){toon.EnableKeyword("UTS_RP_BUILTIN");EditorUtility.SetDirty(toon);}
            var newPixels=Render(material,"after.png");
            if(!material.shader.isSupported||newPixels.magenta>oldPixels.magenta*.1f||oldPixels.magenta<100)throw new Exception("RedKnight render comparison failed: "+oldPixels.magenta+" -> "+newPixels.magenta);
            if(material.GetTexture("_MainTex")!=texture||material.GetTextureScale("_MainTex")!=textureScale||material.GetTextureOffset("_MainTex")!=textureOffset)throw new Exception("Texture settings changed");
            foreach(var toon in toonMaterials)AssetDatabase.SaveAssetIfDirty(toon);
            File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",pipeline="Built-in",shader=material.shader.name,shaderSupported=material.shader.isSupported,initialBuiltinKeyword=initialKeyword,builtinKeyword=material.IsKeywordEnabled("UTS_RP_BUILTIN"),texture=AssetDatabase.GetAssetPath(texture),magentaBefore=oldPixels.magenta,magentaAfter=newPixels.magenta,materialGuid=AssetDatabase.AssetPathToGUID(MaterialPath),correctedMaterials=toonMaterials.Select(AssetDatabase.GetAssetPath).ToArray(),keywords=material.shaderKeywords,shaderMessages=ShaderUtil.GetShaderMessages(material.shader).Select(m=>new{severity=m.severity.ToString(),m.message,m.file,m.line}).ToArray()},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/verify.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=asyncCompilation;if(before!=null)UnityEngine.Object.DestroyImmediate(before);}
    }
    private static (int magenta,int pixels) Render(Material material,string filename)
    {
        var preview=new PreviewRenderUtility();Texture2D result=null;
        try
        {
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suriyun/RedKnight/Prefab/M05/M05.prefab"));
            preview.AddSingleGO(model);model.SetActive(true);
            var renderers=model.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
            if(renderers.Length==0)throw new Exception("RedKnight has no visible renderer");
            int replacements=0;
            foreach(var renderer in renderers)
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)if(AssetDatabase.GetAssetPath(materials[i])==MaterialPath){materials[i]=material;replacements++;}
                renderer.sharedMaterials=materials;
            }
            if(replacements==0)throw new Exception("RedKnight prefab does not reference M05 material");
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
            preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(.01f,size*.67f);
            preview.camera.transform.position=bounds.center+new Vector3(2,.8f,4).normalized*size*3;preview.camera.transform.LookAt(bounds.center);
            preview.camera.nearClipPlane=.001f;preview.camera.farClipPlane=size*10+10;preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.07f,.08f,.1f);
            preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);preview.lights[1].intensity=.4f;
            preview.ambientColor=new Color(.25f,.25f,.25f);
            preview.BeginStaticPreview(new Rect(0,0,512,512));preview.Render();result=preview.EndStaticPreview();
            File.WriteAllBytes(Work+"/"+filename,result.EncodeToPNG());var colors=result.GetPixels32();
            int magenta=colors.Count(c=>c.r>180&&c.b>180&&c.g<80);
            return(magenta,colors.Length);
        }
        finally{if(result!=null)UnityEngine.Object.DestroyImmediate(result);preview.Cleanup();}
    }
}
#endif
