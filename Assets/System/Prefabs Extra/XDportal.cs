using System;
using UnityEngine;

public class XDportal : MonoBehaviour
{
    public Transform endPoint;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var tt = other.GetComponentInParent<ObjHolder>();
        if (tt == null) return;
        if (tt.obj.it != ItemType.monster) return;
        
        tt.obj.main.transform.name = tt.obj.main.transform.name.Replace("_move", "");
        
        tt.obj.main.transform.position = endPoint.position;
        tt.obj.Position = endPoint.position;
        tt.obj.AdjustPosition();
        SoundManager.instance.PlayAny("teleport");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        var tt = other.GetComponentInParent<ObjHolder>();
        if (tt == null) return;
        if (tt.obj.it != ItemType.monster) return;
        
        tt.obj.main.transform.name = tt.obj.main.transform.name.Replace("_move", "");
        
        tt.obj.main.transform.position = endPoint.position;
        tt.obj.Position = endPoint.position;
        tt.obj.AdjustPosition();
        SoundManager.instance.PlayAny("teleport");
    }
}
