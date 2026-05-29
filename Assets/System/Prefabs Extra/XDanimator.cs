using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDanimator : ComponentBehavior
{
    // Start is called before the first frame update
    public RObj mon;
    
    private Animator anim;
    private SpriterAnim anim1;
    private PrAnimat anim2;
    
    //params:
    //pr
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        

        
        anim = mon.visuals["vis_main"].GetComponentInChildren<Animator>();
        anim1 = mon.visuals["vis_main"].GetComponent<SpriterAnim>();
        
        if (pars.ContainsKey("pr") && (anim == null || !anim.enabled))
        {
            mon.visuals["vis_main"].AddComponent<PrAnimat>();
        }        
        
        anim2 = mon.visuals["vis_main"].GetComponent<PrAnimat>();
    }

    public string prevState = "";
    public void SetState(string state)
    {
        if (state == prevState) return;
        prevState = state;

        if (state == "attack")
        {
            FunctionTimer.Create(() => SetState("idle"), 0.5f);
        }

        if (anim != null && anim.enabled)
        {
            string sNew = state;
            if (anim.GetComponent<AnimConvert>() != null)
            {
                sNew = anim.GetComponent<AnimConvert>().anims[state];
            }
            anim.CrossFade(sNew, 0.2f);
        }
        else if (anim1 != null)
        {
            anim1.CrossFade(state, 0.2f);
        }
        else if (anim2 != null)
        {
            anim2.CrossFade(state, 0.2f);
        }
    }

}
