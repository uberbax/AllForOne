using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShieldEffect : MonoBehaviour
{
    List<float> speeds = new List<float>();
    private void OnEnable()
    {
        speeds.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            var c = transform.GetChild(i).GetComponent<SpriteRenderer>();
            c.color = new Color(c.color.r, c.color.g, c.color.b, 0);
            speeds.Add(Random.Range(0.5f,1f));
        }
        
    }

    private void Update()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var c = transform.GetChild(i).GetComponent<SpriteRenderer>();
            var sc = c.color;
            sc.a += Time.deltaTime * speeds[i];
            c.color = sc;
        }
    }
}
