using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using UnityEngine;

public class XDcoll : ComponentBehavior
{
    public void AfterSet(string par)
    {
        if (pars.ContainsKey("val"))
            transform.position += new Vector3(0, float.Parse(pars["val"], CultureInfo.InvariantCulture), 0);
        if (pars.ContainsKey("scale"))
            transform.localScale *= float.Parse(pars["scale"], CultureInfo.InvariantCulture);
        
    }

    private void OnTriggerEnter(Collider col)
    {
        var mm = GetComponentInParent<ObjHolder>();

        if (col.gameObject == mm.gameObject) return;
        
        var m0 = col.gameObject.GetComponentInParent<ObjHolder>();
        
        if (mm.obj.it == ItemType.projectile && col.gameObject.name.IndexOf("deadzone") >= 0)
        {
            mm.obj.Destroy();
            return;
        }        

        Do(mm, m0);        
    }
    
    private void OnTriggerEnter2D(Collider2D col)
    {
        //Debug.Log("COLLIDED: " +name + " " + transform.parent.name + " " + col.gameObject.name);
        var mm = GetComponentInParent<ObjHolder>();

        if (col.gameObject == mm.gameObject) return;
        
        var m0 = col.gameObject.GetComponentInParent<ObjHolder>();
        
        if (mm.obj.it == ItemType.projectile && col.gameObject.name.IndexOf("deadzone") >= 0)
        {
            mm.obj.Destroy();
            return;
        }    
        
        //questionable
        if (mm.obj.it == ItemType.projectile 
            && col.gameObject.layer == LayerMask.NameToLayer("Nopass")
            && mm.obj.exact == null
            && mm.obj.GetPar("empty_req") < 1 && mm.obj.GetPar("ricochet") < 1)
        {
            
            if (mm.obj.GetPar("aoe") > 0)
            {
                SkillExecutor.instance.Explode(mm.obj);
            }
            else
            {
                mm.obj.Destroy();
            }
            
            return;
        }   

        Do(mm, m0);
    }

    private void GetReflectDir(RObj mmObj, Collider2D col)
    {
        var pnts = UtilsControl.Instance.GetAllColliderBoundaryPoints(col);
        
    }

    public void Do(ObjHolder mm, ObjHolder m0)
    {
        //we hit no monster
        if (m0 == null) return;
        
        //if (MainStates.instance.dmgTimes.ContainsKey())
        
        if (mm.obj.exact != null && m0.obj != mm.obj.exact)
            return;
        
        if (mm.obj.it == ItemType.projectile && mm.obj.GetPar("pen_cnt") < 0)
        {
            //mm.obj.Destroy();
            return;
        }      
        
        if (m0.obj.it != ItemType.monster || 
            (mm.obj.it != ItemType.projectile && mm.obj.it != ItemType.item)
            ) return;

        //if (MainStates.instance.dmgTimes.ContainsKey(mm.obj.owner.RID))
        //{
        //    Debug.Log(Time.time - MainStates.instance.dmgTimes[mm.obj.owner.RID]);
        //}
        
        //target enemy
        if (
            (!MainStates.CompareTags(m0.obj, mm.obj.owner) && (mm.obj.GetPar("target") == 0 || mm.obj.dbObj.pars["target"] == 0))
            || mm.obj.GetPar("target") == 2
            )
        {
            if (mm.obj.GetPar("aoe") > 0)
            {
                SkillExecutor.instance.Explode(mm.obj, mm.obj.exact);
                return;
            }
            //explode
            MainStates.instance.DealDamage(m0.obj, mm.obj);

            if (mm.obj.effect != null)
            {
                var g = Instantiate(mm.obj.effect, transform.position, transform.rotation);
                Destroy(g, 1);
            }
            
            if (mm.obj.GetPar("bounce") > 0)
            {
                mm.name = mm.name.Replace("_move","");
                
                var md =mm.GetComponent<MoveDir>();
                if (md !=null && md.cr != null)
                    StopCoroutine(md.cr);
                
                Debug.Log(mm.name);
                mm.obj.ChangePar("bounce", -1);
                var rr = SkillExecutor.instance.ExecuteSkill(mm.obj.owner, mm.obj, null, mm.obj.Position, true, 
                    new List<RObj>{m0.obj}, null, true);
                
                if (rr != ExecReso.OK)
                    mm.obj.Destroy();
                
                return;
            }
            
            mm.obj.ChangePar("pen_cnt", -1);
            if (mm.obj.GetPar("pen_cnt") <= 0)
                mm.obj.Destroy();
            //
            
        } //target ally
        else if (
            (MainStates.CompareTags(m0.obj, mm.obj.owner) &&
                 (mm.obj.GetPar("target") == 1 || mm.obj.dbObj.pars["target"] == 1))
            || mm.obj.GetPar("target") == 2
            )
        {
            if (mm.obj.GetPar("aoe") > 0)
            {
                SkillExecutor.instance.Explode(mm.obj);
                return;
            }
            //explode
            MainStates.instance.DealDamage(m0.obj, mm.obj);
            //pen_cnt on heal ?
            //end effect ?
            if (mm.obj.effect != null)
            {
                var g = Instantiate(mm.obj.effect, m0.transform.position, transform.rotation);
                Destroy(g, 1);
            }

            if (mm.obj.GetPar("bounce") > 0)
            {
                mm.name = mm.name.Replace("_move","");
                
                var md =mm.GetComponent<MoveDir>();
                if (md !=null && md.cr != null)
                    StopCoroutine(md.cr);
                
                Debug.Log(mm.name);
                mm.obj.ChangePar("bounce", -1);
                var rr = SkillExecutor.instance.ExecuteSkill(mm.obj.owner, mm.obj, null, mm.obj.Position, true, 
                    new List<RObj>{m0.obj}, null, true);
                
                if (rr != ExecReso.OK)
                    mm.obj.Destroy();
                
                return;
            }
            
            mm.obj.ChangePar("pen_cnt", -1);
            if (mm.obj.GetPar("pen_cnt") <= 0)
                mm.obj.Destroy();
        }        
    }
    
}
