using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioClip click;

    public AudioSource source;
    private void Awake()
    {
        instance = this;
    }

    private RObj mon;
    void Start()
    {
        if (!MainStates.instance.all.ContainsKey("settings"))
        {
            Invoke("Start", 0.1f);
            return;
        }
        Debug.Log("SOUND");
        mon = MainStates.instance.all["settings"];
    }

    public void PlayClick()
    {
        AudioSource.PlayClipAtPoint(click, Camera.main.transform.position, mon.GetPar("volume_sound"));
    }

    public void PlayEffect(AudioClip clip)
    {
        if (clip == null || mon == null) return;

        float volume = Mathf.Clamp01(mon.GetPar("volume_sound"));
        if (volume <= 0f) return;

        var listenerCamera = Camera.main;
        AudioSource.PlayClipAtPoint(clip, listenerCamera != null ? listenerCamera.transform.position : transform.position, volume);
    }

    public void PlayAny(string sound, int spellType = 0)
    {
        if (mon == null) return;
        
        if (ResourceHolder.instance.sounds.ContainsKey(sound))
        {
            AudioSource.PlayClipAtPoint(ResourceHolder.instance.sounds[sound], Camera.main.transform.position, mon.GetPar("volume_sound"));
        }
        else if (spellType == 1)
        {
            Debug.Log(mon);
            AudioSource.PlayClipAtPoint(ResourceHolder.instance.sounds["buff"], Camera.main.transform.position, mon.GetPar("volume_sound"));
        }
        else if (spellType == 2)
        {
            AudioSource.PlayClipAtPoint(ResourceHolder.instance.sounds["spell"], Camera.main.transform.position, mon.GetPar("volume_sound"));
        }
    }

    public void PlayMusic()
    {
        
    }
    
    
    
}
