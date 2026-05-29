using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPrice : MonoBehaviour
{
    public List<Bon> req = new List<Bon>();
    
    public List<TextMeshProUGUI> price;
    public List<Image> icon;

    private void Update()
    {
        var hv = MainStates.instance.HaveAmount(req);

        for (int i = 0; i < req.Count; i++)
        {
            price[i].GetComponent<CanvasGroup>().alpha = 1;
            icon[i].GetComponent<CanvasGroup>().alpha = 1;
            
            price[i].text = hv ? req[i].Value.ToString() : "<color=red>" + req[i].Value.ToString() + "</color>";
            icon[i].sprite = ResourceHolder.instance.items[req[i].Key];
        }

        for (int i = req.Count; i < price.Count; i++)
        {
            price[i].GetComponent<CanvasGroup>().alpha = 0;
            icon[i].GetComponent<CanvasGroup>().alpha = 0;
        }
        
    }
}
