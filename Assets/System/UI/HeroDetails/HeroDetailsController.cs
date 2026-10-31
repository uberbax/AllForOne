using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable window; views resolve the nearest parent ObjHolder.</summary>
public sealed class HeroDetailsController : MonoBehaviour
{
    public const string ResourcePath = "UI_HeroDetails";
    [SerializeField] private Button upgradeButton, closeButton;
    [SerializeField] private Toggle maxToggle;
    [SerializeField, Min(0)] private int maxLevel;
    [SerializeField, Min(1)] private int maxLevelsPerClick = 100;
    private HeroDetailsPart[] parts;
    private ObjHolder holder;
    private float nextRefresh;
    private bool busy;
    private void Awake()
    {
        if (upgradeButton != null) upgradeButton.onClick.AddListener(Upgrade);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }
    private void OnEnable()
    {
        parts = GetComponentsInChildren<HeroDetailsPart>(true);
        UIfiller.acts += Refresh; Refresh(); nextRefresh = Time.unscaledTime + .25f;
    }
    private void OnDisable() { UIfiller.acts -= Refresh; }
    private void OnDestroy()
    {
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(Upgrade);
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }
    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .25f; Refresh();
    }
    public void Show(RObj hero)
    {
        if (holder == null) holder = GetComponentInParent<ObjHolder>(true);
        if (holder == null) { Debug.LogError("HeroDetails needs a parent ObjHolder", this); return; }
        holder.asDBObj = ""; holder.asObj = ""; holder.asCurObj = ""; holder.asMain = false;
        holder.obj = hero; holder.noTrack = true; gameObject.SetActive(true); Refresh();
    }
    public void Hide() { gameObject.SetActive(false); }
    public static int SkillIndex(int milestoneLevel) => Math.Max(0, milestoneLevel / 10 - 1) % 2;
    public static int SkillRank(int milestoneLevel) => Math.Max(0, milestoneLevel / 10 - 1) / 2 + 1;
    public static int UnlockedRank(int heroLevel, int skillIndex) => Math.Max(0, (heroLevel / 10 + 1 - skillIndex) / 2);
    public static string SkillId(RObj hero, int index)
    {
        var list = hero?.dbObj?.skills;
        return list != null && index >= 0 && index < list.Count && !string.IsNullOrWhiteSpace(list[index]) && list[index] != "x" ? list[index] : "";
    }
    public static string Pretty(string id) => string.IsNullOrWhiteSpace(id) ? "—" : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' '));
    internal static bool IsOwned(RObj hero)
    {
        var state = MainStates.instance;
        return hero?.dbObj != null && hero.it == ItemType.monster && string.IsNullOrEmpty(hero.shardID) &&
            state != null && state.HasMain() && state.mainPlayer.inventory.Contains(hero);
    }
    internal static List<Bon> Price(RObj hero)
    {
        if (hero?.dbObj == null || UpgradeSystem.instance == null) return null;
        // The native price appends gold; merge repeated keys before checking affordability.
        var source = UpgradeSystem.instance.GetPrice(hero, "upgrade");
        var result = new List<Bon>();
        foreach (var cost in source)
        {
            if (string.IsNullOrEmpty(cost.Key) || cost.Value <= 0) continue;
            var existing = result.Find(c => c.Key == cost.Key);
            if (existing != null) existing.Value = (int)Math.Min(int.MaxValue, (long)existing.Value + cost.Value);
            else result.Add(new Bon { Key = cost.Key, Value = cost.Value });
        }
        return result;
    }
    public void Refresh()
    {
        if (parts == null) parts = GetComponentsInChildren<HeroDetailsPart>(true);
        foreach (var part in parts) if (part != null) part.Refresh();
        if (holder == null) holder = GetComponentInParent<ObjHolder>(true);
        var hero = holder != null ? holder.obj : null;
        var price = Price(hero);
        bool capped = hero != null && maxLevel > 0 && hero.GetPar("level") >= maxLevel;
        if (upgradeButton != null) upgradeButton.interactable = !busy && !capped && IsOwned(hero) && price != null && MainStates.instance.HaveAmount(price);
    }
    public void Upgrade()
    {
        if (busy) return;
        if (holder == null) holder = GetComponentInParent<ObjHolder>(true);
        var hero = holder != null ? holder.obj : null;
        if (!IsOwned(hero)) return;
        busy = true; int bought = 0; var state = MainStates.instance;
        try
        {
            int count = maxToggle != null && maxToggle.isOn ? Mathf.Clamp(maxLevelsPerClick, 1, 1000) : 1;
            for (int i = 0; i < count; i++)
            {
                if (!IsOwned(hero) || (maxLevel > 0 && hero.GetPar("level") >= maxLevel)) break;
                var price = Price(hero);
                if (price == null || !state.HaveAmount(price)) break;
                state.DelItems(price); hero.ChangePar("level", 1); bought++;
                EventManager.INV("evt_full_upgrade", new ArgPass { who = hero });
            }
            if (bought > 0) { if (state.useSave) state.Save(); UIfiller.GlobalRefresh(); }
        }
        finally { busy = false; Refresh(); }
    }
}
