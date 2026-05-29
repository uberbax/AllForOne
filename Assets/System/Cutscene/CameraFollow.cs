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
    [ContextMenu("CalcDelta")]
    public void CalculateDelta()
    {
        delta = -target.position + transform.position;
    }
    
    private void Start()
    {
        savedIni = transform.position - target.position;
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
    }

}