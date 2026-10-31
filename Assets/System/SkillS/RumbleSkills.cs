using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Special casts described in SkillS/skills.txt. Numeric balance stays in SKILLS2.</summary>
public static class RumbleSkills
{
    public static readonly string[] Ids = {
        "water_strike", "waterspout", "venom_flask", "venom_zone", "crush", "iron_whirlwind",
        "frost_nova", "blizzard", "bombard", "blazing_cannonball", "feather_barrage", "poisonous_fan",
        "mysterious_potion", "energy_torrent", "dual_pistols", "sniper_rifle", "healing_strike",
        "tranquility", "heal", "light_of_life"
    };
    private static readonly HashSet<string> supported = new HashSet<string>(Ids, StringComparer.Ordinal);
    public static bool Handles(string id) => id != null && supported.Contains(id);
    public static bool Alive(RObj unit) => unit != null && unit.main != null && unit.main.activeInHierarchy && unit.GetPar("health") > 0;
    public static bool SameSide(RObj a, RObj b) => a != null && b != null && a.tags.Count > 0 && b.tags.Count > 0 && a.tags[0] == b.tags[0];
    public static Vector3 Flat(Vector3 value) { if (MainStates.is3D) value.y = 0; else value.z = 0; return value; }
    public static float Distance(Vector3 a, Vector3 b) => Flat(a - b).magnitude;
    public static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        var delta = Flat(b - a); var relative = Flat(p - a);
        float t = delta.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector3.Dot(relative, delta) / delta.sqrMagnitude) : 0;
        return (relative - delta * t).magnitude;
    }
    public static List<RObj> Targets(RObj who, Vector3 center, float range, bool allies, List<RObj> except = null, string tag = "", List<RObj> buffer = null)
    {
        var result = buffer ?? new List<RObj>();result.Clear();
        if (MainStates.instance == null) return result;
        foreach (var pair in MainStates.instance.allMetas)
        {
            var unit = pair.Value;
            if (!Alive(unit) || unit.it != ItemType.monster || SameSide(who, unit) != allies ||
                (except != null && except.Contains(unit)) || (tag != "" && !unit.META_TAGS.Contains(tag)) ||
                (!allies && unit.GetPar("immortal") > 0) || Distance(center, unit.Position) > range) continue;
            if (!result.Contains(unit)) result.Add(unit);
        }
        if (allies && Alive(who) && Distance(center, who.Position) <= range && !result.Contains(who) &&
            (except == null || !except.Contains(who)) && (tag == "" || who.META_TAGS.Contains(tag))) result.Add(who);
        return result;
    }
    public static ExecReso Execute(SkillExecutor executor, RObj who, RObj skill, RObj preferred,
        Vector3 overPos, bool overCd, List<RObj> except, string reqTag, Dictionary<string, float> addPars)
    {
        if (!Alive(who) || skill == null || DatabaseAll.instance == null) return ExecReso.NO_TARGETS;
        bool secondary = overPos != default;
        if (!overCd && !secondary && skill.GetPar("cd") > 0) return ExecReso.NO_TARGETS;
        float cost = skill.GetPar("manacost") * Mathf.Max(0, 1 + who.GetPar("mana_reduction") / 100f);
        if (!overCd && !secondary && who.GetPar("mana") < cost) return ExecReso.NO_MANA;
        bool allies = skill.GetPar("target") == 1;
        var origin = secondary ? overPos : who.Position;
        var targets = Targets(who, origin, skill.GetPar("range"), allies, except, reqTag);
        if (skill.GetPar("filter_self") > 0) targets.Remove(who);
        if (allies && skill.dbObj.ID != "tranquility")
            targets.Sort((a,b) => HealthRatio(a).CompareTo(HealthRatio(b)));
        else targets.Sort((a,b) => Distance(origin,a.Position).CompareTo(Distance(origin,b.Position)));
        if (targets.Count == 0) return ExecReso.NO_TARGETS;
        var target = preferred != null && targets.Contains(preferred) ? preferred : targets[0];
        if (ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.req_line_sight) > 0 && !MainStates.is3D &&
            Physics2D.Linecast(origin, target.Position, LayerMask.GetMask("Nopass")).collider != null) return ExecReso.NO_SIGHT;
        var runner = who.main.GetComponent<RumbleSkillRunner>();
        if (runner == null) runner = who.main.AddComponent<RumbleSkillRunner>();
        // Charge once per cast, never per projectile, bounce, or area tick.
        if (!secondary)
        {
            if (cost > 0 && (who.GetPar("mana_siphon") <= 0 || UnityEngine.Random.Range(0f,100f) >= who.GetPar("mana_siphon")))
                MainStates.instance.AddMana(who, -cost);
            skill.SetPar("cd", skill.GetPar("cooldown"));
            if (who.visuals.TryGetValue("animator", out var visual) && visual != null)
            {
                var animator = visual.GetComponentInChildren<XDanimator>();
                if (animator != null) animator.SetState("attack");
            }
            if (SoundManager.instance != null) SoundManager.instance.PlayAny(skill.dbObj.ID);
        }
        // Every cast owns separate projectile data; a later cast cannot mutate an earlier burst.
        var payload = CreatePayload(who, skill.dbObj.ID, addPars);
        runner.Begin(who, payload, target, origin);
        EventManager.INV("skill_casted", new ArgPass { who = who, who2 = skill });
        return ExecReso.OK;
    }
    private static float HealthRatio(RObj unit) => unit.GetPar("health") / Mathf.Max(1,unit.GetPar("max_health"));
    public static RObj CreatePayload(RObj owner, string id, Dictionary<string,float> addPars = null)
    {
        var result=DatabaseAll.instance.CreateProjectile(owner,id,Vector3.zero,false,false,addPars:addPars);
        // Data-only casts/statuses are held by their coroutine/timedBuffs, not by the world registry.
        if(MainStates.instance.all.TryGetValue(result.RID,out var registered)&&ReferenceEquals(result,registered))
            MainStates.instance.all.Remove(result.RID);
        return result;
    }
}

