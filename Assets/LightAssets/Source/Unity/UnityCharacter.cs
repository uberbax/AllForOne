using System;
using DG.Tweening;
using Features;
using UnityEngine;
using Util;

public struct CharacterInput
{
    public float horizontal;
    public float vertical;

    public bool shootDown;
    public bool shootUp;
    public Vector2 direction;
    public bool dash;

    public Vector2 GetAxis()
    {
        return new Vector2(horizontal, vertical);
    }
}

public class UnityCharacter : MonoBehaviour, LightSourceListener
{
    public static Vector3 playerPosition;
    public static UnityCharacter player;

    public GameObject pickupArrowVFX;

    public Transform rollRoot;
    public Transform sprite;
    public Transform arrowSpawn;

    public GameObject dashGlow;

    public SpriteRenderer normal;
    public SpriteRenderer roll;

    public Transform bow;
    public Transform bowContanier;
    public GameObject bowRelaxed;
    public GameObject bowFixed;
    
    public GameObject arrowIndicator;

    public float movespeed = 3f;
    public float rollspeed = 3f;

    public float bounceAmplitude = 0.05f;
    public float bounceFrequency = 10f;

    public bool isDead;

    public float rollCooldown;
    public float maxRollTime;
    public float rollInvulTime;
    public bool isRolling;
    public bool isInputDisabled;

    public float lightDeathTime = 3f;

    public float delay = 0.2f;

    public float colradius = 0.15f;

    Vector2 rollDirection;

    float rollTime;
    float rollCDTime;

    public UnityProjectile shotArrow;

    CharacterInput _input;

    float time;

    float holdTime;

    int facing = 1;
    public int lightSources;

    float deathTimer;
    float invultime;
    float deathByDarkK;

    float walkstepCD = 0f;

    void Awake()
    {
        player = this;
    }

    void Update()
    {
        var shaking = Vector3.zero;
        if (bowFixed.activeSelf)
            shaking = UnityEngine.Random.insideUnitCircle * 0.02f;
        bowContanier.position = normal.transform.position + shaking;
        
        delay -= Time.deltaTime;
        if (delay > 0)
            return;

        if (isDead)
        {
            bowContanier.gameObject.SetActive(false);
            return;
        }
        

        if (isInputDisabled)
            return;

        var inputAxis = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        _input = new CharacterInput()
        {
            horizontal = inputAxis.x,
            vertical = inputAxis.y,
            shootDown = Input.GetMouseButton(0),
            shootUp = Input.GetMouseButtonUp(0),
            dash = Input.GetButtonDown("Jump"),
            direction = GetAimDirection()
        };

        SetInput();
    }

    void SetInput()
    {
        var isActiveBowFixed = _input.shootDown && shotArrow == null;
        if (!bowFixed.activeSelf && isActiveBowFixed)
        {
            SFX.i.BowThread();
        }
        bowFixed.SetActive(isActiveBowFixed);
        bowRelaxed.SetActive(!bowFixed.activeSelf);

        if (_input.shootDown)
            holdTime += Time.deltaTime;
        
        if (_input.shootUp && shotArrow == null)
        {
            var aimDirection = GetAimDirection();

            if (aimDirection.x > 0)
                facing = 1;
            if (aimDirection.x < 0)
                facing = -1;

            bowContanier.DOPunchScale(Vector3.one * 0.25f, 0.4f, 15, 0.25f);
            
            SpawnProjectile(aimDirection);
        }

        if (_input.GetAxis() != Vector2.zero)
        {
            if (_input.dash && !isRolling && rollCDTime < 0)
            {
                SFX.i.Roll();
                
                isRolling = true;
                invultime = rollInvulTime;
                rollTime = maxRollTime;
                rollCDTime = rollCooldown;
                rollDirection = _input.GetAxis();
            }
        }
    }

    Vector3 GetAimDirection()
    {
        var worldPos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return (worldPos - (Vector2)transform.position).normalized;
    }

    void OnDrawGizmos()
    {
        if (Camera.main == null) return;

        var worldpoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldpoint.z = 0;
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(worldpoint, 0.01f);
    }

    void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime;
        
        arrowIndicator.SetActive(shotArrow == null && !isRolling);
        bowContanier.gameObject.SetActive(!isRolling);
        
        bow.right = GetAimDirection();
        
        playerPosition = transform.position;

