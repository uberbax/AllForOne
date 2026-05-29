using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class UnityLightFlicker : MonoBehaviour
{
    public float flickerStrength = 0.01f;
    public float flickerAmp = 10f;

    public GameObject darknessImage;

    float clock;
    Vector3 originScale;
    
    SpriteRenderer spriteren;

    void Awake()
    {
        darknessImage.SetActive(true);
        spriteren = darknessImage.GetComponent<SpriteRenderer>();
        // spriteren.color = new Color(0, 0, 0, 0);
    }
    
    void Start()
    {
        clock = Random.Range(-10f, 10f);
        originScale = transform.localScale;
    }

    void Update()
    {
        clock += Time.deltaTime;

        // spriteren.color = Color.Lerp(spriteren.color, Color.black, 0.1f);
        transform.localScale = originScale + Vector3.one * Mathf.Sin(clock * flickerAmp) * flickerStrength;
    }
}