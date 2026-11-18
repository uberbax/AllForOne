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
    UiLoopVideo animVideo;
    //params:
    //pr
    
    public void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        

        
        anim = mon.visuals["vis_main"].GetComponentInChildren<Animator>();
        anim1 = mon.visuals["vis_main"].GetComponent<SpriterAnim>();
        
        guiControls = mon.visuals["vis_main"].GetComponent<GUIControls>();
        
        animVideo = mon.visuals["vis_main"].GetComponent<UiLoopVideo>();
        
        anim2 = mon.visuals["vis_main"].GetComponent<PrAnimat>();        
        
        if (pars.ContainsKey("pr") && (anim == null && guiControls == null && animVideo == null || !anim.enabled))
        {
            mon.visuals["vis_main"].AddComponent<PrAnimat>();
        }        
        
    }

    public string prevState = "";
    private int stateVersion;
    private Coroutine hitRoutine;
    private IDisposable hitAction;

    public void PlayHit()
    {
        if (mon == null || mon.main == null || !mon.main.activeInHierarchy || mon.GetPar("health") <= 0) return;
        if (animVideo != null)
        {
            if (animVideo.PlayHitReaction()) UnitActionTracker.WatchAnimation(mon);
            return;
        }
        if (anim == null || !anim.enabled) return;
        string hit = AnimatorStateName("hit");
        if (!anim.HasState(0, Animator.StringToHash(hit))) return;
        if (hitRoutine != null) StopCoroutine(hitRoutine);
        hitAction?.Dispose();
        var action = UnitActionTracker.Begin(mon);
        hitAction = action;
        hitRoutine = StartCoroutine(PlayAnimatorHit(hit, stateVersion, action));
    }

    private string AnimatorStateName(string state)
    {
        var convert = anim.GetComponent<AnimConvert>();
        return convert != null && convert.anims.ContainsKey(state) ? convert.anims[state] : state;
    }

    private IEnumerator PlayAnimatorHit(string hit, int version, IDisposable action)
    {
        try
        {
            anim.CrossFade(hit, .05f);
            float deadline = Time.unscaledTime + 5f;
            bool entered = false;
            while (anim != null && mon != null && mon.GetPar("health") > 0 && version == stateVersion &&
                Time.unscaledTime < deadline)
            {
                var state = anim.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(hit)) { entered = true; if (state.normalizedTime >= 1) break; }
                else if (entered) break;
                yield return null;
            }
            if (anim != null && version == stateVersion && mon != null && mon.GetPar("health") > 0)
                anim.CrossFade(AnimatorStateName(string.IsNullOrEmpty(prevState) ? "idle" : prevState), .05f);
        }
        finally
        {
            action?.Dispose();
            if (ReferenceEquals(hitAction, action)) { hitAction = null; hitRoutine = null; }
        }
    }

    private void OnDisable()
    {
        if (hitRoutine != null) StopCoroutine(hitRoutine);
        hitRoutine = null;
        hitAction?.Dispose();
        hitAction = null;
    }

    public void SetState(string state, string onlyFrom = "", bool autoIdle = true)
    {
        if (state == prevState) return;
        if (onlyFrom != "" && prevState != onlyFrom) return;
        prevState = state;
        int version = ++stateVersion;

        if (state == "attack" && autoIdle)
        {
            FunctionTimer.Create(() =>
            {
                if (this != null && version == stateVersion && prevState == "attack" &&
                    mon != null && mon.GetPar("health") > 0)
                    SetState("idle");
            }, animVideo != null ? 3f : 0.5f);
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

        if (animVideo != null)
        {
            animVideo.CrossFade(prevState);
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
