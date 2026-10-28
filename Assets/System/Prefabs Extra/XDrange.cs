using System;
using TMPro;
using UnityEngine;

public class XDrange : MonoBehaviour
{
    public void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("MAIN: " + other.gameObject.name);
        var cc = other.transform.parent;
        if (cc != null)
        {
            var dd = cc.GetComponent<ObjHolder>();
            if (dd != null && dd.obj.it == ItemType.monster && dd.obj.tags[0] == "enemy")
            {
                dd.obj.AddMeta("sword");
                dd.obj.RemoveViz("rand_move");
                dd.obj.RemoveViz("info");
            }
        }
    }
}
