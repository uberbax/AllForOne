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

    public string trackWhat = "health";
    public string trackWhere = "head";
    public float dlt = 0.3f;
    public float dlt2 = 0;
    public bool samePos = false;
    public bool useMax = false;
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

        if (head == null && trackWhere == "bot_health")
        {
            head = mon.visMain.transform.Find("legs");
            dlt = 0;
            dlt2 = -0.4f;
        }

        if (head == null && trackWhere == "bot_mana")
        {
            head = mon.visMain.transform.Find("legs");
            dlt = 0;
            dlt2 = -0.7f;
        }

        if (mon.GetPar("is_boss") > 0)
        {
            transform.GetChild(0).gameObject.SetActive(false);
            return;
        }
        
        Update();
    }

    void Update()
    {
        if (head != null)
        {
            if (samePos)
            {
                transform.GetChild(0).position = head.position + new Vector3(0, dlt2, 0);
            }
            else
            {
                fill.transform.parent.position = head.position;
                hp.transform.position = head.position + new Vector3(0, dlt, 0);                
            }

        }

        if (mon == null)
        {
            mon = GetComponentInParent<ObjHolder>().obj;
        }
        
        float ratio = mon.GetPar(trackWhat) / mon.GetPar("max_" + trackWhat);
        fill.fillAmount = ratio;
        if (fillMed.fillAmount > ratio)
        {
            fillMed.fillAmount -= Time.deltaTime * spd;
        }
        else
        {
            fillMed.fillAmount = ratio;
        }

        if (useMax)
        {
            hp.text = mon.GetPar(trackWhat) + "/" + mon.GetPar("max_" + trackWhat);
        }
        else
        {
            hp.text = ((int)mon.GetPar(trackWhat)).ToString();
        }

        
        fill.gameObject.SetActive(ratio < 1 || !nofull);
        hp.gameObject.SetActive(!notext);
    }
}
