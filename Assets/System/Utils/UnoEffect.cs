using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnoEffect : MonoBehaviour
{
    public AnimationCurve ac;
    public UtilsControl.CurveType curve;
    public Action act;
    public float time = 1;
    public float speed = 1;
    public float evKoef = 1;
    public float wait;
    public float waitBetween;
    public string actStr = "destroy";
    public bool pong = false;
    public int rotMask = 0;
    public Color color = Color.white;

    public Transform asOther;

    public bool once = true;
    private bool was = false;
    public bool takeWaitFromAnimat = false;
    public void OnEnable()
    {
        if (takeWaitFromAnimat)
        {
            wait = GetComponentInParent<Animato>().wait;
        }
        
        if (once && was) return;
        was = true;
        
        if (actStr == "destroy")
        {
            act = () =>
            {
                if (this == null) return;
                Destroy(gameObject);
            };
        }
        
        UtilsControl.Instance.ApplyCurve(asOther == null ? transform : asOther, ac, curve, act, time, speed, evKoef, wait, color, pong, rotMask, waitBetween:waitBetween);
    }


}
