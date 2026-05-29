using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDlevel : ComponentBehavior
{
    private RObj mon;

    public Image fill;
    public Transform lines;
    public TextMeshProUGUI txtLevel;
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        Fill();
    }


    public void Fill()
    {
        MainStates.instance.GetMeExpPars(mon, out float rat,out float cr, out float cm, out float lvl);
        fill.fillAmount = rat;
        //starsNum.text = ct.ToString();
        int pep = (int)cm / 100 - 1;
        for (int i = 0; i < pep; i++)
            Instantiate(lines.GetChild(0).gameObject, lines);
        
        for (int i= 0 ; i < lines.childCount; i++)
            lines.GetChild(i).gameObject.SetActive(false);

        cm /= 100;

        for (int i = 0; i < pep; i++)
        {
            lines.GetChild(i).gameObject.SetActive(true);
            
            
            lines.GetChild(i).GetComponent<RectTransform>().anchorMin = new Vector2((i + 1) / (float)cm, 0.5f);
            lines.GetChild(i).GetComponent<RectTransform>().anchorMax = new Vector2((i + 1) / (float)cm, 0.5f);
            lines.GetChild(i).GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
            //lines.GetChild(i).GetComponent<RectTransform>().offsetMin = Vector2.zero;
            //lines.GetChild(i).GetComponent<RectTransform>().offsetMax = Vector2.zero;
            
        }
        
        txtLevel.text = "Lv." + mon.GetPar("level").ToString();

    }

    private float prevExp = -1;
    private float prevMaxExp = -1;
    
    private void Update()
    {
        if (mon.GetPar("exp") != prevExp || mon.GetPar("exp") != prevMaxExp)
            Fill();

        prevExp = mon.GetPar("exp");
        prevMaxExp = mon.GetPar("exp");
    }
}
