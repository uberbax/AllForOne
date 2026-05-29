using System.Collections.Generic;
using UnityEngine;

public class ExtraAll : MonoBehaviour
{
    public bool isSpawned = false;
    public List<string> spawns = new List<string>();
    
    public bool isTriggered = false;
    
    public List<Transform> otherPositions = new List<Transform>();
    public Transform posBehind;
    
    public void TriggerAttack()
    {
        if (!isTriggered)
        {
            var obj = GetComponent<ObjHolder>().obj;
            obj.AddMeta("CUR_BATTLE");
        }
        isTriggered = true;
        
        if (!isSpawned)
        {
            isSpawned = true;
            
            for (int i = 0; i < spawns.Count; i++)
            {
                var yu = spawns[i].Split(',');
                var spwned = WaveSpawner.instance.DoSpawnAny(new List<Bon>
                {
                    new Bon
                    {
                        Key = yu[0],
                        Value = 1,
                        Val3 = int.Parse(yu[1])
                    }
                }, "enemy", otherPositions[i], otherPositions[i], false, Vector3.zero, Vector3.zero,
                    overridesViz:new List<(string, string)>{ ("hp","") });

                spwned[0].main.transform.localScale = new Vector3(-1, 1, 1);
                spwned[0].META_TAGS.Add("CUR_BATTLE");
            }
        }

    }
}
