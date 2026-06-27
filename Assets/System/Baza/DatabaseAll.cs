using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DatabaseAll : MonoBehaviour
{
    public GameObject emptyProj;
    
    public static DatabaseAll instance;

    public Dictionary<string, ElTasko> allTasks => ConfigLoader.Instance.allTasks;
    public Dictionary<string, ElTasko> allShop => ConfigLoader.Instance.allShop;
    

    public Dictionary<string, Obj> heroes = new Dictionary<string, Obj>();
    public Dictionary<string, Obj> skills = new Dictionary<string, Obj>();
    public Dictionary<string, Obj> items = new Dictionary<string, Obj>();
    public Dictionary<string, Obj> buildings = new Dictionary<string, Obj>();

    private void Awake()
    {
        instance = this;
        ConfigLoader.onHeroesParsed += OnHeroesParsed;
        ConfigLoader.onSkillsParsed += OnSkillsParsed;
        ConfigLoader.onArtefactsParsed += OnItemsParsed;
        ConfigLoader.onBuildingsParsed += OnBuildingsParsed;
        
    }

    private void OnBuildingsParsed(Dictionary<string, FormatBuilding> obj)
    {
        foreach (var v in obj)
        {
            var o = new Obj();
            o.ID = v.Key;
            o.pars.Add("slot", -1);
            o.skills = v.Value.actions;
            
            Debug.Log(v.Key);
            
            buildings.Add(o.ID, o);
        }
    }

    private void OnSkillsParsed(List<FormatSkill> objs)
    {
        Debug.Log("hah " + objs.Count);
        foreach (var v in objs)
        {
            var o = new Obj();
            o.ID = v.skillName.ToLower();
            /*if (v.ATTACK != 0)*/ o.pars.Add("attack", v.ATTACK);
            /*if (v.ATTACK != 0)*/ o.pars.Add("attack_prc", v.ATTACK_PRC);
            if (v.HEALTH != 0) o.pars.Add("health", v.HEALTH);
            if (v.MAX_HEALTH != 0) o.pars.Add("max_health", v.MAX_HEALTH);
            if (v.MANA != 0) o.pars.Add("mana", v.MANA);
            if (v.MAX_MANA != 0) o.pars.Add("max_mana", v.MAX_MANA);
            if (v.DEF != 0) o.pars.Add("def", v.DEF);
            if (v.RES != 0) o.pars.Add("res", v.RES);
            if (v.DEF_PRC != 0) o.pars.Add("def_prc", v.DEF_PRC);
            if (v.RES_PRC != 0) o.pars.Add("res_prc", v.RES_PRC);
            if (v.SPEED != 0) o.pars.Add("speed", v.SPEED);
            if (v.PEN_CNT != 0) o.pars.Add("pen_cnt", v.PEN_CNT);
            if (v.RANGE != 0)
            {
                var over = ConfigLoader.GetMetaParamValue("skill_range");
                if (over > 0) o.pars.Add("range", over);
                else o.pars.Add("range", v.RANGE);
            }
            
            o.pars.Add("instant", v.INSTANT);
            
            //player ? eney, all
            if (v.TAG_APPLY == "enemy") o.pars.Add("target",0);
            else if (v.TAG_APPLY == "all") o.pars.Add("target",2);
            else o.pars.Add("target",1);
            
            o.pars.Add("filter_self", v.FILTER_SELF);
            o.pars.Add("filter_hp", SkillExecutor.mapFilter[v.FILTER_HP]);
            o.pars.Add("filter_range", SkillExecutor.mapFilter[v.FILTER_RANGE]);
            o.pars.Add("filter_atk", SkillExecutor.mapFilter[v.FILTER_ATK]);
            
            o.pars.Add("cooldown", v.COOLDOWN);
            
            o.pars.Add("action_req", v.ACTION_REQ);
            o.pars.Add("empty_req", v.EMPTY_REQ);
            
            o.pars.Add("aoe", v.aoe);
            o.pars.Add("proj_amount", v.amount);
            o.pars.Add("travel", v.travel);
            o.pars.Add("cd_red", v.cdReduction);
            o.pars.Add("time", v.time);
            o.pars.Add("dmg_every", v.dmgEvery);
            o.pars.Add("targets", v.targets);
            o.pars.Add("mana_req", v.MANA_REQ);
            o.pars.Add("shield", v.SHIELD);
            o.pars.Add("angle", v.ANGLE);
            o.pars.Add("dt", v.DT);
            o.pars.Add("first", v.FIRST);
            o.pars.Add("crit_chance", v.CRIT_CHANCE);
            o.pars.Add("crit_dmg", v.CRIT_DMG);
            o.pars.Add("lifesteal_prc", v.LIFESTEAL_PRC);
            o.pars.Add("regen", v.REGEN);
            o.pars.Add("unique", v.unique);
            o.pars.Add("manacost", v.manaCost);
            
            o.pars.Add("dodge", v.dodge);
            o.pars.Add("dodge_prc", v.dodge_prc);
            o.pars.Add("accuracy", v.accuracy);
            o.pars.Add("apoints", v.apoints);
            
            o.pars.Add("immortal", v.immortal);
            o.pars.Add("req2", v.req2);
            
            o.pars.Add("ricochet", v.ricochet);
            o.pars.Add("bounce", v.bounce);
            
            o.pars.Add("level", 1);
            
            o.second = v.SECOND;
            
            o.onDeath = v.onDeath;
            o.onDmg = v.onDmg;
            o.spawn = v.spawn;

            o.buffsApplied = v.buffApply;
            o.extraPars = v.PARS;
            o.labelis = v.affected;
            
            skills.Add(o.ID, o);
        }
    }

    private void OnItemsParsed(List<FormatArtefact> objs)
    {
        Debug.Log("hah " + objs.Count);
        foreach (var v in objs)
        {
            var o = new Obj();
            o.ID = v.skillName.ToLower();
            /*if (v.ATTACK != 0)*/ o.pars.Add("attack", v.ATTACK);
            /*if (v.ATTACK != 0)*/ o.pars.Add("attack_prc", v.ATTACK_PRC);
            if (v.HEALTH != 0) o.pars.Add("health", v.HEALTH);
            if (v.MAX_HEALTH != 0) o.pars.Add("max_health", v.MAX_HEALTH);
            if (v.MANA != 0) o.pars.Add("mana", v.MANA);
            if (v.MAX_MANA != 0) o.pars.Add("max_mana", v.MAX_MANA);
            if (v.DEF != 0) o.pars.Add("def", v.DEF);
            if (v.RES != 0) o.pars.Add("res", v.RES);
            if (v.DEF_PRC != 0) o.pars.Add("def_prc", v.DEF_PRC);
            if (v.RES_PRC != 0) o.pars.Add("res_prc", v.RES_PRC);
            if (v.SPEED != 0) o.pars.Add("speed", v.SPEED);
            if (v.PEN_CNT != 0) o.pars.Add("pen_cnt", v.PEN_CNT);
            o.pars.Add("instant", v.INSTANT);
            
            if (v.SLOT != "") o.pars.Add("slot", MainStates.slots[v.SLOT]);
            if (v.RARITY != "") o.pars.Add("rarity", MainStates.rarity[v.RARITY]);
            
            o.pars.Add("level", 1);
            
            if (v.SLOT == "none")
                o.pars.Add("max_stack", 1000000);
            else 
                o.pars.Add("max_stack", 1);
            
            o.refSkill = v.REF_SKILL;
            
            o.sizeX = v.size / 10;
            o.sizeY = v.size % 10;
            o.price = v.price;
            
            items.Add(o.ID, o);

        }
    }

    private void OnHeroesParsed(List<FormatHero> objs)
    {
        Debug.Log("hah " + objs.Count);
        foreach (var v in objs)
        {
            var o = new Obj();
            o.ID = v.monsterName.ToLower();
            o.pars.Add("attack", v.attack);
            o.pars.Add("health", v.health);
            o.pars.Add("max_health", v.maxHealth);
            o.pars.Add("mana", v.mana);
            o.pars.Add("max_mana", v.maxMana);
            o.pars.Add("def", v.armor);
            o.pars.Add("res", v.magicResist);
            o.pars.Add("speed", v.speed);
            o.pars.Add("max_stack", 1);
            o.pars.Add("exp", 0);
            o.pars.Add("slot", -1);
            
            o.pars.Add("lifesteal_prc", v.lifestealPrc);
            o.pars.Add("regen", v.regen);
            o.pars.Add("drop_pick", v.dropPick);
            
            o.pars.Add("dodge", v.dodge);
            o.pars.Add("accuracy", v.accuracy);
            o.pars.Add("apoints", v.apoints);
            o.pars.Add("no_move", v.noMove);
            o.pars.Add("do_nothing", v.doNothing);
            o.pars.Add("immortal", v.immortal);
            o.pars.Add("passable", v.passable);
            o.pars.Add("max_dmg_taken", v.maxDmgTaken);
            o.pars.Add("building", v.building);
            
            o.drop = v.drop;
            o.onDeath = v.onDeath;
            o.onDmg = v.onDmg;
            o.dropPerHit = v.dropPerHit;
            o.dynamic = v.dynamic;
            
            o.pars.Add("max_c_exp", 1);
            o.pars.Add("c_exp", 0);
            
            o.labelis.Add(v.origins[0]);
            o.labelis.Add(v.classes[0]);
            o.skills.Add(v.skillBasic);
            o.skills.AddRange(v.skillOthers);
            o.pars.Add("level", 1);

            if (v.level <= 1)
            {
                heroes.Add(o.ID, o);
            }
            else
            {
                heroes.Add(o.ID + v.level, o);
            }
        }

    }

    public RObj CreateItem(string id, int amount, bool withEmpty = false, bool withVisual = false)
    {
        string other = "";
        if (id.IndexOf("shard_") >= 0)
        {
            other = id.Substring(6);
            id = "shard";
        }
        var r = new RObj(id, amount, 1, withEmpty, Vector3.zero, withVisual, ItemType.item);
        r.shardID = other;
        return r;
    }
    
    public RObj CreateMonster(string id, int amount, bool withEmpty = false, bool withVisual = false)
    {
        var r = new RObj(id, amount, 1, withEmpty, Vector3.zero, withVisual, ItemType.monster);
        return r;
    }

    public RObj CreateProjectile(RObj who, string skl, Vector3 dlt, bool withEmpty = true, bool withVisual = true, GameObject mainViz = null)
    {
        var r = new RObj(skl, 1, 1, withEmpty, who.Position + dlt, withVisual, ItemType.projectile, own:who, asMainViz:mainViz);
        
        return r;
    }

    public RObj CreateBuilding(RObj who, string id, Vector3 dlt, bool withEmpty = true, bool withVisual = true)
    {
        var r = new RObj(id, 1, 1, withEmpty, who.Position + dlt, withVisual, ItemType.building, own:who);
        
        return r;
    }
    public RObj CreateAny(string id, bool isEnemy, int amount, GameObject g, string overID = "", GameObject asMainWith = null, bool withVisual = true, bool withEmpty = true, int level = 1)
    {
        if (heroes.ContainsKey(id))
        {
            var res = new RObj(id, 1, level, withEmpty, g.transform.position, withVisual, ItemType.monster, overVis:g, isEnemy:isEnemy, overID:overID, asMainViz:asMainWith);
            return res;
        }
        else if (items.ContainsKey(id))
        {
            var res = new RObj(id, amount, level, withEmpty, g.transform.position, withVisual, ItemType.item, overVis:g, overID:overID, asMainViz:asMainWith);
            return res;
        }
        else if (buildings.ContainsKey(id))
        {
            var res = new RObj(id, 1, level, withEmpty, g.transform.position,withVisual, ItemType.building, overVis:g, overID:overID, asMainViz:asMainWith);
            return res;
        }
        else if (skills.ContainsKey(id))
        {
            var r = new RObj(id, 1, level, withEmpty, g.transform.position, withVisual, ItemType.projectile, overVis:g, overID:overID, asMainViz:asMainWith);
        
            return r;

        }
        else
        {
            //unknown
            var r = new RObj(id, 1, level, true, g.transform.position, true, ItemType.unknown, overVis:g, overID:id, asMainViz:asMainWith);
        
            return r;
        }

        return null;
    }

    public GameObject CreateOnlyVizual(RObj r, Vector3 pos)
    {
        var rr = new GameObject();
        rr.name = r.dbObj.ID;
        rr.transform.parent = MainStates.instance.root;
        r.main = rr;

        var u = r.main.AddComponent<ObjHolder>();
        u.obj = r;
        rr.transform.position = pos;
        //

            if (r.it == ItemType.projectile)
            {
                Debug.Log(r.owner.dbObj + " " + r.dbObj.ID);
                var c = ResourceHolder.instance.GetMeSkillEtc(r.owner.dbObj, r.dbObj.ID);
                
                GameObject ww = null;
                if (c != null) ww = c.proj;
                else
                {
                    ww = new GameObject();
                    ww.name = "EMPT";
                }
                
                var bb = GameObject.Instantiate(ww, r.main.transform);
                if (!r.HasVis("vis_main")) r.visuals.Add("vis_main", bb);
                r.effect = c?.projHit;
                r.selfEffect = c?.effSelf;
            }
            else if (r.it == ItemType.monster)
            {
                {
                    var g = ResourceHolder.instance.monsters[r.dbObj.ID];
                    var bb = GameObject.Instantiate(g, r.main.transform);
                    r.visuals.Add("vis_main", bb);                    
                }
                
            }
            else if (r.it == ItemType.item)
            {
                {
                    var g = ResourceHolder.instance.itemsGO[r.dbObj.ID];
                    var bb = GameObject.Instantiate(g, r.main.transform);
                    r.visuals.Add("vis_main", bb);
                }
            }
            else if (r.it == ItemType.building)
            {
                {
                    var g = ResourceHolder.instance.buildingsGO[r.dbObj.ID];
                    var bb = GameObject.Instantiate(g, r.main.transform);
                    r.visuals.Add("vis_main", bb);
                }
            }
            

        
        return rr;
    }
    public void Init()
    {
        instance = this;
    }
    
    
}
