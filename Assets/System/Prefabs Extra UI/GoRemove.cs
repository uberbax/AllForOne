using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoRemove : MonoBehaviour
{
    private RObj mon;
    ObjHolder holder;
    private void OnEnable()
    {
        holder = GetComponentInParent<ObjHolder>();
        mon = holder.obj;
        var b = GetComponent<Button>();
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() =>
            {
                mon.owner2.adorments.Remove(mon);
                mon.owner2.RecalcPars();
                mon.owner2 = null;
                UIfiller.GlobalRefresh();
            }
        );
    }


}
