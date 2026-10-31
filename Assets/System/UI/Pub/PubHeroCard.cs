using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(ObjHolder))]
public sealed class PubHeroCard : MonoBehaviour
{
    public Image portrait, rarityLine, highlight;
    public TMP_Text heroName, chance, owned, quantity, rarityLabel;
    public GameObject newBadge;
    private ObjHolder objectHolder;
    private GameObject ownedProgress;
    private GoAscend ascensionView;
    private TMP_Text mileageLabel;
    private Image mileageIcon;
    private Vector3 originalScale;
    private float pulse;
    private bool rouletteSelected;
    private bool recruitmentBusy, ascensionWasEnabled, wasInteractable, wasBlocking;
    private CanvasGroup interactionGroup;
    public string HeroId { get; private set; }
    private void Awake()
    {
        var holder = EnsureObjHolder();
        // Reserve an empty UI value before ObjHolder.OnEnable can resolve its serialized asDBObj.
        if (holder.obj == null) holder.obj = new RObj { RID = "" };
    }

    public void Bind(PubHero hero, RObj player, int offerCount, int multiplier = 1)
    {
        HeroId = hero?.Id;
        var cardHolder = EnsureObjHolder();
        EnsureMileageView();
        EnsureOwnedView();
        if (originalScale == Vector3.zero) originalScale = transform.localScale;
        if (hero == null)
        {
            cardHolder.asDBObj = ""; cardHolder.obj = null;
            heroName.text = "No hero";chance.text = "";owned.text = "";portrait.enabled = false;
            newBadge.SetActive(false);if (ascensionView != null) ascensionView.Refresh();mileageLabel.transform.parent.gameObject.SetActive(false);return;
        }
        if (cardHolder.asDBObj != hero.Id || cardHolder.obj == null || cardHolder.obj.dbObj != hero.Definition)
        {
            cardHolder.asDBObj = hero.Id;
            // A UI-only definition view prevents ObjHolder's asDBObj fallback from spawning a world monster.
            cardHolder.obj = new RObj
            {
                RID = hero.Id, dbID = hero.Id, dbObj = hero.Definition, it = ItemType.monster,
                curPars = hero.Definition != null
                    ? new System.Collections.Generic.Dictionary<string, float>(hero.Definition.pars)
                    : new System.Collections.Generic.Dictionary<string, float>()
            };
        }
        heroName.text = hero.Name;
        portrait.sprite = FindPortrait(hero); portrait.enabled = portrait.sprite != null;
        chance.text = (100f / Mathf.Max(1, offerCount)).ToString("0.0") + "%";
        int displayedMultiplier = Mathf.Clamp(multiplier, 1, 3);
        quantity.text = "×" + (hero.Quantity * displayedMultiplier);
        mileageLabel.transform.parent.gameObject.SetActive(true);
        mileageLabel.text = (hero.TotalMileageTokens * displayedMultiplier).ToString();
        mileageIcon.sprite = ResourceHolder.instance != null ? ResourceHolder.instance.GetIcon("mileage_token") : null;
        mileageIcon.enabled = mileageIcon.sprite != null;
        var ownedHero = HeroAscension.FindOwned(player, hero.Id);
        if (ownedHero != null) cardHolder.obj = ownedHero;
        if (ascensionView != null) ascensionView.Refresh();
        var holder = ResourceHolder.instance;
        Color color = PubUiTheme.Blue;
        if (holder != null && holder.rareColors.TryGetValue(hero.Rarity, out var configured)) color = configured;
        color.a = 1;
        if (rarityLine != null) rarityLine.color = color;
        if (rarityLabel != null) rarityLabel.text = holder != null && holder.rareString.TryGetValue(hero.Rarity,out var label) ? label : "COMPANION";
    }
    public ObjHolder EnsureObjHolder()
    {
        if (objectHolder == null && !TryGetComponent(out objectHolder))
            objectHolder = gameObject.AddComponent<ObjHolder>();
        objectHolder.noTrack = true;
        return objectHolder;
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
        if (ascensionView != null) return;
        ascensionView = GetComponentInChildren<GoAscend>(true);
        if (ascensionView == null)
        {
            var prefab = Resources.Load<GameObject>(GoAscend.ResourcePath);
            if (prefab == null) return;
            ascensionView = Instantiate(prefab, transform, false).GetComponent<GoAscend>();
            ascensionView.name = "HeroAscension";
            var rect = (RectTransform)ascensionView.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, -274);
            var legacy = transform.Find("OwnedProgress");
            if (legacy != null) legacy.gameObject.SetActive(false);
        }
        ownedProgress = ascensionView.gameObject;
        owned = ascensionView.ShardLabel;
        ascensionView.SetNotOwnedVisual(newBadge);
    }
    public void Pulse() { pulse = 1; if (originalScale == Vector3.zero) originalScale = transform.localScale; }
    public void SetRouletteHighlight(bool selected)
    {
        rouletteSelected = selected; pulse = 0;
        if (originalScale == Vector3.zero) originalScale = transform.localScale;
        transform.localScale = originalScale * (selected ? 1.045f : 1f);
        if (highlight != null)
        {
            var color = PubUiTheme.Blue; color.a = selected ? .9f : 0;
            highlight.color = color; highlight.raycastTarget = false;
        }
    }
    public void SetRecruitmentBusy(bool value)
    {
        if (recruitmentBusy == value) return;
        recruitmentBusy = value;
        if (value)
        {
            if (interactionGroup == null && !TryGetComponent(out interactionGroup)) interactionGroup = gameObject.AddComponent<CanvasGroup>();
            wasInteractable = interactionGroup.interactable; wasBlocking = interactionGroup.blocksRaycasts;
            interactionGroup.interactable = interactionGroup.blocksRaycasts = false;
            ascensionWasEnabled = ascensionView != null && ascensionView.enabled;
            // Preserve the displayed shard count until the roulette reveals the committed reward.
            if (ascensionWasEnabled) ascensionView.enabled = false;
        }
        else
        {
            if (interactionGroup != null) { interactionGroup.interactable = wasInteractable; interactionGroup.blocksRaycasts = wasBlocking; }
            if (ascensionView != null && ascensionWasEnabled) ascensionView.enabled = true;
        }
    }
    private void Update()
    {
        if (rouletteSelected) return;
        if (pulse <= 0) return;
        pulse = Mathf.Max(0, pulse - Time.unscaledDeltaTime * 2);
        transform.localScale = originalScale * (1 + .07f * Mathf.Sin(pulse * Mathf.PI));
        if (highlight != null) { var color=highlight.color;color.a=pulse*.5f;highlight.color=color; }
    }
    private void OnDisable()
    {
        SetRecruitmentBusy(false);
        rouletteSelected = false;
        pulse=0;if(originalScale!=Vector3.zero)transform.localScale=originalScale;
        if(highlight!=null){var color=highlight.color;color.a=0;highlight.color=color;}
    }
}
