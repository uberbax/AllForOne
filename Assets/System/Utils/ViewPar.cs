using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ViewPar : MonoBehaviour
{
    public Transform fromWhere;
    public string parName;
    public TextMeshProUGUI parText;
    public TextMeshProUGUI parNm;
    public Image parImg;

    private RObj mon;
    public bool isMain = false;
    public List<string> doPars = new List<string>();
    public bool everyTurn = false;
    private void OnEnable()
    {
        if (isMain)
        {
            
            mon = fromWhere == null ? GetComponentInParent<ObjHolder>().obj : fromWhere.GetComponentInParent<ObjHolder>().obj;

            List<string> gg = new List<string>();
            if (doPars.Count == 0)
                gg = mon.GetNonZeroPars(new List<string>
                    { "attack", "max_health", "def", "res", "crit_chance", "crit_dmg" });
            else gg = doPars;
            
            var d = gg.Count - transform.childCount;
            if (d > 0)
            {
                for (int i = 0; i < d; i++)
                {
                    Instantiate(transform.GetChild(0).gameObject, transform);
                }
            }
            //
            for (int i = 0; i < transform.childCount; i++)
            {
                if (i >= gg.Count)
                    transform.GetChild(i).gameObject.SetActive(false);
                else
                {
                    transform.GetChild(i).gameObject.SetActive(true);
                    transform.GetChild(i).GetComponent<ViewPar>().parName = gg[i];
                    transform.GetChild(i).GetComponent<ViewPar>().Fill();
                }
            }
            
        }
    }

    public void Fill()
    {
        mon = fromWhere == null ? GetComponentInParent<ObjHolder>().obj : fromWhere.GetComponentInParent<ObjHolder>().obj;

        if (parName.IndexOf("/") >= 0)
        {
            var ff = parName.Split("/");
            
            if (parNm) parNm.text = ConfigLoader.Instance.GetMeLocale(ff[0]);
            if (parImg) parImg.sprite = ResourceHolder.instance.pars[ff[0]];
            parText.text = mon.GetPar(ff[0]) + "/" + mon.GetPar(ff[1]);
        }
        else
        {
             if (parNm) parNm.text = ConfigLoader.Instance.GetMeLocale(parName);
             if (parImg) parImg.sprite = ResourceHolder.instance.pars[parName];
             parText.text = mon.GetPar(parName).ToString();
        }

    }

    private void Update()
    {
        if (everyTurn)
        {
            if (isMain) OnEnable();
            else Fill();
        }
    }
}
