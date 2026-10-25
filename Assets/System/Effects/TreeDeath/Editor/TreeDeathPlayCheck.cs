#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
public static class TreeDeathPlayCheck
{
    const string Flag="Assets/System/Effects/TreeDeath/Editor/.check-tree-death";
    [InitializeOnLoadMethod] static void Init(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&File.Exists(Flag)){File.Delete(Flag);Run();}};}
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static async void Run()
    {
        var cleanup=new List<GameObject>();var effects=new List<TreeDeathEffect>();
        try{
            await Task.Delay(1500);
            Require(!TreeDeathEffect.IsTree(new RObj{dbID="slime"}),"Non-tree selected");
            var catalog=AssetDatabase.LoadAssetAtPath<PlagueTreeCatalog>("Assets/System/Effects/PlagueTrail/TreeCatalog.asset");
            var pair=catalog.replacements.First(p=>p.living.name=="Tree_Green_07");
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/System/Effects/PlagueTrail/WitheredTree.mat");
            var spriteMaterial=AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            for(int i=0;i<2;i++){
                var tree=new GameObject("Temporary death tree "+i,typeof(SpriteRenderer));cleanup.Add(tree);
                tree.transform.position=new Vector3(20000+i*20,20000,0);tree.transform.rotation=Quaternion.Euler(-30,0,0);tree.transform.localScale=new Vector3(1.1f,.9f,1);
                var source=tree.GetComponent<SpriteRenderer>();source.sprite=i==0?pair.living:pair.dead;source.sharedMaterial=i==0?spriteMaterial:material;source.flipX=true;
                if(i==1){
                    var living=pair.living.bounds;var dead=pair.dead.bounds;
                    var scale=new Vector2(living.size.x/dead.size.x,living.size.y/dead.size.y);
                    var offset=(Vector2)living.center-Vector2.Scale((Vector2)dead.center,scale);
                    var props=new MaterialPropertyBlock();props.SetVector("_PlagueTransform",new Vector4(scale.x,scale.y,offset.x,offset.y));source.SetPropertyBlock(props);
                }
                var attacker=new GameObject("Temporary attacker");cleanup.Add(attacker);attacker.transform.position=tree.transform.position+Vector3.left*2;
                var obj=new RObj{dbID="tree",RID="tree_death_check_"+i,main=tree,lastDmgFrom=new RObj{main=attacker}};obj.visuals["vis_main"]=tree;
                Require(TreeDeathEffect.TryPlay(obj),"Death effect failed to start");
                var effect=UnityEngine.Object.FindObjectsByType<TreeDeathEffect>(FindObjectsSortMode.None).First(e=>e.IsFalling&&!effects.Contains(e));effects.Add(effect);cleanup.Add(effect.gameObject);
                Require(!source.enabled,"Original tree still renders");
                Require(!TreeDeathEffect.TryPlay(obj),"Duplicate death effect");
                Require(effect.GetComponentsInChildren<SpriteRenderer>().Length==2,"Missing falling snapshot or stump");
                if(i==1){var clone=effect.FallingPivot.GetComponentInChildren<SpriteRenderer>();var props=new MaterialPropertyBlock();clone.GetPropertyBlock(props);Require(props.GetVector("_PlagueTransform").x>1,"Withered shader deformation not copied");}
                UnityEngine.Object.Destroy(tree);
            }
            await Task.Delay(500);
            foreach(var effect in effects){
                Require(effect!=null&&effect.IsFalling,"Fall vanished with owner");
                float angle=Quaternion.Angle(Quaternion.identity,effect.FallingPivot.rotation);Require(angle>5&&angle<88,"Tree did not lean progressively");
                var renderer=effect.FallingPivot.GetComponentInChildren<SpriteRenderer>();Require(renderer.color.a>0&&renderer.color.a<1,"Tree did not fade progressively");
                var stump=effect.Stump;var bounds=stump.sprite.bounds;
                Vector3 foot=stump.transform.TransformPoint(new Vector3(bounds.center.x,bounds.min.y,0));Require(Vector3.Distance(foot,effect.transform.position)<.005f,"Stump moved from ground pivot");
            }
            await Task.Delay(1000);
            foreach(var effect in effects){
                Require(effect!=null&&!effect.IsFalling&&!effect.enabled,"Stump effect still updating or destroyed");
                Require(effect.FallingPivot==null&&effect.GetComponentsInChildren<SpriteRenderer>().Length==1,"Falling visuals not removed");
                Require(effect.Stump.color.a>.999f,"Stump is not fully visible");
                Require(effect.GetComponentsInChildren<Collider>().Length==0&&effect.GetComponentsInChildren<Collider2D>().Length==0,"Stump has collision");
                Require(effect.GetComponentsInChildren<ObjHolder>().Length==0,"Stump is a targetable unit");
            }
            File.WriteAllText(Path.Combine(Path.GetTempPath(),"tree-death-check.txt"),"PASS: live and withered tree snapshots at tilted camera orientation; progressive fall and fade; original owner destruction does not interrupt; one effect per tree; stump stays grounded and opaque; falling visuals removed; stump update disabled, no colliders/ObjHolder; non-tree excluded.");
        }catch(Exception e){File.WriteAllText(Path.Combine(Path.GetTempPath(),"tree-death-check.txt"),"FAIL: "+e);Debug.LogException(e);}
        finally{foreach(var go in cleanup)if(go!=null)UnityEngine.Object.Destroy(go);}
    }
}
#endif
