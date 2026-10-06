using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterTraitController : MonoBehaviour
{
    [SerializeField] private CharacterTraitDefinition[] traits = CharacterTraitDefinition.Defaults();
    [SerializeField] private CharacterTraitNode[] nodes;
    [SerializeField] private CharacterTraitEnhance enhance;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMPro.TMP_Text balanceText;
    [SerializeField] private TMPro.TMP_Text goldBalanceText;
    [SerializeField] private Image goldBalanceIcon;
    [SerializeField] private TMPro.TMP_Text res1BalanceText;
    [SerializeField] private Image res1BalanceIcon;

    private CharacterTraitDefinition pending;
    private RObj pendingPlayer;
    private PlayerData pendingData;
    private int pendingLevel;
    private bool upgrading;
    private Coroutine refreshRoutine;

    public Color colorCan = Color.white;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (nodes != null)
            for (int i = 0; i < nodes.Length; i++)
                if (nodes[i] != null) nodes[i].Bind(this, i);
        if (enhance != null) enhance.Bind(this);
        CancelUpgrade();
    }

    private void OnEnable() { refreshRoutine = StartCoroutine(RefreshWhileVisible()); }
    private void OnDisable()
    {
        if (refreshRoutine != null) StopCoroutine(refreshRoutine);
        refreshRoutine = null;
        CancelUpgrade();
    }
    private void OnDestroy() { if (closeButton != null) closeButton.onClick.RemoveListener(Hide); }

    // Also handles the player becoming available after this panel's OnEnable and changes in resources.
    private IEnumerator RefreshWhileVisible()
    {
        var interval = new WaitForSecondsRealtime(0.25f);
        while (true) { Refresh(); yield return interval; }
    }

    public void Show() { gameObject.SetActive(true); transform.SetAsLastSibling(); Refresh(); }
    public void Hide() { CancelUpgrade(); gameObject.SetActive(false); }

    public void RequestUpgrade(string parameter)
    {
        if (traits == null) return;
        if (parameter == "crit_dmg") parameter = "crit_damage";
        for (int i = 0; i < traits.Length; i++)
            if (traits[i] != null && traits[i].Parameter == parameter) { RequestUpgrade(i); return; }
    }

    public void RequestUpgrade(int index)
    {
        if (upgrading || enhance == null || traits == null || index < 0 || index >= traits.Length ||
            traits[index] == null || !traits[index].IsValid || !TryGetPlayer(out var state, out var player)) return;
        pending = traits[index];
        pendingPlayer = player;
        pendingData = state.playerData;
        pendingLevel = CharacterTraitProgression.GetLevel(pendingData, pending);
        enhance.gameObject.SetActive(true);
        enhance.transform.SetAsLastSibling();
        Refresh();
    }

    public void ConfirmUpgrade()
    {
        if (upgrading || pending == null || !TryGetPlayer(out var state, out var player)) return;
        if (player != pendingPlayer || state.playerData != pendingData)
        { CancelUpgrade(); return; }
        upgrading = true;
        try
        {
            if (!CharacterTraitProgression.TryUpgrade(state.playerData, player, pending, pendingLevel))
            { Refresh(); return; }
            var completed = pending;
            CancelUpgrade(); // Invalidate the confirmation before callbacks/save can re-enter.
            try
            {
                ModelStatistics.instance.SetStatValue(completed.StatKey,
                    CharacterTraitProgression.GetLevel(state.playerData, completed));
            }
            finally
            {
                // The completed purchase must persist even if a task/UI notification throws.
                state.Save();
                PlayerPrefs.Save();
                Refresh();
            }
        }
        finally { upgrading = false; }
    }

    public void CancelUpgrade()
    {
        pending = null;
        pendingPlayer = null;
        pendingData = null;
        if (enhance != null) enhance.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        bool ready = TryGetPlayer(out var state, out var player);
        if (goldBalanceText != null && res1BalanceText != null)
        {
            if (balanceText != null) balanceText.text = "";
            RefreshBalance("gold", goldBalanceText, goldBalanceIcon, player, ready);
            RefreshBalance("res1", res1BalanceText, res1BalanceIcon, player, ready);
        }
        else if (balanceText != null) balanceText.text = ready
            ? CharacterTraitProgression.GetAmount(player, "gold") + " gold     " + CharacterTraitProgression.GetAmount(player, "res1") + " res1"
            : "Loading...";
        if (nodes != null)
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] == null) continue;
                var trait = traits != null && i < traits.Length ? traits[i] : null;
                nodes[i].Refresh(trait, CharacterTraitProgression.GetLevel(state?.playerData, trait), ready);
            }
        if (pending == null || enhance == null) return;
        if (!ready || player != pendingPlayer || state.playerData != pendingData)
        { CancelUpgrade(); return; }
        int level = CharacterTraitProgression.GetLevel(state.playerData, pending);
        if (level != pendingLevel) { CancelUpgrade(); return; }
        enhance.Refresh(pending, level, player, CharacterTraitProgression.CanAfford(player, pending));
    }

    private static void RefreshBalance(string itemId, TMPro.TMP_Text amountText, Image icon, RObj player, bool ready)
    {
        var sprite = ResourceHolder.instance != null ? ResourceHolder.instance.GetIcon(itemId) : null;
        if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; }
        string amount = ready ? CharacterTraitProgression.GetAmount(player, itemId).ToString() : "...";
        amountText.text = sprite != null ? amount : amount + " " + itemId;
    }

    private static bool TryGetPlayer(out MainStates state, out RObj player)
    {
        state = MainStates.instance;
        player = null;
        return state != null && ModelStatistics.instance != null && state.playerData?.playerStats != null &&
               state.all != null && state.all.TryGetValue("main_player", out player) && player != null;
    }
}
