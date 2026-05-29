using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Xdtimer : ComponentBehavior
{
    public RObj mon;
    
    public Image fillImage;
    public TextMeshProUGUI timeLeft;

    private float tMax = 0;
    private float t = 0;
    
    //onEnd ?
    private bool done = false;
    public Action onEnd = null;
    
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        tMax = mon.dynamic.time;
    }
    // Update is called once per frame
    void Update()
    {
        if (done) return;
        
        t += Time.deltaTime;
        if (t >= tMax) t = tMax;
        
        if (timeLeft)
        {
            timeLeft.text = (tMax - t).ToString();
        }

        if (fillImage)
        {
            fillImage.fillAmount = t / tMax;
        }

        if (t >= tMax)
        {
            done = true;
            mon.ChangePar("built", 1);
            mon.RemoveViz("timer");
            if (onEnd != null) onEnd();
        }
    }
}
