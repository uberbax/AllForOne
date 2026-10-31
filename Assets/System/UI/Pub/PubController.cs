using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PubController : MonoBehaviour
{
    [Header("Recruitment Pool")]
    [Tooltip("Allowed hero IDs from Heroes (including the level suffix, if any). Empty list = all eligible heroes. hero_ and tree prefixes are always excluded.")]
    [SerializeField] private List<string> allowedHeroIds = new List<string>();

    [Header("Balance")]
    [SerializeField] private string currencyId = "gold";
    [SerializeField, Min(1)] private int recruitPrice = 100;
    [SerializeField, Min(0)] private int refreshPrice = 25;
    [SerializeField, Min(1)] private int refreshMinutes = 30;
    [SerializeField, Min(1)] private int bonusEvery = 20;
    [Header("View")]
    [SerializeField] private PubHeroCard[] cards;
    [SerializeField] private Button recruitButton, refreshButton, closeButton, resultCloseButton;
    [SerializeField] private Button[] multiplierButtons;
    [SerializeField] private Image[] multiplierBackgrounds;
    [SerializeField] private Image balanceIcon, recruitIcon, refreshIcon, progressFill;
    [SerializeField] private TMP_Text balanceLabel, recruitCostLabel, refreshCostLabel, timerLabel, progressLabel, statusLabel;
    [SerializeField] private GameObject resultPanel, opener;
    [SerializeField] private TMP_Text resultTitle, resultSubtitle;
    [SerializeField] private Image[] resultIcons;
    [SerializeField] private TMP_Text[] resultNames, resultCounts;
    [SerializeField] private RectTransform safeArea, designContent;
    private PubRecruitment model;
    private PlayerData loadedData;
    private int multiplier = 1;
    private bool busy;
    private float nextTick;
    private Coroutine reveal;
    private Rect previousSafeArea;
    private Vector2 previousScreen, previousContentSize;
    public bool IsBusy => busy;
    public IReadOnlyList<PubHero> Offers => model != null ? model.Offers : Array.Empty<PubHero>();
    public int RecruitPrice => recruitPrice;
    public int RefreshPrice => refreshPrice;
    public int RefreshMinutes => refreshMinutes;

    private void Awake()
    {
        recruitButton.onClick.AddListener(Recruit);
        refreshButton.onClick.AddListener(InstantRefresh);
        closeButton.onClick.AddListener(Hide);
        resultCloseButton.onClick.AddListener(CloseResult);
        for (int i = 0; i < multiplierButtons.Length; i++)
        {
            int value = i + 1;
            multiplierButtons[i].onClick.AddListener(() => SelectMultiplier(value));
        }
        resultPanel.SetActive(false);
    }
    private void OnEnable() { ConfigLoader.onParseEnded += ReloadConfig; if (opener != null) opener.SetActive(false); nextTick = 0; ApplySafeArea(); }
    private void ReloadConfig() { model = null; loadedData = null; nextTick = 0; }
    private void OnDisable()
    {
        ConfigLoader.onParseEnded -= ReloadConfig;
        if (reveal != null) StopCoroutine(reveal);
        reveal = null; busy = false;
        ClearRoulette();
        if (cards != null) foreach (var card in cards) if (card != null) card.SetRecruitmentBusy(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (opener != null) opener.SetActive(true);
    }
    private void Update()
    {
        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + .25f; ApplySafeArea();Refresh();
    }
    public void Show() { gameObject.SetActive(true); transform.SetAsLastSibling(); Refresh(); }
    public void Hide() { if (!busy) gameObject.SetActive(false); }
    public void CloseResult() { resultPanel.SetActive(false); Refresh(); }
    public void SelectMultiplier(int value) { if (busy) return; multiplier = Mathf.Clamp(value, 1, 3); Refresh(); }

    private bool EnsureModel()
    {
        var state = MainStates.instance;
        if (!PubRecruitment.Ready(state) || ConfigLoader.Instance == null || !ConfigLoader.parseEnded) return false;
        if (model == null || !ReferenceEquals(loadedData, state.playerData))
        {
            model = new PubRecruitment(Guid.NewGuid().GetHashCode()); loadedData = state.playerData;
            model.BuildPool(ConfigLoader.Instance.heroes, DatabaseAll.instance.heroes, allowedHeroIds);
            if (model.RestoreOrRefresh(loadedData, DateTime.UtcNow, refreshMinutes)) PubRecruitment.Persist(state);
        }
        return model.Offers.Count > 0;
    }
    public void Refresh()
    {
        if (recruitButton == null) return;
        bool ready = EnsureModel();
        if (ready && !busy && DateTime.UtcNow.Ticks >= model.NextRefreshTicks)
        {
            model.Refresh(loadedData, DateTime.UtcNow, refreshMinutes);PubRecruitment.Persist(MainStates.instance);
        }
        int balance = ready ? PubRecruitment.Currency(MainStates.instance.mainPlayer, currencyId) : 0;
        balanceLabel.text = balance.ToString("N0");
        recruitCostLabel.text = ((long)recruitPrice * multiplier).ToString("N0");
        refreshCostLabel.text = refreshPrice.ToString("N0");
        recruitButton.interactable = ready && !busy && !resultPanel.activeSelf && balance >= (long)recruitPrice * multiplier;
        refreshButton.interactable = ready && !busy && !resultPanel.activeSelf && balance >= refreshPrice;
        closeButton.interactable = !busy;
        for (int i = 0; i < multiplierButtons.Length; i++)
        {
            multiplierButtons[i].interactable = !busy && !resultPanel.activeSelf;
            multiplierBackgrounds[i].color = multiplier == i + 1 ? PubUiTheme.Blue : PubUiTheme.White;
        }
        if (ResourceHolder.instance != null && ResourceHolder.instance.items.TryGetValue(currencyId,out var icon))
        { balanceIcon.sprite = recruitIcon.sprite = refreshIcon.sprite = icon; }
        if (!ready)
        {
            statusLabel.text = ConfigLoader.parseEnded ? "No eligible heroes in Heroes" : "Loading heroes…";
            timerLabel.text = "--:--";return;
        }
        var player = MainStates.instance.mainPlayer;
        foreach (var card in cards) if (card != null) card.SetRecruitmentBusy(busy);
        if (!busy)
            for (int i = 0; i < cards.Length; i++) cards[i].Bind(i < model.Offers.Count ? model.Offers[i] : null, player, model.Offers.Count, multiplier);
        int progress = model.TotalRecruits % Mathf.Max(1, bonusEvery);
        progressLabel.text = progress + " / " + bonusEvery;
        progressFill.fillAmount = (float)progress / Mathf.Max(1, bonusEvery);
        double seconds = Math.Max(0, TimeSpan.FromTicks(Math.Max(0, model.NextRefreshTicks - DateTime.UtcNow.Ticks)).TotalSeconds);
        timerLabel.text = "New heroes in " + TimeSpan.FromSeconds(Math.Ceiling(seconds)).ToString(@"mm\:ss");
    }
    public void Recruit()
    {
        if (busy || resultPanel.activeSelf || !EnsureModel()) return;
        busy = true;
        if (!model.TryRecruit(MainStates.instance, multiplier, currencyId, recruitPrice, bonusEvery,
            out var rewards, out int bonus, out string error))
        { busy = false;statusLabel.text = error;Refresh();return; }
        statusLabel.text = "Recruiting…";Refresh();
        reveal = StartCoroutine(Reveal(rewards, bonus));
    }
    private IEnumerator Reveal(List<PubHero> rewards, int bonus)
    {
        int visible = Mathf.Min(cards.Length, model.Offers.Count);
        int winner = Array.FindIndex(cards, card => card != null && card.HeroId == rewards[0].Id);
        var path = PubRoulette.Path(visible, winner, UnityEngine.Random.Range(0, Mathf.Max(1, visible)));
        for (int step = 0; step < path.Length; step++)
        {
            for (int i = 0; i < cards.Length; i++)
                if (cards[i] != null) cards[i].SetRouletteHighlight(i == path[step]);
            float t = (float)step / Mathf.Max(1, path.Length - 1);
            yield return new WaitForSecondsRealtime(Mathf.Lerp(.04f, .19f, t * t));
        }
        yield return new WaitForSecondsRealtime(.3f);
        ClearRoulette();
        if (winner >= 0 && winner < cards.Length) cards[winner].Pulse();
        var groups = rewards.GroupBy(h => h.Id).ToArray();
        for (int i = 0; i < resultIcons.Length; i++)
        {
            resultIcons[i].transform.parent.gameObject.SetActive(i < groups.Length);
            if (i >= groups.Length) continue;
            var hero = groups[i].First();
            resultIcons[i].sprite = PubHeroCard.FindPortrait(hero);
            resultNames[i].text = hero.Name + (groups[i].Any(h => h.GrantedHero) ? " • NEW" : "");
            resultCounts[i].enableAutoSizing = true; resultCounts[i].fontSizeMin = 18;
            resultCounts[i].text = "Shards ×" + groups[i].Sum(h => h.Quantity) + "\nMileage ×" + groups[i].Sum(h => h.TotalMileageTokens);
        }
        resultTitle.text = "RECRUITED";
        int newHeroes = rewards.Count(h => h.GrantedHero);
        resultSubtitle.text = "Shards + " + rewards.Sum(h => h.TotalMileageTokens) + " mileage tokens" +
            (newHeroes > 0 ? " • New heroes: " + newHeroes : "") + (bonus > 0 ? " • Bonus rewards ×" + (bonus + 1) : "");
        resultPanel.SetActive(true);statusLabel.text = "Rewards added to your inventory";busy = false;reveal = null;
        Refresh();UIfiller.GlobalRefresh();
    }
    public void InstantRefresh()
    {
        if (busy || resultPanel.activeSelf || !EnsureModel()) return;
        busy = true;
        bool refreshed = model.TryPaidRefresh(MainStates.instance, currencyId, refreshPrice, refreshMinutes, DateTime.UtcNow, out string error);
        busy = false; statusLabel.text = refreshed ? "New heroes are ready" : error; Refresh();
    }
    private void ClearRoulette()
    {
        if (cards == null) return;
        foreach (var card in cards) if (card != null) card.SetRouletteHighlight(false);
    }
    private void ApplySafeArea()
    {
        if (safeArea == null) return;
        var screen = new Vector2(Screen.width, Screen.height);var area = Screen.safeArea;
        if (screen == previousScreen && area == previousSafeArea && safeArea.rect.size == previousContentSize) return;
        previousScreen = screen;previousSafeArea = area;
        if (screen.x <= 0 || screen.y <= 0) return;
        safeArea.anchorMin = area.min / screen;safeArea.anchorMax = area.max / screen;
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        previousContentSize = safeArea.rect.size;
        if (designContent != null && previousContentSize.x > 0 && previousContentSize.y > 0)
            designContent.localScale = Vector3.one * Mathf.Min(previousContentSize.x / 1080f, previousContentSize.y / 2160f);
    }
#if UNITY_EDITOR
    public void EditorPreview(PubHero[] heroes, Sprite gold)
    {
        for (int i = 0; i < cards.Length; i++) cards[i].Bind(i < heroes.Length ? heroes[i] : null, null, heroes.Length);
        balanceLabel.text = "1,250";recruitCostLabel.text = recruitPrice.ToString();refreshCostLabel.text = refreshPrice.ToString();
        progressLabel.text = "4 / " + bonusEvery;progressFill.fillAmount = 4f / bonusEvery;
        timerLabel.text = "New heroes in 29:41";statusLabel.text = "One of these heroes is guaranteed";
        balanceIcon.sprite = recruitIcon.sprite = refreshIcon.sprite = gold;
        resultPanel.SetActive(false);
        for (int i = 0; i < multiplierBackgrounds.Length; i++)multiplierBackgrounds[i].color = i == 0 ? PubUiTheme.Blue : PubUiTheme.White;
    }
#endif
}
