#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class TavernVideoSetup
{
    private const string Root="Assets/System/UI/Video";
    private const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\tavern-video";
    private static Scene previewScene;
    private static VideoPlayer preview;
    private static RenderTexture target;
    private static double started;
    private static VideoClip clip;
    private static bool assassin;
    private static string RequestPath=>Work+(assassin?"/assassin.request":"/build.request");
    [InitializeOnLoadMethod] private static void Init()
    {
        if(File.Exists(Work+"/build.request"))EditorApplication.delayCall+=Build;
        if(File.Exists(Work+"/assassin.request"))EditorApplication.delayCall+=BuildAssassin;
    }
    private static RectTransform Tavern()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/Game_Dark1.unity")throw new Exception("Open Game_Dark1 in Edit Mode");
        return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RectTransform>(true))
            .Single(t=>string.Equals(t.name,"TAVERN",StringComparison.OrdinalIgnoreCase)&&t.parent!=null&&t.parent.name=="MANAGEMENT");
    }
    [MenuItem("Tools/UI/Tavern Video/Install Barmen")]
    public static void Build(){StartBuild(false);}
    [MenuItem("Tools/UI/Tavern Video/Install Assassin")]
    public static void BuildAssassin(){StartBuild(true);}
    private static void StartBuild(bool isAssassin)
    {
        if(preview!=null)return;
        assassin=isAssassin;
        try
        {
            Tavern();
            clip=AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/System/Art/Videos/"+(assassin?"assasin.mp4":"barmen.mp4"));
            if(clip==null||clip.width==0||clip.height==0)throw new Exception("Tavern video is not imported as a VideoClip");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/UIGreenKey.shader");
            if(shader==null||!shader.isSupported||ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"))throw new Exception("Green-key shader is unsupported or has compiler errors");
            previewScene=EditorSceneManager.NewPreviewScene();
            var go=new GameObject("Tavern video decode preview",typeof(VideoPlayer));SceneManager.MoveGameObjectToScene(go,previewScene);
            preview=go.GetComponent<VideoPlayer>();preview.playOnAwake=false;preview.clip=clip;preview.audioOutputMode=VideoAudioOutputMode.None;
            preview.renderMode=VideoRenderMode.RenderTexture;
            target=new RenderTexture((int)clip.width,(int)clip.height,0,RenderTextureFormat.ARGB32);target.Create();preview.targetTexture=target;
            preview.sendFrameReadyEvents=true;preview.prepareCompleted+=Prepared;preview.frameReady+=FrameReady;preview.errorReceived+=Failed;
            started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;preview.Prepare();
        }
        catch(Exception e){Fail(e);}
    }
    private static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if(EditorApplication.timeSinceStartup-started>40)Fail(new Exception("Video preview decode timed out"));
    }
    private static void Prepared(VideoPlayer player){player.Play();}
    private static void Failed(VideoPlayer player,string error){Fail(new Exception(error));}
    private static void Set(UnityEngine.Object targetObject,string field,UnityEngine.Object value)
    {var so=new SerializedObject(targetObject);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static Texture2D Read(RenderTexture source)
    {
        var old=RenderTexture.active;
        try{RenderTexture.active=source;var texture=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0);texture.Apply();return texture;}
        finally{RenderTexture.active=old;}
    }
    private static void FrameReady(VideoPlayer player,long frame)
    {
        if(frame<Math.Min(5L,(long)clip.frameCount-1))return;
        player.Pause();player.frameReady-=FrameReady;
        Texture2D poster=null;
        try
        {
            var tavern=Tavern();
            var oldGirl=tavern.Find(assassin?"LeftPart/frame/herotable/hero/ava (1)":"LeftPart/frame/back/avamask/girl") as RectTransform;
            if(oldGirl==null)throw new Exception("Original Tavern character not found");
            poster=Read(target);
            File.WriteAllBytes(Work+(assassin?"/assassin-frame.png":"/video-frame.png"),poster.EncodeToPNG());
            if(!AssetDatabase.IsValidFolder(Root+"/Art"))AssetDatabase.CreateFolder(Root,"Art");
            string posterPath=Root+"/Art/"+(assassin?"AssassinPoster.png":"BarmenPoster.png");
            File.WriteAllBytes(posterPath,poster.EncodeToPNG());AssetDatabase.ImportAsset(posterPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(posterPath);importer.mipmapEnabled=false;importer.SaveAndReimport();
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/UIGreenKey.shader");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/BarmenGreenKey.mat");
            if(material==null){material=new Material(shader);material.SetFloat("_GreenSimilarity",.25f);material.SetFloat("_GreenSoftness",.15f);material.SetFloat("_Despill",.9f);AssetDatabase.CreateAsset(material,Root+"/BarmenGreenKey.mat");}
            material.shader=shader;EditorUtility.SetDirty(material);
            ValidateKey(material);
            ExportKeyedFrame(material,poster);
            string videoName=assassin?"AssassinVideo":"BarmenVideo";
            var go=oldGirl.parent.Find(videoName)?.gameObject;
            if(go==null)
            {
                go=new GameObject(videoName,typeof(RectTransform),typeof(RawImage),typeof(VideoPlayer));Undo.RegisterCreatedObjectUndo(go,"Add Tavern video");go.transform.SetParent(oldGirl.parent,false);go.layer=oldGirl.gameObject.layer;
                var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(oldGirl.rect.height*(float)clip.width/clip.height,oldGirl.rect.height);
                rect.localRotation=oldGirl.localRotation;rect.localScale=oldGirl.localScale;rect.position=oldGirl.TransformPoint(oldGirl.rect.center);rect.SetSiblingIndex(oldGirl.GetSiblingIndex()+1);
            }
            var image=go.GetComponent<RawImage>();image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(posterPath);image.material=material;image.raycastTarget=false;
            var video=go.GetComponent<VideoPlayer>();video.clip=clip;video.playOnAwake=false;video.isLooping=true;video.audioOutputMode=VideoAudioOutputMode.None;
            video.renderMode=VideoRenderMode.RenderTexture;video.targetTexture=null;video.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
            var controller=go.GetComponent<UiLoopVideo>();if(controller==null)controller=Undo.AddComponent<UiLoopVideo>(go);
            Set(controller,"clip",clip);Set(controller,"poster",image.texture);
            Undo.RecordObject(oldGirl.gameObject,"Disable original Tavern character");oldGirl.gameObject.SetActive(false);
            EditorUtility.SetDirty(image);EditorUtility.SetDirty(video);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(tavern.gameObject.scene))throw new Exception("Scene save failed");
            File.WriteAllText(Work+(assassin?"/assassin-result.json":"/result.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",path=AnimationUtility.CalculateTransformPath(go.transform,null),oldCharacterDisabled=!oldGirl.gameObject.activeSelf,width=clip.width,height=clip.height,fps=clip.frameRate,duration=clip.length,decodedFrame=frame,shaderSupported=shader.isSupported,greenKeyGpuCheck=true,blackPreserved=true,loop=video.isLooping},Newtonsoft.Json.Formatting.Indented));
            File.Delete(RequestPath);
        }
        catch(Exception e){Fail(e);}
        finally{if(poster!=null)UnityEngine.Object.DestroyImmediate(poster);Cleanup();}
    }
    private static void ValidateKey(Material material)
    {
        var source=new Texture2D(3,1,TextureFormat.RGBA32,false);source.filterMode=FilterMode.Point;source.SetPixels(new[]{Color.green,Color.red,Color.black});source.Apply();
        var result=new RenderTexture(3,1,0,RenderTextureFormat.ARGB32);result.Create();var old=RenderTexture.active;Texture2D read=null;
        try
        {
            RenderTexture.active=result;GL.Clear(true,true,Color.clear);Graphics.Blit(source,result,material);read=Read(result);
            if(read.GetPixel(0,0).a>.01f||read.GetPixel(1,0).a<.99f||read.GetPixel(2,0).a<.99f)throw new Exception("GPU green-key alpha validation failed");
        }
        finally{RenderTexture.active=old;result.Release();UnityEngine.Object.DestroyImmediate(result);UnityEngine.Object.DestroyImmediate(source);if(read!=null)UnityEngine.Object.DestroyImmediate(read);}
    }
    private static void ExportKeyedFrame(Material material,Texture2D source)
    {
        var result=new RenderTexture(source.width,source.height,0,RenderTextureFormat.ARGB32);result.Create();var old=RenderTexture.active;Texture2D read=null;
        try{RenderTexture.active=result;GL.Clear(true,true,Color.clear);Graphics.Blit(source,result,material);read=Read(result);File.WriteAllBytes(Work+(assassin?"/assassin-key-frame.png":"/green-key-frame.png"),read.EncodeToPNG());}
        finally{RenderTexture.active=old;result.Release();UnityEngine.Object.DestroyImmediate(result);if(read!=null)UnityEngine.Object.DestroyImmediate(read);}
    }
    private static void Fail(Exception error){File.WriteAllText(Work+(assassin?"/assassin-error.txt":"/error.txt"),error.ToString());Debug.LogException(error);Cleanup();}
    private static void Cleanup()
    {
        EditorApplication.update-=Tick;
        if(preview!=null){preview.prepareCompleted-=Prepared;preview.frameReady-=FrameReady;preview.errorReceived-=Failed;preview.Stop();preview.targetTexture=null;}
        if(previewScene.IsValid())EditorSceneManager.ClosePreviewScene(previewScene);preview=null;previewScene=default;
        if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);target=null;}
    }
}
#endif
