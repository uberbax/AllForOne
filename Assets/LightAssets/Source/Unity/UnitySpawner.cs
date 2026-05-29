using System.Collections.Generic;
using UnityEngine;

public class UnitySpawner : MonoBehaviour
{
    public float range = 0.5f;
    public GameObject vfx;
    public GameObject vfx2;
    public GameObject who;
    public List<Transform> where;

    void Update()
    {
        if (Vector2.Distance(UnityCharacter.playerPosition, transform.position) < range)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        SFX.i.EnemyAppear();
        
        foreach (var t in where)
        {
            if (vfx != null)
                Instantiate(vfx, t.position, Quaternion.identity, transform.root);
            if (vfx2 != null)
                Instantiate(vfx2, t.position, Quaternion.identity, transform.root);
            Instantiate(who, t.position, Quaternion.identity, transform.root);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
        Gizmos.DrawSphere(transform.position, range);
    }
}