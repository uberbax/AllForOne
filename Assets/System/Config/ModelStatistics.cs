using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public partial class ModelStatistics : MonoBehaviour
{
    public static ModelStatistics instance;

    public Dictionary<string, BattleStat> battleStatsRound = new Dictionary<string, BattleStat>();
    public Dictionary<string, BattleStat> battleStats = new Dictionary<string, BattleStat>();

    private void OnDestroy()
    {
        instance = null;
    }

    private void Awake()
    {
        instance = this;

        //var tt = GetComponent<Inventary>();
        //tt.onAdd = UpdateTasksGet;
        //tt.onDel = UpdateTasksDel;
        
        EventManager.SUB("UNIT_DEAD", UnitDead);
        EventManager.SUB("USER_PRESS_BATTLE", UserPressBattle);
        EventManager.SUB("USER_LVLUP_UNIT", UserLvlupUnit);
        EventManager.SUB("USER_DRAW_UNIT", UserDrawUnit);
    }

    private void UserDrawUnit(ArgPass obj)
    {
        IncreaseStatValue("draw_unit", obj.num);
    }

    private void UserLvlupUnit(ArgPass obj)
    {
        IncreaseStatValue("lvlup_unit", 1);
    }
    
    

    private void UserPressBattle(ArgPass obj)
    {
        //clear stats
        //after that continue gather them
        battleStats.Clear();        
        battleStatsRound.Clear();
        
        IncreaseStatValue("play_battle", 1);
    }

    public int GetPlaceInStat(string who, int level, string tp)
    {
        int res = battleStats.Count;
        var l = "";
        BattleStat bs = null;
        foreach (var v in battleStats)
        {
            if (v.Value.tag != "player") continue;
            if (v.Value.nm.ToLower() == who && v.Value.lvl == level)
            {
                bs = v.Value;
                l = v.Key;
            }
        }

        if (l == "") return res;

        res = 0;
        if (tp == "dealt")
        {
            foreach (var v in battleStats)
            {
                if (v.Value.damageDealt > bs.damageDealt) res++;
            }
        }
        if (tp == "taken")
        {
            foreach (var v in battleStats)
            {
                if (v.Value.damageShield > bs.damageShield) res++;
            }
        }
        if (tp == "heal")
        {
            foreach (var v in battleStats)
            {
                if (v.Value.damageHeal > bs.damageHeal) res++;
            }
        }
        
        
        
        return res;
    }
    
    public long GetDealt(RObj who)
    {
        foreach (var v in battleStatsRound)
        {
            if (v.Value.mon == who)
                return v.Value.damageDealt;
        }

        return 0;
    }
    public void MonsterRegisterTaken(RObj who1, int amount)
    {
        if (who1.dbObj.ID.IndexOf("wall") >= 0 || who1.GetPar("dont_track") >= 0)
            return;
        //if (MainStates.instance.curBattleState.Value != MainStates.BattleStates.in_progress) return;

        RObj who = who1;
        //if (who1.owner != null) who = who1.owner;
        
        if (amount > 0)
        {
            BattleStat bs = new BattleStat();
            bs.id = who.RID;
            if (battleStats.ContainsKey(who.RID))
                bs = battleStats[who.RID];
            else
            {
                bs.mon = who;
                bs.tag = who.tags[0];
                bs.nm = who.dbObj.ID;
                bs.id = who.RID;
                
                bs.lvl = (int)who.GetPar("level");
                bs.race = who.labels[0];
                bs.classR = who.labels[1];
                
                bs.ava = ResourceHolder.instance.GetAva(who.dbObj.ID.ToLower());
                battleStats.Add(bs.id, bs);
            }

            bs.damageShield += amount;
        }
        
        if (amount > 0)
        {
            BattleStat bs = new BattleStat();
            bs.id = who.RID;
            if (battleStatsRound.ContainsKey(who.RID))
                bs = battleStatsRound[who.RID];
            else
            {
                bs.mon = who;
                bs.tag = who.tags[0];
                bs.nm = who.dbObj.ID;
                bs.id = who.RID;
                
                bs.lvl = (int)who.GetPar("level");
                bs.race = who.labels[0];
                bs.classR = who.labels[1];
                
                bs.ava = ResourceHolder.instance.GetAva(who.dbObj.ID.ToLower());
                battleStatsRound.Add(bs.id, bs);
            }

            bs.damageShield += amount;
        }
    }
    public void MonsterRegisterDealt(RObj who1, int amount)
    {
        if (who1.dbObj.ID.IndexOf("wall") >= 0 || who1.GetPar("dont_track") > 0)
            return;
        //if (MainStates.instance.curBattleState.Value != MainStates.BattleStates.in_progress) return;
        
        RObj who = who1;
        //if (who1.owner != null) who = who1.owner;

        BattleStat bs = new BattleStat();
        bs.id = who.RID;
        if (battleStats.ContainsKey(who.RID))
            bs = battleStats[who.RID];
        else
        {
            bs.mon = who;
            bs.tag = who.tags[0];
            bs.nm = who.dbObj.ID;
            bs.id = who.RID;
            
            bs.lvl = (int)who.GetPar("level");
            bs.race = who.labels.Count > 0 ? who.labels[0] : "animal";
            bs.classR = who.labels.Count > 1 ? who.labels[1] : "animal";
            
            bs.ava = ResourceHolder.instance.GetAva(who.dbObj.ID.ToLower());
            battleStats.Add(bs.id, bs);
        }

        if (amount > 0)
            bs.damageDealt += amount;
        else
            bs.damageHeal += -amount;
        
        //well, same for round stats
        bs = new BattleStat();
        bs.id = who.RID;
        if (battleStatsRound.ContainsKey(who.RID))
            bs = battleStatsRound[who.RID];
        else
        {
            bs.mon = who;
            bs.tag = who.tags[0];
            bs.nm = who.dbObj.ID;
            bs.id = who.RID;
            
            bs.lvl = (int)who.GetPar("level");
            bs.race = who.labels.Count > 0 ? who.labels[0] : "animal";
            bs.classR = who.labels.Count > 1 ? who.labels[1] : "animal";
            
            bs.ava = ResourceHolder.instance.GetAva(who.dbObj.ID.ToLower());
            battleStatsRound.Add(bs.id, bs);
        }

        if (amount > 0)
            bs.damageDealt += amount;
        else
            bs.damageHeal += -amount;
    }

    public (float, float, float, float) GetMaxStat()
    {
        (float, float, float, float) mm = (0,0,0,0);

    foreach (var v in battleStats)
        {
            if (v.Value.damageDealt > mm.Item1) mm.Item1 = v.Value.damageDealt;
            if (v.Value.damageHeal > mm.Item1) mm.Item1 = v.Value.damageHeal;
            if (v.Value.damageShield > mm.Item1) mm.Item1 = v.Value.damageShield;

            if (v.Value.damageDealt > mm.Item2) mm.Item2 = v.Value.damageDealt;
            if (v.Value.damageHeal > mm.Item3) mm.Item3 = v.Value.damageHeal;
            if (v.Value.damageShield > mm.Item4) mm.Item4 = v.Value.damageShield;

        }

        return mm;
    }
    
    private void UnitDead(ArgPass obj)
    {
        var mm = obj.who;
        if (mm.tags.Contains("player"))
        {
            IncreaseStatValue("lost_any", 1);
            return;
        }
        
        UpdateTasksKill(mm);
    }

    public void TakeDynamic(string id)
    {
        MainStates.instance.playerData.dynTaken.Add(id);
    }
    
    public int GetStatValue(string val, bool withAdd = true)
    {
        var gg = MainStates.instance.playerData.playerStats.Find(x => string.Equals(x.Key, val));
        int res = 0;
        if (gg == null)
        {
            res = 0;
        }
        else
        {
            res = gg.Value;
        }

        if (withAdd)
        {
            var gg1 = MainStates.instance.playerData.playerStats.Find(x => string.Equals(x.Key,"add_" + val));
            if (gg1 != null)
                res += gg1.Value;
        }

        return res;

    }

    public string GetTaskByReq(Bon req)
    {
        foreach (var v in DatabaseAll.instance.allTasks)
        {
            var f = v.Value;
            foreach (var c in f.reqFinish)
            {
                if (c.val == req.Value.ToString() && c.what == req.Key)
                {
                    return f.id;
                }
                
            }
        }

        return "";
    }
    
    public string GetStatByTask(TaskType tp, string val)
    {
        if (tp == TaskType.battle_complete)
        {
            return "finish_" + val;
        }
        else if (tp == TaskType.gather)
        {
            return "get_" + val;
        }
        else if (tp == TaskType.have_stat)
        {
            return val;
        }
        else if (tp == TaskType.spend)
        {
            return "spend_" + val;
        }
        else if (tp == TaskType.kill)
        {
            return "kill_" + val;
        }

        return val;
    }
    public int GetStatValue(TaskType tp, string val)
    {
        if (tp == TaskType.have_item)
        {
            var td = MainStates.instance.playerData.inventory.Find(x => x.dbObj.ID == val);
            if (td == null) return 0;
            return (int)td.GetPar("amount");
        }
        
        var tt = GetStatByTask(tp, val);
        return GetStatValue(tt);
    }
    
    public void SetStatValueForce(string val, int kk)
    {
        var gg = MainStates.instance.playerData.playerStats.Find(x => x.Key == val);
        if (gg == null)
        {
            MainStates.instance.playerData.playerStats.Add(new Bon{Key = val, Value = kk});
        }
        else
        {
            gg.Value = kk;
        }
        UpdateAllTasks();
    }
    
    //if bigger
    public void SetStatValue(TaskType tsk, string val, int kk)
    {
        var ll = GetStatByTask(tsk, val);
        SetStatValue(ll, kk);
    }
    public void SetStatValue(string val, int kk)
    {
        var gg = MainStates.instance.playerData.playerStats.Find(x => x.Key == val);
        if (gg == null)
        {
            MainStates.instance.playerData.playerStats.Add(new Bon{Key = val, Value = kk});
        }
        else
        {
            if (gg.Value < kk)
                gg.Value = kk;
        }
        UpdateAllTasks();
    }

    public void IncreaseStatValue(string val, int kk)
    {
        var gg = MainStates.instance.playerData.playerStats.Find(x => x.Key == val);
        if (gg == null)
        {
            MainStates.instance.playerData.playerStats.Add(new Bon{Key = val, Value = kk});
        }
        else
        {
            gg.Value += kk;
        }
        UpdateAllTasks();
    }


    public void UpdateTasksGet(List<RObj> itms)
    {
        //taken item
        foreach (var v in itms)
        {
            IncreaseStatValue("get_" + v.dbObj.ID, (int)v.upgradePars["amount"]);
        }
        //
        //tasks
        UpdateAllTasks();
    }

    public void UpdateTasksDel(List<Bon> itms)
    {
        //taken item
        foreach (var v in itms)
        {
            IncreaseStatValue("del_" + v.Key, v.Value);
        }
        //
        //tasks
        UpdateAllTasks();
    }
    public void UpdateTasksKill(RObj mon)
    {
        //killed monster
        IncreaseStatValue("kill_" + mon.dbObj.ID, 1);
        IncreaseStatValue("kill_any", 1);
        UpdateAllTasks();
    }

    public bool GetCompareResult(string stat, string compar, int val, int dlta)
    {
        var aa = GetStatValue(stat);

        bool q = true;
        
        var aa1 = val;
        if (compar == "==" && aa+dlta != aa1) q = false;
        if (compar == ">=" && aa+dlta < aa1) q = false;
        if (compar == ">" && aa+dlta <= aa1) q = false;
        if (compar == "<=" && aa+dlta > aa1) q = false;                        
        if (compar == "<" && aa+dlta >= aa1) q = false;

        return q;

    }

    public bool IsReady(List<UnoReq> reqs, int startStat)
    {
        bool q = true;
                foreach (var g in reqs)
                {
                    if (g.typo == TaskType.gather)
                    {
                        var aa = GetStatValue("get_" + g.what);
                        var aa1 = int.Parse(g.val);
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.battle_complete)
                    {
                        var aa = GetStatValue("finish_" + g.what);
                        var aa1 = int.Parse(g.val) + startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.complete_other)
                    {
                        var bb = MainStates.instance.playerData.playerTasks.Find(x => x.id == g.what && x.taken);
                        if (bb == null) q = false;
                    }
                    else if (g.typo == TaskType.have_stat)
                    {
                        var aa = GetStatValue(g.what);
                        var aa1 = int.Parse(g.val);
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.have_dyn)
                    {
                        var gg = MainStates.instance.playerData.dynTaken.Contains(g.what);
                        q = gg;
                    }
                    else if (g.typo == TaskType.kill)
                    {
                        var aa = GetStatValue(g.what);
                        var aa1 = int.Parse("kill_" + g.val);
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.spend)
                    {
                        var aa = GetStatValue("spend_" + g.what);
                        var aa1 = int.Parse(g.val);
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.have_item)
                    {
                        var aa = MainStates.instance.all["main_player"].inventory.Sum(x =>
                        {
                            if (x.dbObj.ID == g.what) return x.GetPar("amount");
                            else return 0;
                        });
                        var aa1 = int.Parse(g.val) + startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.have_par)
                    {
                        var aa = MainStates.instance.all["main_player"].GetPar(g.what);
                        var aa1 = int.Parse(g.val) + startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.have_skill)
                    {
                        var aas = MainStates.instance.all["main_player"].buffs.Find(x => x.dbObj.ID == g.what);
                        int aa = 0;
                        if (aas != null) aa = (int)aas.GetPar("level");
                        
                        var aa1 = int.Parse(g.val) + startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else
                    {
                        //talk
                    }
                    
                }

                return q;
    }

    public void UpdateAllTasks()
    {
        //we take task from 
        foreach (var vv in DatabaseAll.instance.allTasks)
        {
            var v = vv.Value;
            //playerTasks
            var tt = MainStates.instance.playerData.playerTasks.Find(x => x.id == v.id);
            if (tt != null && (tt.taken || tt.completed)) continue;
            
            //check if completed
            //playerStats
            if (tt != null)
            {
                //its started
                bool q = true;
                foreach (var g in v.reqFinish)
                {
                    if (g.typo == TaskType.gather)
                    {
                        var aa = GetStatValue("get_" + g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.battle_complete)
                    {
                        var aa = GetStatValue("finish_" + g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.complete_other)
                    {
                        var bb = MainStates.instance.playerData.playerTasks.Find(x => x.id == g.what && x.taken);
                        if (bb == null) q = false;
                    }
                    else if (g.typo == TaskType.have_stat)
                    {
                        var aa = GetStatValue(g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.kill)
                    {
                        var aa = GetStatValue("kill_" + g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.spend)
                    {
                        var aa = GetStatValue("spend_" + g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.have_dyn)
                    {
                        q = MainStates.instance.playerData.dynTaken.Contains(g.what);
                    }
                    else if (g.typo == TaskType.have_item)
                    {
                        var aa = MainStates.instance.all["main_player"].inventory.Sum(x =>
                        {
                            if (x.dbObj.ID == g.what) return x.GetPar("amount");
                            else return 0;
                        });
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else if (g.typo == TaskType.have_par)
                    {
                        var aa = MainStates.instance.all["main_player"].GetPar(g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        if (g.compar == "==" && aa != aa1) q = false;
                        if (g.compar == ">=" && aa < aa1) q = false;
                        if (g.compar == ">" && aa <= aa1) q = false;
                        if (g.compar == "<=" && aa > aa1) q = false;                        
                        if (g.compar == "<" && aa >= aa1) q = false;
                    }
                    else
                    {
                        
                        //talk
                    }
                    
                }

                if (q)
                {
                    tt.completed = true;
                    if (ConfigLoader.GetMetaParamValue("autotake_tasks") > 0)
                    {
                        tt.taken = true;
                        if (v.autoTake)
                        {
                            MainStates.instance.AddItems(v.rewards);
                        }
                    }
                    //can be grabbed
                }
            }
            else
            {
                //its not started
                bool q = true;
                q = IsReady(v.reqStart, tt == null ? 0 : tt.startStat);
                
                if (q)
                {
                    if (v.startStatNew)
                    {
                        //we calculate initial stat
                        
                        //calculate it from require finish
                        var pp = new TasksProg
                        {
                            completed = false, id = v.id, taken = false,
                            startTime = DateTimeOffset.Now.ToUnixTimeSeconds()
                        };

                        int st = 0;
                        foreach (var g in v.reqFinish)
                        {
                            if (g.typo == TaskType.gather)
                            {
                                var aa = GetStatValue("get_" + g.what);
                                var aa1 = int.Parse(g.val);
                                st = aa;
                                if (g.compar == "==" && aa != aa1) q = false;
                                if (g.compar == ">=" && aa < aa1) q = false;
                                if (g.compar == ">" && aa <= aa1) q = false;
                                if (g.compar == "<=" && aa > aa1) q = false;                        
                                if (g.compar == "<" && aa >= aa1) q = false;
                            }
                            else if (g.typo == TaskType.battle_complete)
                            {
                                var aa = GetStatValue("finish_" + g.what);
                                var aa1 = int.Parse(g.val) + tt.startStat;
                                if (g.compar == "==" && aa != aa1) q = false;
                                if (g.compar == ">=" && aa < aa1) q = false;
                                if (g.compar == ">" && aa <= aa1) q = false;
                                if (g.compar == "<=" && aa > aa1) q = false;                        
                                if (g.compar == "<" && aa >= aa1) q = false;
                            }
                            else if (g.typo == TaskType.complete_other)
                            {
                                var bb = MainStates.instance.playerData.playerTasks.Find(x => x.id == g.what && x.taken);
                                if (bb == null) q = false;
                            }
                            else if (g.typo == TaskType.have_dyn)
                            {
                                q = MainStates.instance.playerData.dynTaken.Contains(g.what);
                            }
                            else if (g.typo == TaskType.have_stat)
                            {
                                var aa = GetStatValue(g.what);
                                var aa1 = int.Parse(g.val);
                                st = aa;
                                if (g.compar == "==" && aa != aa1) q = false;
                                if (g.compar == ">=" && aa < aa1) q = false;
                                if (g.compar == ">" && aa <= aa1) q = false;
                                if (g.compar == "<=" && aa > aa1) q = false;                        
                                if (g.compar == "<" && aa >= aa1) q = false;
                            }
                            else if (g.typo == TaskType.kill)
                            {
                                var aa = GetStatValue(g.what);
                                var aa1 = int.Parse(g.val);
                                st = aa;
                                if (g.compar == "==" && aa != aa1) q = false;
                                if (g.compar == ">=" && aa < aa1) q = false;
                                if (g.compar == ">" && aa <= aa1) q = false;
                                if (g.compar == "<=" && aa > aa1) q = false;                        
                                if (g.compar == "<" && aa >= aa1) q = false;
                            }
                            else if (g.typo == TaskType.spend)
                            {
                                var aa = GetStatValue("spend_" + g.what);
                                var aa1 = int.Parse(g.val);
                                st = aa;
                                if (g.compar == "==" && aa != aa1) q = false;
                                if (g.compar == ">=" && aa < aa1) q = false;
                                if (g.compar == ">" && aa <= aa1) q = false;
                                if (g.compar == "<=" && aa > aa1) q = false;                        
                                if (g.compar == "<" && aa >= aa1) q = false;
                            }
                            else
                            {
                                //talk
                            }
                            
                        }

                        pp.startStat = st;
                        
                        MainStates.instance.playerData.playerTasks.Add(pp);
                    }
                    else
                    {
                        MainStates.instance.playerData.playerTasks.Add(new TasksProg{completed = false, id = v.id, taken = false, startTime = DateTimeOffset.Now.ToUnixTimeSeconds()});                        
                    }
                    

                }
            }
        }
    }

    public bool CheckCondition(UnoCond u)
    {
        bool b = IsReady(u.reqs, 0);
        return b;
    }

    public bool CheckCondition(List<UnoReq> u)
    {
        bool b = IsReady(u, 0);
        return b;
    }
    public void GetMeProgress(ElTasko v, out float me, out float all)
    {
        //playerTasks
        me = 0;
        all = 1;
        //Debug.Log(v.id + " " + MainStates.instance.playerData.playerTasks.Count);
        var tt = MainStates.instance.playerData.playerTasks.Find(x => x.id == v.id);
        //if (tt != null && (tt.taken || tt.completed)) return;
        //Debug.Log(tt);
        
        //cant be null ?
        bool q = true;
                foreach (var g in v.reqFinish)
                {
                    if (g.typo == TaskType.gather)
                    {
                        var aa = GetStatValue("get_" + g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        me = aa - tt.startStat;
                        all = int.Parse(g.val);
                    }
                    if (g.typo == TaskType.battle_complete)
                    {
                        var aa = GetStatValue("finish_" + g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        me = aa - tt.startStat;
                        all = int.Parse(g.val);
                    }
                    else if (g.typo == TaskType.complete_other)
                    {
                        var bb = MainStates.instance.playerData.playerTasks.Find(x => x.id == g.what && x.taken);
                        all = 1;
                        if (bb == null) q = false;
                        else me = 1;
                    }
                    else if (g.typo == TaskType.have_stat)
                    {
                        var aa = GetStatValue(g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        me = aa - tt.startStat;
                        all = int.Parse(g.val);
                    }
                    else if (g.typo == TaskType.kill)
                    {
                        var aa = GetStatValue("kill_" + g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        me = aa - tt.startStat;
                        all = int.Parse(g.val);
                    }
                    else if (g.typo == TaskType.spend)
                    {
                        var aa = GetStatValue("spend_" + g.what);
                        var aa1 = int.Parse(g.val) + tt.startStat;
                        me = aa - tt.startStat;
                        all = int.Parse(g.val);
                    }
                    else if (g.typo == TaskType.have_item)
                    {
                        var td = MainStates.instance.playerData.inventory.Find(x => x.dbObj.ID == g.what);
                        if (td != null)
                        {
                            me = td.GetPar("amount");
                        }
                        else me = 0;
                        all = int.Parse(g.val);

                    }
                    else
                    {
                        //talk
                    }
                    
                }
    }

    public void TakeTaskReward(ElTasko task)
    {
        var bb = MainStates.instance.playerData.playerTasks.Find(x => x.id == task.id);
        bb.takenTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        bb.taken = true;

        MainStates.instance.AddItems(task.rewards);

    }

    public string GetTaskDescription(ElTasko tsk)
    {
        if (tsk.description.Length > 2) return tsk.description;
        else
        {
            string s = "";
            if (tsk.reqFinish[0].typo == TaskType.gather)
            {
                s = "Gather " + tsk.reqFinish[0].val + " " + tsk.reqFinish[0].what;
            }
            else if (tsk.reqFinish[0].typo == TaskType.kill)
            {
                s = "Kill " + tsk.reqFinish[0].val + " " + (tsk.reqFinish[0].what == "any" ? "monsters (any)" : tsk.reqFinish[0].what );
            }
            else if (tsk.reqFinish[0].typo == TaskType.spend)
            {
                s = "Spend " + tsk.reqFinish[0].val + " " + tsk.reqFinish[0].what;
            }
            
            return s;
        }
    }
    public void ItemBuyed(string wha)
    {

        var yy = MainStates.instance.playerData.playerShop.Find(x => x.id == wha);
        if (yy == null)
        {
            var ff = new TasksProg();
            ff.id = wha;
            ff.curNum = 1;
            ff.takenTime = TimeManager.instance.GetCurrentTimeLong();
            MainStates.instance.playerData.playerShop.Add(ff);
        }
        else
        {
            yy.curNum++;
            yy.takenTime = TimeManager.instance.GetCurrentTimeLong();
        }

    }
    
    public string CalcMe(string str)
    {
        if (!str.Contains('{'))
            return str;

        var a0 = str.IndexOf('{');
        var a1 = str.IndexOf('}');

        var ee = GetStatValue(str. Substring(a0 + 1, a1 - a0 - 1));
        var bb = str.Substring(a0, a1 - a0+1);

        return str.Replace(bb, ee.ToString());

    }

    public int GetPlayerValue(string s)
    {
        var hh = MainStates.instance.playerData.cGame.playerPars.Find(x => x.Key == s);
        if (hh != null)
            return hh.Value;
        else return 0;
    }

    public void SetPlayerValue(string s, int val)
    {
        var hh = MainStates.instance.playerData.cGame.playerPars.Find(x => x.Key == s);
        if (hh != null)
            hh.Value = val;
        else
            MainStates.instance.playerData.cGame.playerPars.Add( new Bon { Key = s, Value = val });
    }

    public List<string> GetPlayerKeys(string pref)
    {
        List<string> keys = new List<string>();
        foreach (var v in MainStates.instance.playerData.cGame.playerPars)
        {
            if (v.Key.IndexOf(pref) >= 0) keys.Add(v.Key);
        }
        return keys;
    }

}

public enum TaskType
{
    kill,
    gather,
    complete_other,
    have_stat,
    talk,
    spend,
    have_item,
    battle_complete,
    have_par,
    have_skill,
    have_dyn
}


[System.Serializable]
public class ElTasko
{
    public enum Category
    {
        common,
        daily,
        weekly,
        events,
        
        
        
        gold,
        gem,
        exchange,
        bundles,
        
        
        other
    }

    public Category category = Category.common;
    
    public string id;
    public List<Bon> rewards = new List<Bon>();
    public List<UnoReq> reqStart = new List<UnoReq>();
    public List<UnoReq> reqFinish = new List<UnoReq>();
    public List<Bon> reqItems = new List<Bon>();
    public long expire;
    public string description;

    public Sprite icon;
    //
    public string realID;
    public float realPrice;
    public int limit = -1;
    public float freeEvery = -1;
    public bool startStatNew = false;
    public bool autoTake = false;
}


[System.Serializable]
public class UnoCond
{
    public string id = "";
    public List<UnoReq> reqs = new List<UnoReq>();
}


[System.Serializable]
public class UnoReq
{
    public TaskType typo;
    public string what;
    public string val;
    // = < > <= >=
    public string compar;
}

[System.Serializable]
public class TasksProg
{
    public string id;
    public bool completed;
    public bool taken;
    
    public long takenTime;
    public long startTime;
    //only item buyed
    public int curNum = 0;
    
    public int startStat = 0;
}

public class BattleStat
{
    public string id;
    public string nm;
    public string tag = "player";
    public long damageDealt = 0;
    public long damageHeal = 0;
    public long damageShield = 0;
    public bool dead = false;
    //later change
    public Sprite ava;
    public RObj mon;

    //
    public int lvl = 1;
    public string classR = "warrior";
    public string race = "human";
}

[System.Serializable]
public class PlayerData
{
    public PGame pGame = new PGame(); 
    public CGame cGame = new CGame(); 
    //
    public List<Bon> playerStats = new List<Bon>();
    public List<string> dynTaken = new List<string>();
    public List<TasksProg> playerTasks = new List<TasksProg>();
    public List<TasksProg> playerShop = new List<TasksProg>();
    public List<TasksProg> playerMail = new List<TasksProg>();
    public List<Building> buildings = new List<Building>();
    
    //???
    public List<RObj> inventory = new List<RObj>();

}

[System.Serializable]
public class PGame
{
    public int turnNum;
    public List<URace> races = new List<URace>();
}

[System.Serializable]
public class URace
{
    public bool isPlayer = false;
    public int score;
    public int curStars;
    public int starsIncome;
    public Races race;
    public List<Bon> tech = new List<Bon>();

    //units, etc

}

[System.Serializable]
public class CGame
{
    public List<Bon> playerPars = new List<Bon>();
    //units, etc
    

}

public enum Races
{
    human,
    elf,
    orc
}


[System.Serializable]
public class Building
{
    public float xPos;
    public float yPos;
    public float zPos;

    public string buildingName = "empty";
    public int bLevel = 1;


    public long startBuild = 0;
    public long startUpgrade = 0;


    public List<DLong> recipes = new List<DLong>();

    public string buildFor = "";

}


[System.Serializable]
public class DLong
{
    public string Key = "";
    public long strt = 0;
}