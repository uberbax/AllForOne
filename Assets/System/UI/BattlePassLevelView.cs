using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattlePassLevelView : MonoBehaviour
{
    [Serializable]
    public sealed class RewardTrack
    {
        public Button button;
        public Image icon;
        public TMP_Text amount;
        public GameObject normal, complete, check, locked, dim;

        public void Fill(List<Bon> rewards)
        {
            var text = new StringBuilder();
            Sprite sprite = null;
            foreach (var reward in rewards)
            {
                if (text.Length > 0) text.Append("\n");
                text.Append(reward.Value);
                if (rewards.Count > 1) text.Append(" × ").Append(reward.Key);
                if (sprite == null && ResourceHolder.instance != null) sprite = ResourceHolder.instance.GetIcon(reward.Key);
            }
            if (amount != null) amount.text = text.ToString();
            if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; icon.preserveAspect = true; }
        }
        public void State(bool eligible, bool taken, bool hasRewards)
        {
            if (button != null) button.interactable = eligible && !taken && hasRewards;
            if (normal != null) normal.SetActive(!taken);
            if (complete != null) complete.SetActive(taken);
            if (check != null) check.SetActive(taken);
            if (locked != null) locked.SetActive(!eligible && !taken && hasRewards);
            if (dim != null) dim.SetActive(!eligible || taken || !hasRewards);
        }
    }
    [SerializeField] private string taskId;
    [SerializeField] private RewardTrack free = new RewardTrack();
    [SerializeField] private RewardTrack premium = new RewardTrack();
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text thresholdText;
    [SerializeField] private GameObject reachedStep;
    [SerializeField] private GameObject lockedStep;
    private BattlePassController owner;
    private ElTasko task;
    private bool subscribed;
    private ResourceHolder iconsFrom;
    public string TaskId => taskId;

    public void Bind(BattlePassController controller, ElTasko stage, int number)
    {
        owner = controller; task = stage; taskId = stage.id;
        if (stageText != null) stageText.text = number.ToString();
        if (thresholdText != null) thresholdText.text = BattlePassController.RequiredPoints(task).ToString();
        if (!subscribed)
        {
            if (free.button != null) free.button.onClick.AddListener(ClaimFree);
            if (premium.button != null) premium.button.onClick.AddListener(ClaimPremium);
            subscribed = true;
        }
        FillRewards(); RefreshState();
    }
    private void FillRewards()
    {
        free.Fill(task.rewards); premium.Fill(task.rewardsPremium); iconsFrom = ResourceHolder.instance;
    }
    public void RefreshState()
    {
        if (task == null || owner == null) return;
        if (iconsFrom != ResourceHolder.instance) FillRewards();
        bool reached = owner.Points >= BattlePassController.RequiredPoints(task);
        free.State(reached, owner.IsTaken(taskId, false), task.rewards.Count > 0);
        premium.State(reached && owner.HasPremium, owner.IsTaken(taskId, true), task.rewardsPremium.Count > 0);
        if (reachedStep != null) reachedStep.SetActive(reached);
        if (lockedStep != null) lockedStep.SetActive(!reached);
    }
    private void ClaimFree() => owner?.Claim(taskId, false);
    private void ClaimPremium() => owner?.Claim(taskId, true);
    private void OnDestroy()
    {
        if (!subscribed) return;
        if (free.button != null) free.button.onClick.RemoveListener(ClaimFree);
        if (premium.button != null) premium.button.onClick.RemoveListener(ClaimPremium);
    }
}
