using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GoAscend : MonoBehaviour
{
    public const string ResourcePath = "UI_HeroAscension";
    [SerializeField] private GameObject content;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text levelLabel, shardLabel;
    [SerializeField] private Image shardIcon, progress;
    [SerializeField] private GameObject notOwnedVisual;
    private ObjHolder holder;
    private float nextRefresh;
    private bool busy;
    public TMP_Text ShardLabel => shardLabel;
    public bool IsOwned { get; private set; }
    private void Awake() { button.onClick.AddListener(DoAscend); }
    private void OnEnable() { UIfiller.acts += Refresh; nextRefresh = 0; Refresh(); }
    private void OnDisable() { UIfiller.acts -= Refresh; }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(DoAscend); }
    private void OnTransformParentChanged() { holder = null; }
    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .25f; Refresh();
    }
    public void SetNotOwnedVisual(GameObject visual) { notOwnedVisual = visual; }
    private RObj Resolve(out RObj player)
    {
        player = null;
        if (holder == null) holder = GetComponentInParent<ObjHolder>();
        var candidate = holder != null ? holder.obj : null;
        var state = MainStates.instance;
        if (candidate?.dbObj == null || state == null || !state.HasMain()) return null;
        player = state.mainPlayer;
        if (player.inventory.Contains(candidate) && candidate.it == ItemType.monster && string.IsNullOrEmpty(candidate.shardID)) return candidate;
        return HeroAscension.FindOwned(player, candidate.dbObj.ID);
    }
    public void Refresh()
    {
        if (content == null) return;
        var hero = Resolve(out var player);
        IsOwned = hero != null;
        if (content.activeSelf != IsOwned) content.SetActive(IsOwned);
        bool showNew = !IsOwned && holder != null && holder.obj?.dbObj != null;
        if (notOwnedVisual != null && notOwnedVisual.activeSelf != showNew) notOwnedVisual.SetActive(showNew);
        if (!IsOwned) { button.interactable = false; return; }
        int level = HeroAscension.Level(hero), shards = HeroAscension.Shards(player, hero.dbObj.ID), cost = HeroAscension.NextCost(level);
        levelLabel.SetText("{0}", level);
        shardLabel.SetText("{0}/{1}", shards, cost);
        if (progress != null) progress.fillAmount = Mathf.Clamp01((float)shards / cost);
        button.interactable = !busy && shards >= cost && level < int.MaxValue;
        var resources = ResourceHolder.instance;
        shardIcon.sprite = resources != null && resources.items.TryGetValue("shard", out var sprite) ? sprite : null;
        shardIcon.enabled = shardIcon.sprite != null;
    }
    public void DoAscend()
    {
        if (busy) return;
        busy = true;
        try
        {
            var hero = Resolve(out var player);
            if (!HeroAscension.TryAscend(player, hero, out _)) return;
            var state = MainStates.instance;
            if (state.useSave) state.Save();
            UIfiller.GlobalRefresh();
        }
        finally { busy = false; Refresh(); }
    }
}
