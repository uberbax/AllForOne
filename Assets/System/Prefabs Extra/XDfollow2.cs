using System;
using UnityEngine;

public class XDfollow2 : ComponentBehavior
{
    private RObj mon;
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }

    private void Update()
    {
        var h = MainStates.instance.all["main_player"].Position + mon.localPos - mon.Position;

        if (mon.lastAction == "no_target" || MainStates.isMovingByControl)
        {
            h.z = 0;
            if (h.magnitude > 1)
            {
                mon.visuals["animator"].GetComponent<XDanimator>().SetState("walk");
                if (h.x > 0)
                {
                    mon.SetScale(h.x > 0);
                }
                else if (h.x < 0)
                {
                    mon.SetScale(h.x > 0);
                }

                mon.main.transform.position += h.normalized * Time.deltaTime * 2;
            }
            else
            {
                mon.visuals["animator"].GetComponent<XDanimator>().SetState("idle");
            }
        }
    }
}
