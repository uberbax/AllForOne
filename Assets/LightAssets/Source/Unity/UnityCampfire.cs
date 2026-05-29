using System.Collections.Generic;
using Unity.Cinemachine;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class UnityCampfire : MonoBehaviour, ImpactListener
{
    public Transform playerTarget;
    public UnityLightEmitter lightEmitter;
    public bool isLit;

    public GameObject litBlow;
    
    public GameObject nextStage;
    
    public UnityBoss boss;
    public List<Transform> lightPos;
    public GameObject bossLighter;

    public List<GameObject> enableAfterDefend;
    
    public List<GameObject> defenders;

    public int defenderIndex;
    public int waveIndex;
    public int waves;
    public float waveCooldown = 2f;
    
    public float defenderCooldown = 1f;
    public float defenderRange = 3f;
    public float defenderExpellRange = 6f;

    public float activationRange = 1f;

    public int index;

    float cooldown = 0.25f;

    float defenceWait;
    bool isDefending;
    List<GameObject> livingDefenders = new List<GameObject>();
    Collider2D col2d;

    void Awake()
    {
        col2d = gameObject.GetComponent<Collider2D>();
    }

    void Update()
    {
        if (UnityCharacter.playerPosition.x > transform.position.x)
        {
            TryLoadNextStage();
        }
        
        lightEmitter.gameObject.SetActive(isLit);
        col2d.enabled = !isLit;
        
        if (!UnityCharacter.player) return;

        if (!isLit)
        {
            if (Vector2.Distance(UnityCharacter.playerPosition, transform.position) < activationRange)
            {
                SFX.i.Bonfire();
                Light(isTriggeredByPlayer: true);
            }
            return;
        }

        cooldown -= Time.deltaTime;

        if (cooldown > 0)
            return;

        DefendeLogic();

        // var playerCollider = UnityCharacter.player.GetComponent<Collider2D>();
        // if (!lightEmitter.illuminated.Contains(playerCollider))
        // {
        //     Destroy(gameObject);
        // }
    }

    void TryLoadNextStage()
    {
        if (nextStage != null)
        {
            nextStage.SetActive(true);
            nextStage = null;
        }
    }

    void DefendeLogic()
    {
        if (defenceWait > 0)
        {
            defenceWait -= Time.fixedDeltaTime;

            if (defenceWait <= 0)
            {
                UnityUIAnnouncement.i.Show("WAVE " + (1 + waveIndex) + "/" + waves, 1.25f);
            }
            
            return;
        }
        
        
        if (isDefending)
        {
            if (defenderIndex < defenders.Count)
            {
                cooldown = defenderCooldown;

                var defender = defenders[defenderIndex];
                var liveDefender = Instantiate(defender, GetDefenderPos(), Quaternion.identity);

                livingDefenders.Add(liveDefender);
                
                defenderIndex++;
            }
            else
            {
                foreach (var ld in livingDefenders)
                {
                    if (ld != null)
                        return;
                }

                foreach (var ill in lightEmitter.illuminated)
                {
                    if (ill != null)
                    {
                        var unityEnemy = ill.GetComponent<UnityEnemy>();
                        if (unityEnemy)
                            return;
                    }
                }

                if (waveIndex < (waves - 1))
                {
                    waveIndex++;
                    defenderIndex = 0;
                    defenceWait = 1f;
                    return;
                }

                var scaredEnemies = Physics2D.OverlapCircleAll(transform.position, defenderExpellRange, LayerMask.GetMask("Enemy"));

                foreach (var se in scaredEnemies)
                {
                    var unityEnemy = se.GetComponent<UnityEnemy>();
                    if (unityEnemy != null)
                        unityEnemy.ScareAway();
                }
                
                isDefending = false;
                UnityShell.main.data.isDefended = true;
                UnityShell.main.Save();

                var cccamera = FindObjectOfType<CinemachineVirtualCamera>();
                cccamera.Follow = UnityCharacter.player.transform;

                AfterDefendedCutscene();
                
                UnityUIAnnouncement.i.Show("THE BONFIRE IS CLEANSED!");
            }
        }
    }

    void AfterDefendedCutscene()
    {
        var seq = DOTween.Sequence();

        for (var i = enableAfterDefend.Count - 1; i >= 0; i--)
        {
            var e = enableAfterDefend[i];
            seq.AppendCallback(() => e.SetActive(true));
            seq.AppendInterval(0.5f);
        }
    }

    Vector3 GetDefenderPos()
    {
        return transform.position + (Vector3) Random.insideUnitCircle * defenderRange;
    }

    public void Light(bool isTriggeredByPlayer = false)
    {
        if (isLit)
            return;
        
        isLit = true;

        Instantiate(litBlow, transform.position, Quaternion.identity);
        
        if (defenders.Count > 0)
        {
            isDefending = isTriggeredByPlayer;

            if (isTriggeredByPlayer)
            {
                defenceWait = 3f;
                
                var cccamera = FindObjectOfType<CinemachineVirtualCamera>();
                cccamera.Follow = transform;
                
                UnityUIAnnouncement.i.Show("DEFEND THE BONFIRE!");
                UnityShell.main.data.isDefended = false;
            }
        }
        else
        {
            isDefending = false;
        }

        if (isTriggeredByPlayer)
        {
            if (boss != null)
            {
                BossCutscene();
            }
        }
        else
        {
            if (boss != null)
                if (boss.bossmusic != null)
                    boss.bossmusic.SetActive(true);
        }

        if (!isDefending)
            AfterDefendedCutscene();

        TryLoadNextStage();
        
        gameObject.layer = LayerMask.NameToLayer("CampfireLit");

        UnityShell.main.data.campfire = index;
        UnityShell.main.Save();

        var scaleto = lightEmitter.transform.localScale;
        lightEmitter.transform.localScale = Vector3.zero;
        lightEmitter.transform.DOScale(scaleto, 0.25f).SetEase(Ease.InOutSine);
    }

    void BossCutscene()
    {
        var cccamera = FindObjectOfType<CinemachineVirtualCamera>();
        var oldFollowTarget = cccamera.Follow;
        
        cccamera.Follow = null;
        
        var bossPos = boss.transform.position;
        var returnToPos = cccamera.transform.position;

        bossPos.z = returnToPos.z;

        var seq = DOTween.Sequence();
        
        seq.AppendCallback(() => boss.bossmusic.SetActive(true));
        seq.AppendCallback(UnityCharacter.player.DisableInput);
        seq.Append(cccamera.transform.DOMove(bossPos, 3f).SetEase(Ease.InOutSine));
        seq.AppendInterval(1f);

        seq.AppendCallback(() =>
        {
            SFX.i.BossBell();
            UnityUIAnnouncement.i.ShowBoss(boss.announcedName);
        });
        
        for (var i = 0; i < lightPos.Count; i++)
        {
            var i1 = i;
            seq.AppendCallback(() => Instantiate(bossLighter, lightPos[i1].position, Quaternion.identity));
            seq.AppendInterval(0.25f);
        }

        seq.AppendInterval(4f);

        seq.Append(cccamera.transform.DOMove(returnToPos, 3f).SetEase(Ease.InOutSine));

        seq.AppendCallback(() =>
        {
            cccamera.Follow = oldFollowTarget;
            UnityCharacter.player.EnableInput();
        });
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, activationRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, defenderRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, defenderExpellRange);
    }

    public void OnHitByArrow(GameObject arrow)
    {
        Light(true);
    }
}