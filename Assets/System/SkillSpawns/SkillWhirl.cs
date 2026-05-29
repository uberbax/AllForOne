using System;
using UnityEngine;

public class SkillWhirl : SkillBehavior
{
    private void Update()
    {
        //if (target == null) return;
        
        if (dir != Vector3.zero)
            transform.position += dir.normalized * Time.deltaTime * spd;
        
        tm -= Time.deltaTime;
        if (tm <= 0)
        {
            Destroy(gameObject);
        }
    }
}
