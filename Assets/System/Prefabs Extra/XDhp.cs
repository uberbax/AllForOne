using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDhp : ComponentBehavior
{
    public RObj mon;
    public TextMeshProUGUI hp;
    public Image fill;
    public Image fillMed;
    // notext
    private bool notext = false;
    private bool nofull = false;

    private Transform head;
    private float spd = 1;
    
    public void AfterSet(string par)
    {
        if (pars.ContainsKey("notext"))
            notext = true;
        if (pars.ContainsKey("nofull"))
            nofull = true;
    }
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        head = mon.visMain.transform.Find("head");
        Update();
    }

    void Update()
    {
        if (head != null)
        {
            fill.transform.parent.position = head.position;
            hp.transform.position = head.position + new Vector3(0, 0.3f, 0);
        }

        float ratio = mon.GetPar("health") / mon.GetPar("max_health");
        fill.fillAmount = ratio;
        if (fillMed.fillAmount > ratio)
        {
            fillMed.fillAmount -= Time.deltaTime * spd;
        }
        else
        {
            fillMed.fillAmount = ratio;
        }
        
        hp.text = mon.GetPar("health").ToString();
        
        fill.gameObject.SetActive(ratio < 1 || !nofull);
        hp.gameObject.SetActive(!notext);
    }
}
