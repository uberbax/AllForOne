using UnityEngine;

public class UnityBossPart : MonoBehaviour, ImpactListener
{
    public GameObject onDeath;
    public GameObject vfx;

    public void OnHitByArrow(GameObject arrow)
    {
        Kill();

        arrow.GetComponent<UnityProjectile>().SendBack();
    }

    public void Kill()
    {
        SFX.i.HitEyes();

        if (onDeath != null)
            Instantiate(onDeath, transform.position, Quaternion.identity, transform.parent);
        if (vfx != null)
            Instantiate(vfx, transform.position, Quaternion.identity, transform.parent);
        Destroy(gameObject);
    }
}