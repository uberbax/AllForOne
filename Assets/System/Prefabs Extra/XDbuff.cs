using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDbuff : ComponentBehavior
{
    public RObj mon;
    // notext
    private bool notext = false;
    private bool nofull = false;

    private Transform head;
    private float spd = 1;

    public string trackWhat = "health";
    public string trackWhere = "head";
    public float dlt = 0.3f;
    public void AfterSet(string par)
    {
        if (pars.ContainsKey("notext"))
            notext = true;
        if (pars.ContainsKey("nofull"))
            nofull = true;
        
        if (pars.ContainsKey("track"))
            trackWhat = pars["track"];
        
        
    }
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        head = mon.visMain.transform.Find(trackWhere);
        Update();
    }

    void Update()
    {
        if (head != null)
        {
            transform.position = head.position;
            //fill.transform.parent.position = head.position;
            //hp.transform.position = head.position + new Vector3(0, dlt, 0);
        }

        var m = mon.timedBuffs.Count - transform.childCount;
        for (int i = 0; i < m; i++)
            Instantiate(transform.GetChild(0).gameObject, transform);

        for (int i = mon.timedBuffs.Count; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
        
        for (int i = 0; i < mon.timedBuffs.Count; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
            var ii = transform.GetChild(i);
            var bb = mon.timedBuffs[i];
            ii.Find("icon").GetComponent<Image>().sprite = ResourceHolder.instance.skills[bb.dbObj.ID];
            ii.Find("txt").GetComponent<TextMeshProUGUI>().text = bb.dbObj.ID + "(" + (int)bb.GetPar("timeLeft") + ")";
        }

    }
}