public sealed class RumbleSkillRunner : MonoBehaviour
{
    private readonly List<RumbleMovementLock> movementLocks = new List<RumbleMovementLock>();
    private readonly List<RumbleVfx.Lease> packEffects = new List<RumbleVfx.Lease>();
    private readonly Dictionary<Transform,Quaternion> spinningVisuals = new Dictionary<Transform,Quaternion>();
    public void Begin(RObj who, RObj skill, RObj target, Vector3 origin) => StartCoroutine(Cast(who, skill, target, origin));
    private static float Value(RObj skill, string name, float fallback) { float value=skill.GetPar(name); return value>0?value:fallback; }
    private IEnumerator Cast(RObj who, RObj skill, RObj target, Vector3 origin)
    {
        switch (skill.dbObj.ID)
        {
            case "water_strike":
            case "feather_barrage":
                for (int i=0;i<Mathf.Max(1,(int)skill.GetPar("proj_amount"));i++)
                {
                    if (!RumbleSkills.Alive(who)) yield break;
                    var direction=RumbleSkills.Flat((RumbleSkills.Alive(target)?target.Position:origin+Vector3.right)-origin).normalized;
                    if(direction.sqrMagnitude<.01f)direction=Vector3.right;
                    StartCoroutine(Pierce(who,skill,origin,direction));
                    if(i+1<skill.GetPar("proj_amount"))yield return new WaitForSeconds(Value(skill,"dt",.2f));
                }
                break;
            case "waterspout": case "venom_zone": case "blizzard":
                yield return Field(who,skill,target.Position,false); break;
            case "iron_whirlwind":
                yield return Field(who,skill,who.Position,true); break;
            case "crush":
                var landing=target.Position+RumbleSkills.Flat(who.Position-target.Position).normalized*.6f;
                yield return Leap(who,landing,Value(skill,"leap_ms",450)/1000f);
                if(!RumbleSkills.Alive(who))yield break;
                foreach(var victim in RumbleSkills.Targets(who,who.Position,skill.GetPar("aoe"),false))
                {
                    Hit(who,skill,victim);
                    if(RumbleSkills.Alive(victim))StartCoroutine(Launch(victim,.65f));
                }
                Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Impact,who.Position,skill.GetPar("aoe"),.8f);
                break;
            case "venom_flask":
                var visited=new List<RObj>();var next=target;var start=origin;
                for(int i=0;i<=Mathf.Max(0,(int)skill.GetPar("ricochet"));i++)
                {
                    if(!RumbleSkills.Alive(who)||!RumbleSkills.Alive(next))break;
                    yield return Projectile(who,skill,next,start,"rumble_poison");
                    visited.Add(next);start=next.Position;
                    var choices=RumbleSkills.Targets(who,start,Value(skill,"bounce_range",3),false,visited);
                    choices.Sort((a,b)=>RumbleSkills.Distance(start,a.Position).CompareTo(RumbleSkills.Distance(start,b.Position)));
                    next=choices.Count>0?choices[0]:null;
                }
                break;
            case "poisonous_fan":
                var fan=RumbleSkills.Targets(who,origin,skill.GetPar("range"),false);
                fan.Sort((a,b)=>RumbleSkills.Distance(origin,a.Position).CompareTo(RumbleSkills.Distance(origin,b.Position)));
                for(int i=0;i<Mathf.Min(fan.Count,(int)Value(skill,"targets",5));i++)
                    StartCoroutine(Projectile(who,skill,fan[i],origin,"rumble_poison"));
                var allies=RumbleSkills.Targets(who,origin,Value(skill,"leap_range",6),true,new List<RObj>{who});
                allies.Sort((a,b)=>RumbleSkills.Distance(origin,a.Position).CompareTo(RumbleSkills.Distance(origin,b.Position)));
                if(allies.Count>0)yield return Leap(who,allies[0].Position+RumbleSkills.Flat(origin-allies[0].Position).normalized*.8f,.4f);
                break;
            case "tranquility":
                foreach(var ally in RumbleSkills.Targets(who,who.Position,skill.GetPar("aoe"),true))
                { ApplyStatus(who,ally,"rumble_invincible");Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Shield,ally.Position,.7f,Value(skill,"time",4),follow:ally.main.transform); }
                Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Cast,who.Position,skill.GetPar("aoe"),.8f);
                break;
            case "heal":
                if(RumbleSkills.Alive(target))Heal(who,skill,target);
                break;
            case "light_of_life":
                BeamEffect(skill.dbObj.ID,origin,target.Position,.16f,.5f);
                foreach(var ally in RumbleSkills.Targets(who,target.Position,skill.GetPar("aoe"),true))Heal(who,skill,ally);
                break;
            case "healing_strike":
                for(int i=0;i<Mathf.Max(1,(int)skill.GetPar("proj_amount"));i++)
                {
                    if(!RumbleSkills.Alive(who)||!RumbleSkills.Alive(target))break;
                    Hit(who,skill,target);
                    Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Cast,who.Position,1,.35f,target.Position-who.Position);
                    Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Impact,target.Position,.45f,.5f);
                    foreach(var ally in RumbleSkills.Targets(who,who.Position,skill.GetPar("aoe"),true))Heal(who,skill,ally);
                    if(i+1<skill.GetPar("proj_amount"))yield return new WaitForSeconds(Value(skill,"dt",.2f));
                }
                break;
            default:
                for(int i=0;i<Mathf.Max(1,(int)skill.GetPar("proj_amount"));i++)
                {
                    if(!RumbleSkills.Alive(who))break;
                    if(!RumbleSkills.Alive(target))
                    {
                        var choices=RumbleSkills.Targets(who,who.Position,skill.GetPar("range"),false);
                        if(choices.Count==0)break;target=choices[0];
                    }
                    string status=skill.dbObj.ID=="blazing_cannonball"?"rumble_burn":
                        (skill.dbObj.ID=="mysterious_potion"||skill.dbObj.ID=="energy_torrent"?RandomPotion():null);
                    StartCoroutine(Projectile(who,skill,target,who.Position,status));
                    if(i+1<skill.GetPar("proj_amount"))yield return new WaitForSeconds(Value(skill,"dt",.15f));
                }
                break;
        }
    }
    private IEnumerator Projectile(RObj who,RObj skill,RObj target,Vector3 origin,string status)
    {
        var end=target.Position;var position=origin;
        string visualId=(skill.dbObj.ID=="mysterious_potion"||skill.dbObj.ID=="energy_torrent")&&status!=null?status:skill.dbObj.ID;
        float size=skill.dbObj.ID=="dual_pistols"?.10f:skill.dbObj.ID=="sniper_rifle"?.15f:skill.dbObj.ID=="frost_nova"?.35f:.24f;
        var visual=Effect(visualId,RumbleVfxLibrary.Stage.Projectile,origin,size,-1,end-origin);float elapsed=0;
        Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Cast,origin,.35f,.2f,end-origin);
        if(skill.dbObj.ID=="sniper_rifle")BeamEffect(skill.dbObj.ID,origin,end,.08f,.15f);
        bool arc=skill.GetPar("travel")==1;
        float duration=Mathf.Clamp(RumbleSkills.Distance(origin,end)/Value(skill,"speed",12),.06f,2);
        try
        {
            while(elapsed<duration&&RumbleSkills.Alive(who))
            {
                if(RumbleSkills.Alive(target))end=target.Position;
                elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);
                var previous=position;position=Vector3.Lerp(origin,end,t);if(arc)position+=Up*Mathf.Sin(t*Mathf.PI)*1.2f;
                if(visual.Transform!=null)
                {
                    visual.Transform.position=position;var tangent=position-previous;
                    if(tangent.sqrMagnitude>.00001f)visual.Transform.rotation=Quaternion.LookRotation(tangent.normalized,MainStates.is3D?Vector3.up:Vector3.forward);
                }
                yield return null;
            }
            if(!RumbleSkills.Alive(who))yield break;
            float radius=skill.GetPar("aoe");
            if(radius>0)
            {
                foreach(var victim in RumbleSkills.Targets(who,end,radius,false))Hit(who,skill,victim,status);
                Effect(visualId,RumbleVfxLibrary.Stage.Impact,end,radius,.8f);
            }
            else if(RumbleSkills.Alive(target))
            {Hit(who,skill,target,status);Effect(visualId,RumbleVfxLibrary.Stage.Impact,end,.4f,.6f);}
        }
        finally{visual.Stop();}
    }
    private IEnumerator Pierce(RObj who,RObj skill,Vector3 origin,Vector3 direction)
    {
        float distance=0,range=skill.GetPar("range"),radius=Value(skill,"aoe",.35f);
        var visited=new List<RObj>();var visual=Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Projectile,origin,radius,-1,direction);
        try
        {
            while(distance<range&&RumbleSkills.Alive(who))
            {
                float next=Mathf.Min(range,distance+Value(skill,"speed",12)*Time.deltaTime);
                var a=origin+direction*distance;var b=origin+direction*next;if(visual.Transform!=null)visual.Transform.position=b;
                foreach(var victim in RumbleSkills.Targets(who,origin,range+radius,false,visited))
                    if(RumbleSkills.SegmentDistance(victim.Position,a,b)<=radius){visited.Add(victim);Hit(who,skill,victim);Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Impact,victim.Position,.4f,.6f);}
                distance=next;yield return null;
            }
        }
        finally{visual.Stop();}
    }
    private IEnumerator Field(RObj who,RObj skill,Vector3 center,bool followCaster)
    {
        float age=0,nextTick=0,duration=Value(skill,"time",3),interval=Value(skill,"dmg_every",.5f),radius=skill.GetPar("aoe");
        var visual=Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Area,center,radius,-1);
        var targets=new List<RObj>();float nextScan=0;
        var spinning=followCaster&&who.HasVisMain()?who.visMain.transform:null;
        var rotation=spinning!=null?spinning.localRotation:Quaternion.identity;
        if(spinning!=null)spinningVisuals.TryAdd(spinning,rotation);
        try
        {
            while(age<duration&&RumbleSkills.Alive(who))
            {
                if(followCaster)center=who.Position;if(visual.Transform!=null)visual.Transform.position=center;
                if(spinning!=null)spinning.localRotation=rotation*Quaternion.AngleAxis(-age*720,MainStates.is3D?Vector3.up:Vector3.forward);
                if(age>=nextScan)
                {RumbleSkills.Targets(who,center,radius,false,buffer:targets);nextScan=age+.1f;}
                foreach(var victim in targets)
                {
                    if(!RumbleSkills.Alive(victim)||RumbleSkills.Distance(center,victim.Position)>radius)continue;
                    if(skill.GetPar("pull_speed")>0)Pull(victim,center,skill.GetPar("pull_speed")*Time.deltaTime);
                    if(age+0.0001f>=nextTick)Hit(who,skill,victim,skill.dbObj.ID=="blizzard"?"rumble_frostbite":null);
                }
                if(age+0.0001f>=nextTick)nextTick+=interval;
                age+=Time.deltaTime;yield return null;
            }
        }
        finally{if(spinning!=null){spinning.localRotation=rotation;spinningVisuals.Remove(spinning);}visual.Stop();}
    }
    private static void Hit(RObj who,RObj skill,RObj victim,string status=null)
    {
        if(!RumbleSkills.Alive(victim)||!RumbleSkills.Alive(who))return;
        skill.Position=victim.Position;
        MainStates.instance.DealDamage(victim,skill);
        if(status!=null&&RumbleSkills.Alive(victim)&&victim.GetPar("immortal")<=0)ApplyStatus(who,victim,status);
    }
    public static void ApplyStatus(RObj who,RObj victim,string id)
    {
        if(DatabaseAll.instance==null||!DatabaseAll.instance.skills.ContainsKey(id))return;
        if(id=="rumble_frostbite")ApplyStatus(who,victim,"rumble_chilled");
        var definition=DatabaseAll.instance.skills[id];
        foreach(var previous in victim.timedBuffs)
            if(previous.dbObj.ID==id){previous.SetPar("timeLeft",definition.pars["time"]);return;}
        var buff=RumbleSkills.CreatePayload(who,id);
        buff.SetPar("timeLeft",buff.GetPar("time"));buff.SetPar("timeEvery",buff.GetPar("dmg_every"));
        victim.timedBuffs.Add(buff);victim.RecalcPars();
    }
    private void Heal(RObj who,RObj skill,RObj target)
    {
        float amount=skill.GetPar("health")+skill.GetPar("heal_flat")+who.GetMainPar("magic")*skill.GetPar("heal_prc")/100f;
        amount*=Mathf.Max(0,1+who.GetPar("heal_amplify")/100f);
        MainStates.instance.DealHeal(target,amount);
        Effect(skill.dbObj.ID,RumbleVfxLibrary.Stage.Heal,target.Position,.7f,.7f,follow:target.main.transform);
    }
    private static string RandomPotion(){int roll=UnityEngine.Random.Range(0,3);return roll==0?"rumble_burn":roll==1?"rumble_frostbite":"rumble_poison";}
    private static Vector3 Up=>Vector3.up;
    private static void Pull(RObj target,Vector3 center,float step)
    {
        var position=target.Position+Vector3.ClampMagnitude(RumbleSkills.Flat(center-target.Position),step);
        Move(target,position);
    }
    private static void Move(RObj unit,Vector3 point)
    {
        if(unit.main==null)return;
        var agent=unit.main.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if(agent!=null&&agent.enabled&&agent.isOnNavMesh)
        {
            if(UnityEngine.AI.NavMesh.SamplePosition(point,out var hit,.7f,agent.areaMask))agent.Warp(hit.position);
        }
        else
        {
            if(!MainStates.is3D&&Physics2D.Linecast(unit.Position,point,LayerMask.GetMask("Nopass")).collider!=null)return;
            unit.main.transform.position=point;
        }
        unit.Position=unit.main.transform.position;
    }
    private IEnumerator Leap(RObj unit,Vector3 destination,float duration)
    {
        if(!RumbleSkills.Alive(unit))yield break;
        var motion=RumbleMovementLock.Acquire(unit);movementLocks.Add(motion);
        var origin=unit.Position;var visual=unit.visMain!=null?unit.visMain.transform:null;
        var visualOrigin=visual!=null?visual.localPosition:Vector3.zero;float age=0;
        try
        {
            while(age<duration&&RumbleSkills.Alive(unit))
            {
                age+=Time.deltaTime;float t=Mathf.Clamp01(age/duration);Move(unit,Vector3.Lerp(origin,destination,t));
                if(visual!=null)visual.localPosition=visualOrigin+Up*Mathf.Sin(t*Mathf.PI)*1.1f;
                yield return null;
            }
        }
        finally{if(visual!=null)visual.localPosition=visualOrigin;if(motion!=null)motion.Release();movementLocks.Remove(motion);}
    }
    private IEnumerator Launch(RObj unit,float duration)
    {
        if(unit.visMain==null)yield break;
        var motion=RumbleMovementLock.Acquire(unit);movementLocks.Add(motion);
        var visual=unit.visMain.transform;var start=visual.localPosition;float age=0;
        try
        {
            while(age<duration&&RumbleSkills.Alive(unit))
            {age+=Time.deltaTime;if(visual!=null)visual.localPosition=start+Up*Mathf.Sin(Mathf.Clamp01(age/duration)*Mathf.PI)*1.3f;yield return null;}
        }
        finally{if(visual!=null)visual.localPosition=start;if(motion!=null)motion.Release();movementLocks.Remove(motion);}
    }
    private RumbleVfx.Lease Effect(string id,RumbleVfxLibrary.Stage stage,Vector3 position,float radius,float duration,Vector3 direction=default,Transform follow=null)
    {
        packEffects.RemoveAll(effect=>!effect.IsAlive);
        var effect=RumbleVfx.Play(id,stage,position,radius,duration,direction,follow);
        if(effect.IsAlive)packEffects.Add(effect);
        return effect;
    }
    private void BeamEffect(string id,Vector3 origin,Vector3 destination,float width,float duration)
    {
        var direction=destination-origin;
        var effect=Effect(id,RumbleVfxLibrary.Stage.Beam,origin,1,duration,direction);
        effect.SetBeam(width,direction.magnitude);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        foreach(var motion in movementLocks)if(motion!=null)motion.Release();movementLocks.Clear();
        foreach(var effect in packEffects)effect.Stop();packEffects.Clear();
        foreach(var pair in spinningVisuals)if(pair.Key!=null)pair.Key.localRotation=pair.Value;spinningVisuals.Clear();
    }
}

