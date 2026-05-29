using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using DG.Tweening;
using DG.Tweening.Plugins;
using Features;
using UnityEngine;
using Random = UnityEngine.Random;

public class UnityBoss : MonoBehaviour
{
    public List<UnityBossPart> parts = new List<UnityBossPart>();

    public List<Transform> explosionPoints = new List<Transform>();
    public GameObject explosionVFX;

    public GameObject bossmusic;
    
    public Transform root;

    public string announcedName;
    
    public float bounceFrequency = 1f;
    public float bounceAmplitude = 0.1f;

    public float minionRange = 3f;
    public GameObject minion;

    public float minionSpawnCooldown = 1f;
    public float minionSpawnTimer = 0f;
    public int maxMinions = 4;

    public float activationRange = 4f;
    
    List<GameObject> spawned = new List<GameObject>();
    
    bool isAlive;

    bool isActive;

    bool isPlayingCutscene;

    void Update()
    {
        isAlive = false;
        
        foreach(var part in parts)
            if (part != null)
                isAlive = true;

        if (Input.GetKeyDown(KeyCode.L))
        {
            foreach (var p in parts)
                if (p != null)
                    p.Kill();
        }
        
        if (isAlive)
        {
            float bounce = 1 + Mathf.Sin(Time.time * bounceFrequency) * bounceAmplitude;
            root.localScale = new Vector3(1, bounce, 1f);
        }
        else
        {
            // i've just died

            BossDefeatedCutscene();
            
            foreach (var s in spawned)
            {
                if (s != null)
                    s.GetComponent<UnityEnemy>().Kill();
            }
        }
    }

    void BossDefeatedCutscene()
    {
        if (isPlayingCutscene)
            return;
        isPlayingCutscene = true;
        
        var cccamera = FindObjectOfType<CinemachineVirtualCamera>();
    
        cccamera.Follow = null;
    
        var bossPos = transform.position;
        var returnToPos = cccamera.transform.position;

        bossPos.z = returnToPos.z;

        var seq = DOTween.Sequence();

        seq.AppendCallback(UnityCharacter.player.DisableInput);
        seq.Append(cccamera.transform.DOMove(bossPos, 3f).SetEase(Ease.InOutSine));
        seq.AppendInterval(1f);

        seq.AppendCallback(() =>
        {
            UnityCharacter.player.PickupArrowIfAny();
            UnityUIAnnouncement.i.ShowBoss("GREAT ENEMY DEFEATED");
        });
    
        for (var i = 0; i < explosionPoints.Count; i++)
        {
            var i1 = i;
            seq.AppendCallback(() => Instantiate(explosionVFX, explosionPoints[i1].position, Quaternion.identity));
            seq.AppendInterval(0.25f);
        }

        seq.AppendInterval(4f);
        
        seq.AppendCallback(() =>
        {
            UnityShell.main.Get<ScreenFader>().FadeIn();
        });
        
        seq.AppendInterval(1f);
        
        seq.AppendCallback(() =>
        {
            UnityGameScreen.isgamewin = true;
        });
    }

    void FixedUpdate()
    {
        if (!isAlive)
            return;

        if (!isActive)
        {
            if (Vector2.Distance(transform.position, UnityCharacter.playerPosition) < activationRange)
                isActive = true;
            return;
        }

        minionSpawnTimer += Time.fixedDeltaTime;
        if (minionSpawnTimer > minionSpawnCooldown)
        {
            minionSpawnTimer = 0;

            var n = 0;
            
            foreach(var s in spawned)
                if (s != null)
                    n++;

            if (n >= maxMinions)
                return;

            var instantiate = Instantiate(minion, GetDefenderPos(), Quaternion.identity);
            instantiate.GetComponent<UnityEnemy>().Aggro();
            spawned.Add(instantiate);
        }
    }

    Vector3 GetDefenderPos()
    {
        return transform.position + (Vector3)Random.insideUnitCircle.normalized * minionRange;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, activationRange);
    }
}