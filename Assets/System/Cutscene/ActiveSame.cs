using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ActiveSame : MonoBehaviour
{
    public GameObject main;
    
    public List<GameObject> same = new List<GameObject>();
    public List<GameObject> notSame = new List<GameObject>();
    
    void Update()
    {
        foreach (GameObject go in same) go.SetActive(main.activeSelf);   
        foreach (GameObject go in notSame) go.SetActive(!main.activeSelf);   
    }
}
