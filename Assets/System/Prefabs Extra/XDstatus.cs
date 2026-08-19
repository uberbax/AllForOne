using System.Collections.Generic;
using UnityEngine;

public class XDstatus : ComponentBehavior
{
    private RObj mon;
    private Transform head;

    private List<string> pars = new List<string> { "obj_berserk", "obj_arisen" };
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;

        for (int i = 0; i < pars.Count; i++)
        {
            var f = mon.GetPar(pars[i]);
            if (f > 0)
            {
                var h = Instantiate(ResourceHolder.instance.miscGO[pars[i]], transform);
                h.transform.localPosition -= new Vector3(0, 0.28f, 0);
            }
        }
        
    }
}
