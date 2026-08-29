using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TakeReward : MonoBehaviour
{
    public string stat = "";
    
    public List<Bon> rewards = new List<Bon>();
    
    private Button btn;
    private CanvasGroup cg;

    public TextMeshProUGUI txt;
    public GameObject notify;
    public Sprite canCollect;
    public Sprite taken;
    
    private void Start()
    {
        btn = GetComponent<Button>();
        cg = GetComponent<CanvasGroup>();
        
    }

    void Update()
    {
        var a = ModelStatistics.instance.GetStatValue(stat);
        if (a <= 0)
        {
            btn.interactable = false;
            cg.alpha = 0.5f;
            txt.text = ConfigLoader.Instance.GetMeLocale("collect");
            btn.GetComponent<Image>().sprite = taken;
        }
        else
        {
            var b =  ModelStatistics.instance.GetStatValue(stat + "_taken");
            if (b >= 1)
            {
                btn.interactable = false;
                cg.alpha = 0.5f;
                txt.text = ConfigLoader.Instance.GetMeLocale("taken");
                btn.GetComponent<Image>().sprite = taken;
            }
            else
            {
                notify.SetActive(true);
                btn.interactable = true;
                cg.alpha = 1f;
                txt.text = ConfigLoader.Instance.GetMeLocale("collect");
                btn.GetComponent<Image>().sprite = canCollect;
            }
        }
    }

}