/// <summary>Reference-counted movement suspension. Disabling/destroying the cast owner also releases it.</summary>
public sealed class RumbleMovementLock : MonoBehaviour
{
    private int count;
    private readonly List<MonoBehaviour> suspended=new List<MonoBehaviour>();
    private UnityEngine.AI.NavMeshAgent agent;
    private bool wasStopped;
    private Transform visual;
    private Vector3 visualPosition;
    public static RumbleMovementLock Acquire(RObj unit)
    {
        var result=unit.main.GetComponent<RumbleMovementLock>();if(result==null)result=unit.main.AddComponent<RumbleMovementLock>();
        if(result.count++==0)
        {
            result.visual=unit.visMain!=null?unit.visMain.transform:null;
            if(result.visual!=null)result.visualPosition=result.visual.localPosition;
            foreach(var script in unit.main.GetComponentsInChildren<MonoBehaviour>(true))
                if(script.enabled&&(script is XDcombat||script is XDmove||script is XDfollow||script is XDfollow2||script is XDrand_move))
                {result.suspended.Add(script);script.enabled=false;}
            result.agent=unit.main.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if(result.agent!=null&&result.agent.enabled&&result.agent.isOnNavMesh){result.wasStopped=result.agent.isStopped;result.agent.isStopped=true;}
        }
        return result;
    }
    public void Release()
    {
        if(count<=0||--count>0)return;
        foreach(var script in suspended)if(script!=null)script.enabled=true;suspended.Clear();
        if(visual!=null)visual.localPosition=visualPosition;
        if(agent!=null&&agent.enabled&&agent.isOnNavMesh)agent.isStopped=wasStopped;
    }
    private void OnDisable(){count=1;Release();}
}
