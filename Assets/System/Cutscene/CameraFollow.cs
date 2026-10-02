using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public Transform target;

    public float smoothSpeed = 0.125f;
    public Vector3 offs = new Vector3(0, 0, 1);
    
    public bool look = true;

    public float endDst = 0.1f;
    public Action act;

    public bool resetTargetOnEnd = false;

    private Vector3 savedIni;
    public bool holdZ;

    public Vector3 delta =  Vector3.zero;
    public bool smallify = false;
    private float tm = 1;
    
    [Header("OffScreen params")]
    public bool useOffscreen = false;
    public Transform camLo;
    public Transform camHi;
    public float kf = 1.8f;
    
    [ContextMenu("CalcDelta")]
    public void CalculateDelta()
    {
        delta = -target.position + transform.position;
    }
    
    private void Start()
    {
        if (target == null) return;
        savedIni = transform.position - target.position;
        tm = savedIni.magnitude / smoothSpeed;
    }

    public void SetTarget(Transform target)
    {
        
    }

    void Update ()
    {
        if (target == null) return;

        var offset = transform.position - target.position;
        if (offs.x == 0) offset.x = 0;
        if (offs.y == 0) offset.y = 0;
        if (offs.z == 0) offset.z = 0;

        if (holdZ) offset.z = savedIni.z;

        
        Vector3 desiredPosition = target.position + offset + delta;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;


        var dt = smoothedPosition - offset - delta - target.position;
        if (dt.magnitude < endDst)
        {
            if (resetTargetOnEnd)
                target = null;
            if (act != null)
                act();
            return;
        }        

        if (look)
            transform.LookAt(target);

        if (smallify)
            transform.localScale -= (Time.deltaTime) * Vector3.one;

        if (useOffscreen)
        {
            if (camHi.position.x > PositionSetter.instance.high.position.x)
            {
                delta.x = kf * (PositionSetter.instance.high.position.x - camHi.position.x);
            }
            else if (camLo.position.x < PositionSetter.instance.lo.position.x)
            {
                delta.x = kf * (PositionSetter.instance.lo.position.x - camLo.position.x);
            }
            else
            {
                delta.x = 0;
            }
            //
            
            if (camHi.position.y > PositionSetter.instance.high.position.y)
            {
                delta.y = kf * (PositionSetter.instance.high.position.y - camHi.position.y);
            }
            else if (camLo.position.y < PositionSetter.instance.lo.position.y)
            {
                delta.y = kf * (PositionSetter.instance.lo.position.y - camLo.position.y);
            }
            else
            {
                delta.y = 0;
            }
            
            
        }
    }

}