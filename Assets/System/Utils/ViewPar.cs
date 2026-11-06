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
    private void OnEnable()
    {
        if (isMain)
        {
            
            mon = fromWhere == null ? GetComponentInParent<ObjHolder>().obj : fromWhere.GetComponentInParent<ObjHolder>().obj;
            
            var gg = mon.GetNonZeroPars(new List<string>{"attack","max_health","def","res","crit_chance", "crit_dmg"});
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
        parNm.text = ConfigLoader.Instance.GetMeLocale(parName);
        parImg.sprite = ResourceHolder.instance.pars[parName];
        parText.text = mon.GetPar(parName).ToString();
    }
}
