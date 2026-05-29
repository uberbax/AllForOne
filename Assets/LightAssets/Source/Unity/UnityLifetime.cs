using UnityEngine;

public class UnityLifetime : MonoBehaviour
{
    public float ttl = 1f;
    public GameObject replaceAfterDeath;

    void Update()
    {
        ttl -= Time.deltaTime;
        if (ttl < 0)
        {
            if (replaceAfterDeath != null)
                Instantiate(replaceAfterDeath, transform.position, transform.rotation);

            var lightEmitters = GetComponentsInChildren<UnityLightEmitter>();
            foreach (var le in lightEmitters)
                le.AboutToDie();

            var ile = GetComponent<UnityLightEmitter>();
            if (ile != null)
            {
                ile.AboutToDie();
                Destroy(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}