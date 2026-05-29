using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityArrowPointing : MonoBehaviour
{
    public float amp = 0.25f;
    
    Vector3 origin;
    Vector3 originscale;
    
    void Start()
    {
        origin = transform.position;
        originscale = transform.localScale;
    }

    void Update()
    {
        transform.localScale = originscale + Vector3.one * Mathf.Sin(Time.time) * amp;
    }
}
