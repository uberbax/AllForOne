using System;
using UnityEngine;

public class PlaySound : MonoBehaviour
{
    public string soundName = "";
    private void OnEnable()
    {
        SoundManager.instance.PlayAny(soundName);
    }
}
