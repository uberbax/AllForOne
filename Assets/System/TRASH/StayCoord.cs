using System;
using UnityEngine;

public class StayCoord : MonoBehaviour
{
    public bool y;
    public bool x;
    public bool z;

    private Vector3 savedPos = Vector3.zero;
    
    private void Start()
    {
        if (savedPos == Vector3.zero)
            savedPos = transform.position;
    }

    public void SetPos(Vector3 pos)
    {
        savedPos = pos;
    }

    private void Update()
    {
        if (y && transform.position.y < savedPos.y)
        {
            transform.position = new Vector3(transform.position.x, savedPos.y, transform.position.z);
        }
    }
}
