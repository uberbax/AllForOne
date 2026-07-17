using System;
using System.Collections.Generic;
using UnityEngine;

public class XDclickMove : ComponentBehavior
{

    bool isMoving = false;
    private Vector3 endPos;
    private float speed = 3;
    ObjHolder holder;
    private RObj mon;
    void Start()
    {
        holder = GetComponentInParent<ObjHolder>();
        mon = holder.obj;
    }

    private void Update()
    {
        var v = ModelStatistics.instance.GetStatValue("battle");
        if (v == 2) return;
        
        if (Input.GetMouseButtonDown(0))
        {
            if (UtilsControl.IsPointerOverUIElement()) return;
            //it also might be select or interactable
            
            endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            endPos.z = 0;
            isMoving = true;
            if (mon.HasVis("animator"))
            {
                mon.visuals["animator"].GetComponent<XDanimator>().SetState("walk");
            }
            
            var h = endPos - mon.main.transform.position;
            if (h.x > 0)
            {
                mon.SetScale(h.x > 0);
            }
            else if (h.x < 0)
            {
                mon.SetScale(h.x > 0);
            }
        }
        
        if (!isMoving) return;
        var vec = endPos - holder.transform.position;
        if (vec.magnitude > speed * Time.deltaTime)
        {
            holder.transform.position += vec.normalized * speed * Time.deltaTime;
        }
        else
        {
            holder.transform.position = endPos;
        }
        
        if (vec.magnitude < 0.2f)
        {
            isMoving = false;
            if (mon.HasVis("animator"))
            {
                mon.visuals["animator"].GetComponent<XDanimator>().SetState("idle");
            }
        }
    }
}