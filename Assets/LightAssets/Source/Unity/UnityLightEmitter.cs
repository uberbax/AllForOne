using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public interface LightSourceListener
{
    void OnLit(GameObject source, bool canStun);
    void OnUnLit(GameObject source, bool canStun);
}

public class UnityLightEmitter : MonoBehaviour
{
    public float range = 3f;
    public bool canStun;

    public bool growScale;

    public List<Collider2D> illuminated = new List<Collider2D>();

    static Collider2D[] results = new Collider2D[32];

    void Start()
    {
        if (growScale)
        {
            var scaleto = transform.localScale;
            transform.localScale = Vector3.zero;
            transform.DOScale(scaleto,0.15f);
        }
    }

    public void Update()
    {
        for (var i = 0; i < results.Length; i++)
            results[i] = null;
        
        Physics2D.OverlapCircleNonAlloc(transform.position, range, results);

        foreach (var ri in results)
            if (ri != null)
                if (!illuminated.Contains(ri))
                {
                    var lightSourceListener = ri.gameObject.GetComponent<LightSourceListener>();
                    lightSourceListener?.OnLit(gameObject, canStun);
                    illuminated.Add(ri);
                }

        for (var index = illuminated.Count - 1; index >= 0; index--)
        {
            var il = illuminated[index];
            if (!results.Contains(il))
            {
                var lightSourceListener = il.gameObject.GetComponent<LightSourceListener>();
                lightSourceListener?.OnUnLit(gameObject, canStun);
                illuminated.Remove(il);
            }
        }
    }

    void OnDisable()
    {
        foreach (var ri in illuminated)
        {
            if (ri != null)
            {
                var lightSourceListener = ri.gameObject.GetComponent<LightSourceListener>();
                lightSourceListener?.OnUnLit(gameObject, canStun);
            }
        }

        illuminated.Clear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f,1f,0f,0.15f);
        Gizmos.DrawSphere(transform.position, range);
    }

    public void AboutToDie()
    {
        transform.parent = null;
        transform.DOScale(Vector3.zero,0.15f).OnComplete(DestroySelf);
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}