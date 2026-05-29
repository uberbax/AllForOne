using UnityEngine;

public class MatChanger : MonoBehaviour
{
    public Material mat;
    public Material unlitTpMat;
    
    
    [ContextMenu("Change Mats")]
    public void ChangeMats()
    {
        Change(transform);
    }

    public void Change(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var a =  root.GetChild(i).GetComponent<SpriteRenderer>();
            if (a != null)
                a.material = mat;
            
            var b = root.GetChild(i).GetComponent<SkinnedMeshRenderer>();
            if (b != null)
            {
                var mt = b.material.mainTexture;
                b.material = unlitTpMat;
                b.material.mainTexture = mt;
            }

            Change(root.GetChild(i));
        }
    }


}
