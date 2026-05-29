using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Deck : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform mid;

    private float dstY = 0.5f;
    public float radius = 2f;
    // Update is called once per frame
    public void SortChildren()
    {
        // 1. Get all the child transforms
        int childCount = transform.childCount;
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < childCount; i++)
        {
            children.Add(transform.GetChild(i));
        }

        // 2. Sort the list of children using LINQ OrderBy
        // You can use transform.position.x for global position or transform.localPosition.x for local position
        var sortedChildren = children.OrderBy(child => child.localPosition.x).ToList();

        // 3. Update the sibling indices in the hierarchy
        for (int i = 0; i < sortedChildren.Count; i++)
        {
            // SetSiblingIndex changes the order in the Hierarchy and the rendering order for UI/Sprites
            sortedChildren[i].SetSiblingIndex(i);
        }
    }
    
    [ContextMenu("DoUpdate")]
    public void DoUpdate()
    {
        float lo = 0;
        SortChildren();
        
        List<float> boms = new List<float>();
        float cur = 0;
        
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name.IndexOf("mid") >= 0 || child.name.IndexOf("cancel") >= 0) continue;

            var dlt = child.position - mid.position;
            float add = radius;
            boms.Add(cur);
            if (dlt.y > dstY)
            {
                add = 0;
            }
            else if (dlt.y <= 0)
            {
                //nothing
            }
            else
            {
                add = dstY - dlt.y;
            }

            cur += add;
        }
        //ok
        boms.Add(cur);
        float dd = cur / 2 - radius/2;
        int l = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name.IndexOf("mid") >= 0 || child.name.IndexOf("cancel") >= 0) continue;
            if (child.name.IndexOf("_drag") < 0)
            {
                var hh = child.position;
                //child.position = mid.position + new Vector3(boms[l] - dd, 0, 0);
                child.position = new Vector3(mid.position.x + boms[l] - dd, hh.y, hh.z);
            }
            l++;
        }
    }
}
