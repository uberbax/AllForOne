using UnityEngine;
using Random = UnityEngine.Random;

interface ImpactListener
{
    public void OnHitByArrow(GameObject arrow);
}

public class UnityProjectile : MonoBehaviour
{
    public GameObject glow;
    public GameObject flash;

    public Vector2 direction;

    public float speed = 1f;
    public float cooldown = 1f;
    public float flyCooldown = 5f;
    public float glowCooldown = 0;

    public bool isSticking;
    
    public bool isNonDamaging;
    Vector2 ricochetOrigin;

    public GameObject enableonpickup;
    
    void Awake()
    {
        FixedUpdate();
    }

    public void Shoot(Vector2 vector2)
    {
        direction = vector2;
        ricochetOrigin = transform.position;
    }
    
    public void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime;

        cooldown -= dt;
        flyCooldown -= dt;
        glowCooldown -= dt;

        if (isNonDamaging)
        {
            transform.Rotate(0, 0, Time.fixedDeltaTime * 900f);
            GetComponent<Collider2D>().enabled = false;
        }
        else
        {
            GetComponent<Collider2D>().enabled = true;
        }

        if (glowCooldown < 0)
        {
            var instantiate = Instantiate(glow, transform.position, Quaternion.identity);
            instantiate.AddComponent<UnityLifetime>().ttl = 1f;

            if (isSticking)
                glowCooldown = 0.2f;
            else
                glowCooldown = 0.02f;
        }

        if (!isSticking)
        {
            if (Physics2D.Raycast(transform.position, direction, 2f, LayerMask.GetMask("Enemy")))
            {
                gameObject.layer = LayerMask.NameToLayer("EnemyOnlyArrow");
                Debug.Log("enemy only arrow!");
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer("Default");
            }
            
            transform.position += (Vector3)direction * (speed * dt);

            if (!isNonDamaging)
                transform.right = direction.normalized;

            if (ricochetOrigin != Vector2.zero)
            {
                // sticking timeout logic
                Vector2 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
                var ricochet = (Vector2.Distance(transform.position, ricochetOrigin) > 3f) && isNonDamaging;
                var bigDistance = Vector2.Distance(transform.position, ricochetOrigin) > 4f;
                var graceZone = 60;
                if (screenPosition.x < graceZone || screenPosition.x > Screen.width - graceZone ||
                    screenPosition.y < graceZone || screenPosition.y > Screen.height - graceZone || ricochet || bigDistance)
                {
                    Stick(quiet: false);
                }
            }

            if (flyCooldown < 0)
            {
                Pickup();
            }
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (isSticking)
            return;
        if (isNonDamaging)
            return;
        
        Instantiate(flash, transform.position, Quaternion.identity, transform.root);
        
        Stick();
        
        other.gameObject.GetComponent<ImpactListener>()?.OnHitByArrow(gameObject);
    }

    public void Stick(bool quiet = true)
    {
        if (isSticking)
            return;
        
        if (cooldown > 0.1f)
            cooldown = 0.1f;
        isSticking = true;
        isNonDamaging = false;

        if (Physics2D.OverlapPoint(transform.position, LayerMask.GetMask("WalkingObstacle")))
        {
            SendBack();
        }

        if (!quiet)
            SFX.i.ArrowStuck();
    }

    public void Pickup(bool quiet = false)
    {
        Destroy(gameObject);
        UnityCharacter.player.OnPickupArrow(transform.position);

        if (enableonpickup != null)
            enableonpickup.SetActive(true);

        if (!quiet)
            SFX.i.ArrowPickup();
    }

    public bool CanPickup()
    {
        return cooldown < 0;
    }

    public void SendBack()
    {
        isNonDamaging = true;
        isSticking = false;
        direction = Random.insideUnitCircle.normalized;
        ricochetOrigin = transform.position;
    }
}