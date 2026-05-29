using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XDflash : ComponentBehavior
{
    // Start is called before the first frame update
    private RObj mon;

    private float prevHp = -1;
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    // Update is called once per frame
    void Update()
    {
        var c = mon.GetPar("health");

        if (c != prevHp && prevHp != -1 && prevHp > 0)
        {
            var dlt = c - prevHp;
            if (dlt < 0)
            {
                UtilsControl.Instance.BlinkRed(transform.parent.gameObject);
            }
            else
            {

            }
        }

        prevHp = c;
    }
}
