using System;
using UnityEngine;

public class XDselect : ComponentBehavior
{
    private RObj mon;
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }
    
    /*
    void OnMouseDown()
    {
        MainStates.instance.lastAllySelected = mon;
        EventManager.INV("click_ally", new ArgPass());
    }
    */

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
            {
                var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var hit = Physics2D.Raycast(pos, new Vector2(0, 1), 0.1f, 1 << LayerMask.NameToLayer("Select"));
                //
                if (hit.collider == null || hit.collider.gameObject != this.gameObject) return;
            }
            else
            {
                RaycastHit rh;
                var pos = Camera.main.ScreenPointToRay(Input.mousePosition);
                var hit = Physics.Raycast(pos, out rh, 100, 1 << LayerMask.NameToLayer("Select"));
                //
                if (!hit || rh.collider.gameObject != this.gameObject) return;
            }

            Select(mon);
        }
    }

    public static void Select(RObj mon)
    {
        MainStates.instance.lastAllySelected = mon;
        EventManager.INV("click_ally", new ArgPass());        
    }
}
