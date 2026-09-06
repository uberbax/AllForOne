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
        PlayClip(click);
    }

    public void PlayAny(string sound)
    {
        if (string.IsNullOrEmpty(sound) || ResourceHolder.instance == null || ResourceHolder.instance.sounds == null ||
            !ResourceHolder.instance.sounds.TryGetValue(sound, out var clip))
            return;

        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
            return;

        var position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, position, ResolveSoundVolume());
    }

    private float ResolveSoundVolume()
    {
        if (mon != null)
            return Mathf.Clamp01(mon.GetPar("volume_sound"));
        if (MainStates.instance != null && MainStates.instance.all.TryGetValue("settings", out var settings))
        {
            mon = settings;
            return Mathf.Clamp01(settings.GetPar("volume_sound"));
        }
        return Mathf.Clamp01(PlayerPrefs.GetFloat("volume_sound", 1f));
    }
    
    
    
}
