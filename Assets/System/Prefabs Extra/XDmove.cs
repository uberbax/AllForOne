using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class XDmove : ComponentBehavior
{
    // Start is called before the first frame update
    private Transform mon;
    private RObj rMon;
    private float spd = 6;
    
    public static bool useNavmesh = false; 
    public static bool useMobileJoystick = false; 
    
    public void AfterSet(string val)
    {
        spd =  float.Parse(pars["val"], CultureInfo.InvariantCulture);
    }
    
    void Start()
    {
        mon = transform.parent;
        rMon = GetComponentInParent<ObjHolder>().obj;
    }

    // Update is called once per frame
    void Update()
    {
        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");
        
        Vector3 dVec = new Vector3(h * Time.deltaTime * spd, v * Time.deltaTime * spd);
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {
            
            if (useMobileJoystick)
            {
                //Debug.Log(MainStates.instance.joystick.Horizontal);
                //Debug.Log(MainStates.instance.joystick.Vertical);
                //Debug.Log(MainStates.instance.joystick.Value);
                
                dVec = MainStates.instance.joystick.Value * Time.deltaTime * spd;
                
                //Debug.Log(dVec);
            }
            
            var rh = Physics2D.Raycast(transform.position,
                dVec, 0.3f, LayerMask.GetMask("Nopass"));
            if (rh.collider != null)
            {
                //cant mode
                return;
            }

            if (useNavmesh)
            {
                var hh = mon.position + dVec;
                var f = UtilsControl.IsPointOnNavMesh(hh);
                if (!f) return;
            }
        }
        else
        {
            //var rh = Physics.Raycast(transform.position, new Vector3(h * Time.deltaTime * spd, 0.2f, v * Time.deltaTime * spd), 0.3f, LayerMask.GetMask("Nopass"));
            var rh = Physics.SphereCast(transform.position, 0.3f, new Vector3(h * Time.deltaTime * spd, 0.2f, v * Time.deltaTime * spd), out var hit, 0.3f, LayerMask.GetMask("Nopass"));
            
            if (rh)
            {
                //cant mode
                return;
            }
        }

        Vector3 dVecXY = new Vector3(dVec.x, dVec.y, 0);
        Vector3 dVecXZ = new Vector3(dVec.x, 0, dVec.z);
        
        
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
            mon.position += dVecXY;
        else
            mon.position += dVecXZ;

        var c = Mathf.Abs(dVec.x) + Mathf.Abs(dVec.y);
        if (c > 0.01f)
        {
            if (rMon.HasVis("animator"))
            {
                rMon.visuals["animator"].GetComponent<XDanimator>().SetState("walk");
            }

            if (dVec.x > 0)
            {
                rMon.SetScale(dVec.x > 0);
            }
            else if (dVec.x < 0)
            {
                rMon.SetScale(dVec.x > 0);
            }
        }
        else
        {
            if (rMon.HasVis("animator"))
            {
                rMon.visuals["animator"].GetComponent<XDanimator>().SetState("idle");
            }
        }
    }
}
