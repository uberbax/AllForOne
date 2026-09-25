using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Diagnostics;

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
        "info"
    };

    private List<string> monPool = new List<string>();
    
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

    public void DoSpawn()
    {
        
        int cnt = (int)(d0 / 1.0f);

        
        for (int i = 0; i < cnt; i++)
        {
            var pnt = UtilsControl.Instance.GetRandomFreeInRange(transform.position, d0/3.0f*(i+1),d0/3.0f*i);
            
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

        }

    }
}
