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
    public bool hideIfZero = false;
    private CanvasGroup cg;

    private float dlt3 = 0;
    public void AfterSet(string par)
    {
        if (pars.ContainsKey("notext"))
            notext = true;
        if (pars.ContainsKey("nofull"))
            nofull = true;
        
        if (pars.ContainsKey("track"))
            trackWhat = pars["track"];
        
        if (pars.ContainsKey("d3"))
            dlt3 = float.Parse(pars["d3"]);
        
    }
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        head = mon.visMain.transform.Find(trackWhere);
        cg = GetComponent<CanvasGroup>();

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
        
        if (head == null && trackWhere == "bot_shield")
        {
            head = mon.visMain.transform.Find("legs");
            dlt = 0;
            dlt2 = -1 + dlt3;
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
        
        var mm = mon.GetPar("max_" + trackWhat);
        if (mm == 0) mm = 500; //? its for shield
        var mm0 = mon.GetPar(trackWhat);
        if (hideIfZero)
        {
            if (mm0 <= 0)
                cg.alpha = 0;
            else
                cg.alpha = 1;
        }
        
        float ratio = mm0 / mm;
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
