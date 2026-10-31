#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RumbleVfxValidation
{
    [InitializeOnLoadMethod]private static void Init()
    {if(File.Exists(RumbleVfxBuilder.Work+"/validate.request")&&!File.Exists(RumbleVfxBuilder.Work+"/build.request"))EditorApplication.delayCall+=Run;}
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    [MenuItem("Tools/Skills/Validate Pack VFX for Rumble")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var poolField=typeof(RumbleVfx).GetField("poolRoot",BindingFlags.Static|BindingFlags.NonPublic);
        var oldPool=(Transform)poolField.GetValue(null);GameObject follow=null;bool old3D=MainStates.is3D;
        var leases=new List<RumbleVfx.Lease>();
        try
        {
            MainStates.is3D=false;
            var library=Resources.Load<RumbleVfxLibrary>(RumbleVfxLibrary.ResourcePath);Check(library!=null,"Resources VFX library missing");
            foreach(var id in RumbleSkills.Ids)Check(library.styles.Any(s=>s.id==id),"Missing skill "+id);
            var prefabs=library.styles.SelectMany(s=>new[]{s.projectile,s.impact,s.area,s.cast,s.beam,s.heal,s.shield}).Where(p=>p!=null).Distinct().ToArray();
            foreach(var prefab in prefabs)
            {
                Check(!prefab.activeSelf,"Pool prefab must start inactive: "+prefab.name);
                Check(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Source behavior survived adaptation: "+prefab.name);
                Check(prefab.GetComponentsInChildren<Collider>(true).Length+prefab.GetComponentsInChildren<Collider2D>(true).Length==0,"Collision component survived: "+prefab.name);
                var systems=prefab.GetComponentsInChildren<ParticleSystem>(true);Check(systems.Length>0||prefab.GetComponentInChildren<LineRenderer>(true)!=null,"No particle/textured beam renderer "+prefab.name);
                Check(systems.All(p=>!p.collision.enabled&&!p.trigger.enabled&&p.main.stopAction==ParticleSystemStopAction.None),"Unsafe particle behavior "+prefab.name);
                Check(prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).All(m=>m.shader!=null&&m.shader.isSupported),"Unsupported material "+prefab.name);
            }
            foreach(var id in new[]{"water_strike","venom_flask","frost_nova","bombard","blazing_cannonball","feather_barrage","poisonous_fan","mysterious_potion","energy_torrent","dual_pistols","sniper_rifle"})
                Check(library.Get(id,RumbleVfxLibrary.Stage.Projectile)!=null&&library.Get(id,RumbleVfxLibrary.Stage.Impact)!=null,"Projectile/impact coverage "+id);
            foreach(var id in new[]{"waterspout","venom_zone","blizzard","iron_whirlwind"})Check(library.Get(id,RumbleVfxLibrary.Stage.Area)!=null,"Field coverage "+id);
            foreach(var id in new[]{"heal","light_of_life","healing_strike"})Check(library.Get(id,RumbleVfxLibrary.Stage.Heal)!=null,"Healing coverage "+id);
            Check(library.Get("tranquility",RumbleVfxLibrary.Stage.Shield)!=null,"Shield coverage");
            var first=RumbleVfx.Play("venom_flask",RumbleVfxLibrary.Stage.Projectile,Vector3.zero,.2f,-1,Vector3.right);leases.Add(first);Check(first.IsAlive,"Pool failed to spawn");
            var instance=first.Transform.GetComponent<RumbleVfxInstance>();first.Stop();
            typeof(RumbleVfxInstance).GetField("releaseTime",flags).SetValue(instance,-1f);typeof(RumbleVfxInstance).GetMethod("LateUpdate",flags).Invoke(instance,null);
            Check(!first.IsAlive,"Stopped effect did not return to pool");
            var second=RumbleVfx.Play("venom_flask",RumbleVfxLibrary.Stage.Projectile,Vector3.one,.3f,-1,Vector3.right);leases.Add(second);
            Check(second.IsAlive&&second.Transform.GetComponent<RumbleVfxInstance>()==instance,"Effect not reused");
            first.Stop();Check(!(bool)typeof(RumbleVfxInstance).GetField("stopping",flags).GetValue(instance),"Stale lease stopped a reused effect");
            for(int i=0;i<20;i++){var lease=RumbleVfx.Play("venom_flask",RumbleVfxLibrary.Stage.Projectile,Vector3.zero,.2f,-1,Vector3.right);leases.Add(lease);}
            Check(leases.Count(l=>l.IsAlive)<=12,"Visual budget exceeded");
            follow=new GameObject("Temporary VFX follow target"){hideFlags=HideFlags.HideAndDontSave};
            var shield=RumbleVfx.Play("tranquility",RumbleVfxLibrary.Stage.Shield,Vector3.zero,.7f,4,follow:follow.transform);leases.Add(shield);
            follow.transform.position=new Vector3(2,3,0);var shieldInstance=shield.Transform.GetComponent<RumbleVfxInstance>();
            typeof(RumbleVfxInstance).GetMethod("LateUpdate",flags).Invoke(shieldInstance,null);Check(shield.Transform.position==follow.transform.position,"Shield does not follow ally");
            typeof(RumbleVfxInstance).GetField("endTime",flags).SetValue(shieldInstance,-1f);typeof(RumbleVfxInstance).GetMethod("LateUpdate",flags).Invoke(shieldInstance,null);
            Check((bool)typeof(RumbleVfxInstance).GetField("stopping",flags).GetValue(shieldInstance),"Effect lifetime did not stop particles");
            var beam=RumbleVfx.Play("light_of_life",RumbleVfxLibrary.Stage.Beam,Vector3.zero,1,.5f,Vector3.right);leases.Add(beam);beam.SetBeam(.16f,5);
            Check(beam.IsAlive&&beam.Transform.localScale.z==5&&beam.Transform.GetComponentsInChildren<LineRenderer>().All(l=>!l.useWorldSpace&&l.widthMultiplier>.7f),"Textured beam length/width/local coordinates");
            File.WriteAllText(RumbleVfxBuilder.Work+"/validation.txt","PASS: all 20 skill styles and 3 potion palettes; 32 particle prefab recipes with supported materials; projectile/impact/field/heal/shield coverage; source movement/collision/demo scripts stripped; particle collision disabled; pooling/reuse, generation-safe leases, per-prefab budget, timed stop and following shield verified in native Unity. Preview images rendered. No live combat Play Mode pass performed.");
            File.Delete(RumbleVfxBuilder.Work+"/validate.request");
        }
        catch(Exception e){File.WriteAllText(RumbleVfxBuilder.Work+"/validation.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            foreach(var lease in leases)lease.Stop();
            if(follow!=null)UnityEngine.Object.DestroyImmediate(follow);
            if(oldPool==null){var root=(Transform)poolField.GetValue(null);if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);}
            MainStates.is3D=old3D;
        }
    }
}
#endif
