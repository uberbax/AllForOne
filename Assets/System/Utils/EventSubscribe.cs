using System;
using UnityEngine;

public class EventSubscribe : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string eventName = "";
    public string wh = "";
    public string wh1 = "";

    public ObjHolder toInject;
    public ObjHolder toInjectUpgrade;
    public ObjHolder toInjectAscend;
    
    public GameObject toActivate;
    public GameObject toDeactivate;
    
    
    public void Start()
    {
        EventManager.SUB(eventName, (x) =>
        {
            if (wh != "" && x.what != wh) return;
            if (wh1 != "" && x.what1 != wh1) return;
            
            gameObject.SetActive(false);
            gameObject.SetActive(true);

            if (toActivate != null)
            {
                var b = toActivate.GetComponent<IReceive>();
                if (b != null) b.Receive(x);                
                toActivate.SetActive(true);
                //toActivate.SendMessage("Receive", x);
            }

            if (toDeactivate != null)
            {
                toDeactivate.SetActive(false);
            }

            if (toInjectUpgrade != null)
            {
                var a = MainStates.instance.GenerateUpgrade(x.who);
                a.dltPars = MainStates.instance.GetDiff(x.who, a);
                toInjectUpgrade.obj = a;    
            }
            
            if (toInjectAscend != null)
            {
                var a = MainStates.instance.GenerateAscend(x.who);
                toInjectAscend.obj = a;    
            }

        });
    }
}
