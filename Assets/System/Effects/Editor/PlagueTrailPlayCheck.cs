#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
public static class PlagueTrailPlayCheck
{
    const string Flag="Assets/System/Effects/Editor/.plague-play-check";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.playModeStateChanged += state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && File.Exists(Flag)) { File.Delete(Flag); Run(); }
        };
    }
    static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    static async void Run()
    {
        PlagueTrail trail=null; GameObject proxy=null; FieldInfo targetField=null;
        try {
            await Task.Delay(2000);
            trail=UnityEngine.Object.FindFirstObjectByType<PlagueTrail>();
            Require(trail!=null,"Scene trail missing");
            var player=MainStates.instance.mainPlayer.main.transform;
            var tracked=typeof(PlagueTrail).GetField("trackedTarget",BindingFlags.Instance|BindingFlags.NonPublic);
            Require((Transform)tracked.GetValue(trail)==player,"mainPlayer not tracked");
            targetField=typeof(PlagueTrail).GetField("target",BindingFlags.Instance|BindingFlags.NonPublic);
            proxy=new GameObject("Temporary plague movement probe"); proxy.transform.position=player.position;
            targetField.SetValue(trail,proxy.transform); trail.ClearTrail();
            await Task.Delay(150);
            Require(trail.PatchCount==0,"Idle emission");
            Vector3 start=player.position;
            for(int i=1;i<=45;i++) {
                if(trail==null || !EditorApplication.isPlaying) return;
                float x=(i/45f)*7-3.5f;
                proxy.transform.position=start+new Vector3(x,Mathf.Sin(i*.12f)*.8f-1,0);
                await Task.Delay(35);
            }
            Require(trail.PatchCount>15,"LateUpdate did not emit along movement");
            int count=trail.PatchCount;
            await Task.Delay(500);
            Require(trail.PatchCount==count,"Idle keeps emitting");
            proxy.transform.position+=Vector3.right*50;
            await Task.Delay(100);
            Require(trail.PatchCount==count,"Teleport created bridge");
            int pixels=Capture(trail);
            Require(pixels>100,"Trail not visible over scene ground");
            File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-play-check.txt"),"PASS: real mainPlayer tracked; LateUpdate movement generated "+count+" patches; idle and teleport emitted none; scene render affected "+pixels+" pixels.");
        } catch(Exception e) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-play-check.txt"),"FAIL: "+e); Debug.LogException(e); }
        finally {
            if(trail!=null) { if(targetField!=null) targetField.SetValue(trail,null); trail.ClearTrail(); }
            if(proxy!=null) UnityEngine.Object.DestroyImmediate(proxy);
        }
    }
    static int Capture(PlagueTrail trail)
    {
        var camera=Camera.main;
        Require(camera!=null,"Main Camera missing");
        var renderer=trail.GetComponentInChildren<MeshRenderer>();
        var target=new RenderTexture(1000,600,24);
        var previous=RenderTexture.active; var previousTarget=camera.targetTexture;
        Texture2D before=null,after=null;
        try {
            camera.targetTexture=target;
            before=new Texture2D(1000,600,TextureFormat.RGB24,false);
            after=new Texture2D(1000,600,TextureFormat.RGB24,false);
            renderer.enabled=false; camera.Render(); RenderTexture.active=target;
            before.ReadPixels(new Rect(0,0,1000,600),0,0); before.Apply();
            renderer.enabled=true; camera.Render(); RenderTexture.active=target;
            after.ReadPixels(new Rect(0,0,1000,600),0,0); after.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"plague-scene-preview.png"),after.EncodeToPNG());
            var a=before.GetPixels32(); var b=after.GetPixels32(); int changed=0;
            for(int i=0;i<a.Length;i++) if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>15) changed++;
            return changed;
        } finally {
            renderer.enabled=true; camera.targetTexture=previousTarget; RenderTexture.active=previous;
            if(before!=null) UnityEngine.Object.DestroyImmediate(before);
            if(after!=null) UnityEngine.Object.DestroyImmediate(after);
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
#endif
