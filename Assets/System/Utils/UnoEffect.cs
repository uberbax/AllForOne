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
    void Start()
    {
        if (actStr == "destroy")
        {
            act = () =>
            {
                if (this == null) return;
                Destroy(gameObject);
            };
        }
        
        UtilsControl.Instance.ApplyCurve(transform, ac, curve, act, time, speed, evKoef, wait, color, pong, rotMask, waitBetween:waitBetween);
    }


}
