using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PubHeroCard : MonoBehaviour
{
    public Image portrait, rarityLine, highlight;
    public TMP_Text heroName, chance, owned, quantity, rarityLabel;
    public GameObject newBadge;
    private TMP_Text mileageLabel;
    private Image mileageIcon;
    private Vector3 originalScale;
    private float pulse;
    public void Bind(PubHero hero, RObj player, int offerCount)
    {
        EnsureMileageView();
        if (originalScale == Vector3.zero) originalScale = transform.localScale;
        if (hero == null)
        {
            heroName.text = "No hero";chance.text = "";owned.text = "";portrait.enabled = false;
            newBadge.SetActive(false);mileageLabel.transform.parent.gameObject.SetActive(false);return;
        }
        heroName.text = hero.Name;
        portrait.sprite = FindPortrait(hero); portrait.enabled = portrait.sprite != null;
        chance.text = (100f / Mathf.Max(1, offerCount)).ToString("0.0") + "%";
        quantity.text = "×" + hero.Quantity;
        mileageLabel.transform.parent.gameObject.SetActive(true);
        mileageLabel.text = hero.MileageTokens.ToString();
        mileageIcon.sprite = ResourceHolder.instance != null ? ResourceHolder.instance.GetIcon("mileage_token") : null;
        mileageIcon.enabled = mileageIcon.sprite != null;
        int count = 0;
        if (player?.inventory != null)
            foreach (var item in player.inventory)
                if (item?.dbObj != null && item.dbObj.ID == hero.Definition.ID && item.it == ItemType.monster && string.IsNullOrEmpty(item.shardID))
                    count += Mathf.Max(1, (int)item.GetPar("amount"));
        owned.text = "Owned: " + count;owned.gameObject.SetActive(count > 0);newBadge.SetActive(count == 0);
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
