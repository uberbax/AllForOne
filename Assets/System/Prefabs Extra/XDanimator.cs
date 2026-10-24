using System;
using System.Collections;
using System.Collections.Generic;
using RPGCharacterAnims;
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
    
    GUIControls guiControls;
    //params:
    //pr
    
    public void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        

        
        anim = mon.visuals["vis_main"].GetComponentInChildren<Animator>();
        anim1 = mon.visuals["vis_main"].GetComponent<SpriterAnim>();
        
        if (pars.ContainsKey("pr") && (anim == null || !anim.enabled))
        {
            mon.visuals["vis_main"].AddComponent<PrAnimat>();
        }        
        
        anim2 = mon.visuals["vis_main"].GetComponent<PrAnimat>();
        
        guiControls = mon.visuals["vis_main"].GetComponent<GUIControls>();
    }

    public string prevState = "";
    public void SetState(string state, string onlyFrom = "")
    {
        if (state == prevState) return;
        if (onlyFrom != "" && prevState != onlyFrom) return;
        prevState = state;

        if (state == "attack")
        {
            FunctionTimer.Create(() =>
            {
                if (mon.GetPar("health") > 0)
                    SetState("idle");
            }, 0.5f);
        }

        if (guiControls != null)
        {
            //guiControls.Attacks();
            string da = "";
            
            
            var mm = mon.GetWeapon();
            if (mm != null && prevState == "attack")
            {
                var t = mm.GetPar("anim_type");
                if (t == 1)
                {
                    da = "L";
                }
            }
            
            
            guiControls.CrossFade(prevState + da, 0.2f);
            return;
        }

        if (anim != null && anim.enabled)
        {
            string sNew = state;
            if (anim.GetComponent<AnimConvert>() != null)
            {
                var cc = anim.GetComponent<AnimConvert>(); 
                if (cc.anims.ContainsKey(state))
                    sNew = cc.anims[state];
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
