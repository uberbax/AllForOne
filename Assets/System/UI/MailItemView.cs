using System;
using System.Collections.Generic;
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
    [SerializeField] private RectTransform rewardItemsRoot;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimedState;
    [SerializeField] private CanvasGroup contentGroup;

    private MailController owner;
    private FormatMail mail;
    private Button subscribedButton;
    private float nextTimerRefresh;
    private readonly List<TMP_Text> rewardAmounts = new List<TMP_Text>();
    private readonly List<Image> rewardIcons = new List<Image>();
    private ResourceHolder iconsFrom;

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

        EnsureRewardLayout();
        int rewardIndex = 0;
        if (mail != null && mail.rewards != null)
        {
            foreach (var reward in mail.rewards)
            {
                if (reward == null || string.IsNullOrEmpty(reward.Key))
                    continue;
                if (rewardItemsRoot == null) break;
                if (rewardIndex >= rewardAmounts.Count)
                {
                    var item = Instantiate(rewardAmountText.transform.parent.gameObject, rewardItemsRoot);
                    rewardAmounts.Add(item.GetComponentInChildren<TMP_Text>(true));
                    rewardIcons.Add(item.GetComponentInChildren<Image>(true));
                }
                rewardAmounts[rewardIndex].transform.parent.gameObject.SetActive(true);
                rewardAmounts[rewardIndex].text = reward.Value.ToString();
                SetRewardIcon(rewardIndex, reward.Key);
                rewardIndex++;
            }
        }

        for (int i = rewardIndex; i < rewardAmounts.Count; i++)
            rewardAmounts[i].transform.parent.gameObject.SetActive(false);
        iconsFrom = ResourceHolder.instance;

        RefreshState();
    }

    // Also upgrades existing scene instances that still have the old serialized text/icon references.
    private void EnsureRewardLayout()
    {
        if (rewardAmountText == null || rewardIcon == null) return;
        if (rewardItemsRoot == null)
        {
            var original = rewardAmountText.rectTransform;
            rewardItemsRoot = new GameObject("RewardItemText", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            rewardItemsRoot.gameObject.layer = gameObject.layer;
            rewardItemsRoot.SetParent(original.parent, false);
            rewardItemsRoot.anchorMin = original.anchorMin;
            rewardItemsRoot.anchorMax = original.anchorMax;
            rewardItemsRoot.pivot = original.pivot;
            rewardItemsRoot.anchoredPosition = original.anchoredPosition - new Vector2(80, 0);
            rewardItemsRoot.sizeDelta = original.sizeDelta + new Vector2(80, 0);
            var layout = rewardItemsRoot.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;

            var item = new GameObject("RewardItem", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            item.layer = gameObject.layer;
            item.transform.SetParent(rewardItemsRoot, false);
            var itemLayout = item.GetComponent<HorizontalLayoutGroup>();
            itemLayout.spacing = 8; itemLayout.childAlignment = TextAnchor.MiddleLeft;
            itemLayout.childControlWidth = itemLayout.childControlHeight = true;
            itemLayout.childForceExpandWidth = itemLayout.childForceExpandHeight = false;
            var size = item.GetComponent<LayoutElement>();
            size.minWidth = 96; size.preferredWidth = 140; size.flexibleWidth = 1; size.preferredHeight = 64;
            rewardIcon.transform.SetParent(item.transform, false);
            var iconSize = rewardIcon.GetComponent<LayoutElement>() ?? rewardIcon.gameObject.AddComponent<LayoutElement>();
            iconSize.minWidth = iconSize.preferredWidth = 48;
            iconSize.minHeight = iconSize.preferredHeight = 48;
            rewardIcon.raycastTarget = false;
            rewardAmountText.transform.SetParent(item.transform, false);
            var textSize = rewardAmountText.GetComponent<LayoutElement>() ?? rewardAmountText.gameObject.AddComponent<LayoutElement>();
            textSize.minWidth = 0; textSize.flexibleWidth = 1; textSize.preferredHeight = 64;
            rewardAmountText.enableAutoSizing = true;
            rewardAmountText.fontSizeMin = 16; rewardAmountText.fontSizeMax = 28;
            rewardAmountText.alignment = TextAlignmentOptions.MidlineLeft;
            rewardAmountText.raycastTarget = false;
        }
        if (rewardAmounts.Count > 0) return;
        foreach (Transform item in rewardItemsRoot)
        {
            var amount = item.GetComponentInChildren<TMP_Text>(true);
            var icon = item.GetComponentInChildren<Image>(true);
            if (amount == null || icon == null) continue;
            rewardAmounts.Add(amount); rewardIcons.Add(icon);
        }
    }

    private void SetRewardIcon(int index, string itemId)
    {
        var icon = rewardIcons[index];
        icon.sprite = ResourceHolder.instance != null ? ResourceHolder.instance.GetIcon(itemId) : null;
        icon.enabled = icon.sprite != null;
        icon.preserveAspect = true;
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
        if (mail != null && iconsFrom != ResourceHolder.instance)
            Bind(owner, mail);
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
