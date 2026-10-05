using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays configured mailbox messages and claims their rewards once.</summary>
public sealed class MailController : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private MailItemView rowPrefab;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private Button claimAllButton;
    [SerializeField] private Button closeButton;

    private List<FormatMail> mails = new List<FormatMail>();
    private readonly List<MailItemView> rows = new List<MailItemView>();
    private bool claiming;

    public IReadOnlyList<FormatMail> AllMails => mails;

    private void Awake()
    {
        if (claimAllButton != null) claimAllButton.onClick.AddListener(ClaimAll);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshMails();
    }

    private void Start()
    {
        // ConfigLoader resets its static callbacks in Awake, so subscribe again after all Awakes.
        Subscribe();
        RefreshMails();
    }

    private void Subscribe()
    {
        ConfigLoader.onParseEnded -= RefreshMails;
        ConfigLoader.onParseEnded += RefreshMails;
    }

    private void OnDisable() => ConfigLoader.onParseEnded -= RefreshMails;

    private void OnDestroy()
    {
        ConfigLoader.onParseEnded -= RefreshMails;
        if (claimAllButton != null) claimAllButton.onClick.RemoveListener(ClaimAll);
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RefreshMails();
    }

    public void Hide() => gameObject.SetActive(false);

    public void RefreshMails()
    {
        mails = ConfigLoader.Instance != null && ConfigLoader.Instance.allMails != null
            ? ConfigLoader.Instance.allMails
            : new List<FormatMail>();
        if (content == null || rowPrefab == null) return;

        foreach (var row in rows)
        {
            if (row == null) continue;
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        rows.Clear();
        var ids = new HashSet<string>();
        foreach (var mail in mails)
        {
            if (mail == null || string.IsNullOrEmpty(mail.id) || !ids.Add(mail.id)) continue;
            var row = Instantiate(rowPrefab, content);
            row.Bind(this, mail);
            row.gameObject.SetActive(true);
            rows.Add(row);
        }
        if (emptyState != null) emptyState.SetActive(rows.Count == 0);
        RefreshState();
    }

    public bool IsRewardTaken(string mailId)
    {
        return !string.IsNullOrEmpty(mailId) && ModelStatistics.instance != null &&
               MainStates.instance != null && MainStates.instance.playerData != null &&
               ModelStatistics.instance.GetStatValue(GetTakenStat(mailId), false) > 0;
    }

    /// <summary>Gives the reward and records mail_{id} only after the grant succeeds.</summary>
    public bool ClaimMail(string mailId)
    {
        if (claiming || string.IsNullOrEmpty(mailId) || MainStates.instance == null ||
            MainStates.instance.playerData == null || ModelStatistics.instance == null)
            return false;
        var mail = mails.Find(item => item != null && item.id == mailId);
        if (mail == null || IsRewardTaken(mailId)) return false;
        claiming = true;
        try
        {
            if (mail.rewards != null && mail.rewards.Count > 0)
                MainStates.instance.AddItems(mail.rewards);
            ModelStatistics.instance.SetStatValue(GetTakenStat(mailId), 1);
        }
        finally { claiming = false; }
        RefreshState();
        return true;
    }

    public void ClaimAll()
    {
        // The copy is stable even if a reward callback refreshes the mailbox.
        var pending = new List<FormatMail>(mails);
        foreach (var mail in pending)
            if (mail != null) ClaimMail(mail.id);
    }

    private void RefreshState()
    {
        foreach (var row in rows)
            if (row != null) row.RefreshState();
        if (claimAllButton != null)
            claimAllButton.interactable = mails.Exists(mail => mail != null &&
                !string.IsNullOrEmpty(mail.id) && !IsRewardTaken(mail.id));
    }

    private static string GetTakenStat(string mailId) => "mail_" + mailId;
}
