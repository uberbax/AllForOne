using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class SFXData
{
    public AudioClip clip;
    public float vol = 1f;
}

public class SFX : MonoBehaviour
{
    public static SFX i;

    public static float SFXVOL = 1f;

    public SFXData[] walkStep;
    public SFXData[] bowThread;
    public SFXData[] roll;
    public SFXData[] hitEnemy;
    public SFXData[] death;
    public SFXData[] bonfire;
    public SFXData[] arrowPickup;
    public SFXData[] hitTree;
    public SFXData[] enemyChargeStart;
    public SFXData[] enemyChargeLaunch;
    public SFXData[] enemyAppear;
    public SFXData[] bossBell;
    public SFXData[] hitEyes;
    public SFXData[] menuSound;
    public SFXData[] arrowStuck;
    public SFXData[] burning;
    public SFXData[] bowShoot;

    private void Awake()
    {
        i = this;
    }

    public void Roll()
    {
        PlayAudio(roll);
    }

    public void ArrowPickup()
    {
        PlayAudio(arrowPickup);
    }

    public void Bonfire()
    {
        PlayAudio(bonfire);
    }

    public void Death()
    {
        PlayAudio(death);
    }

    public void HitEnemy()
    {
        PlayAudio(hitEnemy);
    }

    public void BowThread()
    {
        PlayAudio(bowThread);
    }

    public void WalkStep()
    {
        PlayAudio(walkStep);
    }

    public void HitTree()
    {
        PlayAudio(hitTree);
    }

    public void EnemyChargeStart()
    {
        PlayAudio(enemyChargeStart);
    }

    public void EnemyChargeLaunch()
    {
        PlayAudio(enemyChargeLaunch);
    }

    public void EnemyAppear()
    {
        PlayAudio(enemyAppear);
    }

    public void BossBell()
    {
        PlayAudio(bossBell);
    }

    public void HitEyes()
    {
        PlayAudio(hitEyes);
    }

    public void MenuSound()
    {
        PlayAudio(menuSound);
    }

    public void ArrowStuck()
    {
        PlayAudio(arrowStuck);
    }

    public void Burning()
    {
        PlayAudio(burning);
    }

    public void BowShoot()
    {
        PlayAudio(bowShoot);
    }

    public void Mute()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioS in allAudioSources)
        {
            audioS.mute = !audioS.mute;
        }
    }

    public void PlayAudio(SFXData[] clip)
    {
        if (clip.Length == 0)
            return;

        var sfxData = clip[Random.Range(0, clip.Length)];
        PlayAudio(sfxData.clip, sfxData.vol);
    }

    public void PlayAudio(AudioClip clip, float vol = 1f)
    {
        if (clip == null)
            return;
        
        var transformPosition = Camera.main.transform.position + Vector3.forward;
        AudioSource.PlayClipAtPoint(clip, transformPosition, vol * SFXVOL);
    }
}
