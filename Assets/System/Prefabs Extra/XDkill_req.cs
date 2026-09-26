using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDkill_req : ComponentBehavior
{
    public TextMeshProUGUI kills;
    public Image locked;

    private RObj g;
    public bool done = false;
    private void Start()
    {
        g = GetComponentInParent<ObjHolder>().obj;
    }

    private void Update()
    {
        var c = g.GetPar("kill");

        if (c >= 3)
        {
            done = true;
            kills.gameObject.SetActive(false);
            locked.gameObject.SetActive(false);
            
        }
        else
        {
            done = false;
            kills.text = c + "/3";
        }

        
    }
}
