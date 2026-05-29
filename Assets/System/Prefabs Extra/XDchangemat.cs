using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDchangemat : ComponentBehavior
{
    // Start is called before the first frame update
    public RObj mon;
    
    public Material mat;
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;

        mon.visMain.GetComponentInChildren<Renderer>().material = mat;
    }

}
