using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDinfo : ComponentBehavior
{
    // Start is called before the first frame update
    public RObj mon;
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    private void OnMouseUp()
    {
        MainStates.instance.curClick = mon;
        MainStates.instance.UI_infoMon.SetActive(true);
    }
}
