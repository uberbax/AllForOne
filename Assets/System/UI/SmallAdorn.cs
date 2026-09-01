using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmallAdorn : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private ObjHolder other;
    private RObj mon;

    public bool fillFull = false;

    public Transform holder;
    public Transform holderStats;
    
    //stats

    void Start()
    {
        UIfiller.acts += Fill;
    }

    private void OnDisable()
    {
        UIfiller.acts -= Fill;
    }

    private void OnEnable()
    {
        //if (other == null)
        //{
            other = GetComponentInParent<ObjHolder>();
            mon = other.obj;
        //}
        if (mon == null)
        {
            Invoke("OnEnable", 0.1f);
            return;
        }
        
        if (fillFull)
            FillFull();
        else Fill();
    }

    public void Fill()
    {
        var g = mon.GetPar("adorn_count");
        for (int i = 0; i < holder.childCount; i++)
        {
            holder.GetChild(i).gameObject.SetActive(i < g);

        }

        for (int i = 0; i < holder.childCount; i++)
        {
            if (i >= mon.adorments.Count)
            {
                for (int j = 0; j < holder.GetChild(i).childCount; j++)
                {
                    holder.GetChild(i).GetChild(j).gameObject.SetActive(false);
                }
            }
            else
            {
                for (int j = 0; j < holder.GetChild(i).childCount; j++)
                {
                    holder.GetChild(i).GetChild(j).gameObject.SetActive(true);
                }
            }            
        }

        var g0 = new RObj();
        for (int i = 0; i < mon.adorments.Count; i++)
        {
            var p = holder.GetChild(i);
            p.Find("adorn").GetComponent<Image>().sprite = ResourceHolder.instance.items[mon.adorments[i].dbObj.ID];
            p.Find("rarity").GetComponent<Image>().color = ResourceHolder.instance.rareColors[(int)mon.adorments[i].GetPar("rarity")];
            g0 = MainStates.instance.SumRobj(g0, mon.adorments[i]);
        }
        //
        //holderStats
        for (int i = 0; i < holderStats.childCount; i++) holderStats.GetChild(i).gameObject.SetActive(false);
        int l = 0;
        for (int i = 0; i < pars.Count; i++)
        {
            var c = g0.GetPar(pars[i].Item1);
            if (c <= 0) continue;

            holderStats.GetChild(l).gameObject.SetActive(true);
            holderStats.GetChild(l).Find("icon").GetComponent<Image>().sprite = ResourceHolder.instance.pars[pars[i].Item1];
            holderStats.GetChild(l).Find("value").GetComponent<TextMeshProUGUI>().text = (int)(c) + pars[i].Item2;
            l++;


        }
        
    }

    private List<(string, string)> pars = new List<(string, string)>
    {
        ("attack",""), ("max_health",""), ("def",""), ("magic",""), 
        ("res",""), ("crit_chance",""), ("crit_dmg","")
    };

    public void FillFull()
    {

    }
}
