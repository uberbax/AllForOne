#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
public static class LootBeamPlayCheck
{
    const string Flag="Assets/System/Art/Beams/Editor/.loot-beam-check";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && File.Exists(Flag)) { File.Delete(Flag); Run(); }
        };
    }
    static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    static async void Run()
    {
        var parents=new List<GameObject>();
        try {
            await Task.Delay(1500);
            var items=DatabaseAll.instance.items;
            var resources=new List<string>();
            foreach(var id in new[]{"gold","res1","res2","exp"}) {
                if(!items.TryGetValue(id,out var definition)) continue;
                var resource=new RObj{it=ItemType.item,dbObj=definition,dbID=id};
                Require(!LootRarityBeam.IsEligible(resource),"Resource classified as non-resource: "+id+" slot="+resource.GetPar("slot")+" subtype="+resource.GetPar("subtype"));
                resources.Add(id);
            }
            var gear=items.Values.First(o=>o.pars.TryGetValue("slot",out var slot) && slot>=0);
            var beams=new List<LootRarityBeam>(); var objects=new List<RObj>();
            for(int rarity=0;rarity<6;rarity++) {
                var parent=new GameObject("Temporary loot beam "+rarity); parents.Add(parent);
                parent.transform.position=new Vector3(20000+rarity*2,20000,0); parent.transform.localScale=new Vector3(.2f,.3f,.5f);
                var obj=new RObj{it=ItemType.item,dbObj=gear,dbID=gear.ID,RID="beam_check_"+rarity,main=parent};
                obj.upgradePars["rarity"]=rarity; objects.Add(obj);
                var beam=LootRarityBeam.Attach(obj);
                Require(beam!=null && beam.name.StartsWith("LootBeam_"+rarity),"Wrong rarity prefab: "+rarity);
                Require(LootRarityBeam.Attach(obj)==beam && parent.GetComponentsInChildren<LootRarityBeam>().Length==1,"Duplicate beam");
                Require(Vector3.Distance(beam.transform.lossyScale,Vector3.one)<.001f,"Beam inherits loot scale");
                var group=beam.GetComponent<SortingGroup>(); Require(group.sortAtRoot && group.sortingOrder==-4,"Sorting below ground");
                beams.Add(beam);
            }
            await Task.Delay(350);
            for(int rarity=0;rarity<6;rarity++) {
                var props=new MaterialPropertyBlock(); beams[rarity].GetComponentInChildren<SpriteRenderer>().GetPropertyBlock(props);
                Require(props.GetFloat("_Opacity")>.4f,"Beam fade in");
                Color color=props.GetColor("_BeamTint"); Color expected=ResourceHolder.instance.rareColors[rarity];
                Require(Vector4.Distance(color,expected)<.001f,"Rarity theme color mismatch");
            }
            LootRarityBeam.Remove(objects[0]); Require(!beams[0].gameObject.activeSelf,"Pickup did not hide beam");
            await Task.Delay(60); Require(beams[0]==null,"Pickup beam not destroyed");
            UnityEngine.Object.Destroy(parents[1]);
            await Task.Delay(60); Require(beams[1]==null,"Beam survives parent loot destruction");
            File.WriteAllText(Path.Combine(Path.GetTempPath(),"loot-beams-check.txt"),"PASS: real config resources excluded ["+string.Join(",",resources)+"]; all 6 rarity Resources prefabs and theme colors correct; no duplicates; scale independent; root sorting; fade in; pickup cleanup and parent cleanup.");
        } catch(Exception e) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"loot-beams-check.txt"),"FAIL: "+e); Debug.LogException(e); }
        finally { foreach(var parent in parents) if(parent!=null) UnityEngine.Object.Destroy(parent); }
    }
}
#endif
