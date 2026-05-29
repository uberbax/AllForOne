using UnityEngine;

public class UnityTree : MonoBehaviour, ImpactListener
{
    public GameObject burning;

    public void OnHitByArrow(GameObject arrow)
    {
        Instantiate(burning, transform.position, Quaternion.identity);
        Destroy(gameObject);
        
        SFX.i.HitTree();
    }
}