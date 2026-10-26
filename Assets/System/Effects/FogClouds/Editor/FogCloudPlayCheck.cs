#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
public static class FogCloudPlayCheck
{
    const string Flag="Assets/System/Effects/FogClouds/Editor/.check-area-fog";
    [InitializeOnLoadMethod] static void Init(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&File.Exists(Flag)){File.Delete(Flag);Run();}};}
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static async void Run()
    {
        GameObject clone=null;
        try{
            await Task.Delay(1500);
            var original=UnityEngine.Object.FindObjectsByType<FogActivator>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Single(f=>f.transform.parent!=null&&f.transform.parent.name=="FogAreaActivator1");
            clone=UnityEngine.Object.Instantiate(original.gameObject);clone.name="Temporary fog dispersal check";clone.transform.position+=Vector3.right*20000;
            var fog=clone.GetComponent<FogActivator>();
            var renderer=fog.cloudRoot.Find("AnimatedClouds").GetComponent<MeshRenderer>();
            var material=renderer.sharedMaterial;int children=fog.cloudRoot.childCount;
            fog.ResetFog();Require(renderer.enabled&&!fog.IsCleared&&!fog.IsDispersing&&fog.ClearProgress==0,"Reset initial state");
            Require(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount>16,"Cloud mesh empty");
            fog.Activate();Require(fog.IsDispersing,"Activate did not start");
            await Task.Delay(650);
            Require(fog.ClearProgress>0&&fog.ClearProgress<1&&renderer.enabled,"No progressive dispersal");
            float before=fog.ClearProgress;fog.Activate();Require(fog.ClearProgress==before,"Repeated Activate restarted effect");
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            Require(Mathf.Abs(block.GetFloat("_ClearProgress")-fog.ClearProgress)<.001f,"Shader progress not wired");
            var origin=block.GetVector("_RevealOrigin");Require(origin.x>=0&&origin.x<=1&&origin.y>=0&&origin.y<=1,"Reveal origin outside area");
            await Task.Delay(2200);
            Require(fog.IsCleared&&!fog.IsDispersing&&!renderer.enabled,"Completed clouds still render");
            Require(fog.cloudRoot.gameObject.activeSelf&&fog.cloudRoot.childCount==children,"Unrelated hierarchy modified");
            fog.ResetFog();Require(renderer.enabled&&fog.ClearProgress==0,"Reset after completion");
            fog.Activate();await Task.Delay(100);fog.ResetFog();await Task.Delay(250);
            Require(!fog.IsDispersing&&fog.ClearProgress==0&&renderer.enabled,"Reset failed to cancel active effect");
            Require(renderer.sharedMaterial==material,"Per-instance material allocated");
            File.WriteAllText(Path.Combine(Path.GetTempPath(),"area-fog-check.txt"),"PASS: populated rootCloud; progressive Activate; bounded reveal origin; repeat call idempotent; shader property updates; full renderer disable at end; ResetFog reopens and cancels; other children preserved; shared material retained.");
        }catch(Exception e){File.WriteAllText(Path.Combine(Path.GetTempPath(),"area-fog-check.txt"),"FAIL: "+e);Debug.LogException(e);}
        finally{if(clone!=null)UnityEngine.Object.Destroy(clone);}
    }
}
#endif
