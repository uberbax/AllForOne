using System;
using UnityEngine;

public class XDfollow : ComponentBehavior
{
    private RObj mon;
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    private void Update()
    {
        var cc = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mon.main.transform.position = cc;
    }
}
