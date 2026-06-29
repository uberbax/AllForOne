using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Unity.VisualScripting;
using UnityEngine;


public class RandomStringGenerator
{
    // Define the pool of allowed characters (alphanumeric in this case)
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    // Use a single static Random instance to avoid generating the same sequence repeatedly
    private static readonly System.Random random = new System.Random();

    public static string GenerateRandomString(int length)
    {
        // Use a StringBuilder for efficient string concatenation
        StringBuilder sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            // Select a random character from the pool
            int index = random.Next(0, AllowedChars.Length);
            sb.Append(AllowedChars[index]);
        }
        return sb.ToString();
    }
}

//[System.Serializable]
public class Obj
{
    //well basically a name
    public string ID = "";
    
    public Dictionary<string, float> pars = new Dictionary<string, float>();

    [NonSerialized]
    public List<string> labelis = new List<string>();
    [NonSerialized]
    public List<string> skills = new List<string>();
    
    public string drop = "";
    public string onDeath = "";
    public string onDmg = "";
    public string spawn = "";
    public string dropPerHit = "";
    public string dynamic = "";
    
    public List<Bon> extraPars =  new List<Bon>();
    public List<string> buffsApplied = new List<string>();

    public string second = "";
    public string refSkill = "";
    
    public int sizeX = 1;
    public int sizeY = 1;
    public List<Bon> price = new  List<Bon>();

}

//[System.Serializable]
public class RObj
{
    //?
    [NonSerialized]
    public ItemType it;
    
    public string RID = "";
    public bool invertScale = false;
    // should be an id right ?
    [NonSerialized]
    public Obj dbObj;
    
    public Dictionary<string, float> upgradePars = new Dictionary<string, float>();
    public Dictionary<string, float> curPars = new Dictionary<string, float>();

    //?????
    [SerializeReference]
    public List<string> tags = new List<string>();
    public List<string> allyTags = new List<string>();
    public List<string> enemyTags = new List<string>();
    
    [SerializeReference]
    public List<string> labels = new List<string>();
    
    public List<string> META_TAGS = new List<string>();
    
    public List<RObj> inventory = new List<RObj>();
    public List<RObj> buffs = new List<RObj>();
    public List<RObj> actSkills = new List<RObj>();
    public List<RObj> timedBuffs = new List<RObj>();
    
    public RObj usedBy;
    [NonSerialized]
    public RObj owner;
    
    [JsonIgnore]
    public Dictionary<string, GameObject> visuals = new Dictionary<string, GameObject>();
    
    //
    public float pos_x = 0;
    public float pos_y = 0;
    public float pos_z = 0;
    
    public float ref_pos_x = -1;
    public float ref_pos_y = -1;
    
    //
    [JsonIgnore]
    public Vector3 Position
    {
        set
        {
            pos_x = value.x;
            pos_y = value.y;
            pos_z = value.z;
        }

        get
        {
            return new Vector3(pos_x, pos_y, pos_z);
        }
    }
    
    [NonSerialized]
    public GameObject main;
    
    [JsonIgnore]
    public GameObject visMain => visuals["vis_main"];

    [NonSerialized] 
    public GameObject effect;
    
    [NonSerialized] 
    public GameObject selfEffect;

    [NonSerialized]
    public RObj exact;

    [NonSerialized] 
    public FormatDynamic dynamic;

    public string decreaseStatOnDeath = "";

    public string shardID = "";
    
    [NonSerialized]
    public RObj weaponSKill;

    [NonSerialized] 
    public RObj lastDmgFrom;

    public int index = -1;

    [NonSerialized]
    public Vector3 trg = Vector3.zero;

    //on creation
    public List<string> addToWhom = new List<string>();
    public List<Bon> addWhat = new List<Bon>();
    
    public List<Transform> attachables = new List<Transform>();
    
