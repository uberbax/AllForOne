using System;
using System.Globalization;
using UnityEngine;

public class XDrealcol : ComponentBehavior
{
    ObjHolder mon;
    private float colRad = 0.5f;
    
    public void AfterSet(string par)
    {
        if (pars.ContainsKey("val"))
            colRad = float.Parse(pars["val"], CultureInfo.InvariantCulture);
    }

    private CircleCollider2D col;
    private Rigidbody2D rigidbody;
    
    private SphereCollider col3D;
    private Rigidbody rigidbody3D;
    
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>();

        if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
        {
            col = mon.gameObject.AddComponent<CircleCollider2D>();
            rigidbody = mon.gameObject.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0;
            col.radius = colRad;

            // ? pomojet li
            rigidbody.freezeRotation = true;
        }
        else
        {
            col3D = mon.gameObject.AddComponent<SphereCollider>();
            rigidbody3D = mon.gameObject.AddComponent<Rigidbody>();
            rigidbody3D.useGravity = false;
            col3D.center += new Vector3(0, 0.22f, 0);
            col3D.radius = colRad;

            //не, все равно траблы
            //col3D.radius = 0.18f;
            //rigidbody3D.freezeRotation = true;
        }

    }

    private void OnDestroy()
    {
        Destroy(rigidbody);
        Destroy(col);
    }
}
