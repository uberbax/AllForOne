#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RumbleSkillsValidation
{
    private const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\rumble-skills";
    [InitializeOnLoadMethod] private static void Init()
    {
        if(File.Exists(Work+"/validate.request"))EditorApplication.delayCall+=Run;
    }
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static RObj Unit(string id,string side,Vector3 position,GameObject root)
    {
        var definition=new Obj{ID=id};
        foreach(var pair in new Dictionary<string,float>{{"level",1},{"attack",100},{"magic",80},{"health",2000},{"max_health",2000},{"mana",100},{"max_mana",100},{"speed",4}})definition.pars[pair.Key]=pair.Value;
        var unit=new RObj{RID=id,dbID=id,dbObj=definition,it=ItemType.monster};
        unit.tags.Add(side);unit.META_TAGS.Add("test");
        foreach(var key in new[]{"level","registered_damage","registered_mana","rarity"})unit.upgradePars[key]=0;
        unit.upgradePars["used_slot"]=-1;
        unit.main=new GameObject(id);unit.main.transform.SetParent(root.transform,false);unit.main.transform.position=position;unit.Position=position;
        var visual=new GameObject("Visual");visual.transform.SetParent(unit.main.transform,false);unit.visuals["vis_main"]=visual;
        unit.RecalcPars();MainStates.instance.all[id]=unit;MainStates.instance.allMetas[id]=unit;return unit;
    }
    [MenuItem("Tools/Skills/Validate Rumble Skills")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var previousState=MainStates.instance;var previousDb=DatabaseAll.instance;var previousConfig=ConfigLoader.Instance;
        var previousResources=ResourceHolder.instance;
        float previousDt=TimeManager.LAST_DT;bool previous3D=MainStates.is3D;
        var previousDamageTypes=new Dictionary<float,string>(MainStates.reverseDmgTypes);
        var configStatics=typeof(ConfigLoader).GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)
            .Where(f=>!f.IsLiteral&&!f.IsInitOnly).ToDictionary(f=>f,f=>f.GetValue(null));
        GameObject services=null,units=null;Texture2D iconTexture=null;Sprite icon=null;
        try
        {
            services=new GameObject("Rumble skills isolated services");services.SetActive(false);
            var loader=services.AddComponent<ConfigLoader>();ConfigLoader.Instance=loader;
            using(var stream=File.OpenRead("Assets/StreamingAssets/Config_exp_rumble.xlsx"))
                loader.ParseSkills(ExcelHelper.LoadExcel(stream).ToTSV("SKILLS2"));
            var db=services.AddComponent<DatabaseAll>();DatabaseAll.instance=db;
            var resources=services.AddComponent<ResourceHolder>();resources.XD=new StringObjectDictionary();resources.skills=new StringSpriteDictionary();
            iconTexture=new Texture2D(1,1);icon=Sprite.Create(iconTexture,new Rect(0,0,1,1),Vector2.one*.5f);resources.defaultTaskIcon=icon;
            ResourceHolder.instance=resources;RumbleSkillResources.Install(resources);
            Check(RumbleSkills.Ids.All(id=>resources.skills.ContainsKey(id)),"Missing skill icon fallback");
            typeof(DatabaseAll).GetMethod("OnSkillsParsed",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(db,new object[]{loader.skills});
            foreach(var id in RumbleSkills.Ids)Check(db.skills.ContainsKey(id),"Missing runtime skill: "+id);
            Check(RumbleSkills.Ids.Length==20&&RumbleSkills.Ids.Distinct().Count()==20,"Skill ID coverage");
            Check(db.skills["blizzard"].pars["time"]==5&&db.skills["rumble_invincible"].pars["time"]==4,"Specified durations");
            Check(db.skills["poisonous_fan"].pars["targets"]==5&&db.skills["feather_barrage"].pars["proj_amount"]==2,"Target/projectile counts");
            Check(db.skills["healing_strike"].pars.GetValueOrDefault("health")==0,"Healing Strike must not heal enemies through DealDamage");
            var state=services.AddComponent<MainStates>();MainStates.instance=state;state.useSave=false;
            // MainStates.Awake normally initializes this table; isolated inactive services skip Awake.
            MainStates.reverseDmgTypes.Clear();foreach(var pair in MainStates.dmgTypes)MainStates.reverseDmgTypes[pair.Value]=pair.Key;
            units=new GameObject("Rumble skills isolated units");MainStates.is3D=false;
            var caster=Unit("main_player","player",Vector3.zero,units);
            var ally=Unit("ally","player",new Vector3(1,0),units);
            var enemy=Unit("enemy","enemy",new Vector3(2,0),units);
            var distant=Unit("distant","enemy",new Vector3(20,0),units);
            Check(RumbleSkills.Targets(caster,Vector3.zero,4,false).SequenceEqual(new[]{enemy}),"Enemy/range filtering");
            Check(RumbleSkills.Targets(caster,Vector3.zero,4,true).Count==2,"Ally/self filtering");
            Check(RumbleSkills.Targets(caster,Vector3.zero,4,false,new List<RObj>{enemy}).Count==0,"Ricochet exclusion");
            Check(RumbleSkills.Targets(caster,Vector3.zero,4,false,tag:"absent").Count==0,"Required metadata tag");
            Check(Mathf.Abs(RumbleSkills.SegmentDistance(new Vector3(2,.3f),Vector3.zero,new Vector3(4,0))-.3f)<.001f,"Piercing capsule geometry");
            Check(Mathf.Abs(RumbleSkills.SegmentDistance(new Vector3(5,0),Vector3.zero,new Vector3(4,0))-1)<.001f,"Segment endpoints");
            int registered=state.all.Count;
            foreach(var id in RumbleSkills.Ids)
            {
                var skill=RumbleSkills.CreatePayload(caster,id);
                Check(state.all.Count==registered,"Data-only cast leaked into world registry: "+id);
                if(skill.GetMainPar("attack")>0||skill.GetMainPar("magic")>0)
                {
                    enemy.SetPar("registered_damage",0);float hp=enemy.GetPar("health");
                    state.DealDamage(enemy,skill);Check(enemy.GetPar("health")<hp,"Native damage not applied: "+id);
                }
            }
            var noMana=RumbleSkills.CreatePayload(caster,"sniper_rifle");caster.SetPar("mana",-100);
            Check(RumbleSkills.Execute(null,caster,noMana,enemy,default,false,null,"",null)==ExecReso.NO_MANA,"Mana validation");
            caster.SetPar("mana",0);noMana.SetPar("cd",5);
            Check(RumbleSkills.Execute(null,caster,noMana,enemy,default,false,null,"",null)!=ExecReso.OK,"Cooldown validation");
            RumbleSkillRunner.ApplyStatus(caster,enemy,"rumble_poison");
            var poison=enemy.timedBuffs.Single(b=>b.dbObj.ID=="rumble_poison");poison.SetPar("timeLeft",1);
            RumbleSkillRunner.ApplyStatus(caster,enemy,"rumble_poison");
            Check(enemy.timedBuffs.Count(b=>b.dbObj.ID=="rumble_poison")==1&&poison.GetPar("timeLeft")==4,"Poison refresh/stacking");
            RumbleSkillRunner.ApplyStatus(caster,enemy,"rumble_frostbite");
            Check(enemy.GetPar("speed")==3&&enemy.timedBuffs.Count==3,"Frostbite plus native slow");
            enemy.timedBuffs.Clear();enemy.RecalcPars();
            RumbleSkillRunner.ApplyStatus(caster,ally,"rumble_invincible");
            float before=ally.GetPar("health");var shot=RumbleSkills.CreatePayload(enemy,"sniper_rifle");state.DealDamage(ally,shot);
            Check(ally.GetPar("health")==before&&ally.GetPar("immortal")>0,"Native invincibility");
            state.HandleCds(4,ally);Check(ally.GetPar("immortal")==0,"Invincibility expiry");
            enemy.SetPar("registered_damage",0);RumbleSkillRunner.ApplyStatus(caster,enemy,"rumble_burn");
            float beforeDot=enemy.GetPar("health");state.HandleCds(1,enemy);
            Check(enemy.GetPar("health")<beforeDot,"Native DOT ticking");
            state.HandleCds(3,enemy);Check(enemy.timedBuffs.Count==0,"Native DOT expiry");
            var lock1=RumbleMovementLock.Acquire(ally);var lock2=RumbleMovementLock.Acquire(ally);
            ally.visMain.transform.localPosition=Vector3.up;lock1.Release();lock2.Release();
            Check(ally.visMain.transform.localPosition==Vector3.zero,"Reference-counted launch cleanup");
            Check(state.all.Count==registered,"Status payload leaked into world registry");
            File.WriteAllText(Work+"/validation.txt","PASS: actual ExcelHelper -> ConfigLoader -> DatabaseAll; all 20 casts and 5 statuses, duration/count balance, native damage, targeting/side/range/exclusions, piercing geometry, mana/cooldown guards, poison refresh, frostbite+slow, native DOT ticking/expiry, invincibility/expiry, motion cleanup, no data-payload registry leaks. Isolated fixture; no user save changes. Timed cast choreography has not been run in Play Mode.");
            File.Delete(Work+"/validate.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/validation.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            if(units!=null)UnityEngine.Object.DestroyImmediate(units);
            if(services!=null)UnityEngine.Object.DestroyImmediate(services);
            if(icon!=null)UnityEngine.Object.DestroyImmediate(icon);if(iconTexture!=null)UnityEngine.Object.DestroyImmediate(iconTexture);
            MainStates.instance=previousState;DatabaseAll.instance=previousDb;ConfigLoader.Instance=previousConfig;
            ResourceHolder.instance=previousResources;
            foreach(var field in configStatics)field.Key.SetValue(null,field.Value);
            TimeManager.LAST_DT=previousDt;MainStates.is3D=previous3D;
            MainStates.reverseDmgTypes.Clear();foreach(var pair in previousDamageTypes)MainStates.reverseDmgTypes[pair.Key]=pair.Value;
        }
    }
}
#endif
