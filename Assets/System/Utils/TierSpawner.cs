using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Diagnostics;
using Random = UnityEngine.Random;

public class TierSpawner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private float d0 = 1;
    private int tier = 0;

    private List<string> conf1 = new List<string>
    {
        "coll#val:0.5",
        "dmg_track",
        "flash",
        "death",
        "drop",
        "combat",
        "animator#pr:1",
        "realcol#val:0.2",
        "info",
        "status",
        "rand_move"
    };

    private List<string> confChest = new List<string>
    {
        "coll",
        "combat",
        "animator#pr:1",
        "loot#dst:5",
        "shiny"
    };
    
    private List<string> monPool = new List<string>();
    List<RObj> spawned = new List<RObj>();
    
    void Start()
    {
        if (!ConfigLoader.parseEnded || !MainStates.instance.all.ContainsKey("main_player"))
        {
            Invoke("Start", 0.1f);
            return;
        }

        var d1 = name.Substring(4);
        d0 = float.Parse(d1, CultureInfo.InvariantCulture);
        var d2 = transform.parent.name.Substring(4);
        tier = int.Parse(d2);       
        //
        
        monPool = DatabaseAll.instance.GetByTier(tier-1,"monster");
        
        DoSpawn();
    }

    public void DoSpawn(int overCnt = -1)
    {
        
        int cnt = (int)(d0 / 1.0f);
        if (overCnt > 0) cnt = overCnt;
        
        for (int i = 0; i < cnt; i++)
        {
            float r1 = (d0 / cnt) * (i + 1);
            float r2 = (d0 / cnt) * i;
            if (overCnt > 0)
            {
                r1 = d0;
                r2 = 0;
            }
            
            var pnt = UtilsControl.Instance.GetRandomFreeInRange(transform.position, r1 , r2);
            
            var ss = monPool[Random.Range(0, monPool.Count)];
            
            var g = new GameObject();
            g.name = ss + "_spawned";
            g.transform.position = pnt;

            var aa = g.AddComponent<AddedObject>();
            aa.isEnemy = true;
            aa.addedVis = conf1;
            aa.id = ss;
            aa.recreateViz = true;
            aa.addedMeta.Add("wave");

            var r = Random.Range(0, 2);
            if (r < 1)
            {
                aa.addedPars.Add(new Bon{Key = "scale", Value = -1});
            }
            
            //obj_berserk, obj_arisen, is_boss
            bool b = false;
            r = Random.Range(0, 10);
            if (r < 1)
            {
                aa.addedPars.Add(new Bon{Key = "obj_berserk", Value = 1});
                b = true;
            }
            r = Random.Range(0, 10);
            if (r < 1 && !b)
            {
                aa.addedPars.Add(new Bon{Key = "obj_arisen", Value = 1});
                b = true;
            }
            r = Random.Range(0, 10);
            if (r < 1 && !b)
            {
                aa.addedPars.Add(new Bon{Key = "is_boss", Value = 1});
                b = true;
            }

            aa.onAdd = (x) =>
            {
                spawned.Add(x);
                x.main.GetComponent<ObjHolder>().onDestroy += OnDeath;
            };
            aa.Inst();

        }

        //Debug.Log("SPWND: " + spawned.Count);
        SpawnChest();
    }

    public void SpawnChest()
    {
        //var r = 0;
        bool b = false;
        var r = Random.Range(0, 10);
        if (r < 1)
        {
            b = true;
            //id chest
            var pnt = UtilsControl.Instance.GetRandomFreeInRange(transform.position, d0);

            string ss = "chest";
            var g = new GameObject();
            g.name = ss + "_spawned";
            g.transform.position = pnt;

            var aa = g.AddComponent<AddedObject>();
            aa.isEnemy = true;
            aa.addedVis = confChest;
            aa.id = ss;
            aa.recreateViz = true;
            aa.addedMeta.Add("wave");
            
            var mp = DatabaseAll.instance.GetByTier(tier-1,"item");
            var cc = mp[Random.Range(0, mp.Count)];
            //var bb = DatabaseAll.instance.items[mp[0]];
            //var b0 = bb.pars["max_stack"];
            aa.addedInv.Add(cc + ",1" );
        }
        
        r = Random.Range(0, 10);
        if (r < 1 && !b)
        {
            b = true;
            //id chest
            var pnt = UtilsControl.Instance.GetRandomFreeInRange(transform.position, d0);

            string ss = "chest";
            var g = new GameObject();
            g.name = ss + "_spawned";
            g.transform.position = pnt;

            var aa = g.AddComponent<AddedObject>();
            aa.isEnemy = true;
            aa.addedVis = confChest;
            aa.id = ss;
            aa.recreateViz = true;
            aa.addedMeta.Add("wave");
            
            //it should be an item
            //probably tier up ?
            var mp = DatabaseAll.instance.GetByTier(tier-1,"item", 1);
            var cc = mp[Random.Range(0, mp.Count)];

            int rar = Random.Range(1, 4);
            
            aa.addedInv.Add(cc + ",1," + rar);

            aa.onAdd = SpawnedChestWithKill;
            aa.Inst();
        }
    }

    public void SpawnedChestWithKill(RObj obj)
    {
        MainStates.instance.chests.Add(obj);
        obj.SetPar("kill", 0);
        obj.AddViz("kill_req");
    }

    public void OnDeath(RObj obj)
    {
        FunctionTimer.Create(() => DoSpawn(1), 10);
    }
}