    [NonSerialized]
    public List<string> extraBuffs = new List<string>();
    public bool IsIntersected(int index, int sizeX, int sizeY, RObj who)
    {
        int szx = -1;
        int szy = -1;
        if (this.RID == "main_player" && ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
        {
            szx = (int)ConfigLoader.GetMetaParamValue("main_size_x");
            szy = (int)ConfigLoader.GetMetaParamValue("main_size_y");
        }
        else if (ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
        {
            szx = (int)ConfigLoader.GetMetaParamValue("loot_size_x");
            szy = (int)ConfigLoader.GetMetaParamValue("loot_size_y");
        }

        if (szx < 0) return false;

        int x = index / szy;
        int y = index % szy;
        
        //basically its not intersection, its out of bounds
        if (x - sizeX + 1 < 0 || y - sizeY + 1 < 0)
            return true;
        
        bool ii = true;
        foreach (var v in inventory)
        {
            if (v == who) continue;
            int x1 = v.index / szy;
            int y1 = v.index % szy;
            var inter = !(
                x <= x1 - v.dbObj.sizeX ||
                x1 <= x - sizeX ||
                y <= y1 - v.dbObj.sizeY ||
                y1 <= y - sizeY
            );
            if (inter)
            {
                return true;
                ii = false;
                break;
            }
        }

        return false;
    }
    public int GetFreeIndex(int sizeX, int sizeY)
    {
        int find = -1;
        //int[,] takenInvSpace = new int[10, 10];
        //RecalcInvSpace(takenInvSpace);
        int szx = -1;
        int szy = -1;
        if (this.RID == "main_player" && ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
        {
            szx = (int)ConfigLoader.GetMetaParamValue("main_size_x");
            szy = (int)ConfigLoader.GetMetaParamValue("main_size_y");
        }
        else if (ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
        {
            szx = (int)ConfigLoader.GetMetaParamValue("loot_size_x");
            szy = (int)ConfigLoader.GetMetaParamValue("loot_size_y");
        }

        int mx = 10000;
        if (szx >= 0)
            mx = szx * szy;
        
        for (int i = 0; i < mx; i++)
        {
            if (szx < 0)
            {
                var b = inventory.Find(x => x.index == i);
                if (b == null) return i;                
            }
            else
            {
                int x = i / szy;
                int y = i % szy;
                //we need to check bounding boxes
                if (x - sizeX + 1 < 0 || y - sizeY + 1 < 0)
                    continue;

                bool ii = true;
                foreach (var v in inventory)
                {
                    int x1 = v.index / szy;
                    int y1 = v.index % szy;
                    var inter = !(
                                  x <= x1 - v.dbObj.sizeX ||
                                  x1 <= x - sizeX ||
                                  y <= y1 - v.dbObj.sizeY ||
                                  y1 <= y - sizeY
                                  );
                    if (inter)
                    {
                        ii = false;
                        break;
                    }
                }

                if (ii) return i;
            }

        }

        return find;
    }

    public void RecalcInvSpace(int[,] space)
    {
        int szx = 10;
        int szy = 10;
        if (this.RID == "main_player" && ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
        {
            
        }
        else if (ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
        {
            
        }
        
        foreach (var v in inventory)
        {
            
        }
    }
    
    
    public RObj GetSKillReplace(RObj skl)
    {
        if (skl.dbObj.ID == "basic_melee" || skl.dbObj.ID == "basic_range")
        {
            var f = inventory.Find(x => x.GetPar("used_slot") == MainStates.slots["weapon"]);
            if (f != null && f.dbObj.refSkill != "")
            {
                if (weaponSKill == null)
                    weaponSKill = DatabaseAll.instance.CreateProjectile(this, f.dbObj.refSkill, Vector3.zero, false, false);
                if (skl.GetPar("action_req") > 0) weaponSKill.SetPar("action_req", 1);
                return weaponSKill;
            }
            else return skl;
        }
        
        return skl;
    }
    
    public void AddMeta(string what)
    {
        META_TAGS.Add(what);
    }
    
    public void RecreateMain(Vector3 pos)
    {
        
    }
    
    public void AdjustPosition()
    {
        if (ConfigLoader.GetMetaParamValue("mode_isometric") > 0 ||
            ConfigLoader.GetMetaParamValue("mode_manhattan") > 0 ||
            ConfigLoader.GetMetaParamValue("mode_hex") > 0
           )
        {
            var v = PositionSetter.instance.GetClosestPos(Position);
            main.transform.position = v.Item3;
            Position = v.Item3;

            ref_pos_x = v.Item1;
            ref_pos_y = v.Item2;
        }
    }
    
    public void ResetCDs()
    {
        foreach (var v in actSkills)
        {
            v.upgradePars["cd"] = 0;
        }
    }
    public void RecalcPars()
    {
        if (it == ItemType.projectile && dbObj.pars["instant"] == 1)
        {
            if (owner == null)
            {
                Debug.LogError("NO OWNER");
                owner = MainStates.instance.mainPlayer;
            }
            curPars.TryAdd("attack", owner.GetPar("attack") * dbObj.pars["attack_prc"] + dbObj.pars["attack"]);


            upgradePars.TryAdd("cd", 0);
            return;
        }

        curPars.Clear();
        if (dbObj != null)
        {
            var lvl = GetPar("level");
            if (lvl == 0) lvl = 1;
            
            foreach (var v in dbObj.pars)
            {
                float vv = v.Value;
                if (v.Key == "attack" || v.Key == "health" || v.Key == "max_health")
                {
                    vv = vv * MathF.Pow(1.1f, lvl - 1);
                }
                curPars.TryAdd(v.Key, vv);
            }
        }

        foreach (var v in upgradePars)
        {
            AddFinal(v.Key, v.Value);
        }

        foreach (var v in inventory)
        {
            if (!v.upgradePars.ContainsKey("used_slot"))
            {
                v.upgradePars.Add("used_slot", -1);    
            }
            
            //thwbbb
            if (v.upgradePars["used_slot"] < 0 || v.it == ItemType.projectile) continue;
            foreach (var a in v.dbObj.pars)
            {
                if (a.Key == "amount") continue;
                if (a.Key == "rarity") continue;
                AddFinal(a.Key, v.GetPar(a.Key));
            }
        }
        
        foreach (var v in buffs)
        {
            //if (v.upgradePars["used_slot"] < 0) continue;
            foreach (var a in v.dbObj.pars)
            {
                if (a.Key == "amount") continue;
                AddFinal(a.Key, v.GetPar(a.Key));
            }
        }
        //debuffs
        foreach (var v in timedBuffs)
        {
            if (v.GetPar("instant") == 1) continue;
            foreach (var a in v.dbObj.pars)
            {
                if (a.Key == "amount") continue;
                AddFinal(a.Key, v.GetPar(a.Key));
            }
        }
        
        //global buffs from main player
        if (RID != "main_player" && tags.Count > 0 && tags[0] == "player")
        {
            //apply global buffs from player
            foreach (var v in MainStates.instance.mainPlayer.buffs)
            {
                if (v.dbObj.ID.IndexOf("global") >= 0)
                {
                    if (v.dbObj.labelis.Count > 0 && !v.dbObj.labelis.Contains(dbObj.ID)) continue;
                    foreach (var v1 in  v.dbObj.pars)
                    {
                        if (v1.Key == "amount") continue;
                        AddFinal(v1.Key, v.GetPar(v1.Key));
                    }
                    
                }
            }
        }
        
        if (upgradePars.ContainsKey("registered_damage"))
            curPars.TryAdd("registered_damage", upgradePars["registered_damage"]);
        
    }

    public float GetPar(string val)
    {
        //battle_power
        if (val == "battle_power")
        {
            curPars["battle_power"] = curPars["attack"] + curPars["health"] + curPars["def"];
        }
        
        
        if (curPars.ContainsKey(val))
        {
            if (val == "health")
                return curPars[val] - curPars["registered_damage"];
            else return curPars[val];
        }
        else
        {
            if (upgradePars.ContainsKey(val))
            {
                return upgradePars[val];
            }
            else if (dbObj == null)
            {
                return 0;
            }
            else if (dbObj.pars.ContainsKey(val))
            {
                return dbObj.pars[val];
            }
            return 0;
        }
    }

    public void SetPar(string key, float val)
    {
        if (upgradePars.ContainsKey(key))
        {
            upgradePars[key] = val;
        }
        else
        {
            upgradePars.Add(key, val);
        }
        RecalcPars();
    }

    public void AddFinal(string key, float val)
    {
        if (curPars.ContainsKey(key))
        {
            curPars[key] += val;
        }
        else
        {
            curPars.Add(key, val);
        }
    }
    public void ChangePar(string key, float val, bool doRecalc = true)
    {
        if (upgradePars.ContainsKey(key))
        {
            upgradePars[key] += val;
        }
        else
        {
            upgradePars.Add(key, val);
        }

        if (key == "exp")
        {
            float l0 = GetPar("level");
            MainStates.instance.GetMeExpPars(this, out float rat, out float cr, out float cm, out float lvl);
            upgradePars["c_exp"] = cr;
            upgradePars["max_c_exp"] = cm;

            if (lvl != l0)
            {
                EventManager.INV("lvlup", new ArgPass{who = this});
                upgradePars["level"] = lvl;
            }
            
        }
        
        if (doRecalc)
            RecalcPars();
    }
    

    public void AddViz(string what)
    {
        string other = "";
        string change = "";
        if (what.IndexOf('#') >= 0)
        {
            other = what.Substring(what.IndexOf('#') + 1);
            what = what.Substring(0, what.IndexOf('#'));
        }
        
        if (visuals.ContainsKey(what)) return;

        if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0)
        {
            if (what == "coll") change = "coll3D";
            if (what == "select") change = "select3D";
            if (what == "pick") change = "pick3D";
            if (what == "receive") change = "receive3D";
            if (what == "blood") change = "blood3D";
            
        }
        
        if (!ResourceHolder.instance.XD.ContainsKey(what))
        {
            visuals.Add(what, null);
            return;
        }
        
        //Debug.Log(what + " " + main);
        GameObject nn = null;
        if (what == "buyable")
        {
            nn = GameObject.Instantiate(ResourceHolder.instance.XD[change != "" ? change : what]);
            nn.transform.parent = main.transform;
            nn.transform.localPosition = Vector3.zero;
        }
        else
        {
            nn = GameObject.Instantiate(ResourceHolder.instance.XD[change != "" ? change : what], main.transform);
            nn.transform.localPosition = Vector3.zero;
        }


        visuals.Add(what, nn);

        if (other != "")
        {
            nn.GetComponent<ComponentBehavior>().Set(other);
            nn.SendMessage("AfterSet", other, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void RemoveViz(string what)
    {
        if (visuals.ContainsKey(what))
        {
            GameObject.Destroy(visuals[what]);
            visuals.Remove(what);
        }

    }

    public bool HasVis(string what)
    {
        return visuals.ContainsKey(what);
    }


    public RObj Clone()
    {
        var str = JsonConvert.SerializeObject(this);
        var y = JsonConvert.DeserializeObject<RObj>(str);
        y.dbObj = dbObj;
        y.it = it;
        //var g = (RObj)JsonConvert.DeserializeObject(str);
        for (int i = 0; i < y.actSkills.Count; i++)
        {
            y.actSkills[i].it = actSkills[i].it;
            y.actSkills[i].dbObj = actSkills[i].dbObj;
            y.actSkills[i].owner = y;
        }
        for (int i = 0; i < y.buffs.Count; i++)
        {
            y.buffs[i].it = buffs[i].it;
            y.buffs[i].dbObj = buffs[i].dbObj;
            y.buffs[i].owner = y;
        }
        for (int i = 0; i < y.timedBuffs.Count; i++)
        {
            y.timedBuffs[i].it = timedBuffs[i].it;
            y.timedBuffs[i].dbObj = timedBuffs[i].dbObj;
            y.timedBuffs[i].owner = y;
        }
        for (int i = 0; i < y.inventory.Count; i++)
        {
            y.inventory[i].it = inventory[i].it;
            y.inventory[i].dbObj = inventory[i].dbObj;
            y.inventory[i].owner = y;
        }

        y.dynamic = dynamic;
        
        return y;
        //return Cloner.Clone(this);
    }

    public RObj()
    {
        
    }
    public RObj(string id, ItemType tp)
    {
        RID = id;
        it = tp;
    }
    
    public RObj(string id, int amount, int level, bool withEmpty, Vector3 position, bool withVisual, ItemType tp, string overID = "", RObj own = null, bool isEnemy = false, GameObject overVis = null, GameObject asMainViz = null)
    {
        owner = own;
        it = tp;
        
        if (tp == ItemType.monster)
            dbObj = DatabaseAll.instance.heroes[id];
        else if (tp == ItemType.projectile)
            dbObj = DatabaseAll.instance.skills[id];
        else if (tp == ItemType.item)
            dbObj = DatabaseAll.instance.items[id];
        else if (tp == ItemType.building)
            dbObj = DatabaseAll.instance.buildings[id];


        if (dbObj != null && dbObj.dynamic != "")
        {
            dynamic = ConfigLoader.Instance.allDynamic[dbObj.dynamic];
        }

        upgradePars.Add("amount", amount);
        upgradePars.Add("level", level > 0 ? level-1 : 0);
        upgradePars.Add("registered_damage", 0);
        upgradePars.Add("registered_mana", 0);
        upgradePars.Add("used_slot", -1);
        upgradePars.Add("exp", 0);
        
        if (overID == "")
        {
            RID = id + "_" + RandomStringGenerator.GenerateRandomString(5);
        }
        else
        {
            RID = overID;
        }
        
        if (tp == ItemType.monster)
        {
            foreach (var v in dbObj.skills)
            {
                var g = DatabaseAll.instance.CreateProjectile(this, v, Vector3.zero, false, false);
                actSkills.Add(g);
            }
            
            if (isEnemy)
            {
                tags.Add("enemy");
                allyTags.Add("enemy");
                enemyTags.Add("player");
            }
            else
            {
                tags.Add("player");
                allyTags.Add("player");
                enemyTags.Add("enemy");
                
            }
        }
        
        
        RecalcPars();        
        
        if (withEmpty)
        {
            if (asMainViz != null)
            {
                var uр = asMainViz.AddComponent<ObjHolder>();
                uр.obj = this;
                main = asMainViz;
            }
            else
            {
                var rr = new GameObject();
                rr.name = dbObj != null ? dbObj.ID : id;
                rr.transform.parent = MainStates.instance.root;
                main = rr;

                var u = main.AddComponent<ObjHolder>();
                u.obj = this;
                rr.transform.position = position;
                if (overVis != null && overVis.transform.parent != null)
                {
                    rr.transform.SetParent(overVis.transform.parent);
                }                
            }


        }

        if (withVisual)
        {
            if (tp == ItemType.projectile)
            {
                var c = ResourceHolder.instance.GetMeSkillEtc(owner.dbObj, id);
                GameObject proj = ResourceHolder.instance.emptyProj;
                GameObject projHit = ResourceHolder.instance.emptyProj;
                if (c != null && c.proj != null) proj = c.proj;
                var bb = GameObject.Instantiate(proj, main.transform);
                visuals.Add("vis_main", bb);   
                effect = c == null ? projHit : c.projHit;
                selfEffect = c == null ? null : c.effSelf;
            }
            else if (tp == ItemType.monster)
            {
                if (overVis != null)
                {
                    if (overVis != asMainViz)
                    {
                        overVis.transform.parent = main.transform;
                        visuals.Add("vis_main", overVis);
                    }
                    else
                    {
                        visuals.Add("vis_main", overVis);
                    }
                }
                else
                {
                    var g = ResourceHolder.instance.monsters[dbObj.ID];
                    var bb = GameObject.Instantiate(g, main.transform);
                    var hh = bb.GetComponent<VizRedirect>();
                    if (hh != null && hh.visMain != null)
                        visuals.Add("vis_main", hh.visMain);
                    else visuals.Add("vis_main", bb);                    
                }
                
            }
            else if (tp == ItemType.item)
            {
                if (overVis != null)
                {
                    overVis.transform.parent = main.transform;
                    visuals.Add("vis_main", overVis); 
                }
                else
                {
                    var g = ResourceHolder.instance.itemsGO[dbObj.ID];
                    var bb = GameObject.Instantiate(g, main.transform);
                    visuals.Add("vis_main", bb);
                }
            }

            
        }

        if (tp == ItemType.projectile)
        {
            upgradePars.TryAdd("cd", 0);
        }
        

        
        this.Position = position;
        //Debug.Log(RID);
        MainStates.instance.all.Add(RID, this);
        
        if (tp == ItemType.monster)
        {
            upgradePars.TryAdd("regenEvery", 1);
            MainStates.instance.CheckDynamicsCreate(this);
        }

        if (tp == ItemType.projectile)
        {
            //add on creation
            List<string> adds = new List<string>();
            List<Bon> parsToAdd = new List<Bon>();
            bool isWeaponSkill = false;
            
            //thwbbb
            if (owner == null)
                owner = MainStates.instance.mainPlayer;
            
            if (owner.weaponSKill != null && dbObj.ID == owner.weaponSKill.dbObj.ID)
                isWeaponSkill = true;
            
            foreach (var v in owner.buffs)
            {
                foreach (var v1 in v.addToWhom)
                {
                    if (v1 == "weapon_skill" && isWeaponSkill)
                    {
                        //norm
                    }
                    else if (v1 != dbObj.ID) continue;
                
                    foreach (var v2 in v.addWhat)
                    {
                        if (v2.Key != "")
                            adds.Add(v2.Key);
                        else
                            parsToAdd.Add(v2);
                    }
                }
            }

            foreach (var v in parsToAdd)
            {
                ChangePar(v.Val2, v.Value);
            }
            
            extraBuffs.AddRange(adds);
        }

        if (tp == ItemType.projectile && withEmpty)
        {
            if (GetPar("ricochet") > 0)
            {
                var kk = main.AddComponent<CircleCollider2D>();
                var ll = main.AddComponent<XDricocol>();
                var rigidbody = main.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0;
                main.layer = LayerMask.NameToLayer("Bounce");

                kk.radius /= 3;
            }
        }
    }

    public void Destroy()
    {
        MainStates.instance.all.Remove(RID);
        GameObject.Destroy(this.main);
    }


    public void SetScale(bool right)
    {
        var cc = visMain.transform.localScale;
        if (invertScale) right = !right;
        if (right)
        {
            visMain.transform.localScale = new Vector3(-Mathf.Abs(cc.x), cc.y, cc.z);
        }
        else
        {
            visMain.transform.localScale = new Vector3(Mathf.Abs(cc.x), cc.y, cc.z);
        }
    }
    
}

[System.Serializable]
public class Bon
{
    public string Key = "";
    public int Value = 0;
    //also in addWhat it is a param
    public string Val2 = "";
    public int Val3 = 0;
}

[System.Serializable]
public class Bon1
{
    public string Key = "";
    public float Value = 0;
    public string Val2 = "";
}

[System.Serializable]
public class Dops
{
    public GameObject what;
    public float val1;
    public float val2;
}

[System.Serializable]
public class ArgPass
{
    public int num;
    public RObj who;
    public string what = "";
    public string what1 = "";
    public GameObject go;
    public Vector3 pos;
    public RObj who2;
}

[System.Serializable]
public enum ItemType
{
    item,
    monster,
    other,
    building,
    gear,
    projectile,
    note,
    task,
    
    unknown
}

[System.Serializable]
public enum RarityType
{
    common = 0,
    uncommon = 1,
    rare = 2,
    epic = 3,
    legendary = 4,
    mythic = 5
}

[System.Serializable]
public enum ItemSlot
{
    none,
    other,        
    body,
    head,
    weapon,
    boots,
    shield,
    gloves,
    ring,
        
    upgrade,
    tempItem,
    amulet,
    secondary,
    all,
        
    slot1,
    slot2,
    slot3

}

[System.Serializable]
public class Seto
{
    public string name = string.Empty;
    public int group = 0;
    public float weight = 1;
    public int amount1 = 1;
    public int amount2 = 1;
    public int shard = 0;
    public string item;
}

[System.Serializable]
public class OneLoot
{
    public string what;
    public int amount;
    public int tier;
    public string lootSet;
    public string lootCond = "death";
}

public enum ElementType
{
    physical,
    magic,
    pure,

    water,
    fire,
    wood,
    light,
    dark,
    electro,
    ice
}

public enum TravelType
{
    linear = 0,
    parabolik = 1,
    from_above = 2,
    center_spawn = 3,
    custom = 4
}