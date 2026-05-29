using UnityEngine;

public class XDinvscale : ComponentBehavior
{
    void Start()
    {
        var mon = GetComponentInParent<ObjHolder>().obj;
        mon.invertScale = true;
    }

}
