using System.Collections.Generic;
using UnityEngine;

public class SkillBehavior : MonoBehaviour
{
    public RObj target;
    public Vector3 dir;
    public float tm = 1;
    public float spd = 1;
    
    public virtual void SetTarget(RObj targ)
    {
        target = targ;
        dir = targ.Position - transform.position;
    }
    
    public virtual void SetTarget(Vector3 targ)
    {
        dir = targ - transform.position;
    }
}
