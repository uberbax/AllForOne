using System.Collections.Generic;
using UnityEngine;

public class XDdrag : ComponentBehavior
{
    private bool isDrag = false;
    private Vector3 dlt = Vector3.zero;

    private RObj mon;
    private ObjHolder holder;
    void Start()
    {
        holder = GetComponentInParent<ObjHolder>();
        mon = holder.obj;
        gameObject.layer = LayerMask.NameToLayer("Drag");
    }
    
    void OnMouseDown2D()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (isDrag) return;
        
        var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.Raycast(pos, new Vector2(0, 1), 0.1f, 1 << LayerMask.NameToLayer("Drag"));
        
        if (hit.collider == null || hit.collider.gameObject != this.gameObject) return;
        
        Debug.Log("OnMouseDownUnit");
        isDrag = true;
        name += "_drag";
        
        var vec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        dlt = holder.transform.position - vec;
    }
    
    
    public void Released()
    {
        name = name.Replace("_drag", "");
        if (holder.transform.position.x > MainStates.instance.dragHi.position.x)
        {
            holder.transform.position = new Vector3(MainStates.instance.dragHi.position.x, holder.transform.position.y, holder.transform.position.z);
        }
        if (holder.transform.position.x < MainStates.instance.dragLo.position.x)
        {
            holder.transform.position = new Vector3(MainStates.instance.dragLo.position.x, holder.transform.position.y, holder.transform.position.z);
        }
        if (holder.transform.position.y > MainStates.instance.dragHi.position.y)
        {
            holder.transform.position = new Vector3(holder.transform.position.x, MainStates.instance.dragHi.position.y, holder.transform.position.z);
        }
        if (holder.transform.position.y < MainStates.instance.dragLo.position.y)
        {
            holder.transform.position = new Vector3(holder.transform.position.x, MainStates.instance.dragLo.position.y, holder.transform.position.z);
        }
        //main states available fields?
        Boogey(holder.transform);
    }

    public static void Boogey(Transform holder)
    {
         if (MainStates.instance.availableMeRoot != null)
         {
             var ff = UtilsControl.GetMeClosestByMany(new List<Transform>{MainStates.instance.availableMeRoot}, holder.transform.position,
                 out float dd, (x) => x.GetComponent<PositionHolder>().who == null 
                                      || x.GetComponent<PositionHolder>().who == holder);
             
             
             for (int i = 0; i < MainStates.instance.availableMeRoot.childCount; i++)
             {
                 var hh =  MainStates.instance.availableMeRoot.GetChild(i).GetComponent<PositionHolder>();
                 if (hh.who == holder) hh.who = null;
             }
             
             holder.position = ff.transform.position;
             if (MainStates.instance.highlight)
                MainStates.instance.highlight.transform.position = ff.transform.position;
             
             ff.GetComponent<PositionHolder>().who = holder;
             PlacerSystem.instance.allow = true;

         }
         else
         {
             //we just use world
             var hh = UtilsControl.GetMousePoint();
             Vector3 nn = Vector3.zero;
             if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0)
             {
                 var mm = PositionSetter.instance.GetClosestPos(hh);
                 nn = mm.Item3;
                 //is free
                 var k = PositionSetter.instance.IsEmpty(mm.Item1, mm.Item2, out var go);
                 PlacerSystem.instance.allow = k;
             }
             else
             {
                 nn = hh;
                 //is touching ?
                 bool isTouching = Physics2D.OverlapCircle(nn, 0.3f, 1 <<  LayerMask.NameToLayer("Nopass"));
                 PlacerSystem.instance.allow = isTouching;
             }
             
             if (MainStates.instance.highlight)
                 MainStates.instance.highlight.transform.position = nn;
             
             holder.position = nn;

         }
    }
    
    void LateUpdate()
    {
        OnMouseDown2D();
        
        if (Input.GetMouseButtonUp(0) && isDrag)
        {
            isDrag = false;
            Released();
        }
        
        //place
        if (isDrag)
        {
            var vec = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            holder.transform.position = dlt + vec;
        }
        
    }
    
}
