using System;
using UnityEngine;

public class BgScroller : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float speed = 1;
    public Transform loPoint;
    public Transform backs;

    private bool isStopped = false;

    private void Awake()
    {
        EventManager.SUB("movement_event", MovementEvent);
    }

    private void MovementEvent(ArgPass obj)
    {
        if (obj.num == 0)
        {
            isStopped = true;
        }
        else
        {
            isStopped = false;
        }
    }

    void Start()
    {
        if (backs == null)
            backs = transform;
        //set backs
        for (int i = 1; i < backs.childCount; i++)
        {
            var ps = backs.GetChild(i - 1);

            backs.GetChild(i).transform.position = ps.position +  new Vector3( ps.GetComponent<SpriteRenderer>().bounds.size.x,0, 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isStopped) return;
        
        for (int i = 0; i < backs.childCount; i++)
        {
            backs.GetChild(i).position += new Vector3(speed * Time.deltaTime,0,0);
        }

        if (backs.GetChild(0).position.x + backs.GetChild(0).GetComponent<SpriteRenderer>().bounds.size.x/2 <
            loPoint.position.x)
        {
            backs.GetChild(0).SetAsLastSibling();
            Start();
        }
    }
}
