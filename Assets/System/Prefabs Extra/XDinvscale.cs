using UnityEngine;

public class XDinvscale : ComponentBehavior
{
    void Start()
    {
        var mon = GetComponentInParent<ObjHolder>();
        if (!mon || mon.obj == null) return;
        mon.obj.invertScale = true;
    }

}
