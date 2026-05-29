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

        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {
            var rh = Physics2D.Raycast(transform.position,
                new Vector2(h * Time.deltaTime * spd, v * Time.deltaTime * spd), 0.3f, LayerMask.GetMask("Nopass"));
            if (rh.collider != null)
            {
                //cant mode
                return;
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
        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
            mon.position += new Vector3(h * Time.deltaTime * spd, v * Time.deltaTime * spd, 0);
        else
            mon.position += new Vector3(h * Time.deltaTime * spd, 0, v * Time.deltaTime * spd);

        var c = Mathf.Abs(h) + Mathf.Abs(v);
        if (c > 0.01f)
        {
            if (rMon.HasVis("animator"))
            {
                rMon.visuals["animator"].GetComponent<XDanimator>().SetState("walk");
            }

            if (h > 0)
            {
                rMon.SetScale(h > 0);
            }
            else if (h < 0)
            {
                rMon.SetScale(h > 0);
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
