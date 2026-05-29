using System;
using UnityEngine;

public class Animato : MonoBehaviour
{
    public bool waitAsChildIndex = false;
    public bool fadeOnStart = false;
    public bool moveOnStart = false;
    
    public Transform p0;
    public Transform p1;
    
    
    public bool fadeOnClose = true;
    private CanvasGroup cv;

    public float wait = 0;

    private float maxScale = -1;

    public float tm = 1;
    private void OnEnable()
    {
        if (waitAsChildIndex)
            wait = transform.GetSiblingIndex() * 0.33f;
        
        cv = GetComponent<CanvasGroup>();
        if (cv == null)
        {
            cv = gameObject.AddComponent<CanvasGroup>();
        }

        if (moveOnStart)
        {
            transform.position = p0.position;
            UtilsControl.Instance.MoveTo(transform, 2000, p1.position, null, null, useRight:false);
        }

        if (fadeOnStart)
            cv.alpha = 0;
        else
        {
            cv.alpha = 1;
        }

        if (maxScale < 0)
            maxScale = transform.localScale.x;

        transform.localScale = Vector3.zero;
        
        UtilsControl.Instance.ApplyCurve(transform, AnimationCurve.Linear(0,0,1,maxScale), UtilsControl.CurveType.ScaleAbs, null, tm, 1 / tm, 1, wait, Color.white );

        if (fadeOnStart)
        {
            UtilsControl.Instance.ApplyCurve(transform, AnimationCurve.Linear(0,0,1,1), UtilsControl.CurveType.CanvasFade, null, tm, 1 / tm, 1, wait, Color.white );
        }
    }

    private void OnDisable()
    {
        //WE CAN ENABLE ON ONDISABLE
        //THINK ABOUT IT
        if (fadeOnClose)
        {
            cv.alpha = 0;
        }
    }
}
