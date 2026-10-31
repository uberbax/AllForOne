using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>Reads the hero exclusively from its nearest parent ObjHolder.</summary>
public sealed class HeroDetailsPart : MonoBehaviour
{
    public enum Content { Header, Stat, Skill, Milestone, Cost, UpgradeStatus }
    [SerializeField] private Content content;
    [SerializeField] private TMP_Text title, value, detail, iconPlaceholder;
    [SerializeField] private Image icon, fill, background;
    [SerializeField] private string parameter;
    [SerializeField] private int index, milestoneLevel = 10;
    [SerializeField] private Sprite fallbackIcon;
    private ObjHolder holder;
    private RObj namedHero;
    private string displayName;
    private static readonly Dictionary<string, Sprite> skillIcons = new Dictionary<string, Sprite>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetIcons() { skillIcons.Clear(); }
    private static Sprite SkillIcon(ResourceHolder resources, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (resources != null && resources.skills.TryGetValue(id, out var configured) && configured != null) return configured;
        if (!skillIcons.TryGetValue(id, out var sprite))
        { sprite = Resources.Load<Sprite>("HeroSkillIcons/" + id); skillIcons[id] = sprite; }
        return sprite;
    }
    private void OnEnable() { Refresh(); }
    private void OnTransformParentChanged() { holder = null; }
    private static void Text(TMP_Text text, string value) { if (text != null && text.text != value) text.text = value; }
    private void Icon(Sprite sprite)
    {
        if (icon == null) return;
        icon.sprite = sprite != null ? sprite : fallbackIcon; icon.enabled = icon.sprite != null;
        if (iconPlaceholder != null) iconPlaceholder.gameObject.SetActive(!icon.enabled);
    }
    public void Refresh()
    {
        if (holder == null) holder = GetComponentInParent<ObjHolder>(true);
        var hero = holder != null ? holder.obj : null;
        if (hero?.dbObj == null)
        {
            Text(value, "—"); Text(detail, ""); Icon(null);
            if (content == Content.Header) Text(title, "Select a hero");
            else if (content == Content.Skill || content == Content.Milestone) Text(title, "—");
            if (fill != null) fill.fillAmount = 0;
            return;
        }
        var resources = ResourceHolder.instance;
        int level = Mathf.Max(1, (int)hero.GetPar("level"));
        switch (content)
        {
            case Content.Header:
                if (namedHero != hero)
                {
                    namedHero = hero; displayName = HeroDetailsController.Pretty(hero.dbObj.ID);
                    if (ConfigLoader.Instance != null)
                        foreach (var row in ConfigLoader.Instance.heroes)
                            if (row.monsterName == hero.dbObj.ID && !string.IsNullOrWhiteSpace(row.displayName)) { displayName = row.displayName; break; }
                }
                Text(title, displayName); Text(value, "LEVEL " + level); Text(detail, "ASCENSION " + HeroAscension.Level(hero));
                Icon(resources != null && resources.avas.TryGetValue(hero.dbObj.ID, out var portrait) ? portrait : null);
                if (fill != null) fill.fillAmount = (level % 10) / 10f;
                break;
            case Content.Stat:
                float stat = parameter == "battle_power"
                    ? hero.GetPar("attack") + (hero.curPars.ContainsKey("health") && hero.curPars.ContainsKey("registered_damage") ? hero.GetPar("health") : hero.GetPar("max_health")) + hero.GetPar("def")
                    : hero.GetPar(parameter);
                Text(value, stat.ToString("0.#")); break;
            case Content.Skill:
            case Content.Milestone:
                bool milestone = content == Content.Milestone;
                int skillIndex = milestone ? HeroDetailsController.SkillIndex(milestoneLevel) : index;
                string id = HeroDetailsController.SkillId(hero, skillIndex);
                int rank = milestone ? HeroDetailsController.SkillRank(milestoneLevel) : HeroDetailsController.UnlockedRank(level, skillIndex);
                bool unlocked = milestone ? level >= milestoneLevel : rank > 0;
                Text(title, HeroDetailsController.Pretty(id)); Text(value, milestone ? "Lv." + milestoneLevel : "SKILL " + (index + 1));
                Text(detail, string.IsNullOrEmpty(id) ? "Not configured" : milestone ? (unlocked ? "UNLOCKED" : "LOCKED") + "  /  RANK " + rank : rank > 0 ? "RANK " + rank : "UNLOCK Lv." + ((index + 1) * 10));
                Icon(SkillIcon(resources, id));
                if (background != null) background.color = unlocked ? new Color(.12f,.39f,.67f) : new Color(.055f,.075f,.11f);
                if (title != null) title.color = unlocked ? Color.white : new Color(.61f,.68f,.77f);
                break;
            case Content.Cost:
                var price = HeroDetailsController.Price(hero); bool visible = price != null && index < price.Count;
                if (background != null) background.enabled = visible;
                if (!visible) { Text(title, ""); Text(value, ""); Text(detail, ""); if(icon != null)icon.enabled = false; break; }
                var cost = price[index]; Text(title, HeroDetailsController.Pretty(cost.Key)); Text(value, cost.Value.ToString());
                int owned = MainStates.instance != null && MainStates.instance.HasMain() ? MainStates.instance.GetItemsCount(cost.Key) : 0;
                Text(detail, "OWNED " + owned); Icon(resources != null ? resources.GetIcon(cost.Key) : null);
                if (value != null) value.color = owned >= cost.Value ? Color.white : new Color(1,.46f,.43f);
                break;
            case Content.UpgradeStatus:
                Text(value, HeroDetailsController.IsOwned(hero) ? "Upgrade to level " + (level + 1) : "Recruit this hero to upgrade"); break;
        }
    }
}
