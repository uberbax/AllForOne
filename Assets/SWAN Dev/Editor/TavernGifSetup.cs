#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TavernGifSetup
{
    private const string Work = @"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\tavern-gif";
    [InitializeOnLoadMethod] private static void Init()
    {
        if(File.Exists(Work+"/inspect.request"))EditorApplication.delayCall+=Inspect;
        if(File.Exists(Work+"/build.request"))EditorApplication.delayCall+=Build;
    }
    private static RectTransform Tavern()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/Game_Dark1.unity")throw new Exception("Open Game_Dark1 in Edit Mode");
        return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RectTransform>(true))
            .Single(t=>string.Equals(t.name,"Tavern",StringComparison.OrdinalIgnoreCase)&&t.parent!=null&&string.Equals(t.parent.name,"MANAGEMENT",StringComparison.OrdinalIgnoreCase));
    }
    [MenuItem("Tools/UI/Tavern GIF/Inspect")]
    public static void Inspect()
    {
        try
        {
            var tavern=Tavern();Canvas.ForceUpdateCanvases();
            var images=tavern.GetComponentsInChildren<Image>(true).Select(i=>new{path=AnimationUtility.CalculateTransformPath(i.transform,tavern),asset=AssetDatabase.GetAssetPath(i.sprite),rect=i.rectTransform.rect.ToString(),position=i.rectTransform.anchoredPosition.ToString()}).ToArray();
            File.WriteAllText(Work+"/inspect.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rect=tavern.rect.ToString(),images},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/inspect.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/UI/Tavern GIF/Install")]
    public static void Build()
    {
        try
        {
            var tavern=Tavern();
            var oldKnight=tavern.Find("LeftPart/frame/ava") as RectTransform;
            if(oldKnight==null)throw new Exception("Original Tavern knight not found");
            var source=DecodeAndCreatePoster();
            var go=oldKnight.parent.Find("RockingKnightGif")?.gameObject;
            if(go==null){go=new GameObject("RockingKnightGif",typeof(RectTransform),typeof(RawImage));Undo.RegisterCreatedObjectUndo(go,"Add Tavern GIF");go.transform.SetParent(oldKnight.parent,false);}
            Undo.RecordObject(go.transform,"Position Tavern GIF");
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=Vector2.one*oldKnight.rect.height;rect.localScale=oldKnight.localScale;rect.localRotation=oldKnight.localRotation;rect.position=oldKnight.TransformPoint(oldKnight.rect.center);
            rect.SetSiblingIndex(oldKnight.GetSiblingIndex()+1);
            Undo.RecordObject(oldKnight.gameObject,"Disable static Tavern knight");oldKnight.gameObject.SetActive(false);
            var image=go.GetComponent<RawImage>();image.texture=source;image.raycastTarget=false;
            var player=go.GetComponent<ProGifPlayerRawImage>();if(player==null)player=Undo.AddComponent<ProGifPlayerRawImage>(go);
            player.destinationRawImage=image;player.loadPath="";player.optimizeMemoryUsage=true;
            var handler=go.GetComponent<ProGifPlayerHandler>();if(handler==null)handler=Undo.AddComponent<ProGifPlayerHandler>(go);
            handler.m_PlayerComponent=player;handler.m_LocalDirectory=FilePathName.AppPath.StreamingAssetsPath;
            handler.m_FolderName="Gifs";handler.m_FileName="rocking_knight.gif";handler.m_AutoPlayOnEnable=true;handler.m_SetSizeOnLoaded=false;handler.m_PlayWebGif=false;
            EditorUtility.SetDirty(player);EditorUtility.SetDirty(handler);EditorUtility.SetDirty(image);
            if(!EditorSceneManager.SaveScene(tavern.gameObject.scene))throw new Exception("Scene save failed");
            if(oldKnight.gameObject.activeSelf||handler.m_PlayerComponent!=player||image.raycastTarget)throw new Exception("GIF setup validation failed");
            File.WriteAllText(Work+"/result.txt","PASS: Swan GIF player installed at "+AnimationUtility.CalculateTransformPath(go.transform,null)+"; old static knight disabled; autoplay, square aspect and StreamingAssets path configured. Scene saved.");
            File.Delete(Work+"/build.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
    }
    private static Texture2D DecodeAndCreatePoster()
    {
        List<GifTexture> frames=null;int loop=-1,width=0,height=0;
        var decoder=new ProGifDecoder(File.ReadAllBytes("Assets/StreamingAssets/Gifs/rocking_knight.gif"),(f,l,w,h)=>{frames=f;loop=l;width=w;height=h;});
        decoder.SetOptimizeMemoryUsgae(true);
        var stack=new Stack<IEnumerator>();stack.Push(decoder.GetTextureListCoroutine());
        while(stack.Count>0){var current=stack.Peek();if(!current.MoveNext())stack.Pop();else if(current.Current is IEnumerator child)stack.Push(child);}
        if(frames==null||frames.Count!=48||width!=760||height!=760||loop!=0)throw new Exception("Unexpected Swan GIF decode result");
        try
        {
            int transparent=frames[0].m_Colors.Count(c=>c.a==0);
            bool animated=!frames[0].m_Colors.SequenceEqual(frames[frames.Count/2].m_Colors);
            if(!animated||transparent==0)throw new Exception("GIF animation/transparency check failed");
            const string folder="Assets/SWAN Dev/TavernGif";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/SWAN Dev","TavernGif");
            const string path=folder+"/rocking_knight_preview.png";
            File.WriteAllBytes(path,frames[0].GetTexture2D().EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
            File.WriteAllText(Work+"/decode.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{frames=frames.Count,width,height,loop,transparentPixels=transparent,animated,delays=frames.Select(f=>f.m_delaySec).ToArray()},Newtonsoft.Json.Formatting.Indented));
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        finally
        {
            foreach(var frame in frames){frame.m_Colors=null;if(frame.m_texture2d!=null)UnityEngine.Object.DestroyImmediate(frame.m_texture2d);}
        }
    }
}
#endif
