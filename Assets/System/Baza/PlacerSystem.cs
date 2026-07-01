using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlacerSystem : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static PlacerSystem instance;

    private bool isAttached = false;

    public GameObject attach;
    public RObj attachedRo;

    public Action<RObj> onDragEnded = null;
    public Action<RObj> onDragEach = null;
    
    public bool allow = false;
    public float placingZ = -0.1f;

    //
    public bool facingRight = true;
    public bool attachDrag = true;
    
    private void Awake()
    {
        instance = this;
    }

    public void Attach(RObj p)
    {
        isAttached = true;
        RObj o = p.Clone();
        attachedRo = o;
        attach = DatabaseAll.instance.CreateOnlyVizual(o, Vector3.zero);
        attach.name += "_placing";
    }

    private void Update()
    {
        if (!isAttached) return;

        if (Input.GetMouseButtonDown(1))
        {
            Cancel();
            return;
        }

        if (MainStates.instance.highlight)
        {
            if (allow)
                MainStates.instance.highlight.GetComponentInChildren<SpriteRenderer>().color = Color.green;
            else
            {
                MainStates.instance.highlight.GetComponentInChildren<SpriteRenderer>().color = Color.red;
            }
        }

        var ps = UtilsControl.GetMousePoint();
        attach.transform.position = new Vector3(ps.x, ps.y, placingZ);
        
        if (onDragEach != null)
            onDragEach(attachedRo);

        if (Input.GetMouseButtonDown(0) && allow)
        {
            if (onDragEnded != null)
                onDragEnded(attachedRo);
            attach = null;
            isAttached = false;
            allow = false;

            if (attachDrag)
            {
                attachedRo.AddViz("drag");
            }
            
            if (MainStates.instance.highlight)
                MainStates.instance.highlight.transform.position = new Vector3(1000, 1000, 1000);
        }
    }

    public void Cancel()
    {
        Destroy(attach);
        
        attach = null;
        isAttached = false;
        allow = false;

        if (MainStates.instance.highlight)
            MainStates.instance.highlight.transform.position = new Vector3(1000, 1000, 1000);
    }

    private void OnDisable()
    {
        instance = null;
    }
}
