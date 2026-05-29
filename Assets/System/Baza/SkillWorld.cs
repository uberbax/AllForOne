using System;
using UnityEngine;

public class SkillWorld : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private RObj owner;
    public string id;
    public int penCnt = 0;

    public float reactTime = -1;
    private float tm = 1;
    
    public bool destrParent = false;
    public bool destrSelf = false;
    
    private void Start()
    {
        owner = GetComponentInParent<ObjHolder>().obj;
        var a = gameObject.AddComponent<ObjHolder>();
        var rr = DatabaseAll.instance.CreateProjectile(owner, id, Vector3.zero, withEmpty: true, withVisual: false, gameObject );
        
        if (penCnt > 0)
        {
            rr.SetPar("pen_cnt",  penCnt);
        }
        
        a.obj = rr;
        gameObject.AddComponent<XDcoll>();
        
        if (reactTime > 0) tm = reactTime;
    }

    private void Update()
    {
        if (reactTime <=0) return;
        tm -= Time.deltaTime;
        if (tm <= 0)
        {
            gameObject.SetActive(false);
            gameObject.SetActive(true);
            tm = reactTime;
        }
    }

    private void OnDestroy()
    {
        if (destrParent)
            Destroy(transform.parent.gameObject);
    }
}
