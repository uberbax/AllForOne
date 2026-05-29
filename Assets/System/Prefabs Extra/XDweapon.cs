using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDweapon : ComponentBehavior
{
    // Start is called before the first frame update
    public RObj mon;

    private RObj weapon;
    private RObj cProj;
    
    private GameObject spawnedWeapon;
    
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
    }
    
    private float pointChange = 0;
    void Update()
    {
        if (spawnedWeapon != null)
        {
            //rotate to mouse
            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 vec = pos - spawnedWeapon.transform.position;
            vec.Normalize();
            
            spawnedWeapon.transform.right = vec;
            
            var mv = mon.visuals["vis_main"].transform;

            if (Mathf.Abs(vec.x-pointChange) > 0.01f)
            {
                pointChange = vec.x;
                if (vec.x < 0)
                {
                    mv.localScale = new Vector3(-1, mv.localScale.y, mv.localScale.z);
                    //var sv = spawnedWeapon.transform.localScale;
                    //spawnedWeapon.transform.localScale = new Vector3(-1 * Mathf.Abs(sv.x), sv.y, sv.z);
                }
                else
                {
                    mv.localScale = new Vector3(1, mv.localScale.y, mv.localScale.z);
                    //var sv = spawnedWeapon.transform.localScale;
                    //spawnedWeapon.transform.localScale = new Vector3(Mathf.Abs(sv.x), sv.y, sv.z);
                }
            }
            
            //if with proj
            var itm = pars["proj"];
            if (itm != default)
            {
                var cnt = MainStates.instance.GetItemsCount(itm);
                if (cnt > 0 && cProj == null)
                {

                    var prj =Instantiate(ResourceHolder.instance.itemsGO[itm], spawnedWeapon.transform);
                    cProj = DatabaseAll.instance.CreateAny(itm, false, 1, prj);
                }
            }

            //return;
        }
        
        
        var g = mon.inventory.Find(x => x.upgradePars["used_slot"] == MainStates.slots["weapon"]);
        if (g != null && spawnedWeapon == null)
        {
            weapon = g;
            var f0 = g.dbObj.ID;
            spawnedWeapon = Instantiate(ResourceHolder.instance.itemsGO[f0], transform);
            var pnt = mon.visuals["vis_main"].transform.Find("weapon");
            if (pnt)
            {
                //spawnedWeapon.transform.parent = pnt;
                spawnedWeapon.transform.position = pnt.position;
                var m = spawnedWeapon.AddComponent<Follow>();
                m.who = pnt;
            }
        }

        //shoot ?
        if (cProj != null && Input.GetMouseButtonUp(0))
        {
            //shoot
            var itm = pars["proj"];
            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pos.z = MainStates.instance.SCENE_Z;
            
            var dlt = pos - transform.position;
            cProj.main.transform.parent = null;
            UtilsControl.Instance.MoveTo(cProj.main.transform, 15, pos, null, null);
            //depends of owner tagss 
            cProj.SetPar("target", 0);
            cProj.SetPar("pen_cnt", 10);
            cProj.SetPar("attack", weapon.GetPar("attack"));
            
            
            cProj.owner = mon;
            var lf = cProj;
            lf.AddViz("coll#0.5");
            FunctionTimer.Create(() => lf.AddViz("take#0.3"), 1);
            cProj = null;
            MainStates.instance.DelItems(new List<Bon>{new Bon{Key = itm, Value = 1}});
            
        }
        
    }
    

}
