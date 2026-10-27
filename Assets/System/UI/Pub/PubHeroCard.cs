using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PubHeroCard : MonoBehaviour
{
    public Image portrait, rarityLine, highlight;
    public TMP_Text heroName, chance, owned, quantity, rarityLabel;
    public GameObject newBadge;
    public PubPortraitLibrary portraits;
    private Vector3 originalScale;
    private float pulse;
    public void Bind(PubHero hero, RObj player, int offerCount)
    {
        if (originalScale == Vector3.zero) originalScale = transform.localScale;
        if (hero == null)
        {
            heroName.text = "No hero";chance.text = "";owned.text = "";portrait.enabled = false;
            newBadge.SetActive(false);return;
        }
        heroName.text = hero.Name;
        portrait.sprite = FindPortrait(hero, portraits); portrait.enabled = portrait.sprite != null;
        chance.text = (100f / Mathf.Max(1, offerCount)).ToString("0.0") + "%";
        quantity.text = "×1";
        int count = 0;
        if (player?.inventory != null)
            foreach (var item in player.inventory)
                if (item?.dbObj != null && item.dbObj.ID == hero.Definition.ID && item.it == ItemType.monster)
                    count += Mathf.Max(1, (int)item.GetPar("amount"));
        owned.text = "Owned: " + count;owned.gameObject.SetActive(count > 0);newBadge.SetActive(count == 0);
        var holder = ResourceHolder.instance;
        Color color = new Color(.83f,.66f,.42f);
        if (holder != null && holder.rareColors.TryGetValue(hero.Rarity, out var configured)) color = configured;
        color.a = 1;
        if (rarityLine != null) rarityLine.color = color;
        if (rarityLabel != null) rarityLabel.text = holder != null && holder.rareString.TryGetValue(hero.Rarity,out var label) ? label : "COMPANION";
    }
    public static Sprite FindPortrait(PubHero hero, PubPortraitLibrary portraits = null)
    {
        var holder = ResourceHolder.instance;
        if (hero == null) return null;
        var cropped = portraits != null ? portraits.Get(hero.Definition.ID) : null;
        if (cropped != null) return cropped;
        if (holder == null) return null;
        if (holder.avas.TryGetValue(hero.Definition.ID, out var sprite) && sprite != null) return sprite;
        if (holder.monsters.TryGetValue(hero.Definition.ID, out var prefab) && prefab != null)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.sprite != null) return renderer.sprite;
        }
        return holder.defaultTaskIcon;
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
