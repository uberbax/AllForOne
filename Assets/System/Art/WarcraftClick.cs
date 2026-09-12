using System.Collections.Generic;
using UnityEngine;

public class WarcraftClick : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<Transform> arrows = new List<Transform>();
    private float t = 1;
    void Start()
    {
        Debug.Log(transform.position);
        Destroy(gameObject, 0.3f);
        
        for (int i = 0; i < arrows.Count; i++)
            arrows[i].gameObject.SetActive(true);
    }
    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < arrows.Count; i++)
        {
            t -= Time.deltaTime;
            arrows[i].position += arrows[i].up * Time.deltaTime;
            arrows[i].transform.localScale -= new Vector3(Time.deltaTime, Time.deltaTime, Time.deltaTime);
            arrows[i].GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, t);
        }
    }
}
