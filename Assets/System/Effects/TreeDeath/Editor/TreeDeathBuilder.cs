#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using TMPro;
public static class TreeDeathBuilder
{
    const string Root="Assets/System/Effects/TreeDeath";
    const string Flag=Root+"/Editor/.build-tree-death";
    [InitializeOnLoadMethod] static void Init() {if(File.Exists(Flag))EditorApplication.delayCall+=Requested;}
    static void Requested()
    {
        if(EditorApplication.isPlaying)return;File.Delete(Flag);
        try {Build();File.WriteAllText(Path.Combine(Path.GetTempPath(),"tree-death-build.txt"),"PASS: generated stump imported with alpha, cropped Sprite and Resources prefab saved; native three-stage fall preview rendered.");}
        catch(Exception e){File.WriteAllText(Path.Combine(Path.GetTempPath(),"tree-death-build.txt"),"FAIL: "+e);Debug.LogException(e);}
    }
    [MenuItem("Tools/Effects/Build Tree Death")]
    public static void Build()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Tree_Stump.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=512;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        if(!importer.DoesSourceTextureHaveAlpha())throw new Exception("Stump PNG has no alpha");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Tree_Stump.png");
        var sprite=Crop(texture);
        if(!AssetDatabase.IsValidFolder(Root+"/Resources"))AssetDatabase.CreateFolder(Root,"Resources");
        if(!AssetDatabase.IsValidFolder(Root+"/Resources/TreeDeath"))AssetDatabase.CreateFolder(Root+"/Resources","TreeDeath");
        var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("TreeDeathEffect",typeof(TreeDeathEffect),typeof(SortingGroup));SceneManager.MoveGameObjectToScene(root,scene);
        try {
            var pivot=new GameObject("Falling tree");pivot.transform.SetParent(root.transform,false);
            var stump=new GameObject("Stump",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();stump.transform.SetParent(root.transform,false);stump.sprite=sprite;
            stump.sharedMaterial=AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            var fields=new SerializedObject(root.GetComponent<TreeDeathEffect>());fields.FindProperty("fallingPivot").objectReferenceValue=pivot.transform;
            fields.FindProperty("stump").objectReferenceValue=stump;fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Resources/TreeDeath/TreeDeathEffect.prefab");
        } finally {UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        Preview();
    }
    static Sprite Crop(Texture2D texture)
    {
        var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;Texture2D readable=null;
        try {
            Graphics.Blit(texture,rt);RenderTexture.active=rt;readable=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
            readable.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);readable.Apply();var pixels=readable.GetPixels32();
            int lx=texture.width,ly=texture.height,hx=0,hy=0;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++){if(pixels[y*texture.width+x].a<16)continue;lx=Mathf.Min(lx,x);ly=Mathf.Min(ly,y);hx=Mathf.Max(hx,x);hy=Mathf.Max(hy,y);}
            var generated=Sprite.Create(texture,new Rect(lx,ly,hx-lx+1,hy-ly+1),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);generated.name="Tree_Stump";
            string path=Root+"/Tree_Stump_Sprite.asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(existing==null){AssetDatabase.CreateAsset(generated,path);return generated;}
            EditorUtility.CopySerialized(generated,existing);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);UnityEngine.Object.DestroyImmediate(generated);return existing;
        }finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(readable!=null)UnityEngine.Object.DestroyImmediate(readable);}
    }
    static void Preview()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_07.png");
            for(int i=0;i<3;i++){
                var owner=new GameObject("Tree preview "+i);SceneManager.MoveGameObjectToScene(owner,scene);
                var renderer=owner.AddComponent<SpriteRenderer>();renderer.sprite=sprite;
                float scale=3.2f/sprite.bounds.size.y;owner.transform.localScale=Vector3.one*scale;
                owner.transform.position=new Vector3((i-1)*3.5f-sprite.bounds.center.x*scale,-sprite.bounds.min.y*scale,0);
                var attacker=new GameObject("Attacker");SceneManager.MoveGameObjectToScene(attacker,scene);attacker.transform.position=owner.transform.position+Vector3.left;
                var item=new RObj{dbID="tree",RID="tree_preview_"+i,main=owner,lastDmgFrom=new RObj{main=attacker}};item.visuals["vis_main"]=owner;
                if(!TreeDeathEffect.TryPlay(item))throw new Exception("Preview tree fall did not start");
                var effect=scene.GetRootGameObjects().Select(g=>g.GetComponent<TreeDeathEffect>()).Last(e=>e!=null && e.name!="Preview sampled");
                effect.name="Preview sampled";effect.SetPreviewProgress(i==0?0:i==1?.62f:.98f);
                var text=new GameObject("Stage",typeof(TextMeshPro));SceneManager.MoveGameObjectToScene(text,scene);
                var label=text.GetComponent<TextMeshPro>();label.rectTransform.sizeDelta=new Vector2(3,.5f);label.transform.position=new Vector3((i-1)*3.5f,-.6f,0);
                label.fontSize=2;label.alignment=TextAlignmentOptions.Center;label.text=i==0?"0.0 s":i==1?"0.7 s":"1.15 s";
            }
            var go=new GameObject("Preview camera",typeof(Camera));SceneManager.MoveGameObjectToScene(go,scene);var camera=go.GetComponent<Camera>();camera.scene=scene;
            camera.orthographic=true;camera.orthographicSize=2.6f;camera.transform.position=new Vector3(0,1.25f,-10);camera.backgroundColor=new Color(.22f,.29f,.16f);camera.clearFlags=CameraClearFlags.SolidColor;
            var rt=new RenderTexture(1400,650,24);var previous=RenderTexture.active;Texture2D texture=null;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture=new Texture2D(1400,650,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1400,650),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"tree-death-preview.png"),texture.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
