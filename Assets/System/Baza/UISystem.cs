using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISystem : MonoBehaviour
{
    // Start is called before the first frame update
    public static UISystem instance;
    private void Awake()
    {
        instance = this;
    }

    public void FillItem(Bon item, GameObject itm)
    {
        
    }

    public void FillItem(ObjHolder o)
    {
        if (o.filler == null) return;
        var ss = o.filler.fillFunc;
        if (ss.Length > 1)
        {
            var gg = ss.Split(',');
            var kk = o.GetComponent<GBind>();
            for (int i = 0; i < gg.Length; i++)
            {
                //Debug.Log(gg[i] + " " + o.transform);
                bool nt = false;
                var str = gg[i];
                if (str[0] == '!')
                {
                    str = str.Substring(1);
                    nt = true;
                }
                
                var nn = o.transform.Find(str);
                GameObject bb = null;
                if (kk) bb = kk.GetGameobject(str);
                
                if (bb) bb.SetActive(true ^ nt);
                else if (nn) nn.gameObject.SetActive(true ^ nt);
            }
        }
    }
    public void Fill(UIfiller f)
    {
        if (f.nm == "CHEST")
        {
            var g = f.GetComponent<GBind>();
            var a = MainStates.instance.curLoot;

            var tkn = g.GetButton("take");
            if (tkn)
            {
                tkn.onClick.RemoveAllListeners();
                tkn.onClick.AddListener(() =>
                {
                    for (int i = MainStates.instance.curLoot.inventory.Count -1; i>=0; i--)
                    {
                        var v =  MainStates.instance.curLoot.inventory[i];
                        MainStates.instance.AddItem(MainStates.instance.all["main_player"], v);
                    }

                    UIfiller.GlobalRefresh();
                });
            }
        }
        else if (f.nm == "EXPEDITION")
        {
            var o = f.GetComponent<GBind>();
            var sendBtn = o.GetButton("send");
            
        }
    }
}
