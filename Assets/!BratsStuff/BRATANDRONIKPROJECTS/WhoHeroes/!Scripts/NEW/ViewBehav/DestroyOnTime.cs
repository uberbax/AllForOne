using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempObj : MonoBehaviour
{
    public float lifetimer = 0.5f;
    public bool disable = true;
    private bool done=false; 
    private float tm =0;

    // Start is called before the first frame update
    void Start()
    {
        tm = lifetimer;
    }

    // Update is called once per frame
    void Update()
    {
        if(done)
            return;

        tm -= Time.deltaTime;

        if (tm < 0)
        {
            done = true;

            if(!disable)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        done = false;
        tm = lifetimer;
    }
}
