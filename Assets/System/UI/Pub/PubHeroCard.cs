using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PubHeroCard : MonoBehaviour
{
    public Image portrait, rarityLine, highlight;
    public TMP_Text heroName, chance, owned, quantity, rarityLabel;
    public GameObject newBadge;
    private GameObject ownedProgress;
    private TMP_Text levelLabel;
    private Image shardIcon;
    private TMP_Text mileageLabel;
    private Image mileageIcon;
    private Vector3 originalScale;
    private float pulse;
    public void Bind(PubHero hero, RObj player, int offerCount, int multiplier = 1)
    {
        EnsureMileageView();
        EnsureOwnedView();
        if (originalScale == Vector3.zero) originalScale = transform.localScale;
        if (hero == null)
        {
            heroName.text = "No hero";chance.text = "";owned.text = "";portrait.enabled = false;
            newBadge.SetActive(false);ownedProgress.SetActive(false);mileageLabel.transform.parent.gameObject.SetActive(false);return;
        }
        heroName.text = hero.Name;
        portrait.sprite = FindPortrait(hero); portrait.enabled = portrait.sprite != null;
        chance.text = (100f / Mathf.Max(1, offerCount)).ToString("0.0") + "%";
        int displayedMultiplier = Mathf.Clamp(multiplier, 1, 3);
        quantity.text = "×" + (hero.Quantity * displayedMultiplier);
        mileageLabel.transform.parent.gameObject.SetActive(true);
        mileageLabel.text = (hero.MileageTokens * displayedMultiplier).ToString();
        mileageIcon.sprite = ResourceHolder.instance != null ? ResourceHolder.instance.GetIcon("mileage_token") : null;
        mileageIcon.enabled = mileageIcon.sprite != null;
        int level = 0;
        long shards = 0;
        string shardItemId = "shard_" + hero.Id;
        if (player?.inventory != null)
            foreach (var item in player.inventory)
            {
                if (item?.dbObj == null) continue;
                if (item.dbObj.ID == hero.Id && item.it == ItemType.monster && string.IsNullOrEmpty(item.shardID))
                    level = Mathf.Max(level, Mathf.Max(1, (int)item.GetPar("level")));
                if ((item.dbObj.ID == "shard" && item.shardID == hero.Id) || item.dbObj.ID == shardItemId)
                    shards += Mathf.Max(0, (int)item.GetPar("amount"));
            }
        owned.text = shards.ToString("N0");
        levelLabel.text = level.ToString();
        var resources = ResourceHolder.instance;
        shardIcon.sprite = resources != null && resources.items.TryGetValue("shard", out var shardSprite) ? shardSprite : null;
        shardIcon.enabled = shardIcon.sprite != null;
        ownedProgress.SetActive(level > 0); owned.gameObject.SetActive(level > 0);
        newBadge.SetActive(level == 0);
        var holder = ResourceHolder.instance;
        Color color = new Color(.83f,.66f,.42f);
        if (holder != null && holder.rareColors.TryGetValue(hero.Rarity, out var configured)) color = configured;
        color.a = 1;
        if (rarityLine != null) rarityLine.color = color;
        if (rarityLabel != null) rarityLabel.text = holder != null && holder.rareString.TryGetValue(hero.Rarity,out var label) ? label : "COMPANION";
    }
    public static Sprite FindPortrait(PubHero hero)
    {
        var holder = ResourceHolder.instance;
        if (hero == null || holder == null) return null;
        string id = hero.Definition != null ? hero.Definition.ID : hero.Id;
        if (!string.IsNullOrEmpty(id) && holder.avas.TryGetValue(id, out var sprite) && sprite != null)
            return sprite;
        return holder.defaultTaskIcon;
    }
    // Also upgrades existing scene/prefab instances without rebuilding or resetting Inspector settings.
    public void EnsureMileageView()
    {
        if (mileageLabel != null) return;
        var row = transform.Find("MileageReward") as RectTransform;
        if (row == null)
        {
            row = new GameObject("MileageReward", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(transform, false);
            row.anchorMin = row.anchorMax = new Vector2(.5f, 0);
            row.pivot = new Vector2(.5f, .5f);
            row.anchoredPosition = new Vector2(0, -42);
            row.sizeDelta = new Vector2(170, 48);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(row, false);
            var image = icon.GetComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(38, 38);
            image.rectTransform.anchoredPosition = new Vector2(-35, 0);
            var label = new GameObject("Amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(row, false);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = quantity.font; text.fontSize = 30; text.color = quantity.color;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(80, 46);
            text.rectTransform.anchoredPosition = new Vector2(30, 0);
        }
        mileageIcon = row.Find("Icon").GetComponent<Image>();
        mileageLabel = row.Find("Amount").GetComponent<TMP_Text>();
    }
    public void EnsureOwnedView()
    {
        if (ownedProgress != null) return;
        var existing = transform.Find("OwnedProgress");
        if (existing == null)
        {
            var source = newBadge.GetComponent<Image>();
            var row = new GameObject("OwnedProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            row.transform.SetParent(transform, false);
            var rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = owned.rectTransform.anchoredPosition;
            rect.sizeDelta = new Vector2(240, 44);
            var background = row.GetComponent<Image>();
            background.sprite = source.sprite; background.type = source.type;
            background.color = new Color(.18f, .22f, .22f); background.raycastTarget = false;
            var badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badge.transform.SetParent(rect, false);
            var badgeImage = badge.GetComponent<Image>();
            badgeImage.sprite = source.sprite; badgeImage.type = source.type;
            badgeImage.color = quantity.color; badgeImage.raycastTarget = false;
            badgeImage.rectTransform.sizeDelta = new Vector2(58, 44);
            badgeImage.rectTransform.anchoredPosition = new Vector2(-91, 0);
            var level = new GameObject("Level", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            level.transform.SetParent(badge.transform, false);
            var label = level.GetComponent<TextMeshProUGUI>();
            label.font = owned.font; label.fontSize = 27; label.fontStyle = FontStyles.Bold;
            label.color = new Color(.16f, .13f, .10f); label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false; label.text = "1";
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            var icon = new GameObject("ShardIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(rect, false);
            var image = icon.GetComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(30, 30);
            image.rectTransform.anchoredPosition = new Vector2(-37, 0);
            owned.rectTransform.SetParent(rect, false);
            owned.rectTransform.anchorMin = owned.rectTransform.anchorMax = new Vector2(.5f, .5f);
            owned.rectTransform.anchoredPosition = new Vector2(43, 0);
            owned.rectTransform.sizeDelta = new Vector2(118, 42);
            owned.alignment = TextAlignmentOptions.Center; owned.color = quantity.color;
            owned.enableAutoSizing = true; owned.fontSizeMax = 26; owned.fontSizeMin = 16;
            owned.raycastTarget = false; owned.text = "0";
            row.SetActive(false);
            existing = rect;
        }
        ownedProgress = existing.gameObject;
        levelLabel = existing.Find("LevelBadge/Level").GetComponent<TMP_Text>();
        shardIcon = existing.Find("ShardIcon").GetComponent<Image>();
    }
    public void Pulse() { pulse = 1; if (originalScale == Vector3.zero) originalScale = transform.localScale; }
    private void Update()
    {
        if (pulse <= 0) return;
        pulse = Mathf.Max(0, pulse - Time.unscaledDeltaTime * 2);
        transform.localScale = originalScale * (1 + .07f * Mathf.Sin(pulse * Mathf.PI));
        if (highlight != null) { var color=highlight.color;color.a=pulse*.5f;highlight.color=color; }
    }
    private void OnDisable()
    {
        pulse=0;if(originalScale!=Vector3.zero)transform.localScale=originalScale;
        if(highlight!=null){var color=highlight.color;color.a=0;highlight.color=color;}
    }
}
