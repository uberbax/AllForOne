using System.Collections.Generic;
using UnityEngine;

public class ComponentUIBehavior : MonoBehaviour
{

    void OnEnable()
    {
        FunctionTimer.Create(() => Fill(), 0.02f);
    }
    public virtual void Fill()
    {
        
    }
}
