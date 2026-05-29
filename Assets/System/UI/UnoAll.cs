using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnoAll : MonoBehaviour
{
    public string pref = "";
    public string param = "";
    public string param2 = "";
    
    public bool isStat;
    public bool isPar;
    public bool hideEmpty;
    
    public bool ignoreOnce = false;
    
    public RObj mon;

    public bool isButton;
    public GameObject closeObj;
    private bool done = false;

    private ObjHolder o;
    private void Update()
    {
        if (true) //(mon == null || mon.RID == "" || mon.RID == null)
        {
            if (!o) o = GetComponentInParent<ObjHolder>();
            mon = o.obj;
            if (mon == null || mon.RID == "") return;
        }

        if (!done && isButton)
        {
            GetComponent<Button>().onClick.RemoveAllListeners();
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (mon != null)
                {
                    MainStates.instance.ClickedSome(mon, this, o);
                    if (closeObj != null)
                        closeObj.SetActive(false);

                    UIfiller.GlobalRefresh();
                }
            });
            done = true;

        }    
            
        if (done) return;
            
        ResourceHolder.instance.GetResult(this, mon);
    }
}
