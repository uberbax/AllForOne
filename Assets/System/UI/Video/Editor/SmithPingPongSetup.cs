#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public static class SmithPingPongSetup
{
    private const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\tavern-video";
    private static void Init()
    {if(File.Exists(Work+"/smith.request"))EditorApplication.delayCall+=Apply;}
    [MenuItem("Tools/UI/Forge Video/Assign Ping Pong")]
    public static void Apply()
    {
        try
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/Game_Dark1.unity")throw new Exception("Open Game_Dark1 in Edit Mode");
            var source=AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/System/Art/Videos/smith.mp4");
            var clip=AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/System/Art/Videos/smith_pingpong.mp4");
            if(source==null||clip==null||clip.frameCount!=source.frameCount*2-2||clip.width!=source.width||clip.height!=source.height)throw new Exception("Unexpected smith ping-pong clip metadata");
            var controller=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UiLoopVideo>(true)).Single(c=>
                c.name.StartsWith("smithVideo",StringComparison.OrdinalIgnoreCase)&&
                c.GetComponentsInParent<Transform>(true).Any(t=>string.Equals(t.name,"FORGE",StringComparison.OrdinalIgnoreCase)));
            var player=controller.GetComponent<VideoPlayer>();
            if(player==null)throw new Exception("SmithVideo has no VideoPlayer");
            Undo.RecordObjects(new UnityEngine.Object[]{controller,player},"Assign smith ping-pong video");
            var data=new SerializedObject(controller);data.FindProperty("clip").objectReferenceValue=clip;data.ApplyModifiedProperties();
            player.clip=clip;player.isLooping=true;EditorUtility.SetDirty(player);
            if(new SerializedObject(controller).FindProperty("clip").objectReferenceValue!=clip||player.clip!=clip)throw new Exception("Smith video binding failed");
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
            File.WriteAllText(Work+"/smith-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",path=AnimationUtility.CalculateTransformPath(controller.transform,null),video=AssetDatabase.GetAssetPath(clip),sourceFrames=source.frameCount,pingPongFrames=clip.frameCount,fps=clip.frameRate,duration=clip.length,loop=player.isLooping},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/smith.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/smith-error.txt",e.ToString());Debug.LogException(e);}
    }
}
#endif
