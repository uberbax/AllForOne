using System;
using UnityEngine;

public class XDkill_req : ComponentBehavior
{
    private void Start()
    {
        var g = GetComponentInParent<ObjHolder>().obj;
        g.visMain.AddComponent<_2dxFX_Shiny_Reflect>();
    }
}
