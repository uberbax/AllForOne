using System;
using UnityEngine;

public class XDricocol : MonoBehaviour
{
    private RObj mon;

    private void Start()
    {
        mon = GetComponent<ObjHolder>().obj;
    }

    public void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log("---COLLISION---");
        float bounceFactor = 1;
        
        if (mon.GetPar("ricochet") > 0)
        {
            ContactPoint2D contact = other.GetContact(0);
        
            // 2. Get the collision surface's normal direction
            Vector2 normal = contact.normal;
        
            // 3. Get the current velocity of the object
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            var moveDir = GetComponent<MoveDir>();
            //Vector2 incomingVelocity = rb.linearVelocity;
            var incomingVelocity = moveDir.dir;
        
            // 4. Calculate the new velocity using the reflection formula
            float speedAlongNormal = Vector2.Dot(incomingVelocity, normal);
        
            // Optional: Ensure the objects are moving towards each other
            if (speedAlongNormal > 0) return;
        
            float newSpeedAlongNormal = -bounceFactor * speedAlongNormal;
            Vector2 impulse = (newSpeedAlongNormal - speedAlongNormal) * normal;
        
            // 5. Apply the new velocity
            
            //rb.linearVelocity = incomingVelocity + impulse;
            moveDir.dir = new Vector2(incomingVelocity.x, incomingVelocity.y) + impulse;
            moveDir.where = new Vector3(contact.point.x, contact.point.y) + moveDir.dir * 5;
            
            mon.ChangePar("ricochet", -1);
        }
        else
        {
            Destroy(GetComponent<CircleCollider2D>());
        }
        
    }
    
}
