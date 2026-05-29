using System.Collections.Generic;
using UnityEngine;

public class AreaSpawner : MonoBehaviour
{
    public List<GameObject> Objects;
    public List<Sprite> Sprites;
    
    public Transform lo;
    public Transform hi;
    public int num = 100;
    
    [ContextMenu("SpawnArea")]
    public void SpawnArea()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i) == lo ||  transform.GetChild(i) == hi) continue;
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (Objects.Count > 0)
        {
            for (int i = 0; i < num; i++)
            {
                var r = Random.Range(0, Objects.Count);
                var pos = new Vector3(Random.Range(lo.position.x, hi.position.x), Random.Range(lo.position.y, hi.position.y), lo.position.z);
                var b = Instantiate(Objects[r], transform);
                b.transform.position = pos;
            }
        }
        else if (Sprites.Count > 0)
        {
            for (int i = 0; i < num; i++)
            {
                var r = Random.Range(0, Sprites.Count);
                var pos = new Vector3(Random.Range(lo.position.x, hi.position.x), Random.Range(lo.position.y, hi.position.y), lo.position.z);
                var b = new GameObject(Sprites[r].name);
                var b1 = b.AddComponent<SpriteRenderer>();
                b1.sprite = Sprites[r];
                b.transform.parent = transform;
                
                b.transform.position = pos;
            }
        }
        
    }
}
