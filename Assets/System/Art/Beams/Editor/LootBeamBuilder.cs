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

public static class LootBeamBuilder
{
    const string Root="Assets/System/Art/Beams";
    const string Flag=Root+"/Editor/.build-loot-beams";
    static readonly string[] Colors={"White","Green","Blue","Violet","Orange","Red"};
    static readonly string[] Names={"Common","Uncommon","Rare","Epic","Legendary","Mythic"};
    static readonly Color[] Tints={Color.white,new Color(.051f,1,0),new Color(0,.087f,1),new Color(.543f,0,1),new Color(1,.949f,0),new Color(1,.083f,0)};
    [InitializeOnLoadMethod] static void Init() { if(File.Exists(Flag)) EditorApplication.delayCall+=Requested; }
    static void Requested()
    {
        if(EditorApplication.isPlaying) return;
        File.Delete(Flag);
        try { Build(); File.WriteAllText(Path.Combine(Path.GetTempPath(),"loot-beams-build.txt"),"PASS: 6 rarity prefabs, generated white/red PNG import, shader, classification, sprite/material references and native render verified."); }
        catch(Exception e) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"loot-beams-build.txt"),"FAIL: "+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Effects/Build Loot Rarity Beams")]
    public static void Build()
    {
        foreach(var color in new[]{"White","Red"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Glow-"+color+"-01.png");
            if(importer==null) throw new Exception("Generated "+color+" image missing");
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType=SpriteMeshType.Tight; importer.SetTextureSettings(settings);
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.wrapMode=TextureWrapMode.Clamp;
            importer.maxTextureSize=1024; importer.SaveAndReimport();
            if(!importer.DoesSourceTextureHaveAlpha()) throw new Exception("Generated sprite has no alpha");
        }
        if(!AssetDatabase.IsValidFolder(Root+"/Resources")) AssetDatabase.CreateFolder(Root,"Resources");
        if(!AssetDatabase.IsValidFolder(Root+"/Resources/LootBeams")) AssetDatabase.CreateFolder(Root+"/Resources","LootBeams");
        var shader=Shader.Find("AllForOne/Effects/LootRarityBeam");
        if(shader==null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Loot beam shader failed");
        var material=Material(shader,"Beam",0); var groundMaterial=Material(shader,"GroundGlow",1);
        var scene=EditorSceneManager.NewPreviewScene(); var container=new GameObject("Loot beam preview"); SceneManager.MoveGameObjectToScene(container,scene);
        try {
            for(int rarity=0;rarity<6;rarity++) {
                var sprites=AssetDatabase.LoadAllAssetsAtPath(Root+"/Glow-"+Colors[rarity]+"-01.png").OfType<Sprite>().ToArray();
                if(sprites.Length==0) throw new Exception("Beam sprite missing: "+Colors[rarity]);
                if(rarity==0 || rarity==5) sprites[0]=CropGeneratedSprite(sprites[0],Root+"/Glow-"+Colors[rarity]+"-01_Cropped.asset");
                var root=new GameObject("LootBeam_"+rarity,typeof(LootRarityBeam),typeof(SortingGroup)); root.transform.SetParent(container.transform,false);
                var sorting=root.GetComponent<SortingGroup>(); sorting.sortAtRoot=true; sorting.sortingOrder=-4;
                var column=new GameObject("Column",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>(); column.transform.SetParent(root.transform,false);
                column.sprite=sprites[0]; column.sharedMaterial=material; column.sortingOrder=0;
                var bounds=column.sprite.bounds;
                Vector3 scale=new Vector3(.38f/bounds.size.x,3.5f/bounds.size.y,1);
                column.transform.localScale=scale;
                column.transform.localPosition=new Vector3(-bounds.center.x*scale.x,1.75f-bounds.center.y*scale.y,0);
                var glow=GameObject.CreatePrimitive(PrimitiveType.Quad); UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
                glow.name="GroundGlow"; glow.transform.SetParent(root.transform,false); glow.transform.localPosition=new Vector3(0,0,.02f); glow.transform.localScale=new Vector3(.95f,.28f,1);
                var renderer=glow.GetComponent<MeshRenderer>(); renderer.sharedMaterial=groundMaterial; renderer.sortingOrder=-1;
                renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
                var beam=root.GetComponent<LootRarityBeam>(); var fields=new SerializedObject(beam);
                fields.FindProperty("column").objectReferenceValue=column; fields.FindProperty("ground").objectReferenceValue=renderer;
                fields.FindProperty("tint").colorValue=Tints[rarity]; fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Resources/LootBeams/LootBeam_"+rarity+".prefab");
                root.transform.localPosition=new Vector3((rarity-2.5f)*1.5f,0,0); beam.SetPreviewTime(1);
                var label=new GameObject("Label",typeof(TextMeshPro)).GetComponent<TextMeshPro>(); label.transform.SetParent(root.transform,false);
                label.transform.localPosition=new Vector3(0,-.6f,0); label.rectTransform.sizeDelta=new Vector2(1.4f,.5f);
                label.fontSize=2; label.alignment=TextAlignmentOptions.Center; label.text=Names[rarity]; label.color=Tints[rarity];
            }
            ValidateClassification();
            Render(scene);
        }
        finally { UnityEngine.Object.DestroyImmediate(container); EditorSceneManager.ClosePreviewScene(scene); }
    }
    static Sprite CropGeneratedSprite(Sprite source,string path)
    {
        // Derive a sprite rect from alpha, preserving the original generated PNG.
        var texture=source.texture;
        var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        Texture2D readable=null;
        try {
            Graphics.Blit(texture,target); RenderTexture.active=target;
            readable=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
            readable.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0); readable.Apply();
            var pixels=readable.GetPixels32(); int loX=texture.width,loY=texture.height,hiX=0,hiY=0;
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++) {
                if(pixels[y*texture.width+x].a<16) continue;
                loX=Mathf.Min(loX,x); loY=Mathf.Min(loY,y); hiX=Mathf.Max(hiX,x); hiY=Mathf.Max(hiY,y);
            }
            var rect=new Rect(loX,loY,hiX-loX+1,hiY-loY+1);
            var generated=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            generated.name=source.name+"_Cropped";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(existing==null) {AssetDatabase.CreateAsset(generated,path);return generated;}
            EditorUtility.CopySerialized(generated,existing); EditorUtility.SetDirty(existing); AssetDatabase.SaveAssetIfDirty(existing);
            UnityEngine.Object.DestroyImmediate(generated); return existing;
        }
        finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target); if(readable!=null) UnityEngine.Object.DestroyImmediate(readable); }
    }
    static UnityEngine.Material Material(Shader shader,string name,float ground)
    {
        string path=Root+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if(material==null) {material=new UnityEngine.Material(shader); AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_Ground",ground); EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); return material;
    }
    static void ValidateClassification()
    {
        var item=new RObj{it=ItemType.item,dbObj=new Obj()};
        item.dbObj.pars["slot"]=-1; item.dbObj.pars["subtype"]=MainStates.subtypes["currency"];
        if(LootRarityBeam.IsEligible(item)) throw new Exception("Currency has beam");
        item.dbObj.pars["subtype"]=MainStates.subtypes["none"];
        if(LootRarityBeam.IsEligible(item)) throw new Exception("Resource has beam");
        foreach(var subtype in new[]{"weapon","body","potion","usable","adorn"}) {
            item.dbObj.pars["subtype"]=MainStates.subtypes[subtype];
            if(!LootRarityBeam.IsEligible(item)) throw new Exception("Non-resource excluded: "+subtype);
        }
    }
    static void Render(Scene scene)
    {
        var go=new GameObject("Loot beam preview camera",typeof(Camera)); SceneManager.MoveGameObjectToScene(go,scene);
        var camera=go.GetComponent<Camera>(); camera.scene=scene; camera.orthographic=true; camera.orthographicSize=2.7f;
        camera.transform.position=new Vector3(0,1.5f,-10); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.035f,.045f,.06f);
        var rt=new RenderTexture(1200,650,24); var previous=RenderTexture.active; Texture2D texture=null;
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            texture=new Texture2D(1200,650,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1200,650),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"loot-beams-preview.png"),texture.EncodeToPNG());
        } finally { camera.targetTexture=null; RenderTexture.active=previous; if(texture!=null) UnityEngine.Object.DestroyImmediate(texture);
            rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go); }
    }
}
#endif

