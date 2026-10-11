using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class XDshoot : ComponentBehavior
{
    private ObjHolder holder;
    
    Vector3 vv = Vector3.zero;

    public void AfterSet(string par)
    {
        vv = new Vector3(0, float.Parse(pars["val"], CultureInfo.InvariantCulture),0);
    }
    void Start()
    {
        holder = GetComponentInParent<ObjHolder>();
    }
    
    void Update()
    {
        if (UtilsControl.IsPointerOverUIElement()) return;
        
        if (Input.GetMouseButtonDown(0))
        {
            var ro = DatabaseAll.instance.CreateProjectile(holder.obj, "basic_range", vv);
            ro.AddViz("coll");
            Vector3 pos = Vector3.zero;
            if (MainStates.is3D)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var h1 = Physics.Raycast(ray, out RaycastHit hit, 100, 1<< LayerMask.NameToLayer("Earth"));
                pos = hit.point;
                pos.z = 0;
            }
            else 
                pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            ro.main.name = "BOMBOM";
            
            var dlt = pos - holder.transform.position - vv;
            UtilsControl.Instance.MoveTo(ro.main.transform, 15, holder.transform.position + new Vector3(dlt.x, dlt.y, 0)*100, null, null);
        }
    }
}
