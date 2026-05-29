using UnityEngine;

public class UnityEnemy : MonoBehaviour, ImpactListener, LightSourceListener
{
    public Transform root;
    public GameObject corpse;
    public GameObject stunVisual;

    public GameObject hitVFX;
    
    public Transform dashPrepLeg;
    public ParticleSystem dashParticlesPrep;
    public ParticleSystem dashParticlesLaunch;
    public ParticleSystem dashParticlesTravel;
    
    public float moveSpeed = 2f;
    public float wanderSpeed = 0.5f;
    public int hp;
    public float cloakAmplitude = 0.1f;
    public float cloakFrequency = 1f;
    public float stunTimer;

    public float chargeRange = 2f;
    public float chargeTime = 2f;
    public float chargeSpeed = 5f;
    public float chargeDuration = 0.5f;
    public float chargeRestDuration = 1f;

    private float time;
    private float wanderTime;
    private Vector3 wanderDirection;
    private float chargeTimer = 0;
    private float chargeRestTimer = 0;
    private float inChargeTimer = 0f;
    private bool isCharging = false;
    private Vector3 chargeDirection;

    enum State
    {
        Wandering,
        Aggro,
        Charging,
        ChargingRest,
        Scared
    }

    private State currentState = State.Wandering;
    
    float chargeStartCd;

    void Start()
    {
        wanderTime = 0;
    }

    void FixedUpdate()
    {
        time += Time.fixedDeltaTime;

        stunVisual.SetActive(stunTimer > 0);

        chargeStartCd -= Time.fixedDeltaTime;
        
        if (stunTimer > 0)
        {
            stunTimer -= Time.fixedDeltaTime;
            return;
        }

        float xscale = 1 + cloakAmplitude * Mathf.Sin(time * cloakFrequency) * 2f;
        float yscale = 1 + cloakAmplitude * Mathf.Sin(time * cloakFrequency);

        root.localScale = new Vector3(xscale, yscale, 1f);

        if (isCharging)
        {
            if (!dashParticlesTravel.isPlaying)
                dashParticlesTravel.Play();
        }
        else
        {
            if (dashParticlesTravel.isPlaying)
                dashParticlesTravel.Stop();
        }
        
        switch (currentState)
        {
            case State.Wandering:
                if (wanderTime <= 0)
                {
                    // Pick a new random direction or stop
                    if (Random.value < 0.3f)
                    {
                        // Stop
                        wanderDirection = Vector3.zero;
                        wanderTime = Random.Range(1f, 3f); // Stop for 1 to 3 seconds
                    }
                    else
                    {
                        // Move
                        wanderDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;
                        wanderTime = Random.Range(2f, 5f); // Move in this direction for 2 to 5 seconds
                    }
                }

                wanderTime -= Time.fixedDeltaTime;
                transform.position += wanderDirection * Time.fixedDeltaTime * wanderSpeed;

                break;

            case State.Aggro:
                float distanceToPlayer = Vector2.Distance(transform.position, UnityCharacter.playerPosition);
                if (distanceToPlayer <= chargeRange)
                {
                    if (chargeStartCd < 0)
                    {
                        chargeStartCd = 0.2f;
                        SFX.i.EnemyChargeStart();
                        Debug.Log("charge start...");
                    }
                    
                    currentState = State.Charging;
                    chargeTimer = chargeTime;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        UnityCharacter.playerPosition,
                        Time.fixedDeltaTime * moveSpeed
                    );
                }
                break;

            case State.Scared:
                float distanceToPlayerToDie = Vector2.Distance(transform.position, UnityCharacter.playerPosition);
                if (distanceToPlayerToDie > 28f)
                {
                    Kill();
                }

                var awayFromPlayer = -(UnityCharacter.playerPosition - transform.position).normalized;
                transform.position += awayFromPlayer * (Time.fixedDeltaTime * moveSpeed);
                
                if (!dashParticlesTravel.isPlaying)
                    dashParticlesTravel.Play();
                break;
            
            case State.Charging:
                if (isCharging)
                {
                    if (dashParticlesPrep.isPlaying)
                        dashParticlesPrep.Stop();
                    
                    if (inChargeTimer <= 0)
                    {
                        isCharging = false;
                        chargeRestTimer = chargeRestDuration;
                        currentState = State.ChargingRest;
                    }
                    else
                    {
                        inChargeTimer -= Time.fixedDeltaTime;
                    }

                    if (Vector2.Distance(transform.position, UnityCharacter.playerPosition) < 0.25f)
                    {
                        UnityCharacter.player.Kill();
                    }
                    
                    transform.position += chargeDirection * Time.fixedDeltaTime * chargeSpeed;
                }
                else if (chargeTimer > 0)
                {
                    if (!dashParticlesPrep.isPlaying)
                        dashParticlesPrep.Play();
                    
                    chargeDirection = (UnityCharacter.playerPosition - transform.position).normalized;
                    dashPrepLeg.transform.right = chargeDirection;
                    
                    chargeTimer -= Time.fixedDeltaTime;
                }
                else
                {
                    inChargeTimer = chargeDuration;
                    isCharging = true;
                    
                    SFX.i.EnemyChargeLaunch();
                    dashParticlesLaunch.Play();
                }

                break;
            
            case State.ChargingRest:
                stunTimer = chargeRestDuration;
                // if (chargeRestTimer > 0)
                //     chargeRestTimer -= Time.deltaTime;
                // else
                    currentState = State.Aggro;
                break;
        }
    }

    public void OnHitByArrow(GameObject arrow)
    {
        hp--;

        SFX.i.HitEnemy();
        
        Instantiate(hitVFX, arrow.transform.position, Quaternion.identity, transform.parent);
        
        if (hp < 0)
            Kill();
        else
            arrow.transform.parent = transform;
    }

    public void Kill()
    {
        Instantiate(corpse, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void OnLit(GameObject source, bool canStun)
    {
        if (isCharging)
            return;

        if (currentState == State.Scared)
            return;
        
        if (currentState == State.Charging)
            return;
        
        if (currentState == State.ChargingRest)
            return;
        
        Aggro();
        
        return;

        if (canStun)
        {
            if (dashParticlesPrep.isPlaying)
                dashParticlesPrep.Stop();
            if (dashParticlesTravel.isPlaying)
                dashParticlesTravel.Stop();
            
            isCharging = false;
            inChargeTimer = 0;
            
            stunTimer = .25f;
        }
    }

    public void Aggro()
    {
        currentState = State.Aggro;
    }

    public void OnUnLit(GameObject source, bool canStun)
    {
        
    }

    public void ScareAway()
    {
        Debug.Log(name + " is scared");
        currentState = State.Scared;
    }
}
