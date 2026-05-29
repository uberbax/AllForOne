using UnityEngine;

public class Follow : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform who;

    // Update is called once per frame
    void Update()
    {
        transform.position = who.position;
    }
}
