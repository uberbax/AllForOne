using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GUIWhoHeroesNightHUD : MonoBehaviour
{
    private const float RefreshInterval = 0.1f;
    [SerializeField] private GameObject content;
    [SerializeField] private Slider princeHealth;
    [SerializeField] private TextMeshProUGUI nightText;
    [SerializeField] private TextMeshProUGUI controlsText;
    [Header("Game over")]
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private TextMeshProUGUI loseTitle;
    [SerializeField] private TextMeshProUGUI nightReachedLabel;
    [SerializeField] private TextMeshProUGUI nightReachedValue;
    [SerializeField] private TextMeshProUGUI bestNightLabel;
    [SerializeField] private TextMeshProUGUI bestNightValue;
    [SerializeField] private TextMeshProUGUI permanentPerksText;
    [SerializeField] private GameObject rewardSlots;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI restartText;
    private float nextRefreshTime;

    private void Awake()
    {
        EventManager.SUB("PARSE_ENDED", OnParseEnded);
        EventManager.SUB("new_day", OnPhaseChanged);
        EventManager.SUB("new_night", OnPhaseChanged);
        EventManager.SUB("whoheroes_phase_changed", OnPhaseChanged);
        EventManager.SUB("whoheroes_game_over", OnGameOver);
        ResolveLoseScreen();
        restartButton?.onClick.AddListener(Restart);
        if (princeHealth != null)
            princeHealth.interactable = false;
        if (ConfigLoader.parseEnded)
            ApplyConfiguredText();
        Refresh();
    }

    private void Update()
    {
        if (content == null || !content.activeSelf || Time.unscaledTime < nextRefreshTime)
            return;
        nextRefreshTime = Time.unscaledTime + RefreshInterval;
        RefreshValues();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB("PARSE_ENDED", OnParseEnded);
        EventManager.UNSUB("new_day", OnPhaseChanged);
        EventManager.UNSUB("new_night", OnPhaseChanged);
        EventManager.UNSUB("whoheroes_phase_changed", OnPhaseChanged);
        EventManager.UNSUB("whoheroes_game_over", OnGameOver);
        restartButton?.onClick.RemoveListener(Restart);
    }

    private void OnPhaseChanged(ArgPass _)
    {
        if (MainCycle_WhoHeroes.Instance != null &&
            MainCycle_WhoHeroes.Instance.Phase != WhoHeroesPhase.GameOver)
            loseScreen?.SetActive(false);
        ApplyConfiguredText();
        Refresh();
    }

    private void OnParseEnded(ArgPass _)
    {
        ApplyConfiguredText();
        Refresh();
    }

    private void ApplyConfiguredText()
    {
        if (controlsText != null)
            controlsText.text = MainCycle_WhoHeroes.Text("night_controls");
    }

    private void OnGameOver(ArgPass args)
    {
        ResolveLoseScreen();
        if (loseScreen == null)
            return;

        loseScreen.SetActive(true);
        if (loseTitle != null)
            loseTitle.text = MainCycle_WhoHeroes.Text("prince_died");
        if (nightReachedLabel != null)
            nightReachedLabel.text = MainCycle_WhoHeroes.Text("night_reached");
        if (nightReachedValue != null)
            nightReachedValue.text = args?.what ?? "0";
        if (bestNightLabel != null)
            bestNightLabel.text = MainCycle_WhoHeroes.Text("best_night");
        if (bestNightValue != null)
            bestNightValue.text = args?.what1 ?? "0";
        if (permanentPerksText != null)
        {
            var entries = MainCycle_WhoHeroes.PermanentPerkIds
                .Select(id => (id, level: ModelStatistics.instance == null
                    ? 0
                    : ModelStatistics.instance.GetStatValue(id, false)))
                .Where(value => value.level > 0)
                .Select(value => MainCycle_WhoHeroes.Text("perk_level")
                    .Replace("{perk}", MainCycle_WhoHeroes.PermanentPerkTitle(value.id))
                    .Replace("{level}", value.level.ToString()))
                .ToList();
            permanentPerksText.enableAutoSizing = true;
            permanentPerksText.fontSizeMin = 12f;
            permanentPerksText.text = MainCycle_WhoHeroes.Text("permanent_perks") + "\n" +
                                      (entries.Count == 0
                                          ? MainCycle_WhoHeroes.Text("none")
                                          : string.Join("\n", entries));
        }
        if (rewardSlots != null)
            rewardSlots.SetActive(false);
        if (restartText != null)
            restartText.text = MainCycle_WhoHeroes.Text("restart");
    }

    private void ResolveLoseScreen()
    {
        if (loseScreen == null || loseTitle == null || nightReachedLabel == null || nightReachedValue == null ||
            bestNightLabel == null || bestNightValue == null || permanentPerksText == null || rewardSlots == null ||
            restartButton == null || restartText == null)
            Debug.LogError("WhoHeroes night HUD: Inspector references are incomplete.", this);
    }

    private static void Restart()
    {
        EventManager.INV(WhoHeroesEvents.RestartRequested, new ArgPass());
    }

    private void Refresh()
    {
        var visible = MainCycle_WhoHeroes.Instance != null &&
                      MainCycle_WhoHeroes.Instance.Phase == WhoHeroesPhase.Night;
        if (content != null)
            content.SetActive(visible);
        if (visible)
        {
            nextRefreshTime = 0f;
            RefreshValues();
        }
    }

    private void RefreshValues()
    {
        if (MainCycle_WhoHeroes.Instance == null || MainStates.instance == null ||
            !MainStates.instance.all.TryGetValue("main_player", out var prince))
            return;

        var health = Mathf.Max(0f, prince.GetPar("health"));
        var maxHealth = Mathf.Max(1f, prince.GetPar("max_health"));
        if (princeHealth != null)
            princeHealth.normalizedValue = Mathf.Clamp01(health / maxHealth);
        if (nightText != null)
            nightText.text = MainCycle_WhoHeroes.Text("night_hud")
                .Replace("{night}", MainCycle_WhoHeroes.Instance.NightNumber.ToString())
                .Replace("{health}", Mathf.CeilToInt(health).ToString())
                .Replace("{max_health}", Mathf.CeilToInt(maxHealth).ToString());
    }

}
