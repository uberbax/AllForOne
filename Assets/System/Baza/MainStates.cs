using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GameDevWare.Dynamic.Expressions.CSharp;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;
using Random = UnityEngine.Random;

public class MainStates : MonoBehaviour
{
    public int curSp = 0;
    public GameObject wall;
    public string tgBattle = "battle";
    public float SCENE_Z = -3;
    public float LIFT_PROJ = 0;
    public Camera otherCamera;
    public Transform cancelLine;
    public Transform lastCard;
    
    public GameObject UI_skillsAssign;
    
    public GameObject UI_inventory;
    public GameObject UI_inventorySell;
    public GameObject UI_descr;
    public GameObject UI_second;
    public GameObject UI_secondBuy;
    public GameObject UI_noMoney;
    public GameObject UI_skilChose;
    public GameObject UI_infoMon;
    public GameObject UI_smalls;
    public GameObject UI_buildings;
    public GameObject UI_skills;
    public GameObject UI_charEq;
    public GameObject UI_heroRender;
    public GameObject UI_dynamikPrice;
    public GameObject UI_battleSelect;
    public GameObject UI_squadList;
    public GameObject UI_unitsPlaced;
    public GameObject UI_buildingInterface;
    
    
    public StringObjectDictionary GLOBAL_OBJECTS;
    public GameObject MAIN_GAME;
    
    public Transform dragLo;
    public Transform dragHi;
    public Transform dragZone;

    public GameObject highlight;
    public Transform availableMeRoot;
    public Transform availableEnemyRoot;
    
    
    public Transform dropContainer;

    public RObj lastTooltip;
    public RObj curClick;
    public RObj curLoot;
    public RObj lastAllySelected;
    public GameObject lastBattleTrigger;
    
    public List<RObj> curSmalls;
    public Transform posClick;
    public bool isPaused;
    public bool inBattle;

    public List<WaveSpawner> spawners = new List<WaveSpawner>();
    public RObj mainPlayer => all["main_player"];

    public static Dictionary<string, float> slots = new Dictionary<string, float>()
    {
        { "none", -1},
        { "body", 0},
        { "weapon", 1},
        { "helmet", 2},
        { "boots", 3},
        { "amulet", 4},
        { "ring", 5},
    };
    
    public static Dictionary<string, float> rarity = new Dictionary<string, float>()
    {
        { "common", 0},
        { "uncommon", 1},
        { "rare", 1},
        { "epic", 2},
        { "legendary", 3},
        { "mythic", 4}
    };

    public Dictionary<string, RObj> all = new Dictionary<string, RObj>();
    public Dictionary<string, RObj> empties = new Dictionary<string, RObj>();
    
    public List<RObj> combats = new List<RObj>();

    public static MainStates instance;
    public Transform root;
    
    public PlayerData playerData;

    public Vector3 dlt = Vector3.zero;

    public GameObject[,] map;
    public Transform[,] mappingPositions;

    private List<float> expCurve = new List<float>{0,100,200,300,400,500,600,700};

    public string curState = "none";


    //for shoot ?
    public bool freeTargeting = false;

    public long lastAutoRewardTime = 0;
    
    //params !!!
    public static bool lootTakeShowReward = false;
    public static bool disappearLootOnTake = false;
    public static bool allowAutoIterate = true;
    public static string metaCreateLevel = "";
    public static int maxMove = 1;
    
    //
    public Transform trashRoot;
    
    public Vector3 GetRndFree(Vector3 pos, float range)
    {
        if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0)
        {
            var free = PositionSetter.instance.GetAllFreeSquares(pos, (int)range);
            var g = free[Random.Range(0, free.Count)];
            return PositionSetter.instance.floors[g.Item1, g.Item2].transform.position;
        }
        else
        {
            float r = Random.Range(0.5f, range);
            float al = Random.Range(0, 2 * Mathf.PI);
            Vector3 ans = pos;

            for (int i = 0; i < 10; i++)
            {
                if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
                {
                    ans = pos + new Vector3(r * MathF.Sin(al), r * Mathf.Cos(al), 0);
                }

                if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0)
                {
                    ans = pos + new Vector3(r * MathF.Sin(al), 0, r * Mathf.Cos(al));
                }
                
                bool isTouching = Physics2D.OverlapCircle(ans, 0.3f, 1 <<  LayerMask.NameToLayer("Nopass"));
                if (!isTouching) {
                    // The circle is NOT inside or touching any colliders in that layer
                    break;
                }
            }

