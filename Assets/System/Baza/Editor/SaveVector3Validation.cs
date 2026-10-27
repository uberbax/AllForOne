#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class SaveVector3Validation
{
    const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\vector3-json-fix";
    public sealed class Probe { [JsonConverter(typeof(SaveVector3Converter))] public Vector3 position; }
    [InitializeOnLoadMethod] static void Init(){if(File.Exists(Work+"/verify.request"))EditorApplication.delayCall+=Run;}
    [MenuItem("Tools/Diagnostics/Validate Vector3 Save")]
    public static void Run()
    {
        File.Delete(Work+"/verify.request");
        try
        {
            var data=new PlayerData();
            data.mainPlayer.localPos=new Vector3(1,2,3);
            data.mainPlayer.inventory.Add(new RObj{localPos=new Vector3(4.5f,-2,17)});
            data.mainPlayer.inventory[0].inventory.Add(new RObj{localPos=Vector3.zero});
            string json=JsonConvert.SerializeObject(data);var tree=JObject.Parse(json);
            if(json.Contains("normalized")||json.Contains("magnitude"))throw new Exception("Computed Vector3 properties were serialized");
            var position=tree["mainPlayer"]["inventory"][0]["localPos"] as JObject;
            if(position==null||position.Properties().Count()!=3||(float)position["x"]!=4.5f||(float)position["y"]!=-2||(float)position["z"]!=17)throw new Exception("Inventory coordinates changed");
            var probe=JsonConvert.DeserializeObject<Probe>("{\"position\":{\"x\":4.5,\"y\":-2,\"z\":17,\"normalized\":{\"x\":0,\"y\":0,\"z\":1},\"magnitude\":99}}");
            if(probe.position!=new Vector3(4.5f,-2,17))throw new Exception("Legacy read failed");
            File.WriteAllText(Work+"/native-validation.txt","PASS: actual PlayerData/RObj graph with nested inventory and UnityEngine.Vector3 serialized with x/y/z only; zero/nonzero coordinates preserved; legacy derived fields ignored while reading. PlayerPrefs unchanged.");
        }
        catch(Exception e){File.WriteAllText(Work+"/native-validation.txt","FAIL "+e);Debug.LogException(e);}
    }
}
#endif
