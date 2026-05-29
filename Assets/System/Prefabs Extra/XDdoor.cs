using System;
using System.Collections.Generic;
using UnityEngine;

public class XDdoor : ComponentBehavior
{
    public List<Bon> price;
    private bool done = false;

    private float dstTake = 1;

    private (int, int, Vector3) pos;

    public Collider2D realCol;
    public Collider realCol3D;
    private void Start()
    {
        
    }

    void OnMouseUp()
    {
        Debug.Log("DOOR");
        if (done) return;
        var a = MainStates.instance.lastAllySelected == null ? MainStates.instance.all["main_player"] :  MainStates.instance.lastAllySelected;
        
        var rr = MainStates.instance.GetDistance(transform.position, a, out float tt);

            if (rr <= 1)
            {
                if (price.Count > 0)
                {
                    var bb = MainStates.instance.UI_dynamikPrice.GetComponent<Buyable>();
                    bb.SetParams(true, price, "door", "open_door", () => Open(a), true);
                    bb.gameObject.SetActive(true);
                }
                else
                {
                    Open(a);
                }
                
            
        }

    }

    public void Open(RObj a)
    {
        gameObject.layer = 0;
        if (realCol) realCol.enabled = false;
        if (realCol3D) realCol3D.enabled = false;
        
        gameObject.GetComponentInChildren<SpriterAnim>().CrossFade("open", 0.1f);
        done = true;
        pos = PositionSetter.instance.GetClosestPos(transform.position);
        PositionSetter.instance.RemoveWall(pos.Item1, pos.Item2);
        SoundManager.instance.PlayAny("door_open");
        PositionSetter.instance.OpenFog(a.Position);      
        
        
    }
}
