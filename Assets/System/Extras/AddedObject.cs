using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddedObject : MonoBehaviour
{
    public string overID = "";
    public string id = "";
    public bool isEnemy;
    public bool isNeutral;
    public int amount = 1;
    public int level = 1;
    
    public List<string> addedVis = new List<string>();
    public List<string> addedInv = new List<string>();

    public Action<RObj> onAdd = null;
    //----
    //add option as main
    public List<Bon> addedPars = new List<Bon>();
    public List<string> addedMeta = new List<string>();
    public List<Bon> reqPars = new List<Bon>();
    
    public bool asMainViz = false;
    public bool recreateViz = false;
    public bool markSkillsAsReq = false;
    public bool randomizeItemsParams = false;
    
    public List<GameObject> toActivate = new List<GameObject>();
    
    void Start()
    {
        if (!ConfigLoader.parseEnded || !MainStates.instance.all.ContainsKey("main_player"))
        {
            Invoke("Start", 0.1f);
            return;
        }

        GameObject nn = gameObject;
        if (recreateViz)
        {
            nn = Instantiate(ResourceHolder.instance.GetGameobject(id));
            nn.transform.position = transform.position;
            nn.transform.parent = gameObject.transform.parent;
        }
        
        var r = DatabaseAll.instance.CreateAny(id, isEnemy, amount, nn, overID, asMainViz ? gameObject : null, level:level);
        foreach (var v in addedVis)
        {
            r.AddViz(v);
        }
        
        foreach (var v in addedInv)
        {
            var hh = v.Split(',');
            MainStates.instance.AddItem(r, hh[0], int.Parse(hh[1]), randomizeItemsParams);
        }

        foreach (var v in addedPars)
        {
            r.SetPar(v.Key, v.Value);
        }
        
        
        if (onAdd != null)
            onAdd(r);

        foreach (var v in addedMeta)
        {
            r.META_TAGS.Add(v);
        }
        
        if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0)
        {
            r.Position = r.main.transform.position;
            r.AdjustPosition();
        }

        if (markSkillsAsReq)
        {
            foreach (var v in r.actSkills)
            {
                v.SetPar("action_req", 1);
            }
        }

        if (r.HasVis("loot") && reqPars.Count > 0)
        {
            r.visuals["loot"].GetComponent<XDloot>().price = reqPars;
        }
        
        foreach (var v in toActivate)
            v.SetActive(true);
        
        //isNeutral checking
        if (isNeutral)
        {
            if (r.tags.Contains("player"))
                r.tags.Remove("player");
            if (r.tags.Contains("enemy"))
                r.tags.Remove("enemy");
            r.tags.Add("neutral");
        }
        
    }

    
}
