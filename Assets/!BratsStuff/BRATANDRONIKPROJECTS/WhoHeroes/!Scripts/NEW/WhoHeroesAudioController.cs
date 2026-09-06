using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WhoHeroesAudioController : MonoBehaviour
{
    public const string MusicVolumeKey = "volume_music";
    public const string SoundVolumeKey = "volume_sound";

    public static WhoHeroesAudioController Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;

    private readonly Dictionary<string, float> nextAllowedPlayTime = new Dictionary<string, float>();
    private readonly HashSet<Button> boundButtons = new HashSet<Button>();

    public float MusicVolume => ReadStoredVolume(MusicVolumeKey);
    public float SoundVolume => ReadStoredVolume(SoundVolumeKey);

    private void Awake()
    {
        Instance = this;
        ApplyStoredVolumes();
        SubscribeAudioEvents();
    }

    private IEnumerator Start()
    {
        ApplyStoredVolumes();
        if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
            musicSource.Play();

        StartCoroutine(BindButtons());

        while (enabled && !HasMinimusSettings())
            yield return null;

        if (enabled)
            ApplyStoredVolumes();
    }

    private void OnDestroy()
    {
        UnsubscribeAudioEvents();
        foreach (var button in boundButtons)
            if (button != null)
                button.onClick.RemoveListener(OnButtonClicked);
        boundButtons.Clear();

        if (Instance == this)
            Instance = null;
    }

    public void SetMusicVolume(float value)
    {
        SetVolume(MusicVolumeKey, value);
        ApplyMusicVolume(value);
    }

    public void SetSoundVolume(float value)
    {
        SetVolume(SoundVolumeKey, value);
    }

    private void ApplyStoredVolumes()
    {
        var music = MusicVolume;
        var sound = SoundVolume;
        ApplyToMinimusSettings(MusicVolumeKey, music);
        ApplyToMinimusSettings(SoundVolumeKey, sound);
        ApplyMusicVolume(music);
    }

    private void ApplyMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(value);
    }

    private static void SetVolume(string key, float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(key, value);
        ApplyToMinimusSettings(key, value);
    }

    private static void ApplyToMinimusSettings(string key, float value)
    {
        if (MainStates.instance != null && MainStates.instance.all.TryGetValue("settings", out var settings))
            settings.SetPar(key, value);
    }

    private static bool HasMinimusSettings()
    {
        return MainStates.instance != null && MainStates.instance.all.ContainsKey("settings");
    }

    private static float ReadStoredVolume(string key)
    {
        if (PlayerPrefs.HasKey(key))
            return Mathf.Clamp01(PlayerPrefs.GetFloat(key));

        if (MainStates.instance != null && MainStates.instance.all.TryGetValue("settings", out var settings))
        {
            var configured = settings.GetPar(key);
            return Mathf.Clamp01(configured > 1f ? configured / 100f : configured);
        }

        return 1f;
    }

    private void SubscribeAudioEvents()
    {
        EventManager.SUB("new_day", OnNewDay);
        EventManager.SUB("new_night", OnNewNight);
        EventManager.SUB("battle_start", OnBattleStarted);
        EventManager.SUB("battle_ended", OnBattleEnded);
        EventManager.SUB("whoheroes_game_over", OnGameOver);
        EventManager.SUB("unit_damaged", OnUnitDamaged);
        EventManager.SUB("unit_died", OnUnitDied);
        EventManager.SUB(WhoHeroesEvents.ManagementBlocked, OnActionFailed);
        EventManager.SUB(WhoHeroesEvents.ActionFailed, OnActionFailed);
        EventManager.SUB(WhoHeroesEvents.ActionSucceeded, OnActionSucceeded);
        EventManager.SUB(WhoHeroesEvents.PortalAvailable, OnTerritoryAvailable);
        EventManager.SUB(WhoHeroesEvents.TerritoryAvailable, OnTerritoryAvailable);
        EventManager.SUB(WhoHeroesEvents.PortalCaptured, OnPortalCaptured);
        EventManager.SUB(WhoHeroesEvents.PointOfInterestCaptured, OnPointOfInterestCaptured);
        EventManager.SUB(WhoHeroesEvents.NightWavePrepared, OnNightWavePrepared);
        EventManager.SUB(WhoHeroesEvents.PermanentPerkOffered, OnPermanentPerkOffered);
        EventManager.SUB(WhoHeroesEvents.PermanentPerkChosen, OnPermanentPerkChosen);
        EventManager.SUB(WhoHeroesEvents.TraderCompleted, OnTraderCompleted);
        EventManager.SUB(WhoHeroesEvents.ResourceDelivered, OnResourceDelivered);
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    private void UnsubscribeAudioEvents()
    {
        EventManager.UNSUB("new_day", OnNewDay);
        EventManager.UNSUB("new_night", OnNewNight);
        EventManager.UNSUB("battle_start", OnBattleStarted);
        EventManager.UNSUB("battle_ended", OnBattleEnded);
        EventManager.UNSUB("whoheroes_game_over", OnGameOver);
        EventManager.UNSUB("unit_damaged", OnUnitDamaged);
        EventManager.UNSUB("unit_died", OnUnitDied);
        EventManager.UNSUB(WhoHeroesEvents.ManagementBlocked, OnActionFailed);
        EventManager.UNSUB(WhoHeroesEvents.ActionFailed, OnActionFailed);
        EventManager.UNSUB(WhoHeroesEvents.ActionSucceeded, OnActionSucceeded);
        EventManager.UNSUB(WhoHeroesEvents.PortalAvailable, OnTerritoryAvailable);
        EventManager.UNSUB(WhoHeroesEvents.TerritoryAvailable, OnTerritoryAvailable);
        EventManager.UNSUB(WhoHeroesEvents.PortalCaptured, OnPortalCaptured);
        EventManager.UNSUB(WhoHeroesEvents.PointOfInterestCaptured, OnPointOfInterestCaptured);
        EventManager.UNSUB(WhoHeroesEvents.NightWavePrepared, OnNightWavePrepared);
        EventManager.UNSUB(WhoHeroesEvents.PermanentPerkOffered, OnPermanentPerkOffered);
        EventManager.UNSUB(WhoHeroesEvents.PermanentPerkChosen, OnPermanentPerkChosen);
        EventManager.UNSUB(WhoHeroesEvents.TraderCompleted, OnTraderCompleted);
        EventManager.UNSUB(WhoHeroesEvents.ResourceDelivered, OnResourceDelivered);
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    private IEnumerator BindButtons()
    {
        var wait = new WaitForSecondsRealtime(0.5f);
        while (enabled)
        {
            boundButtons.RemoveWhere(button => button == null);
            foreach (var button in Resources.FindObjectsOfTypeAll<Button>()
                         .Where(button => button != null && button.gameObject.scene == gameObject.scene))
            {
                if (!boundButtons.Add(button))
                    continue;
                button.onClick.AddListener(OnButtonClicked);
            }
            yield return wait;
        }
    }

    private void PlayEvent(string key, float cooldown = 0.08f, string channel = null)
    {
        if (SoundManager.instance == null)
            return;

        var limiter = string.IsNullOrEmpty(channel) ? key : channel;
        if (nextAllowedPlayTime.TryGetValue(limiter, out var nextTime) && Time.unscaledTime < nextTime)
            return;
        nextAllowedPlayTime[limiter] = Time.unscaledTime + cooldown;
        SoundManager.instance.PlayAny(key);
    }

    private void OnButtonClicked() => PlayEvent("ui_click", 0.03f, "ui");
    private void OnNewDay(ArgPass _) => PlayEvent("day_start", 0.5f, "phase");
    private void OnNewNight(ArgPass _) => PlayEvent("night_start", 0.5f, "phase");
    private void OnBattleStarted(ArgPass _) => PlayEvent("battle_start", 0.5f, "battle");
    private void OnGameOver(ArgPass _) => PlayEvent("game_over", 0.75f, "game_over");
    private void OnNightWavePrepared(ArgPass _) => PlayEvent("night_warning", 0.5f, "night_warning");
    private void OnPermanentPerkOffered(ArgPass _) => PlayEvent("perk_offer", 0.3f, "perk");
    private void OnPermanentPerkChosen(ArgPass _) => PlayEvent("perk_chosen", 0.3f, "perk");
    private void OnTraderCompleted(ArgPass _) => PlayEvent("trader_complete", 0.25f, "economy");
    private void OnResourceDelivered(ArgPass _) => PlayEvent("resource_delivery", 0.12f, "delivery");
    private void OnActionFailed(ArgPass _) => PlayEvent("ui_error", 0.18f, "ui_error");
    private void OnTerritoryAvailable(ArgPass _) => PlayEvent("territory_unlock", 0.3f, "unlock");
    private void OnPortalCaptured(ArgPass _) => PlayEvent("portal_capture", 0.3f, "capture");
    private void OnPointOfInterestCaptured(ArgPass _) => PlayEvent("poi_capture", 0.3f, "capture");

    private void OnBattleEnded(ArgPass args)
    {
        PlayEvent(args != null && args.num == 0 ? "battle_win" : "battle_lose", 0.75f, "battle");
    }

    private void OnActionSucceeded(ArgPass args)
    {
        var key = args?.what switch
        {
            "buy" => "hire",
            "upgrade" => "upgrade",
            _ => "ui_confirm"
        };
        PlayEvent(key, 0.12f, "action");
    }

    private void OnRefresh(ArgPass args)
    {
        switch (args?.what)
        {
            case "reroll":
                PlayEvent("tavern_reroll", 0.2f, "action");
                break;
            case "start_expedition":
                PlayEvent("expedition_depart", 0.3f, "expedition");
                break;
            case "expedition_returned":
                PlayEvent("expedition_return", 0.3f, "expedition");
                break;
        }
    }

    private void OnUnitDamaged(ArgPass args)
    {
        if (args?.who == null)
            return;
        var princeHit = args.who.RID == "main_player";
        PlayEvent(princeHit ? "prince_hit" : "combat_hit", princeHit ? 0.18f : 0.055f,
            princeHit ? "prince_hit" : "combat_hit");
    }

    private void OnUnitDied(ArgPass args)
    {
        var unit = args?.who;
        if (unit == null)
            return;
        if (unit.RID == "main_player")
            PlayEvent("prince_death", 0.5f, "prince_death");
        else if (unit.tags != null && unit.tags.Contains("player"))
            PlayEvent("ally_death", 0.14f, "ally_death");
        else
            PlayEvent("enemy_death", 0.08f, "enemy_death");
    }
}
