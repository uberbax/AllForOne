using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDinfo : ComponentBehavior
{
    // Start is called before the first frame update
    public RObj mon;
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        if (transform.parent.name.IndexOf("bird_") >= 0)
        {
            var aa = gameObject.AddComponent<CircleCollider2D>();
            aa.radius = 0.5f;
            aa.isTrigger = true;
            aa.offset = new Vector2(0, 0.6f);
        }
    }

    void Update()
    {
        bool wasClick = false;
        if (Input.GetMouseButtonDown(0))
        {
            if (UtilsControl.IsPointerOverUIElement()) return;
            if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
            {
                var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var hit = Physics2D.Raycast(pos, new Vector2(0, 1), 0.1f, 1 << LayerMask.NameToLayer("Click"));
                //
                if (hit.collider == null || hit.collider.gameObject != this.gameObject) return;
                wasClick = true;
            }
        }
        //
        if (wasClick)
        {
            OnClick();
        }
    }
    
    private void OnClick()
    {
        //met codex ?
        ModelStatistics.instance.Codex_MetMonster(mon.dbObj.ID);
        
        MainStates.instance.curClick = mon;
        MainStates.instance.UI_infoMon.SetActive(true);
        ModelStatistics.instance.Codex_MetMonster(mon.dbObj.ID);
    }
}
