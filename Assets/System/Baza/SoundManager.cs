using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioClip click;
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

    public void PlayAny(string sound)
    {
        if (ResourceHolder.instance.sounds.ContainsKey(sound))
        {
            AudioSource.PlayClipAtPoint(ResourceHolder.instance.sounds[sound], Camera.main.transform.position, mon.GetPar("volume_sound"));
        }
    }
    
    
    
}
