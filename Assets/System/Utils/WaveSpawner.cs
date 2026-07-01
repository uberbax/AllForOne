using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class WaveSpawner : MonoBehaviour
{
    public string battleName = "";
    public bool freezeUntilWaveComplete = false;
    public bool spawnByCommand = false;
    public bool spawnAsBattle = false;
    
    
    public bool linearSpawn = true;
    public bool radialSpawn = false;
    public bool randomSpawn = false;
    
    public Transform lo;
    public Transform hi;

    private bool loaded = false;
    private int cur = 0;
    private FormatBattles battle;
    public float timeNext;
    
    public List<string> addedBehaviors = new List<string>();
    
    public List<UnoSide> sides = new List<UnoSide>();
    //debug purpose
    //chose battle
    public TMP_Dropdown dropdown;
    
    public static WaveSpawner instance;

    public bool IsDone = false;
    private void Awake()
    {
        instance = this;
    }

    public void ClearWithMeta(string metaTag)
    {
        List<string> keysToRemove = new List<string>();
        foreach (var v in MainStates.instance.all)
        {
            if (v.Value.META_TAGS.Contains(metaTag))
            {
                keysToRemove.Add(v.Key);
                Destroy(v.Value.main);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            MainStates.instance.all.Remove(key);
        }
    }
    
    public static void ClearExcept(string only = "")
    {
        List<RObj> keysToRemove = new List<RObj>();
        foreach (var v in MainStates.instance.combats)
        {
            if (only != "" && v.META_TAGS.Contains(only))
            {
                keysToRemove.Add(v);
                Destroy(v.main);
                continue;
            }
            
            if (v.RID != "main_player" && only == "")
            {
                keysToRemove.Add(v);
                Destroy(v.main);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            MainStates.instance.combats.Remove(key);
        }
    }
    
    public void SpawnAnyByStats(Dictionary<string, string> map, string tg, Transform lo, Transform hi, string META_TAG, Dictionary<string, List<Vector3>> savedPos)
    {
        List<string> stats = map.Keys.ToList();
        foreach (var v in stats)
        {
            var tr = map[v];
            var hh = savedPos[tr];
            var val = ModelStatistics.instance.GetStatValue(v);
            for (int i = 0; i < val; i++)
            {
                var enm1 = new RObj(tr, 1, 1,
                    true, transform.position, true, ItemType.monster, isEnemy: tg == "enemy", overID:v == "hero_res"?"second_main":"");
        
                enm1.AddViz("hp");
                enm1.AddViz("coll#val:0.0");
                enm1.AddViz("dmg_track");
                enm1.AddViz("flash");
                enm1.AddViz("death");
                enm1.AddViz("drop");
                enm1.AddViz("combat");
                enm1.AddViz("animator#pr:1");
                enm1.AddViz("realcol#val:0.2");
                enm1.AddViz("drag");
                enm1.AddMeta(META_TAG);
                
                //enm1.AddViz("changemat");
                if (i < hh.Count)
                {
                    enm1.main.transform.position = hh[i];
                }
                else
                {
                    enm1.main.transform.position = new Vector3(Random.Range(lo.position.x, hi.position.x),
                        Random.Range(lo.position.y, hi.position.y),
                        Random.Range(lo.position.z, hi.position.z));
                }
            }
        }
    }

    public void AssignBattle(string bt)
    {
        battleName = bt;
        battle = ConfigLoader.Instance.battles.Find(x => x.battleName == battleName);

        spawnByCommand = true;
        IsDone = false;
    }
    
    private void Start()
    {
        battle = ConfigLoader.Instance.battles.Find(x => x.battleName == battleName);
        if (battle == null)
        {
            Invoke("Start", 0.1f);
            return;
        }
        else loaded = true;
        
        EventManager.SUB("spawn_wave", (x) =>
        {
            //DoSpawn();
            DoSpawnAll();
        });

        if (dropdown != null)
        {
            List<string> options = new List<string>();
            foreach (var v in ConfigLoader.Instance.battles)
            {
                options.Add(v.battleName);
            }
            dropdown.AddOptions(options);
            dropdown.onValueChanged.AddListener( (x) =>
            {
                battleName = dropdown.options[dropdown.value].text;
                battle = ConfigLoader.Instance.battles.Find(x => x.battleName == battleName);
            });
        }
    }
    
    private void Update()
    {
        if (!loaded) return;
        if (spawnByCommand) return;
        
        //done spawning ?
        if (cur >= battle.enemies.amounts.Count)
        {
            IsDone = true;
            return;
        }
        
        
        timeNext = battle.enemies.timeSpawns[cur] - TimeManager.instance.tm;
        if (TimeManager.instance.tm >= battle.enemies.timeSpawns[cur])
        {
            DoSpawn();
            //spawn
            cur++;
            if (freezeUntilWaveComplete)
            {
                TimeManager.instance.spd = 0;
            }
        }
    }

    public async void DoSpawn()
    {
        for (int i = 0; i < battle.enemies.amounts[cur]; i++)
                    {
                        var enm1 = new RObj(battle.enemies.heroLevelPosition[cur].Item1, 1, 1,
                            true, transform.position, true, ItemType.monster, isEnemy: true);
        
                        enm1.AddViz("hp");
                        enm1.AddViz("coll#val:0.5");
                        enm1.AddViz("dmg_track");
                        enm1.AddViz("flash");
                        enm1.AddViz("death");
                        enm1.AddViz("drop");
                        enm1.AddViz("combat");
                        enm1.AddViz("animator#pr:1");
                        //enm1.AddViz("realcol");
                        enm1.AddMeta("wave");
                        
                        //enm1.AddViz("realcol");
                        //enm1.AddViz("changemat");
        
                        if (linearSpawn)
                        {
                            enm1.main.transform.position =
                                lo.position + (hi.position - lo.position) * (float)(i) / battle.enemies.amounts[cur];
                        }
                        else if (radialSpawn)
                        {
                            
                        }
                        else if (randomSpawn)
                        {
                            
                        }
                        else
                        {
                            enm1.main.transform.position = transform.position;
                        }

                        if (spawnAsBattle)
                        {
                            enm1.main.GetComponentInChildren<XDcombat>().curTg = MainStates.instance.tgBattle;
                        }
                        
                        await UniTask.Delay(1000);
                    }
    }
    
    public void DoSpawnAll(string btl = "")
    {
        if (btl != "")
            battle = ConfigLoader.Instance.battles.Find(x => x.battleName == btl);
        
        for (int cur1 = 0; cur1 < battle.enemies.amounts.Count; cur1++)
            for (int i = 0; i < battle.enemies.amounts[cur1]; i++)
            {
                var enm1 = new RObj(battle.enemies.heroLevelPosition[cur1].Item1, 1, 1,
                    true, transform.position, true, ItemType.monster, isEnemy: true);
            
                enm1.AddViz("hp#notext:1");
                enm1.AddViz("coll#val:0.5");
                enm1.AddViz("dmg_track");
                enm1.AddViz("flash");
                enm1.AddViz("death");
                enm1.AddViz("drop");
                enm1.AddViz("combat");
                enm1.AddViz("animator#pr:1");
                enm1.AddViz("realcol#val:0.2");
                
                enm1.AddMeta("wave");
                
                //enm1.AddViz("changemat");
            
                if (linearSpawn)
                {
                    var side = sides[battle.enemies.sides[cur1]];
                    enm1.main.transform.position =
                        side.lo.position + (side.hi.position - side.lo.position) * (float)(i) / battle.enemies.amounts[cur1]; 
                }
                else if (radialSpawn)
                {
                                
                }
                else if (randomSpawn)
                {
                    var side = sides[battle.enemies.sides[cur1]];
                    enm1.main.transform.position =
                        new Vector3(Random.Range(side.lo.position.x, side.hi.position.x), Random.Range(side.lo.position.y, side.hi.position.y),
                            Random.Range(side.lo.position.z, side.hi.position.z));       
                }
                            
            }
    }

    public string GetOverride(string s, List<(string, string)> over)
    {
        if (over == null || over.Count == 0) return s;
        var x = s.IndexOf('#');
        string rep = s;
        if (x >= 0) rep = s.Substring(0, x-1);
        
        var g = over.Find(x => x.Item1.IndexOf(rep) >= 0);
        if (g != default)
        {
            if (g.Item2 == "x") return "nope";
            
            if (g.Item2 != "")
            {
                return g.Item1 + "#" + g.Item2;
            }
            else return g.Item1;
        }

        return s;
    }

    public void AddExtras(RObj r, List<(string, string)> over)
    {
        if (over == null || over.Count == 0) return;
        
        foreach (var v in  over)
        {
            if (v.Item2 == "x") continue;
            var al = r.visuals.Keys.ToList();
            var t = al.Contains(v.Item1);
            if (!t)
            {
                string s = v.Item1;
                if (v.Item2 != "") s+= "#" + v.Item2;
                r.AddViz(s);
            }

        }
    }
    
    public List<RObj> DoSpawnAny(List<Bon> what, string tg, Transform lo1, Transform hi1, bool battle, Vector3 summonPos1, Vector3 summonPos2, bool isSummon = false,
        List<(string, string)> overridesViz = null)
    {
        List<RObj> res = new List<RObj>();
        
        foreach (var v in what)
        {
            for (int i = 0; i < v.Value; i++)
            {
                var enm1 = new RObj(v.Key, v.Value < 1 ? 1 : v.Value, v.Val3 < 1 ? 1 : v.Val3,
                    true, transform.position, true, ItemType.monster, isEnemy: tg == "enemy");
        

                enm1.AddViz( GetOverride( "hp#notext:1", overridesViz));
                enm1.AddViz(GetOverride("coll#val:0.5", overridesViz));
                enm1.AddViz(GetOverride("dmg_track", overridesViz));
                enm1.AddViz(GetOverride("flash", overridesViz));
                enm1.AddViz(GetOverride("death", overridesViz));
                enm1.AddViz(GetOverride("drop", overridesViz));
                enm1.AddViz(GetOverride("combat", overridesViz));
                enm1.AddViz(GetOverride("animator#pr:1", overridesViz));
                enm1.AddViz(GetOverride("realcol#val:0.2", overridesViz));
                
                AddExtras(enm1, overridesViz);
                
                enm1.AddMeta(tg == "enemy" ? "wave" : "my_side");
                res.Add(enm1);
                
                //enm1.AddViz("changemat");
                if (lo1 != null)
                {
                    enm1.main.transform.position =
                        new Vector3(Random.Range(lo1.position.x, hi1.position.x),
                            Random.Range(lo1.position.y, hi1.position.y),
                            Random.Range(lo1.position.z, hi1.position.z));
                    
                    enm1.Position = enm1.main.transform.position;
                }
                else
                {
                    enm1.main.transform.position =
                        new Vector3(Random.Range(summonPos1.x, summonPos2.x),
                            Random.Range(summonPos1.y, summonPos2.y),
                            Random.Range(summonPos1.z, summonPos2.z));
                    
                    enm1.Position = enm1.main.transform.position;
                }

                if (battle)
                {
                    enm1.visuals["combat"].GetComponent<XDcombat>().curTg = MainStates.instance.tgBattle;
                }

                if (isSummon)
                {
                    enm1.SetPar("is_summon", 1);
                    if (MainStates.metaCreateLevel != "") enm1.META_TAGS.Add(MainStates.metaCreateLevel);
                }
            }
            
        }

        return res;
    }
}

[System.Serializable]
public class UnoSide
{
    public int num;
    public Transform lo;
    public Transform hi;
}