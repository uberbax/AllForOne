using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultAnim : MonoBehaviour
{
    // Start is called before the first frame update
    public string anim = "";
    void Start()
    {
        Do();
    }

    public void Do()
    {
        if (anim != "")
        {
            GetComponent<Animator>().CrossFade(anim, 0.1f);
        }        
    }
    
}
