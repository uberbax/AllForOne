using System;
using UnityEngine;

public class XDselect : ComponentBehavior
{
    private RObj mon;

    private bool inBattle = false;
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
    
    public void AfterSet(string par)
    {

        if (pars.ContainsKey("in_battle"))
            inBattle = true;

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!Camera.main.orthographic)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                // Cast the ray into the 2D physics space and look for 2D colliders
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
                
                if (!hit || hit.collider.gameObject != this.gameObject) return;
            }
            else
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
            }

            if (inBattle)
            {
                Selecto();
            }
            else 
                Select(mon);
        }
        CheckTarget();
    }

    public void CheckTarget()
    {
        if (!inBattle) return;
        if (mon == null || MainStates.instance.lastTargetSelected == null)
        {
            transform.GetChild(0).gameObject.SetActive(false);
            return;
        }
        
        transform.GetChild(0).gameObject.SetActive(mon == MainStates.instance.lastTargetSelected);
    }
    public void Selecto()
    {
        if (mon.tags.Contains("player")) return;
        MainStates.instance.lastTargetSelected = mon;

    }
    
    public static void Select(RObj mon)
    {
        MainStates.instance.lastAllySelected = mon;
        EventManager.INV("click_ally", new ArgPass());        
    }
    
}
