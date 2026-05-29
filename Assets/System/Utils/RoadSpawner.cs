using System.Collections.Generic;
using Dreamteck.Splines;
using UnityEngine;

public class RoadSpawner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform splineRoot;
    public List<GameObject> singleRoad;

    
    [ContextMenu("CreateRoads")]
    public void CreateRoads()
    {
        for (int i = transform.childCount-1; i>=0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }


        var p = splineRoot.GetComponentsInChildren<SplineComputer>();
        for (int l = 0; l < p.Length; l++)
        {
            for (float i = 0; i < 1.0f; i += 0.05f)
            {
                var nn = p[l].Evaluate(i);
                var hh = Instantiate(singleRoad[Random.Range(0, singleRoad.Count)], transform);
                hh.transform.position = nn.position + new Vector3(0, 0, -0.1f);
                //hh.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            }
        }

    }
    
}
