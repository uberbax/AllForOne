using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class XDfight : ComponentBehavior
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
            var hWeapon = holder.obj.GetWeapon();

            RObj ro = null;

            if (hWeapon == null)
            {
                ro = DatabaseAll.instance.CreateProjectile(holder.obj, "basic_range", vv);
                ro.AddViz("coll");
            }
            else
            {
                ro = DatabaseAll.instance.CreateProjectile(holder.obj, hWeapon.dbObj.refSkill, vv);
                ro.AddViz("coll");
            }



            Vector3 pos = Vector3.zero;
            if (MainStates.is3D)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var h1 = Physics.Raycast(ray, out RaycastHit hit, 100, 1<< LayerMask.NameToLayer("Earth"));
                pos = hit.point;
            }
            else 
                pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            ro.main.name = "BOMBOM";
            
            //inbetween ?
            var tt = holder.obj.visMain.transform.Find("legs");
            var tt1 = holder.obj.visMain.transform.Find("head");
            ro.main.transform.position = (tt.position + tt1.position) / 2;
            
            holder.obj.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("attack");
            //attack
            var dlt = pos - holder.transform.position - vv;

            if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
            {
                dlt.z = 0;
            }

            var range = ro.GetPar("range");
            
            UtilsControl.Instance.MoveTo(ro.main.transform, 15, holder.transform.position + dlt.normalized * (range+1),
                () =>
                {
                    //destroy if melee
                    Destroy(ro.main.gameObject);
                }, null);
        }
    }
}
