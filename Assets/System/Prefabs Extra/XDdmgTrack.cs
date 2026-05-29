using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;

public class XDdmgTrack : ComponentBehavior
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
        var c0 = mon.GetPar("show_message");

        if (c0 > 0)
        {
            mon.SetPar("show_message", 0);
            DamageNumber newDamageNumber = UtilsControl.Instance.prefabPhrase.Spawn(transform.position);
        }

        if (c != prevHp && prevHp != -1)
        {
            var dlt = c - prevHp;
            if (dlt < 0)
            {
                DamageNumber newDamageNumber = UtilsControl.Instance.prefab.Spawn(transform.position, dlt);

                if (mon.GetPar("was_crit") > 0)
                {
                    newDamageNumber.SetScale(2);
                    newDamageNumber.enableRightText = true;
                    newDamageNumber.rightText = "!";
                    mon.SetPar("was_crit", 0);
                }
            }
            else
            {
                DamageNumber newDamageNumber = UtilsControl.Instance.prefabPos.Spawn(transform.position, dlt);
                //newDamageNumber.SetColor(Color.green);
                
                if (mon.GetPar("was_crit") > 0)
                {
                    newDamageNumber.SetScale(2);
                    newDamageNumber.enableRightText = true;
                    newDamageNumber.rightText = "!";
                    mon.SetPar("was_crit", 0);
                }

            }
        }

        prevHp = c;
    }
}
