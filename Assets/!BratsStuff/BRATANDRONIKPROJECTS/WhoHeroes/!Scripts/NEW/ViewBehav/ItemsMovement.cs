using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class MoveSettings
{
    [Header("MOVE")]
    

    [Header("up move")]
    public bool smoveU = true;

    [Header("pulse up-down")]
    public bool spulse = false;

    [Header("forward move")]
    public bool smoveF = false;

    [Header("forward pulse")]
    public bool spulseF = false;


    [Header("SPEEDS")]
    public float rotateSpd = 1f;

    
    public float moveUSpd = 10f;

    
    public float pulseSpd = 3f;

   
    public float moveFSpd = 1f;

    
    public float pulseFSpd = 1f;


    [Header("TIMERS")]
    
    public float pulseTimer = 2f;

    
    public float pulseFTimer = 2f;

    [Header("ROTADE Y")]
    public bool srotate = true;

    [Header("ROTADE Euler")]
    
    public bool eulerRotate = false;

    
    public float eulerRotateSpd = 1f;

    
    public float targetEuler = 90f;
}

public class ItemsMovement : MonoBehaviour
{
    public MoveSettings settings;

    Coroutine rotateC;
    Coroutine moveUC;
    Coroutine moveFC;
    Coroutine pulseC;
    Coroutine pulseFC;
    Coroutine eulerRC;

    void OnEnable()
    {
        ApplySettings();
    }

    public void ApplySettings()
    {
        StopAllCoroutines();

        rotateC = settings.srotate      ? StartCoroutine(RotateObject())       : null;
        moveUC  = settings.smoveU      ? StartCoroutine(MoveObjectUp())       : null;
        pulseC  = settings.spulse       ? StartCoroutine(PulseObject())        : null;
        moveFC  = settings.smoveF       ? StartCoroutine(MoveObjectForward())  : null;
        pulseFC = settings.spulseF      ? StartCoroutine(PulseObjectForward()) : null;

        if (settings.eulerRotate)
        {
            settings.eulerRotate = false;
            eulerRC = StartCoroutine(RotateToEuler());
        }
    }

    public void ChangeSettings(bool rot, bool mu, bool pulse, bool mf, bool eul = false)
    {
        settings.srotate = rot;
        settings.smoveU = mu;
        settings.spulse = pulse;
        settings.smoveF = mf;
        settings.eulerRotate = eul;
        ApplySettings();
    }

    IEnumerator RotateObject()
    {
        while (true)
        {
            transform.Rotate(0f, settings.rotateSpd * Time.deltaTime, 0f);
            yield return null;
        }
    }

    IEnumerator MoveObjectUp()
    {
        while (true)
        {
            transform.Translate(0f, settings.moveUSpd * Time.deltaTime, 0f);
            yield return null;
        }
    }

    IEnumerator MoveObjectForward()
    {
        while (true)
        {
            transform.position += transform.forward * Time.deltaTime * settings.moveFSpd;
            yield return null;
        }
    }

    IEnumerator PulseObject()
    {
        float t = settings.pulseTimer;
        int dir = 1;

        while (true)
        {
            transform.Translate(0, dir * settings.pulseSpd * Time.deltaTime, 0);
            t -= Time.deltaTime;

            if (t <= 0)
            {
                t = settings.pulseTimer;
                dir *= -1;
            }
            yield return null;
        }
    }

    IEnumerator PulseObjectForward()
    {
        float t = settings.pulseFTimer;

        while (true)
        {
            transform.position += transform.forward * settings.pulseFSpd * Time.deltaTime;
            t -= Time.deltaTime;

            if (t <= 0)
            {
                t = settings.pulseFTimer;
                transform.forward = -transform.forward;
            }
            yield return null;
        }
    }

    IEnumerator RotateToEuler()
    {
        float rotated = 0;

        while (rotated < settings.targetEuler)
        {
            float step = settings.eulerRotateSpd * Time.deltaTime;
            rotated += step;

            if (rotated > settings.targetEuler)
                step -= (rotated - settings.targetEuler);

            transform.Rotate(0, step, 0);
            yield return null;
        }
    }
}
