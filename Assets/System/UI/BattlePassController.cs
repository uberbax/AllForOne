using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattlePassController : MonoBehaviour
{
    public const string PointsStat = "battlepass_point";
    public const string PremiumStat = "bought_battlepass";
    [SerializeField] private RectTransform content;
    [SerializeField] private BattlePassLevelView rowTemplate;
    [SerializeField] private Button claimAllButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text premiumText;

    private readonly List<ElTasko> tasks = new List<ElTasko>();
    private readonly List<BattlePassLevelView> rows = new List<BattlePassLevelView>();
    private bool claiming;
    private float nextRefresh;
    public IReadOnlyList<ElTasko> Stages => tasks;
    private static bool Ready => ConfigLoader.Instance != null && ModelStatistics.instance != null &&
        MainStates.instance != null && MainStates.instance.playerData != null && MainStates.instance.HasMain();
    public int Points => Ready ? Mathf.Max(0, ModelStatistics.instance.GetStatValue(PointsStat, false)) : 0;
    public bool HasPremium => Ready && ModelStatistics.instance.GetStatValue(PremiumStat, false) == 1;

    private void Awake()
    {
        if (claimAllButton != null) claimAllButton.onClick.AddListener(ClaimAll);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }
    private void OnEnable() { Subscribe(); RefreshTasks(); }
    private void Start() { Subscribe(); RefreshTasks(); }
    private void Subscribe()
    {
        ConfigLoader.onParseEnded -= RefreshTasks;
        ConfigLoader.onParseEnded += RefreshTasks;
    }
    private void OnDisable() => ConfigLoader.onParseEnded -= RefreshTasks;
    private void OnDestroy()
    {
        ConfigLoader.onParseEnded -= RefreshTasks;
        if (claimAllButton != null) claimAllButton.onClick.RemoveListener(ClaimAll);
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }
    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .25f;
        if (tasks.Count == 0 && Ready) RefreshTasks();
        else RefreshState();
    }
    public void Show() { gameObject.SetActive(true); transform.SetAsLastSibling(); RefreshTasks(); }
    public void Hide() => gameObject.SetActive(false);

    public static bool IsStageId(string id) => !string.IsNullOrEmpty(id) && id.StartsWith("battlepass_", StringComparison.Ordinal) &&
        int.TryParse(id.Substring("battlepass_".Length), NumberStyles.None, CultureInfo.InvariantCulture, out _);
    public static int RequiredPoints(ElTasko task)
    {
        if (task == null) return -1;
        // TASKS currently labels this have_item; the battle pass explicitly tracks the statistic instead.
        foreach (var requirement in task.reqFinish)
            if (requirement.what == PointsStat && int.TryParse(requirement.val, out var points) && points >= 0) return points;
        return -1;
    }
    public static string TakenStat(string id, bool premium) => id + (premium ? "_premium_taken" : "_taken");
    public void RefreshTasks()
    {
        tasks.Clear();
        if (ConfigLoader.Instance != null)
            tasks.AddRange(ConfigLoader.Instance.allTasks.Values.Where(t => t != null && IsStageId(t.id) && RequiredPoints(t) >= 0)
                .OrderBy(RequiredPoints).ThenBy(t => int.Parse(t.id.Substring("battlepass_".Length), CultureInfo.InvariantCulture)));
        if (content == null || rowTemplate == null) return;
        rows.Clear(); rows.AddRange(content.GetComponentsInChildren<BattlePassLevelView>(true));
        for (int i = 0; i < tasks.Count; i++)
        {
            if (i >= rows.Count) rows.Add(Instantiate(rowTemplate, content));
            rows[i].Bind(this, tasks[i], i + 1);
            rows[i].gameObject.SetActive(true);
        }
        for (int i = tasks.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
        RefreshState();
    }
    public bool IsTaken(string id, bool premium)
    {
        if (!Ready) return false;
        if (ModelStatistics.instance.GetStatValue(TakenStat(id, premium), false) >= 1) return true;
        // Respect free rewards already received through the existing task system.
        return !premium && MainStates.instance.playerData.playerTasks.Exists(t => t.id == id && t.taken);
    }
    public bool CanClaim(string id, bool premium)
    {
        if (!Ready || (premium && !HasPremium) || IsTaken(id, premium)) return false;
        var task = tasks.Find(t => t.id == id);
        return task != null && Points >= RequiredPoints(task) && (premium ? task.rewardsPremium : task.rewards).Count > 0;
    }
    public bool Claim(string id, bool premium)
    {
        if (claiming || !CanClaim(id, premium) || DatabaseAll.instance == null) return false;
        var task = tasks.Find(t => t.id == id);
        var rewards = premium ? task.rewardsPremium : task.rewards;
        // Preflight the complete grant so an invalid item cannot leave a partially claimed stage.
        foreach (var reward in rewards)
            if (reward == null || reward.Value <= 0 || (!DatabaseAll.instance.items.ContainsKey(reward.Key) &&
                !DatabaseAll.instance.heroes.ContainsKey(reward.Key) && !DatabaseAll.instance.skills.ContainsKey(reward.Key))) return false;
        claiming = true;
        try
        {
            MainStates.instance.AddItems(rewards);
            if (!premium)
            {
                var progress = MainStates.instance.playerData.playerTasks.Find(t => t.id == id);
                if (progress == null)
                {
                    progress = new TasksProg { id = id, started = 1 };
                    MainStates.instance.playerData.playerTasks.Add(progress);
                }
                progress.taken = progress.completed = true;
                progress.takenTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            ModelStatistics.instance.SetStatValue(TakenStat(id, premium), 1);
        }
        finally { claiming = false; }
        RefreshState();
        return true;
    }
    public void ClaimAll()
    {
        foreach (var task in new List<ElTasko>(tasks)) { Claim(task.id, false); Claim(task.id, true); }
    }
    public void RefreshState()
    {
        int points = Points, reached = 0, previous = 0, next = 0;
        foreach (var task in tasks)
        {
            int threshold = RequiredPoints(task);
            if (points >= threshold) { reached++; previous = threshold; }
            else { next = threshold; break; }
        }
        if (next == 0) next = previous;
        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            progressSlider.minValue = 0; progressSlider.maxValue = 1;
            progressSlider.SetValueWithoutNotify(tasks.Count > 0 && reached == tasks.Count ? 1 : Mathf.InverseLerp(previous, next, points));
        }
        if (progressText != null) progressText.text = points + " / " + next;
        if (pointsText != null) pointsText.text = points.ToString();
        if (levelText != null) levelText.text = reached.ToString();
        if (premiumText != null) premiumText.text = HasPremium ? "Premium Active" : "Premium Locked";
        foreach (var row in rows) if (row != null && row.gameObject.activeSelf) row.RefreshState();
        if (claimAllButton != null) claimAllButton.interactable = tasks.Exists(t => CanClaim(t.id, false) || CanClaim(t.id, true));
    }
}
