using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays one configured message and its reward state.</summary>
public sealed class MailItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text rewardAmountText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimedState;
    [SerializeField] private CanvasGroup contentGroup;

    private MailController owner;
    private FormatMail mail;
    private Button subscribedButton;
    private float nextTimerRefresh;

    public void Bind(MailController controller, FormatMail message)
    {
        owner = controller;
        mail = message;
        BindButton();

        string header = Localize(mail != null ? mail.header : null);
        string description = Localize(mail != null ? mail.description : null);
        if (headerText != null)
            headerText.text = descriptionText != null || string.IsNullOrEmpty(description)
                ? header
                : string.IsNullOrEmpty(header) ? description : "<color=#FF00C0>" + header + "</color>" + "\n" + description;
        if (descriptionText != null)
            descriptionText.text = description;

        var rewardText = new StringBuilder();
        Sprite firstIcon = null;
        if (mail != null && mail.rewards != null)
        {
            foreach (var reward in mail.rewards)
            {
                if (reward == null || string.IsNullOrEmpty(reward.Key))
                    continue;
                if (rewardText.Length > 0)
                    rewardText.Append("  ·  ");
                rewardText.Append(reward.Value).Append(" × ").Append(Localize(reward.Key));
                if (firstIcon == null && ResourceHolder.instance != null)
                    firstIcon = ResourceHolder.instance.GetIcon(reward.Key);
            }
        }

        if (rewardAmountText != null)
            rewardAmountText.text = rewardText.ToString();
        if (rewardIcon != null)
        {
            rewardIcon.sprite = firstIcon;
            rewardIcon.enabled = firstIcon != null;
            rewardIcon.preserveAspect = true;
        }

        RefreshState();
    }

    public void RefreshState()
    {
        bool taken = mail != null && owner != null && owner.IsRewardTaken(mail.id);
        if (claimedState != null)
            claimedState.SetActive(taken);
        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(!taken);
            claimButton.interactable = mail != null && owner != null && !taken;
        }
        if (contentGroup != null)
            contentGroup.alpha = taken ? 0.6f : 1f;
        RefreshTimer();
    }

    private void OnEnable()
    {
        if (mail != null)
            RefreshState();
    }

    private void Update()
    {
        if (mail != null && Time.unscaledTime >= nextTimerRefresh)
            RefreshTimer();
    }

    private void RefreshTimer()
    {
        nextTimerRefresh = Time.unscaledTime + 1f;
        if (timerText == null)
            return;

        timerText.text = string.Empty;
        if (mail == null)
            return;

        long now = DateTime.Now.Ticks;
        if (mail.startDate > now && mail.startDate <= DateTime.MaxValue.Ticks)
        {
            timerText.text = "From " + new DateTime(mail.startDate).ToString("dd.MM.yyyy");
            return;
        }
        if (mail.endDate <= 0 || mail.endDate > DateTime.MaxValue.Ticks)
            return;
        if (mail.endDate <= now)
        {
            timerText.text = Localize("Expired");
            return;
        }

        var remaining = TimeSpan.FromTicks(mail.endDate - now);
        timerText.text = remaining.Days > 0
            ? remaining.Days + "d " + remaining.Hours + "h"
            : remaining.Hours + "h " + remaining.Minutes + "m";
    }

    private void BindButton()
    {
        if (subscribedButton == claimButton)
            return;
        if (subscribedButton != null)
            subscribedButton.onClick.RemoveListener(Claim);
        subscribedButton = claimButton;
        if (subscribedButton != null)
            subscribedButton.onClick.AddListener(Claim);
    }

    private void Claim()
    {
        if (mail != null && owner != null && owner.ClaimMail(mail.id))
            RefreshState();
    }

    private void OnDestroy()
    {
        if (subscribedButton != null)
            subscribedButton.onClick.RemoveListener(Claim);
    }

    private static string Localize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        var config = ConfigLoader.Instance;
        if (config != null && config.doctLoc != null && config.doctLoc.ContainsKey(value.ToLower()))
            return config.GetMeLocale(value);
        return value;
    }
}
