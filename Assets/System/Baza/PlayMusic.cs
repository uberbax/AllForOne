using System;
using UnityEngine;

public class PlayMusic : MonoBehaviour
{
    public string musicName = "";
    public AudioClip music;
    private void OnEnable()
    {
        SoundManager.instance.source.clip = music;
        SoundManager.instance.source.Play();
    }
}
