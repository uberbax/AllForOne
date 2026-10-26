using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;

public class XDdmgTrack : ComponentBehavior
{
    public static float scaleMult = 1;
    // Start is called before the first frame update
    private RObj mon;

    private float prevHp = -1;

    private Transform head;
    
    //
    private bool trackMana = true;
    private bool trackShield = true;
    
    private float prevMp = -1;
    private float prevShld = -1;
    
    internal void Bind(RObj owner)
    {
        if (mon == owner && mon != null) return;
        mon = owner;
        head = null;
        if (mon != null && mon.visuals.TryGetValue("vis_main", out var visual) && visual != null)
            head = visual.transform.Find("head");
    }

    private void Start()
    {
        if (mon != null) return; // An event may arrive before Start.
        var holder = GetComponentInParent<ObjHolder>();
        if (holder != null) Bind(holder.obj);
    }

    public void AfterSet(string par)
    {

        if (pars.ContainsKey("mana"))
            trackMana = false;

    }

    // Called by parameter mutations; no per-frame polling.
    public void Changed(float hpDlt, float manaDlt, float shldDlt, float msgDlt)
    {
        if (mon == null)
        {
            var holder = GetComponentInParent<ObjHolder>();
            if (holder != null) Bind(holder.obj);
            if (mon == null) return;
        }


        Vector3 where = transform.position;
        if (head != null)
            where = head.position;
        
        if (msgDlt > 0)
        {
            mon.SetPar("show_message", 0);
            DamageNumber newDamageNumber = UtilsControl.Instance.prefabPhrase.Spawn(where);
        }

        if (hpDlt != 0)
        {
            var dlt = hpDlt;
            if (dlt < 0)
            {
                DamageNumber newDamageNumber = UtilsControl.Instance.prefab.Spawn(where, dlt);

                if (mon.GetPar("was_crit") > 0)
                {
                    newDamageNumber.SetScale(2);
                    newDamageNumber.enableRightText = true;
                    newDamageNumber.rightText = "!";
                    mon.SetPar("was_crit", 0);
                }
                
                newDamageNumber.gameObject.transform.localScale *= scaleMult;
            }
            else
            {
                DamageNumber newDamageNumber = UtilsControl.Instance.prefabPos.Spawn(where, dlt);
                //newDamageNumber.SetColor(Color.green);
                
                if (mon.GetPar("was_crit") > 0)
                {
                    newDamageNumber.SetScale(2);
                    newDamageNumber.enableRightText = true;
                    newDamageNumber.rightText = "!";
                    mon.SetPar("was_crit", 0);
                }
                
                newDamageNumber.gameObject.transform.localScale *= scaleMult;

            }
        }
        
        //

        if (trackMana)
        {
            if (manaDlt != 0)
            {
                var dlt = manaDlt;
                DamageNumber newDamageNumber = UtilsControl.Instance.prefabMana.Spawn(where, dlt);

            }
        }
        
        //
        
        if (shldDlt != 0)
        {
            var dlt = shldDlt;
            DamageNumber newDamageNumber = UtilsControl.Instance.prefabWard.Spawn(where, dlt);

        }
        
        //prevShld = cs;
        
        
    }
}