        if (invultime > 0)
            invultime -= dt;

        if (lightSources <= 0)
        {
            deathTimer += dt;

            if (deathTimer > lightDeathTime)
            {
                Kill();
            }
        }
        else if (deathTimer > 0)
        {
            deathTimer -= dt;
        }
        deathByDarkK = Mathf.Clamp01(deathTimer / lightDeathTime);

        if (isRolling)
        {
            rollTime -= dt;

            if (rollTime < 0)
                isRolling = false;

            normal.gameObject.SetActive(false);
            roll.gameObject.SetActive(true);

            roll.transform.rotation = Quaternion.Euler(0, 0, -Time.time * 1999f * Mathf.Sign(rollDirection.x));

            var moveVector = rollDirection * (rollspeed * dt);
            MoveBy(moveVector.x, moveVector.y);
        }
        else
        {
            rollCDTime -= dt;
            roll.transform.rotation = Quaternion.Euler(0, 0, 0);

            normal.gameObject.SetActive(true);
            roll.gameObject.SetActive(false);

            var moveVector = _input.GetAxis() * (movespeed * dt);
            MoveBy(moveVector.x, moveVector.y);
        }

        time += dt;

        if (isRolling)
        {
            if (rollDirection.x > 0)
                facing = 1;
            if (rollDirection.x < 0)
                facing = -1;
        }
        else
        {
            if (GetAimDirection().x > 0)
                facing = 1;
            if (GetAimDirection().x < 0)
                facing = -1;
        }

        if (shotArrow != null && shotArrow.CanPickup())
        {
            // collect phase
            const float pickupRadius = 0.5f;
            if (Vector2.Distance(shotArrow.transform.position, transform.position) < pickupRadius)
            {
                shotArrow.Pickup();
            }
        }

        Vector3 walkbounce = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 20f) * 0.05f), 0);
        if (_input.GetAxis().magnitude > 0)
        {
            sprite.localPosition = walkbounce;
            sprite.localScale = new Vector3(facing, 1, 1);

            walkstepCD -= Time.deltaTime;

            if (!isRolling)
            {
                if (walkstepCD < 0)
                {
                    SFX.i.WalkStep();
                    walkstepCD = 0.25f;
                }
            }
        }
        else
        {
            if (!isDead)
            {
                // breathing effect
                float bounce = 1 + Mathf.Sin(time * bounceFrequency) * bounceAmplitude;
                sprite.localScale = new Vector3(facing, bounce, 1f);
                sprite.localPosition = Vector3.zero;
            }
        }

        if (isDead)
        {
            arrowIndicator.SetActive(false);
            dashGlow.SetActive(false);
            sprite.rotation = Quaternion.Euler(0, 0, 90);
        }
        else
        {
            dashGlow.SetActive(rollCDTime < 0);
        }
    }

    public void MoveBy(float dx, float dy)
    {
        var delta = new Vector3(dx, dy);
        transform.position += delta;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, colradius);
    }

    public void Kill()
    {
        if (isDead)
            return;

        if (invultime > 0)
            return;

        _input = new CharacterInput();
        isDead = true;
        isRolling = false;
        rollTime = -1;

        SFX.i.Death();
    }

    void SpawnProjectile(Vector2 arg0)
    {
        shotArrow = UnityUtil.QuickInstantiate<UnityProjectile>("entity/arrow", arrowSpawn.position);
        shotArrow.speed *= (1f + Mathf.Clamp01(holdTime));
        shotArrow.Shoot(arg0);

        holdTime = 0;

        SFX.i.BowShoot();
    }

    public void OnLit(GameObject source, bool isStrong)
    {
        if (isStrong)
        {
            deathTimer = 0;
            lightSources++;
        }
    }

    public void OnUnLit(GameObject source, bool isStrong)
    {
        if (isStrong)
            lightSources--;
    }

    public float GetDeathByDarknessK()
    {
        return deathByDarkK;
    }

    public void DisableInput()
    {
        _input = new CharacterInput();
        isInputDisabled = true;
    }

    public void EnableInput()
    {
        _input = new CharacterInput();
        isInputDisabled = false;
    }

    public void PickupArrowIfAny()
    {
        if (shotArrow != null)
            shotArrow.Pickup();
    }

    public void OnPickupArrow(Vector2 pos)
    {
        Instantiate(pickupArrowVFX, pos, Quaternion.identity);
    }
}