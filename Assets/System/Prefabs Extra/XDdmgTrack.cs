using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;

public class XDdmgTrack : ComponentBehavior
{
    // Start is called before the first frame update
    private RObj mon;

    private float prevHp = -1;

    private Transform head;
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        head = mon.visMain.transform.Find("head");
    }

    // Update is called once per frame
    void Update()
    {
        var c = mon.GetPar("health");
        var c0 = mon.GetPar("show_message");

        Vector3 where = transform.position;
        if (head != null)
            where = head.position;
        
        if (c0 > 0)
        {
            mon.SetPar("show_message", 0);
            DamageNumber newDamageNumber = UtilsControl.Instance.prefabPhrase.Spawn(where);
        }

        if (c != prevHp && prevHp != -1)
        {
            var dlt = c - prevHp;
            if (dlt < 0)
            {
                DamageNumber newDamageNumber = UtilsControl.Instance.prefab.Spawn(where, dlt);

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
                DamageNumber newDamageNumber = UtilsControl.Instance.prefabPos.Spawn(where, dlt);
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
