using System;
using UnityEngine;

public class XDshiny : ComponentBehavior
{
    private void Start()
    {
        var g = GetComponentInParent<ObjHolder>().obj;
        g.visMain.AddComponent<_2dxFX_Shiny_Reflect>();
    }
}
