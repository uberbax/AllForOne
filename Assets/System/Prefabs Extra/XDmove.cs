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

    public void TryMove(Vector3 dVec)
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");
        
        Vector3 dVec = new Vector3(h * Time.deltaTime * spd, v * Time.deltaTime * spd);
        if (useMobileJoystick)
        {
            dVec = MainStates.instance.joystick.Value * Time.deltaTime * spd;
        }        
        
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {

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
        Vector3 dVecRes = Vector3.zero;

        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {
            dVecRes = dVecXY;
            mon.position += dVecXY;
        }
        else
        {
            dVecRes = dVecXZ;
            mon.position += dVecXZ;
        }

        var c = Mathf.Abs(dVec.x) + Mathf.Abs(dVec.y);
        if (c > 0.002f)
        {
            UtilsControl.CalculateLayer(rMon.main.gameObject);
            //Debug.Log("da");
            MainStates.isMovingByControl = true;
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

            /*
            if (MainStates.whenMoveMinionsMove)
            {
                foreach (var v1 in PartyController.instance.party)
                {
                    //try move by dvec
                    v1.main.transform.position += dVecRes;
                }
            }
            */
        }
        else
        {
            //Debug.Log("no");
            MainStates.isMovingByControl = false;
            if (rMon.HasVis("animator"))
            {
                rMon.visuals["animator"].GetComponent<XDanimator>().SetState("idle", "walk");
            }
        }
    }
}
