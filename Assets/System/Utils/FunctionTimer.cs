using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class FunctionTimer : MonoBehaviour
{
    public static FunctionTimer instance;

    private void Awake()
    {
        instance = this;
    }
    
    private void OnDestroy()
    {
        instance = null;
    }

    public static void Create(Action act, float timer)
    {
        /*
        MethodBase caller = new StackTrace().GetFrame(1).GetMethod();
        string callerMethodName = caller.Name;
        string calledMethodName = MethodBase.GetCurrentMethod().Name;
  
        Console.WriteLine("The caller method is: " + callerMethodName);
        */
        if (timer <= 0)
        {
            if (act != null) act();
        }
        else
            instance.StartCoroutine(instance.CreateP(act, timer, /*callerMethodName*/ "go"));
    }

    public static void Create(Action act, float timer, Func<bool> check)
    {
        Debug.Log("Created from ???");
        /*
        MethodBase caller = new StackTrace().GetFrame(1).GetMethod();
        string callerMethodName = caller.Name;
        string calledMethodName = MethodBase.GetCurrentMethod().Name;
  
        Console.WriteLine("The caller method is: " + callerMethodName);
        */
        if (timer <= 0 && check())
        {
            if (act != null) act();
        }
        else
            instance.StartCoroutine(instance.CreateP(act, timer, /*callerMethodName*/ "go", check));
    }
    public IEnumerator CreateP(Action act, float timer, string from)
    {
        yield return new WaitForSeconds(timer);

        if (act != null) act();
    }
    
    public IEnumerator CreateP(Action act, float timer, string from, Func<bool> check)
    {
        
        yield return new WaitForSeconds(timer);

        while (!check())
        {
            yield return null;
        }
        
        Debug.Log("CreateP " + from);
        
        if (act != null) act();
    }
}