            return ans;
        }
    }


    public RObj FindClosestObj(RObj from, string id, bool isEnemy, bool isNeutral)
    {
        float dst = 1000;
        RObj ans = null;
        foreach (var v in combats)
        {
            if (v.dbObj.ID != id) continue;
            if (isNeutral && !v.tags.Contains("neutral")) continue;
            else
            {
                var g = CompareTags(v, from);
                if (g && isEnemy || !g && !isEnemy)
                {
                    continue;                
                }                
            }
            
            var h = GetDistance(from, v, out float d1);
            if (h < dst)
            {
                dst = h;
                ans = v;
            }

        }
        
        return ans;
    }
    
    public void PickDrop(RObj item)
    {
        UtilsControl.Instance.MoveTo(item.main.transform, 20, dropContainer.position, () =>
        {
            AddItem(mainPlayer, item);
        }, null);
        
    }
    public void GetMeExpPars(RObj obj, out float rat, out float cr, out float cm, out float level)
    {
        var cur = obj.upgradePars["exp"];
        int l = -1;
        for (int i = 0; i < expCurve.Count; i++)
        {
            if (cur < expCurve[i])
            {
                l = i;
                break;
            }
        }

        if (l < 0)
        {
            level = expCurve.Count;
            rat = 1;
            cr = expCurve[(int)level-1];
            cm = expCurve[(int)level-1];
        }
        else
        {
            level = l;
            
            cr = cur - expCurve[l-1];
            cm = expCurve[l];
            
            rat = cr / cm;            
        }

    }
    private void Awake()
    {
        instance = this;
        EventManager.SUB("PARSE_ENDED", B);
        DragObject.onEndDragGlobal = OnEndDrag;
        EventManager.SUB("next_hero", NextHero);
        EventManager.SUB("battle_start", BattleStarted);

        if (trashRoot == null)
        {
            var g = new GameObject();
            g.name = "TrahsRoot";
            g.transform.parent = transform;
            g.transform.localPosition = Vector3.zero;
            trashRoot = g.transform;
        }
    }

    private void BattleStarted(ArgPass obj)
    {
        TimeManager.instance.tm = 0;
        for (int i = 0; i < curSp; i++)
        {
            var v = spawners[i];
            v.spawnByCommand = false;
            v.IsDone = false;
            v.spawnAsBattle = true;
        }
    }

    public void NextHero(ArgPass b)
    {
        RObj kk = lastAllySelected == null ? mainPlayer : lastAllySelected;
        int l = -1;
        var cc1 = combats.FindAll(x => x.tags.Contains("player"));
        for (int i = 0; i < cc1.Count; i++)
            if (cc1[i] == kk)
                l = i;
        
        l = (l+1)%cc1.Count;
        lastAllySelected = cc1[l];
        UI_charEq.GetComponent<UIfiller>().OnEnable();
        UI_heroRender.GetComponent<UIfiller>().OnEnable();
        UI_inventory.GetComponent<UIfiller>().OnEnable();
        UI_skills.GetComponent<UIfiller>().OnEnable();
        
    }

    public void OnEndDrag(Transform t, Vector3 dlt)
    {
        UIfiller gf = null;
        Transform gs = null;
        float dst = 1e+10f;
        foreach (var v in UIfiller.instances)
        {
            if (!v.gameObject.activeInHierarchy) continue;
            
            if (v.slots.Count > 0)
            {
                for (int i = 0; i < v.slots.Count; i++)
                {
                    var kk = v.slots[i].position - t.position - dlt;
                    kk.z = 0;
                    if (kk.magnitude < dst)
                    {
                        dst = kk.magnitude;
                        gf = v;
                        gs = v.slots[i];
                    }
                }    
            }

            //if (gf != null) break;

            if (v.root != null)
            {
                for (int i = 0; i < v.root.childCount; i++)
                {
                    var kk = v.root.GetChild(i).position - t.position - dlt;
                    kk.z = 0;
                    if (kk.magnitude < dst)
                    {
                        dst = kk.magnitude;
                        gf = v;
                        gs = v.root.GetChild(i);
                    }
                }
            }
            
            //if (gf != null) break;

        }
        
        //Debug.Log(dst + gs.name);
        if (dst > 70) return;
        //filler is compatible
        var tf = t.GetComponent<ObjHolder>().filler;
        var ti = t.GetComponent<ObjHolder>().obj;
        int iver = gs.GetSiblingIndex();
        if (tf == gf && (ti == null || iver == ti.index))
        {
            tf.OnEnable();
            return;
        }
        if (tf.compatibility != gf.compatibility) return;


        var d = gs.GetComponent<UnoSlot>();
        if (d != null)
        {
            if (d.eqNum != ti.dbObj.pars["slot"]) return;
            GetCommandResult(tf.command, tf.param, tf.transform, "del", ti);
            GetCommandResult(gf.command, gf.param, tf.transform, "add", ti);
            Equip(lastAllySelected == null ? mainPlayer : lastAllySelected, ti);
            //event ?
            UI_skills.GetComponent<UIfiller>().OnEnable();
        }
        else
        {
            GetCommandResult(tf.command, tf.param, null, "del", ti);
            ti.index = -1;
            ti.index = iver;
            GetCommandResult(gf.command, gf.param, null, "add", ti, !tf.ignoreInvAny);
            ti.SetPar("used_slot", -1);
            ti.RecalcPars();
            //event?
            UI_skills.GetComponent<UIfiller>().OnEnable();
        }

        
        tf.OnEnable();
        if (tf != gf) gf.OnEnable();

        if (UI_inventory.activeSelf)
        {
            UI_inventory.GetComponent<UIfiller>().OnEnable();
        }
    }

    public void B(ArgPass evt)
    {
        foreach (var v in ConfigLoader.Instance.allDynamic)
        {
            if (v.Value.subscribe != "x" && v.Value.subscribe != "")
            {
                EventManager.SUB(v.Value.subscribe, (x) =>
                {
                    if (v.Value.filterNum == -1 || v.Value.filterNum != -1 && x.num == v.Value.filterNum)
                        ExecuteDone(v.Value);
                });
            }
        }
    }

    public void CreateLevelAtPos(int x, int y, string level)
    {
        PositionSetter.instance.ClearWalls();
        var a = ConfigLoader.Instance.levels[level];
        int curSp = 0;
        int u = 0;
        for (int i = 0; i < a.n; i++)
        {
            for (int j = 0; j < a.m; j++)
            {
                if (a.map[i][j].pars[0] == "x") continue;
                
                if (a.map[i][j].pars[0] == "1")
                {
                    var e = Instantiate(wall, PositionSetter.instance.wallRoot);
                    e.transform.position = PositionSetter.instance.floors[x + a.n - i - 1,y + j].transform.position;
                    PositionSetter.instance.walls[x + a.n - i - 1, y + j] = e.transform;
                }
                else if (a.map[i][j].pars[0] == "s")
                {
                    spawners[curSp].AssignBattle(a.map[i][j].pars[1]);
                    spawners[curSp].transform.position = PositionSetter.instance.floors[x + a.n - i - 1,y + j].transform.position;
                    curSp++;
                    
                }
                else
                {
                    var jj = PositionSetter.instance.floors[x + a.n - i - 1, y + j].transform;
                    var gg = a.map[i][j].pars;
                    var ll = WaveSpawner.instance.DoSpawnAny(new List<Bon> { new Bon { Key = gg[0], Value = 1 } }, "enemy",
                        null, null, false, jj.position, jj.position);

                    if (metaCreateLevel != "")
                    {
                        for (int h = 0 ; h < ll.Count; h++)
                            ll[h].META_TAGS.Add(metaCreateLevel);
                    }
                }
                
            }
        }
        
        this.curSp = curSp;
    }
    
    public void ReplaceObj(RObj who, string newId)
    {
        if (who.dbObj.ID == newId) return;
        who.RemoveViz("vis_main");
        var f = DatabaseAll.instance.heroes[newId];
        who.dbObj = f;
        var ee = Instantiate(ResourceHolder.instance.monsters[newId], who.main.transform);
        who.visuals.Add("vis_main", ee);
    }

    public bool HaveAmount(List<Bon> what)
    {
        bool res = true;
        foreach (var v in what)
        {
            var g = GetItemsCount(v.Key);
            if (g < v.Value)
            {
                return false;
            }
        }

        return res;
    }

    public bool HaveDynamic(string id)
    {
        return (playerData.dynTaken.Contains(id));
    }

    public void DelItems(List<Bon> what)
    {
        var a = all["main_player"].inventory;
        List<RObj> toDelete = new List<RObj>();
        foreach (var v in what)
        {
            var hh = a.Find(x => x.dbObj.ID == v.Key);
            if (hh != null)
            {
                hh.ChangePar("amount", -v.Value, true);
                if (hh.GetPar("amount") <= 0)
                    toDelete.Add(hh);
            }
        }
        
        foreach (var v in toDelete)
            a.Remove(v);
    }

    public void Buy(List<Bon> reqs, Action onFail, Action onSuccess)
    {
        if (HaveAmount(reqs))
        {
            DelItems(reqs);
            if (onSuccess != null) onSuccess();
        }
        else
        {
            if (onFail != null) onFail();
        }
    }

    public string CalculateValue(string dynamicID)
    {
        string calculated = dynamicID;
        var c1 = dynamicID.IndexOf("{");
        var c2 = dynamicID.IndexOf("}");
        string other = dynamicID.Substring(c2 + 1);
        if (c1 >= 0)
        {
            var str  = dynamicID.Substring(c1+1, c2 - c1 - 1);
            //it can be also not a stat
            var c3 = str.IndexOf(".");
            if (c3 >= 0)
            {
                var  str2 = str.Substring(0, c3);
                str = str.Substring(c3 + 1);
                if (str2 == "meta")
                {
                    str = ConfigLoader.GetMetaParamValue(str).ToString();
                }
                
                calculated = dynamicID.Substring(0, c1) + str + other;
            }
            else
            {
                var p = ModelStatistics.instance.GetStatValue(str);
                calculated = dynamicID.Substring(0, c1) + p + other;
            }
        }
        
        c1 = calculated.IndexOf("{");
        if (c1 >= 0) calculated = CalculateValue(calculated);
        
        return calculated;
    }

    public float CalculateValueFloat(string dynamicID)
    {
        var ss = CalculateValue(dynamicID);
        var f = CSharpExpression.Evaluate<float>(ss);

        return f;
    }

    public void AcquireSkill(RObj who, string sklName)
    {
        var tt = sklName[sklName.Length - 1];
        if (tt >= '0' && tt <= '9')
        {
            var sklName1 = UtilsControl.GetPrev(sklName);
            var g = who.actSkills.Find(x => x.dbObj.ID == sklName1);
            if (g != null)
            {
                who.actSkills.Remove(g);
            }
        }
        //
        var h0 = DatabaseAll.instance.CreateProjectile(who, sklName, Vector3.zero, false, false); 
        who.actSkills.Add(h0);
    }
    
    public void AcquirePasSkill(RObj who, string sklName)
    {
        var tt = sklName[sklName.Length - 1];
        if (tt >= '0' && tt <= '9')
        {
            var sklName1 = sklName.Substring(0, sklName.Length - 1);
            var g = who.buffs.Find(x => x.dbObj.ID == sklName1);
            if (g != null)
            {
                who.buffs.Remove(g);
            }
        }
        //
        var h0 = DatabaseAll.instance.CreateProjectile(who, sklName, Vector3.zero, false, false); 
        who.buffs.Add(h0);
    }

    public void CheckDynamicsCreate(RObj who)
    {
        for (int i = 0; i < playerData.dynTaken.Count; i++)
        {
            var s = ConfigLoader.Instance.allDynamic[playerData.dynTaken[i]];
            if (s.create.IndexOf("create") < 0) continue;

            var ss = s.create.Substring(7);
            if (who.dbObj != null && ss == who.dbObj.ID)
            {
                ExecuteDone(s, true, who);
            }
            else if (ss == who.RID)
            {
                ExecuteDone(s, true, who);
            }
        }
    }

    public List<Bon> GetInventoryBon(RObj mon)
    {
        List<Bon> res = new List<Bon>();
        foreach (var v in mon.inventory)
        {
            if (v.shardID != "")
            {
                res.Add(new Bon{Key = "shard_" + v.shardID, Value = (int)v.GetPar("amount")});
            }
            else res.Add(new Bon{Key = v.dbObj.ID, Value = (int)v.GetPar("amount")});
        }

        return res;
    }
    
    public void ExecuteDone(string id)
    {
        ExecuteDone(ConfigLoader.Instance.allDynamic[id]);
    }
    public void ExecuteDone(FormatDynamic res, bool force = false, RObj who = null, GameObject whoActivate = null, Transform par = null)
    {
        Debug.Log("Executing Resolve");
        if (res.create != "" && !force) return;
        
        
        //for (int i = 0; i < res.disabledObjects.Count; i++)
        //    res.disabledObjects[i].SetActive(false);
        
        //for (int i = 0; i < res.enabledObjects.Count; i++)
        //    res.disabledObjects[i].SetActive(true);

        List<Bon> reso = new List<Bon>();
        
        //recals items get
        for (int i = 0; i < res.itemsGet.Count; i++)
        {
            if (res.itemsGet[i].Val2 != "")
            {
                res.itemsGet[i].Value = (int)CalculateValueFloat(res.itemsGet[i].Val2);
            }
            reso.Add(res.itemsGet[i]);
        }
        
        for (int i = 0; i < res.parUpgrade.Count; i++)
        {
            if (res.parUpgrade[i].Val2 != "")
            {
                res.parUpgrade[i].Value = (int)CalculateValueFloat(res.parUpgrade[i].Val2);
            }
        }
        
        for (int i = 0; i < res.parentUpgrade.Count; i++)
        {
            if (res.parentUpgrade[i].Val2 != "")
            {
                res.parentUpgrade[i].Value = (int)CalculateValueFloat(res.parentUpgrade[i].Val2);
            }
        }
        
        for (int i = 0; i < res.statsInc.Count; i++)
        {
            if (res.statsInc[i].Val2 != "")
            {
                res.statsInc[i].Value = (int)CalculateValueFloat(res.statsInc[i].Val2);
            }
        }
        
        for (int i = 0; i < res.statsExact.Count; i++)
        {
            if (res.statsExact[i].Val2 != "")
            {
                res.statsExact[i].Value = (int)CalculateValueFloat(res.statsExact[i].Val2);
            }
        }
        
        
        if (res.itemsGet.Count > 0)
            AddItems(res.itemsGet, who);

        if (res.statsInc.Count > 0)
        {
            IncStats(res.statsInc, mainPlayer);
        }
        
        if (res.statsExact.Count > 0)
        {
            SetStats(res.statsExact, mainPlayer);
        }
        
        if (res.parUpgrade.Count > 0)
        {
            IncPars(res.parUpgrade, who == null ? mainPlayer : who);
        }
        
        if (res.parentUpgrade.Count > 0)
        {
            var x = par.GetComponent<ObjHolder>().obj;
            IncPars(res.parentUpgrade, x);
        }

        if (res.eventTrigger != "")
        {
            EventManager.INV(res.eventTrigger, new ArgPass{what = res.eventVal, what1 = res.id, num = res.eventNum});
        }

        if (res.skillUnlock.Count > 0)
        {
            foreach (var v in res.skillUnlock)
            {
                AcquireSkill(mainPlayer, v);
            }

        }
        //
        if (res.skillPasUnlock.Count > 0)
        {
            foreach (var v in res.skillPasUnlock)
            {
                AcquirePasSkill(mainPlayer, v);
            }

        }
        
        //
        if (res.reward == "reward")
        {
            PopupoManager.instance.ShowRewards(reso);
        }
        else if (res.reward != "")
        {
            PopupoManager.instance.ShowRewardsInside(reso, res.reward);
        }

        foreach (var v in res.toActivate)
        {
            v.gameObject.SetActive(true);
            v.gameObject.SendMessage("Activate", res.param, SendMessageOptions.DontRequireReceiver);
        }
        foreach (var v in res.toDeactivate)
        {
            v.gameObject.SetActive(false);
        }

        foreach (var v in res.objActivate)
        {
            GLOBAL_OBJECTS[v].SendMessage("Activate", res.param, SendMessageOptions.DontRequireReceiver);
        }

        if (res.call != "")
        {
            MAIN_GAME.SendMessage(res.call, res.param, SendMessageOptions.DontRequireReceiver);
        }

        if (res.dialog != "")
        {
            Dialoguer.instance.ShowDialogue(res.dialog, whoActivate);
        }
        
    }
    
    public int GetItemsCount(string param)
    {
        var a = all["main_player"].inventory.Find(x => x.dbObj.ID == param);
        if (a == null) return 0;
        else return (int)a.upgradePars["amount"];
    }

    public float GetLowestDistanceSkills(RObj who)
    {
        float d = 0;
        foreach (var v in who.actSkills)
        {
            if (v.dbObj.ID.IndexOf("basic") >= 0)
            {
                d = v.GetPar("range");
                break;
            }
            
            if (v.GetPar("range") > d)
            {
                d = v.GetPar("range");
            }
        }
        return d;
    }

    public List<RObj> GetAlliesInRange(RObj who, float range)
    {
        List<RObj> allies = new List<RObj>();

        foreach (var v in combats)
        {
            var f = CompareTags(who, v);
            if (!f) continue;
            var d = GetDistance(who, v, out float dd);
            if (d > range) continue;
            allies.Add(v);
        }
        
        return allies;
    }
    
    public RObj GetClosestEnemy(RObj who, out float dst, string reqViz = "", string reqTag = "", Vector3 fromPos = default)
    {
        var hl = who.timedBuffs.Find(x => x.dbObj.ID == "taunt");
        if (hl != null)
        {
            dst = GetDistance(who, hl.owner, out float dd);
            return hl.owner;
        }
        
        dst = 1e+10f;
        RObj ans = null;
        foreach (var v in combats)
        {
            if (v.tags[0] == who.tags[0]) continue;
            if (v.GetPar("health") <= 0) continue;
            if (reqViz != "" && !v.HasVis(reqViz)) continue;
            if (reqTag != "" && !v.META_TAGS.Contains(reqTag)) continue;
            if (v.GetPar("immortal") > 0) continue;
            
            float d = 0;
            if (fromPos == default) d = GetDistance(who, v, out float dd);
            else d = GetDistance(fromPos, v, out float dd);
            
            if (d < dst)
            {
                dst = d;
                ans = v;
            }
        }
        
        return ans;
    }

    public float GetDistance(RObj who, RObj v, out float st)
    {
         var vec = v.Position - who.Position;
         st = vec.magnitude;
         
         if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0) vec.z = 0;
         if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0) vec.y = 0;

         if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0)
         {
             if (who.ref_pos_x < 0) who.AdjustPosition();
             if (v.ref_pos_x < 0) v.AdjustPosition();
             return Mathf.Max(Mathf.Abs(v.ref_pos_x - who.ref_pos_x), Mathf.Abs(v.ref_pos_y - who.ref_pos_y));
         }
         else
         {
             return vec.magnitude;
         }
    }

    public static bool CompareTags(RObj a, RObj b)
    {
        if (a.tags.Contains("player") && b.tags.Contains("player")) return true;
        if (a.tags.Contains("enemy") && b.tags.Contains("enemy")) return true;
        return false;
    }

    public float GetDistance(int x0, int y0, int x1, int y1)
    {
        if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0)
        {
            return Mathf.Max(Mathf.Abs(x0 - x1), Mathf.Abs(y0 - y1));
        }
        else if (ConfigLoader.GetMetaParamValue("mode_isometric") > 0)
        {
            //Debug.Log(x0 +" " +y0+" "+x1+" "+y1);
            int x2 = Mathf.Abs(x0 - x1);
            int y2 = Mathf.Abs(y0 - y1);
            int ans = 0;
            while (true)
            {
                if (x2 == 0)
                {
                    ans = ans + y2;
                    break;
                }

                if (y2 == 0)
                {
                    ans = ans + (x2 / 2 + x2 % 2);
                    break;
                }

                if (x2 % 2 == 0)
                {
                    x2--;
                    y2--;
                    ans++;
                }
                else
                {
                    x2--;
                    ans++;
                }
            }

            //Debug.Log(ans);
            return ans;
        }
        else if (ConfigLoader.GetMetaParamValue("mode_hex") > 0)
        {
            //Debug.Log(x0 +" " +y0+" "+x1+" "+y1);
            int x2 = Mathf.Abs(x0 - x1);
            int y2 = Mathf.Abs(y0 - y1);
            int ans = 0;
            while (true)
            {
                if (x2 == 0)
                {
                    ans = ans + y2;
                    break;
                }

                if (y2 == 0)
                {
                    ans = ans + x2;
                    break;
                }

                if (x2 % 2 == 0)
                {
                    x2--;
                    ans++;
                }
                else
                {
                    x2--;
                    y2--;
                    ans++;
                }
            }

            //Debug.Log(ans);
            return ans;
        }
        else
        {
            return MathF.Sqrt(Mathf.Pow(x1 - x0, 2) + Mathf.Pow(y1 - y0, 2));
        }
    }
    
    public float GetDistance(Vector3 who, RObj v, out float dst)
    {
        var vec = v.Position - who;
         
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0) vec.z = 0;
        if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0) vec.y = 0;
         dst = vec.magnitude;
        if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0)
        {
            var jj = PositionSetter.instance.GetClosestPos(who);
            if (v.ref_pos_x < 0) v.AdjustPosition();
            return Mathf.Max(Mathf.Abs(v.ref_pos_x - jj.Item1), Mathf.Abs(v.ref_pos_y - jj.Item2));
        }
        else if (ConfigLoader.GetMetaParamValue("mode_isometric") > 0 || ConfigLoader.GetMetaParamValue("mode_hex") > 0)
        {
            var jj = PositionSetter.instance.GetClosestPos(who);
            if (v.ref_pos_x < 0) v.AdjustPosition();
            return GetDistance((int)v.ref_pos_x, (int)v.ref_pos_y, jj.Item1, jj.Item2);
        }
        else
        {
            return vec.magnitude;
        }
    }
    
    public List<RObj> GetCommandResult(string command, string param, Transform t = null, string extra = "", RObj rr = null, bool overIndex = false)
    {
        if (command == "BY_PARENT_TASK")
        {
            var n = t.GetComponentInParent<AbsHolder>();
            var b = DatabaseAll.instance.allTasks[n.id];
            var h = CreateItems(b.rewards);
            return h;
        }
        if (command == "BY_SELF")
        {
            var n = t.GetComponentInParent<UIfiller>(true);
            var b = n.selfReward;
            var h = CreateItems(b);
            return h;
        }
        if (command == "BY_ID")
        {
            return new List<RObj>{all[param]};
        }
        if (command == "LAST_SELECTED")
        {
            return new List<RObj>{lastAllySelected == null ? mainPlayer : lastAllySelected};
        }
        if (command == "GET_SKILLS")
        {
            return all[param].actSkills;
        }
        if (command == "GET_SKILLS_SELECT")
        {
            RObj wha = lastAllySelected == null ? mainPlayer : lastAllySelected;

                var g = wha.actSkills;
                List<RObj> res = new List<RObj>();
                foreach (var item in g)
                    res.Add(wha.GetSKillReplace(item));

                return res;

        }
        if (command == "GET_ALLY_SQUAD")
        {
            //and not is summon ?
            var v = combats.FindAll(x => x.tags.Contains("player") && !x.META_TAGS.Contains("npc"));
            return v;
        }
        if (command == "GET_SKILLS_NO_BASIC")
        {
            return all[param].actSkills.FindAll(x => x.dbObj.ID.IndexOf("basic") < 0 && x.GetPar("action_req") > 0);
        }
        else if (command == "GET_ITEMS_COUNT")
        {
            var a = all["main_player"].inventory.Find(x => x.dbObj.ID == param);
            //bad one
            if (a == null)
            {
                if (!empties.ContainsKey(param))
                {
                    var g = DatabaseAll.instance.CreateItem(param, 0);
                    empties.Add(param, g);
                    a = g;
                }
                else
                {
                    a = empties[param];
                }
                
            }
            
            return new List<RObj> { a};
        }
        else if (command == "GET_ITEMS")
        {
            if (extra == "add")
            {
                var hh = lastAllySelected;
                if (hh == null) hh = mainPlayer;
                
                if (rr.dbObj.pars["max_stack"] > 1)
                {
                    AddItem(hh, rr, overIndex);
                }
                else
                {
                    AddItem(hh, rr, overIndex);
                    rr.owner = hh;
                }

                return null;
            }

            if (extra == "del")
            {
                var hh = lastAllySelected;
                if (hh == null) hh = mainPlayer;
                hh.inventory.Remove(rr);
                rr.owner = null;
                return null;
            }
            
            RObj hh1 = lastAllySelected == null ? mainPlayer : lastAllySelected;
            
            var res = hh1.inventory.FindAll(x => x.GetPar("amount") > 0);
            var gk = param.Split(',');

            for (int l = 0; l < gk.Length; l++)
            {
                
                if (gk[l] == "can_equip")
                {
                    res = res.FindAll(x => x.dbObj.pars["slot"] >= 0);
                }                
                
                if (gk[l] == "not_equiped")
                {
                    res = res.FindAll(x => x.GetPar("amount") > 0 && x.GetPar("used_slot") < 0);
                }
                
                
                if (gk[l] == "items")
                {
                    res = res.FindAll(x => x.it == ItemType.item);
                }
                
                if (gk[l] == "dynamic")
                {
                    res = res.FindAll(x => x.dynamic != null);
                }
                

                if (gk[l] == "monsters")
                {
                    res = res.FindAll(x => x.it == ItemType.monster);
                }
                
                if (gk[l] == "skills")
                {
                    res = res.FindAll(x => x.it == ItemType.projectile);
                }
                
                if (gk[l] == "act_skills")
                {
                    res = res.FindAll(x => x.it == ItemType.projectile && x.dbObj.ID.IndexOf("pass_") < 0);
                }
                
                if (gk[l] == "pas_skills")
                {
                    res = res.FindAll(x => x.it == ItemType.projectile && x.dbObj.ID.IndexOf("pass_") >= 0);
                }
                
                if (gk[l] == "not_skills")
                {
                    res.RemoveAll(x => x.it == ItemType.projectile);
                }
                
            }
            //ok but now (!) if the inventory is SPARSE, we need to do something
            var flr = t.GetComponent<UIfiller>();
            if (!flr.ignoreInvAny && ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
            {
                var sparsed = new List<RObj>();
                res.Sort((x,y) => x.index.CompareTo(y.index));
                int prevInd = 0;
                foreach (var item in res)
                {
                    for (int i = prevInd; i < item.index; i++)
                        sparsed.Add(null);
                    prevInd = item.index + 1;
                    sparsed.Add(item);
                }
                return sparsed;
            }

            return res;            
            
        }
        else if (command == "GET_ITEMS_OTHER")
        {
            if (extra == "add")
            {
                AddItem(curLoot, rr, overIndex);
                return null;
            }

            if (extra == "del")
            {
                curLoot.inventory.Remove(rr);
                rr.owner = null;
                return null;
            }
            
            
            var res = curLoot.inventory.FindAll(x => x.GetPar("amount") > 0);
            var gk = param.Split(',');

            for (int l = 0; l < gk.Length; l++)
            {
                
                if (gk[l] == "not_equiped")
                {
                    res = res.FindAll(x => x.GetPar("amount") > 0 && x.GetPar("used_slot") < 0);
                }

                if (gk[l] == "can_equip")
                {
                    res = res.FindAll(x => x.dbObj.pars["slot"] >= 0);
                }

            }
            
            //ok but now (!) if the inventory is SPARSE, we need to do something
            var flr = t.GetComponent<UIfiller>();
            if (!flr.ignoreInvAny && ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
            {
                var sparsed = new List<RObj>();
                res.Sort((x,y) => x.index.CompareTo(y.index));
                int prevInd = 0;
                foreach (var item in res)
                {
                    for (int i = prevInd; i < item.index; i++)
                        sparsed.Add(null);
                    prevInd = item.index + 1;
                    sparsed.Add(item);
                }
                return sparsed;
            }
            
            return res;            
            
        }
        else if (command == "GET_UNITS_PLACED")
        {
            var res = mainPlayer.inventory.FindAll(x => x.it == ItemType.monster);
            var gk = param.Split(',');

            for (int l = 0; l < gk.Length; l++)
            {
                
                if (gk[l] == "not_equiped")
                {
                    res = res.FindAll(x => x.GetPar("amount") > 0 && x.GetPar("used_slot") < 0);
                }

                if (gk[l] == "can_equip")
                {
                    res = res.FindAll(x => x.dbObj.pars["slot"] >= 0);
                }

            }
            
            //ok but now (!) if the inventory is SPARSE, we need to do something
            var flr = t.GetComponent<UIfiller>();
            if (!flr.ignoreInvAny && ConfigLoader.GetMetaParamValue("use_inv_any") > 0)
            {
                var sparsed = new List<RObj>();
                res.Sort((x,y) => x.index.CompareTo(y.index));
                int prevInd = 0;
                foreach (var item in res)
                {
                    for (int i = prevInd; i < item.index; i++)
                        sparsed.Add(null);
                    prevInd = item.index + 1;
                    sparsed.Add(item);
                }
                return sparsed;
            }
            
            return res;            
            
        }
        else if (command == "GET_ITEM_INFO")
        {
            return new List<RObj> {curClick};
        }
        else if (command == "GET_ITEM_TOOLTIP")
        {
            return new List<RObj> {lastTooltip};
        }
        else if (command == "GET_ITEMS_BY_SLOTS")
        {
            if (extra == "add")
            {
                RObj hh =  lastAllySelected == null ? mainPlayer : lastAllySelected;
                hh.inventory.Add(rr);
                return null;
            }

            if (extra == "del")
            {
                RObj hh =  lastAllySelected == null ? mainPlayer : lastAllySelected;
                hh.inventory.Remove(rr);
                return null;
            }
            
            var cc = param.Split(',');

            List<RObj> res = new List<RObj>();
            for (int i = 0; i < cc.Length; i++)
            {
                var h = float.Parse(cc[i]);
                RObj wha = lastAllySelected;
                if (wha == null) wha = mainPlayer;
                var g = wha.inventory.Find(x => x.GetPar("used_slot") == h);
                res.Add(g);
            }

            return res;
        }
        else if (command == "GET_SKILL_ROLL")
        {
            var res = GetSkillsRoll();
            return res;
        }
        else if (command == "GET_BUILDING_ROLL")
        {
            var res = GetBuildingsRoll();
            return res;
        }
        
        return null;
    }

    public List<RObj> GetSkillsRoll()
    {

        var h0 = DatabaseAll.instance.CreateProjectile(all["main_player"], "pass_atk", Vector3.zero, false, false);
        var h1 = DatabaseAll.instance.CreateProjectile(all["main_player"], "pass_hp", Vector3.zero, false, false);
        var h2 = DatabaseAll.instance.CreateProjectile(all["main_player"], "pass_hp", Vector3.zero, false, false);


        return new List<RObj> {h0, h1, h2};
    }
    public List<RObj> GetBuildingsRoll()
    {

        var h0 = DatabaseAll.instance.CreateBuilding(all["main_player"], "barracks1", Vector3.zero, false, false);
        var h1 = DatabaseAll.instance.CreateBuilding(all["main_player"], "garden1", Vector3.zero, false, false);
        var h2 = DatabaseAll.instance.CreateBuilding(all["main_player"], "alchemy1", Vector3.zero, false, false);


        return new List<RObj> {h0, h1, h2};
    }
    

    public void ClickedSome(RObj o, UnoAll u, ObjHolder h)
    {
        
        Debug.Log("BOM");
        if (h.filler != null && h.filler.replaceClick.Length > 1)
        {
            u.param = h.filler.replaceClick;
        }

        if (h.filler != null)
        {
            curClick = o;
            EventManager.INV("clicked", new ArgPass { what = h.filler.nm, what1 = u.param });
        }

        //changed
        if (u.param == "click")
        {
            UI_descr.GetComponent<UIfiller>().otherContext = h.filler;
            UI_descr.SetActive(true);
        }
        else if (u.param == "equip_exp")
        {
            //bad shit
            if (o.dynamic != null)
            {
                var h1 = UtilsControl.GetLowest(o.dynamic.id);
                if (h1 == o.dynamic.id) return;
            }
            
            var a1 = all["main_player"];
            var b1 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 20);
            if (b1 == null) o.SetPar("used_slot", 20);
            var b2 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 21);
            if (b2 == null) o.SetPar("used_slot", 21);
            var b3 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 22);
            if (b3 == null) o.SetPar("used_slot", 22);
            var b4 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 23);
            if (b4 == null) o.SetPar("used_slot", 23);
            
        }
        else if (u.param == "equip_skills")
        {
            //bad shit
            if (o.dynamic != null)
            {
                var h1 = UtilsControl.GetLowest(o.dynamic.id);
                if (h1 == o.dynamic.id) return;
            }
            
            var a1 = all["main_player"];

            if (o.dbObj.ID.IndexOf("pass") < 0)
            {
                var b1 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 50);
                if (b1 == null) o.SetPar("used_slot", 50);
                var b2 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 51);
                if (b2 == null) o.SetPar("used_slot", 51);
                var b3 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 52);
                if (b3 == null) o.SetPar("used_slot", 52);
                var b4 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 53);
                if (b4 == null) o.SetPar("used_slot", 53);
            }
            else
            {
                var b1 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 60);
                if (b1 == null) o.SetPar("used_slot", 60);
                var b2 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 61);
                if (b2 == null) o.SetPar("used_slot", 61);
                var b3 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 62);
                if (b3 == null) o.SetPar("used_slot", 62);
                var b4 = a1.inventory.Find(x => x.upgradePars["used_slot"] == 63);
                if (b4 == null) o.SetPar("used_slot", 63);
            }
        }
        else if (u.param == "placing")
        {
            //we clone it ?
            PlacerSystem.instance.Attach(h.obj);
        }
        else if (u.param == "unequip_exp")
        {
            o.SetPar("used_slot", -1);
        }
        else if (u.param == "buy")
        {
            var gg = UpgradeSystem.instance.GetPrice(o, u.param);
            var q = HaveAmount(gg);
            if (q)
            {
                DelItems(gg);
                if (o.it == ItemType.projectile)
                    AddBuff(all["main_player"], o);
                    else AddItem(all["main_player"], o);
                
                all["main_player"].RecalcPars();
                OnBuy(o);
            }
            else
            {
                UI_noMoney.SetActive(true);
            }
        }
        else if (u.param == "upgrade")
        {
            var gg = UpgradeSystem.instance.GetPrice(o, u.param, u.param2);
            var q = HaveAmount(gg);

            var str = "level";
            if (u.param2 != "") str = u.param2;
            
            if (q)
            {
                if (o.dynamic != null)
                {
                    var g0 = UtilsControl.GetNext(o.dynamic.id);
                    if (ConfigLoader.Instance.allDynamic.ContainsKey(o.dynamic.id))
                    {
                        DelItems(gg);
                        ExecuteDone(o.dynamic);
                        if (!ConfigLoader.Instance.allDynamic.ContainsKey(g0))
                        {
                            o.SetPar("max", 1);
                            return;
                        }
                        o.dynamic = ConfigLoader.Instance.allDynamic[g0];
                    }
                    
                }
                else
                    o.ChangePar(str, 1);
            }
            else
            {
                UI_noMoney.SetActive(true);
            }
        }
        else if (u.param == "sell")
        {
            var gg = UpgradeSystem.instance.GetPrice(o, u.param);
            AddItems(gg);
            //?
            if (ConfigLoader.GetMetaParamValue("buyback") > 0)
                AddItems(new List<Bon>{new Bon{Key = o.dbObj.ID, Value = (int)o.upgradePars["amount"]}}, curLoot);
            
            DelItems(new List<Bon>{new Bon{Key = o.dbObj.ID, Value = (int)o.upgradePars["amount"]}});
        }
        else if (u.param == "take_skill")
        {
            AddBuff(all["main_player"], o);
        }
        else if (u.param == "equip")
        {
            Equip(all["main_player"], o);
        }
        else if (u.param == "cast")
        {
            SkillExecutor.instance.CastSkill(lastAllySelected == null ? mainPlayer : lastAllySelected, o);
        }
        else if (u.param == "select")
        {
            XDselect.Select(o);
        }
    }

    public string GetGeneratedDynamicDescr(string id)
    {
        string res = "GEN:" + id + " ";
        var f = ConfigLoader.Instance.allDynamic[id];

        foreach (var v in f.statsInc)
        {
            res += v.Key + "," + v.Value + "#";
        }

        foreach (var v in f.itemsGet)
        {
            res += v.Key + "," + v.Value + "#";
        }
        
        return res;
    }
    
    public void OnBuy(RObj o)
    {
        if (o.it == ItemType.building)
        {
            UI_buildings.SetActive(false);
            PlacerSystem.instance.Attach(o);
        }
    }

    public void AddItems(List<RObj> items)
    {
        foreach (var v in items)
        {
            if (v == null) continue;
            AddItem(all["main_player"],v);
        }
    }

    public RObj GetSkill(string id, RObj who = null)
    {
        if (who == null) who = all["main_player"];
        var a = who.actSkills.Find(x => x.dbObj.ID == id);
        return a;
    }
    
    public RObj GetBuff(string id, RObj who = null)
    {
        if (who == null) who = all["main_player"];
        var a = who.buffs.Find(x => x.dbObj.ID == id);
        return a;
    }
    public void AddItems(List<Bon> rewards, RObj who = null)
    {
        //who ?
        foreach (var v in rewards)
        {
            var ii = DatabaseAll.instance.CreateItem(v.Key, v.Value);
            if (who != null)
                AddItem(who, ii);
            else
            {
                AddItem(all["main_player"], ii);
            }
        }
    }

    public List<RObj> CreateItems(List<Bon> rewards)
    {
        List<RObj> res = new List<RObj>();
        foreach (var v in rewards)
        {
            var ii = DatabaseAll.instance.CreateItem(v.Key, v.Value);
            res.Add(ii);
        }

        return res;
    }

    public RObj AddItem(RObj who, string what, int amount, bool randomizeStats = false)
    {
        if (what == "exp")
        {
            who.ChangePar("exp", amount, true);
            return null;
        }

        string other = "";
        if (what.IndexOf("shard_") >= 0)
        {
            other = what.Substring(6);
            what = "shard";
        }
        
        var f = DatabaseAll.instance.CreateItem(what, amount);
        if (other != "") f.shardID = other;
        if (f.it == ItemType.item && randomizeStats)
        {
            RandomizeItemStats(f);
        }
        
        AddItem(who, f);
        return f;
    }
    
    public void AddItem(RObj who, RObj what, bool overIndex = false)
    {
        while (true)
        {
            var a = who.inventory.Find(x =>
                x.dbObj.ID == what.dbObj.ID && x.shardID == what.shardID && x.upgradePars["amount"] < x.dbObj.pars["max_stack"]);

            if (a == null)
            {
                if (!overIndex)
                {
                    var hi = who.GetFreeIndex(what.dbObj.sizeX, what.dbObj.sizeY);
                    what.index = hi;
                }
                else
                {
                    //it may be oversect
                    var cc = who.IsIntersected(what.index, what.dbObj.sizeX, what.dbObj.sizeY, what);
                    if (cc)
                    {
                        var hi = who.GetFreeIndex(what.dbObj.sizeX, what.dbObj.sizeY);
                        what.index = hi;
                    }
                }

                who.inventory.Add(what);
                //ne nravitsya chast c projectile thwbbb
                if (what.owner != null && what.it != ItemType.projectile)
                {
                    what.owner.inventory.Remove(what);
                }
                    what.owner = who;
                return;
            }
            else
            {
                var dlt = -a.upgradePars["amount"] + a.dbObj.pars["max_stack"];
                
                if (dlt >= what.upgradePars["amount"])
                {
                    a.ChangePar("amount", what.upgradePars["amount"]);
                    what.ChangePar("amount", -what.upgradePars["amount"]);
                    return;
                }
                
                a.ChangePar("amount", dlt);
                what.ChangePar("amount", -dlt);
            }
        }
    }

    public void AddBuff(RObj who, RObj what)
    {
        who.buffs.Add(what);
        who.RecalcPars();

        if (what.dbObj.ID.IndexOf("global") >= 0)
        {
            
        }
        
    }
    public void Equip(RObj who, RObj what)
    {
        if (!who.inventory.Contains(what))
        {
            who.inventory.Add(what);
            what.owner = who;
        }

        var b = who.inventory.Find(
            x => x.curPars["used_slot"] >= 0 && x.curPars["used_slot"] == what.dbObj.pars["slot"]);

        if (b != null)
        {
            b.upgradePars["used_slot"] = -1;
            b.RecalcPars();
        }

        if (b == what)
        {
            who.RecalcPars();
            return;
        }
        
        what.SetPar("used_slot", what.dbObj.pars["slot"]);
        what.RecalcPars();
        who.RecalcPars();
        
        EventManager.INV("equip_change", new ArgPass{});
    }


    public Transform minT;
    public Transform maxT;
    
    public GameObject GetBinome(float x1, float y1)
    {
        if (ConfigLoader.GetMetaParamValue("runtime_mapping") > 0)
        {
            var ps = GetClosestPos(new Vector3(x1, y1, 0));
            return map[(int)ps.x, (int)ps.y];
        }
        else
        {
            return map[(int)x1, (int)y1];
        }
    }

    private Vector2 vecMin = new Vector2(0, 0);
    private Vector2 vecMax = new Vector2(10, 10);
    private int nh = 10;
    private int mh = 10;
    
    public Vector2 GetClosestPos(Vector3 vec, bool zetZero = true, List<Transform> overs = null)
    {
        if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0)
            zetZero = false;
        
        
        Vector2 ans = new Vector2(vec.x, vec.y);
        float min = 1e+10f;

        if (overs != null && overs.Count > 0)
        {
            int u = -1;
            for (int i = 0; i < overs.Count; i++)
            {
                var vec1 = vec - overs[i].position;
                if (zetZero) vec1.z = 0;
                float dst = vec1.magnitude;
                if (dst < min)
                {
                    min = dst;
                    u = i;
                }
            }
            
            return GetClosestPos(overs[u].position, zetZero);
        }

        if (ConfigLoader.GetMetaParamValue("use_dist_heur") > 0)
        {

            var xx = new Vector2(vec.x - minT.position.x, vec.y - minT.position.y);
            if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0)
                xx = new Vector2(vec.x - minT.position.x, vec.z - minT.position.z);

            xx.x /= (maxT.position.x - minT.position.x);

            if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0)
                xx.y /= (maxT.position.z - minT.position.z);
            else xx.y /= (maxT.position.y - minT.position.y);

            xx.x = vecMin.x + (vecMax.x - vecMin.x) * xx.x;
            xx.y = vecMin.y + (vecMax.y - vecMin.y) * xx.y;


            if (xx.x % 1 < 0.5f) xx.x = MathF.Floor(xx.x);
            else xx.x = MathF.Ceiling(xx.x);
            if (xx.y % 1 < 0.5f) xx.y = MathF.Floor(xx.y);
            else xx.y = MathF.Ceiling(xx.y);

            for (int i0 = -2; i0 < 3; i0++)
            for (int j0 = -2; j0 < 3; j0++)
            {
                int i = (int) (xx.x + i0);
                int j = (int) (xx.y + j0);
                if (i < 0 || i >= nh || j < 0 || j >= mh) continue;

                if (mappingPositions[i, j] == null) continue;
                var vec1 = vec - mappingPositions[i, j].position;
                if (zetZero) vec1.z = 0;

                float dst = vec1.magnitude;
                if (dst < min)
                {
                    min = dst;
                    ans.x = i;
                    ans.y = j;
                }
            }

            return ans;
        }


        
        for (int i = 0; i < nh; i++)
            for (int j = 0; j < mh; j++)
            {
                if (mappingPositions[i, j] == null) continue;
                var vec1 = vec - mappingPositions[i, j].position;
                if (zetZero) vec1.z = 0;
                
                float dst = vec1.magnitude;
                if (dst < min)
                {
                    min = dst;
                    ans.x = i;
                    ans.y = j;
                }
            }
            
          
        //Debug.Log("ANS : " + ans + " " + min);
        
        return ans;
    }
    
    public int GetManLen(int x0, int y0, int x1, int y1)
    {
        return Mathf.Max(Mathf.Abs(x0 - x1), Mathf.Abs(y0 - y1));
    }

    public void ApplyPlayerConfigParams(RObj who)
    {
        AddItems(ConfigLoader.Instance.allPlayer[0].items, who);
        AddStats(ConfigLoader.Instance.allPlayer[0].stats, who);
        AddDynamice(ConfigLoader.Instance.allPlayer[0].dynTaken);
    }

    public void AddDynamice(List<string> dynamic)
    {
        foreach (var v in dynamic)
        {
            playerData.dynTaken.Add(v);
        }
    }
    
    
    public void AddStats(List<Bon> stats, RObj who)
    {
        foreach (var stat in stats)
        {
            ModelStatistics.instance.SetStatValue(stat.Key, stat.Value);
        }
    }

    public void IncStats(List<Bon> stats, RObj who)
    {
        foreach (var stat in stats)
        {
            ModelStatistics.instance.IncreaseStatValue(stat.Key, stat.Value);
        }
    }
    
    public void SetStats(List<Bon> stats, RObj who)
    {
        foreach (var stat in stats)
        {
            ModelStatistics.instance.SetStatValue(stat.Key, stat.Value);
        }
    }
    
    public void IncPars(List<Bon> pars, RObj who)
    {
        foreach (var stat in pars)
        {
            who.ChangePar(stat.Key, stat.Value);
        }
    }
    
    public void AddAsResource(GameObject b)
    {
        
    }

    public Dictionary<string, float> dmgTimes = new Dictionary<string, float>();
    
    public void DealHeal(RObj who, float val)
    {
        var h = who.GetPar("registered_damage");
        h -= val;
        if (h < 0) h = 0;
        who.SetPar("registered_damage", h);
        //effect regen possibly
    }
    public void DealRegen(RObj who)
    {
        var a = who.GetPar("regen");
        var h = who.GetPar("registered_damage");
        h -= a;
        if (h < 0) h = 0;
        who.SetPar("registered_damage", h);
        //effect regen possibly
    }

    public void ShowRange(LineRenderer range, Vector3 pos, float skl, float size)
    {
        if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0)
        {
            var gg = PositionSetter.instance.GetClosestPos(pos);

            if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
            {
                Vector3 l1 = new Vector3(gg.Item3.x - size * (skl + 0.5f), gg.Item3.y - size * (skl + 0.5f),
                    gg.Item3.z);
                Vector3 l2 = new Vector3(gg.Item3.x - size * (skl + 0.5f), gg.Item3.y + size * (skl + 0.5f),
                    gg.Item3.z);
                Vector3 l3 = new Vector3(gg.Item3.x + size * (skl + 0.5f), gg.Item3.y + size * (skl + 0.5f),
                    gg.Item3.z);
                Vector3 l4 = new Vector3(gg.Item3.x + size * (skl + 0.5f), gg.Item3.y - size * (skl + 0.5f),
                    gg.Item3.z);
                Vector3[] poses = new[] { l1, l2, l3, l4, l1 };
                range.positionCount = poses.Length;
                range.SetPositions(poses);
            }
            else
            {
                Vector3 l1 = new Vector3(gg.Item3.x - size * (skl + 0.5f), gg.Item3.y, gg.Item3.z - size * (skl + 0.5f));
                Vector3 l2 = new Vector3(gg.Item3.x - size * (skl + 0.5f), gg.Item3.y, gg.Item3.z + size * (skl + 0.5f));
                Vector3 l3 = new Vector3(gg.Item3.x + size * (skl + 0.5f), gg.Item3.y, gg.Item3.z + size * (skl + 0.5f));
                Vector3 l4 = new Vector3(gg.Item3.x + size * (skl + 0.5f), gg.Item3.y, gg.Item3.z - size * (skl + 0.5f));
                Vector3[] poses = new[] { l1, l2, l3, l4, l1 };
                range.positionCount = poses.Length;
                range.SetPositions(poses);
            }
        }
    }

    public void RandomizeItemStats(RObj who)
    {
        int u0 = Random.Range(0, 40);
        who.ChangePar("attack", u0);
        int u1 = Random.Range(0, 40);
        who.ChangePar("max_health", u1);
        who.ChangePar("health", u1);
        
    }
    
    public void DealDamage(RObj a, RObj skl)
    {
        Debug.Log("DAMAGE: " +skl.RID + " " +skl.dbObj.ID + " " + a.RID + " " + a.dbObj.ID);
        var atk = skl.GetPar("attack");
        var sh = skl.GetPar("shield");
        
        var h = a.GetPar("registered_damage");
        var sha = a.GetPar("shield");
        dmgTimes[skl.owner.RID] = Time.time;

        if (skl.GetPar("empty_req") > 0)
        {
            if (skl.trg != Vector3.zero)
            {
                skl.main.transform.position = skl.trg;
                skl.Position = skl.trg; 
            }
            else
            {
                var kk = GetRndFree(a.Position, skl.GetPar("range"));
                skl.main.transform.position = kk;
                skl.Position = kk;                
            }
        }

        if (skl.dbObj.ID.IndexOf("summon") >= 0)
        {
            var gg = WaveSpawner.instance.DoSpawnAny(skl.dbObj.extraPars, skl.owner.tags[0], null,
                null, a.visuals["combat"].GetComponent<XDcombat>().curTg == tgBattle,
                skl.Position, skl.Position, true);
            foreach (var v in gg)
            {
                v.AdjustPosition();
            }
        }
        
        if (skl.dbObj.ID.IndexOf("teleport") >= 0)
        {
            a.main.transform.position = skl.Position;
            a.Position = skl.Position;
            a.AdjustPosition();
            
        }
        //push ?
        if (skl.dbObj.ID.IndexOf("push") >= 0)
        {
            if (ConfigLoader.GetMetaParamValue("mode_manhattan") >= 0)
            {
                var t = PositionSetter.instance.GetAllFreeSquares(a.Position, 1);
                var g = PositionSetter.instance.GetFarthestPosDot(a.Position - skl.Position,  a.Position, t, out float de);
                
                var vec = a.Position - skl.Position;
                if (vec.magnitude < de && g != default)
                {
                    UtilsControl.Instance.MoveTo(a.main.transform, 10, g, () =>
                    {
                        a.Position = g;
                        a.AdjustPosition();
                    }, null, useRight:false);
                }
            }
            else
            {
                var dir = a.Position - skl.Position;
                if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0) dir.z = 0;
                if (ConfigLoader.GetMetaParamValue("coord_mode_xz") > 0) dir.y = 0;

                var ep = a.Position + dir * 5;
                UtilsControl.Instance.MoveTo(a.main.transform, 20, ep, null, null, useRight:false);

            }
        }
        
        
        //shield
        //self shield or 
        
        a.ChangePar("shield", sh);
        var crt = skl.GetPar("crit_chance");
        var rl = Random.Range(0, 1f);
        bool wasCrit = false;
        if (rl < crt)
        {
            wasCrit = true;
            atk *= (1 + skl.GetPar("crit_dmg"));
        }

        if (sh > 0 && a.RID == "main_player")
        {
            var gg = a.main.GetComponent<MainEffector>();
            if (gg != null && gg.effects.ContainsKey("shield"))
            {
                gg.effects["shield"].SetActive(true);
                FunctionTimer.Create(() =>
                {
                    gg.effects["shield"].SetActive(false);
                }, 1);
            }
        }
        
        if (atk > 0)
        {
            if (skl.owner != a)
            {
                var kk = a.GetPar("dodge");
                var roll = Random.Range(0, 100);
                if (roll < kk)
                {
                    a.SetPar("show_message", 1);
                    return;
                }
            }

            a.lastDmgFrom = skl.owner;
            //on dmg
            if (a.dbObj.onDmg != "")
            {
                var h0 = DatabaseAll.instance.CreateProjectile(a, a.dbObj.onDmg, Vector3.zero, false, false);
                SkillExecutor.instance.ExecuteSkill(a, h0);
            }
            
            foreach (var v in a.buffs)
            {
                if (v.dbObj.onDmg == "") continue;
                var h1 = DatabaseAll.instance.CreateProjectile(a, v.dbObj.onDmg, Vector3.zero, false, false);
                SkillExecutor.instance.ExecuteSkill(a, h1);
            }
            //

            if (a.GetPar("immortal") > 0)
            {
                return;
            }

            var db = a.GetPar("dmg_block");
            var k = Mathf.Min(atk, db);
            atk -= k;
            
            var md = a.GetPar("max_dmg_taken");
            if (md > 0 && atk > md) atk = md;
            
            
            k = Mathf.Min(atk, sha);
            atk -= k;
            a.ChangePar("shield", -k);
        }
        //we get through buffs and timed buffs to reduce shield
        for (int i = 0; i < a.buffs.Count; i++)
        {
            if (atk <= 0) break;
            var sh1 = a.buffs[i].GetPar("shield");
            var k = Mathf.Min(atk, sh1);
            atk -= k;
            a.buffs[i].ChangePar("shield", -k);
        }
        //
        for (int i = 0; i < a.timedBuffs.Count; i++)
        {
            if (atk <= 0) break;
            var sh1 = a.timedBuffs[i].GetPar("shield");
            var k = Mathf.Min(atk, sh1);
            atk -= k;
            a.timedBuffs[i].ChangePar("shield", -k);
        }
        //
        
        if (wasCrit)
            a.SetPar("was_crit", 1);
        
        a.ChangePar("registered_damage", atk);
        
        var dlt = a.GetPar("registered_damage");
        if (dlt < 0)
            a.SetPar("registered_damage", 0);

        var adds = new List<string>(skl.dbObj.buffsApplied);
        adds.AddRange(skl.extraBuffs);

        foreach (var v in adds)
        {
            //check for once ?
            var ee = ConfigLoader.Instance.skills.Find(x => x.skillName == v);
            var cc = a.timedBuffs.Find(x => x.dbObj.ID == v);
            if (cc != null && ee.unique > 0)
            {
                cc.SetPar("timeLeft", ee.time);
                continue;
            }

            if (v.IndexOf("exec_") >= 0)
            {
                var v1 = v.Substring(5);
                var h0 = DatabaseAll.instance.CreateProjectile(skl.owner, v1, Vector3.zero, false, false);
                h0.Position = skl.Position;
                DealDamage(a, h0);
            }
            else
            {
                var h0 = DatabaseAll.instance.CreateProjectile(skl.owner, v, Vector3.zero, false, false);
                h0.SetPar("timeLeft", h0.GetPar("time"));
                h0.SetPar("timeEvery", h0.GetPar("dmg_every"));
                
                a.timedBuffs.Add(h0);                
            }

        }
        //now time for adds
        
        //
        if (atk > 0)
        {
            var vv = skl.owner.GetPar("lifesteal_prc") * atk;
            if (vv > 0)
            {
                DealHeal(skl.owner, vv);
            }
            
        }
    }

    private void OnDestroy()
    {
        all.Clear();
        allVisuals.Clear();
    }

    public GameObject selected;
    public void ShowLastSelected()
    {
        if (!selected) return;
        if (lastAllySelected != null)
        {
            selected.transform.position = lastAllySelected.Position;
        }
        else
        {
            selected.transform.position = new Vector3(1000, 1000, 1000);
        }
    }

    public void DoNaprig(RObj who, RObj target)
    {
        Vector3 saved1 = who.Position;
        Vector3 saved2 = who.Position +
                         (target.Position - who.Position) * ConfigLoader.GetMetaParamValue("naprig_prc");
            
        UtilsControl.Instance.MoveTo(who.main.transform, 5, saved2, () =>
        {
            UtilsControl.Instance.MoveTo(who.main.transform, 5, saved1, () =>
            {
                
            }, null,useRight:false,ignoreFlip:true);
        }, null,useRight:false,ignoreFlip:true);
    }
    
    public void DoNaprig(RObj who, Vector3 target)
    {
        Vector3 saved1 = who.Position;
        Vector3 saved2 = who.Position +
                         (target - who.Position) * ConfigLoader.GetMetaParamValue("naprig_prc");
            
        UtilsControl.Instance.MoveTo(who.main.transform, 5, saved2, () =>
        {
            UtilsControl.Instance.MoveTo(who.main.transform, 5, saved1, () =>
            {
                
            }, null,useRight:false, ignoreFlip:true);
        }, null,useRight:false, ignoreFlip:true);
    }
    
    public static List<Vizualo> allVisuals = new List<Vizualo>();

    private void Update()
    {
        ShowLastSelected();
        
        foreach (var v in allVisuals)
        {
            if (!v) continue;
            v.Updateo();
        }
        
        //decrease cds !    
        foreach (var v in all)
        {
            if (v.Value.it == ItemType.projectile && v.Value.upgradePars["cd"] >= 0)
            {
                v.Value.upgradePars["cd"] -= TimeManager.LAST_DT;
            }

            int cnt = 0;
            List<RObj> toDelete = new List<RObj>();
            foreach (var v1 in v.Value.timedBuffs)
            {
                if (v1.upgradePars["timeLeft"] == -1) continue;
                v1.upgradePars["timeLeft"] -=  TimeManager.LAST_DT;
                v1.upgradePars["timeEvery"] -=  TimeManager.LAST_DT;
                
                if (v1.upgradePars["timeEvery"] < 0)
                {
                    //deal damage
                    DealDamage(v.Value, v1);
                    v1.upgradePars["timeEvery"] = v1.dbObj.pars["dmg_every"];
                }

                if (v1.upgradePars["timeLeft"] <= 0)
                {
                    toDelete.Add(v1);
                    cnt++;
                }
                
            }

            if (v.Value.upgradePars.ContainsKey("regenEvery"))
            {
                v.Value.upgradePars["regenEvery"] -=  TimeManager.LAST_DT;
                if (v.Value.upgradePars["regenEvery"] < 0)
                {
                    //deal damage
                    DealRegen(v.Value);
                    v.Value.upgradePars["regenEvery"] = 1;
                }
            }

            //
            if (cnt > 0)
            {
                foreach (var v1 in toDelete)
                {
                    v.Value.timedBuffs.Remove(v1);
                    v.Value.RemoveViz(v1.dbObj.ID);
                }

                v.Value.RecalcPars();

            }

            foreach (var v1 in v.Value.timedBuffs)
                {
                    if (!v.Value.HasVis(v1.dbObj.ID))
                        v.Value.AddViz(v1.dbObj.ID);
                }
            //shield
            float sh = 0;
            sh += v.Value.GetPar("shield");
            foreach (var v1 in v.Value.timedBuffs)
                sh += v1.GetPar("shield");

            if (sh > 0 && v.Value.main != null && !v.Value.HasVis("shield"))
                v.Value.AddViz("shield");
            
            if (sh <= 0 && v.Value.main != null && v.Value.HasVis("shield"))
                v.Value.RemoveViz("shield");
            //

        }
        
    }

    public void NextWeapon()
    {
        var all = mainPlayer.inventory.FindAll(x => x.it == ItemType.item && x.GetPar("slot") == 1);
        var a = mainPlayer.inventory.Find(x => x.GetPar("used_slot") == 1);
        if (a == null)
        {
            if (all.Count > 0)
            {
                Equip(mainPlayer, all[0]);
            }
        }
        else
        {
            int l = -1;
            for (int i = 0; i < all.Count; i++)
                if (all[i] == a)
                {
                    l = i;
                }

            var b = all[(l + 1) % all.Count];
            Equip(mainPlayer, b);

        }
    }
    
    public bool InIteration = false;
    public string lastBattle;
    public int lastBattleResult = 0;
    
    public IEnumerator OneIteration(bool exceptMain = false, float tm = 0.5f, string metaContain = "")
    {
        if (InIteration) yield break;
        InIteration = true;
        /*
        foreach (var v in all)
        {
            if (v.Value.META_TAGS.Contains("my_side") || v.Value.META_TAGS.Contains("wave")) acts.Add(v.Value);
        }
        */
        combats.RemoveAll(x => !x.HasVis("combat") || x.main.GetComponentInChildren<XDcombat>() == null);

        for (int i = 0; i < combats.Count; i++)
        {
            if (exceptMain && combats[i].RID == "main_player") continue;
            if (metaContain != "" && !combats[i].META_TAGS.Contains(metaContain))
            {
                continue;
            }
            
            if (!combats[i].HasVis("combat")) continue;
            if (combats[i].GetPar("do_nothing") > 0) continue;

            while (combats[i].main.name.IndexOf("_move") >= 0)
            {
                yield return null;
            }
            
            var gg = combats[i].visuals["combat"].GetComponent<XDcombat>();
            if (gg == null) continue;
            
            gg.Iteration(true, reqTag: metaContain); 
            yield return new WaitForSeconds(tm);
        }

        InIteration = false;
    }

    public Vector3 MovePath(RObj mon, RObj c)
    {
        mon.SetScale(c.Position.x > mon.Position.x);
        
        var c0 = ConfigLoader.GetMetaParamValue("mode_isometric");
        var c1 = ConfigLoader.GetMetaParamValue("mode_manhattan");
        var c2 = ConfigLoader.GetMetaParamValue("mode_hex");

        if (c0 < 1 && c2 < 1 && c1 < 1)
        {
            if (ConfigLoader.GetMetaParamValue("use_2d_navmesh") > 0)
            {
                var g = mon.main.GetOrAddComponent<PathfindingMovement>();
                
                //g.target = c.main.transform;
                //g.FindNewPath();
                
                if (g.currentPath == null ||g.currentPath.Count == 0 || 
                    g.target != c.main.transform || g.CheckDistance() > 0.5f)
                {
                    g.SetTarget(c.main.transform);
                }
                
                
                int where = 0;
                float dp = mon.GetPar("speed") * ConfigLoader.GetMetaParamValue("global_speed");
                
                //no path for some reason ?
                if (g.currentPath.Count <= g.currentWaypointIndex) return Vector3.zero;
                
                var f1 = new Vector3(g.currentPath[g.currentWaypointIndex].x, g.currentPath[g.currentWaypointIndex].y, g.currentPath[g.currentWaypointIndex].z) - mon.Position;
                
                if (f1.magnitude < dp * Time.deltaTime)
                {
                    g.currentWaypointIndex++;
                }
                else
                {
                    var vec = (new Vector3(g.currentPath[g.currentWaypointIndex].x, g.currentPath[g.currentWaypointIndex].y, g.currentPath[g.currentWaypointIndex].z) -
                               mon.Position).normalized;

                    mon.main.transform.position += vec * Time.deltaTime * dp;
                }

            }
            else
            {
                var vec = (c.Position - mon.Position).normalized;
                float dp = mon.GetPar("speed") * ConfigLoader.GetMetaParamValue("global_speed");
                mon.main.transform.position += vec * Time.deltaTime * dp;
            }

            mon.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("walk");
        }
        else
        {
            var v = PositionSetter.instance.GetPath(mon.Position, c.Position, mon.dbObj.sizeX, mon.dbObj.sizeY, mon);
            if (v.Count > 0)
            {
                //move to first
                var ep = PositionSetter.instance.floors[v[0].Item1, v[0].Item2];
                mon.ref_pos_x = v[0].Item1;
                mon.ref_pos_y = v[0].Item2;
                
                UtilsControl.Instance.MoveTo(mon.main.transform, ConfigLoader.GetMetaParamValue("global_move"), ep.transform.position, () =>
                {
                    mon.ref_pos_x = v[0].Item1;
                    mon.ref_pos_y = v[0].Item2;
                    if (mon == mainPlayer)
                    {
                        EventManager.INV("main_move", new ArgPass{pos = mon.Position});
                    }
                }, null, useRight:false);
            }
        }
        
        return Vector3.zero;
    }

    public bool IsItemLevelapable(RObj item)
    {
        var g = UpgradeSystem.instance.GetPrice(item, "upgrade");
        return HaveAmount(g);
    }

    public bool IsItemMergable(RObj item)
    {
        return false;
    }

    public int HaveUncollectedMail()
    {
        return 0;
    }
}
