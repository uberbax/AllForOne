using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoHighlight : MonoBehaviour
{
    private RObj mon;
    ObjHolder holder;
    private CanvasGroup cg;
    private void OnEnable()
    {
        cg = GetComponent<CanvasGroup>();
        holder = GetComponentInParent<ObjHolder>();
        mon = holder.obj;
    }

    private void Update()
    {
        if (MainStates.instance.curObjs["cur_hero"] == mon)
        {
            cg.alpha = 1;
        }
        else
        {
            cg.alpha = 0;
        }
    }
}
