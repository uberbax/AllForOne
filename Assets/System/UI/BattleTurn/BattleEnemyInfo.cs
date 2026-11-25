using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleEnemyInfo : MonoBehaviour
{
    [SerializeField] private MainCycleDark battle;
    [SerializeField] private GameObject details;
    [SerializeField] private GameObject empty;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text enemyName, enemyClass, rarity, health, mana, damage, armor, dodge, speed;
    [SerializeField] private TMP_Text[] resistValues;
    [SerializeField] private RectTransform skillList;
    [SerializeField] private RectTransform skillPrefab;
    [System.Serializable]
    private struct SkillArt
    {
        public string id;
        public Sprite sprite;
    }
    [SerializeField] private SkillArt[] skillArt;
    [SerializeField] private Sprite defaultSkillArt;

    private static readonly string[] ResistKeys =
        { "res_water", "res_fire", "res_earth", "res_light", "res_dark", "res_stun", "res_sleep" };
    private RObj displayed;
    private float nextRefresh;

    private void OnEnable() { nextRefresh = 0; displayed = null; Refresh(); }
    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextRefresh) Refresh();
    }

    private void Refresh()
    {
        nextRefresh = Time.unscaledTime + .12f;
        var enemy = battle != null ? battle.InspectedBattleEnemy() : null;
        if (details.activeSelf != (enemy != null)) details.SetActive(enemy != null);
        if (empty.activeSelf != (enemy == null)) empty.SetActive(enemy == null);
        if (enemy == null) { displayed = null; return; }
        var resources = ResourceHolder.instance;
        if (enemy != displayed)
        {
            displayed = enemy;
            enemyName.text = DisplayName(enemy.dbObj.ID);
            var labels = enemy.dbObj.labelis;
            enemyClass.text = labels.Count == 0 ? "Enemy" : "Enemy " + DisplayName(labels[labels.Count - 1]);
            int rank = enemy.dbObj.ID == "cultist_boss" ? (int)RarityType.epic : (int)enemy.GetPar("rarity");
            rarity.text = resources != null && resources.rareString.TryGetValue(rank, out var rankName)
                ? DisplayName(rankName) : ((RarityType)rank).ToString();
            rarity.color = resources != null && resources.rareColors.TryGetValue(rank, out var tint) ? tint : Color.white;
            portrait.sprite = resources != null ? resources.GetAva(enemy.dbObj.ID) : null;
            portrait.enabled = portrait.sprite != null;
        }
        health.SetText("{0} / {1}", enemy.GetPar("health"), enemy.GetPar("max_health"));
        mana.SetText("{0} / {1}", enemy.GetPar("mana"), enemy.GetPar("max_mana"));
        damage.SetText("{0}", enemy.GetPar("attack"));
        float defense = enemy.GetPar("def");
        armor.SetText("{0}%", Mathf.Round(100f * defense / Mathf.Max(1f, 100f + defense)));
        dodge.SetText("{0}", enemy.GetPar("dodge"));
        speed.SetText("{0}", enemy.GetPar("speed"));
        for (int i = 0; i < ResistKeys.Length; i++)
            resistValues[i].SetText("{0}%", enemy.GetPar(ResistKeys[i]));
        for (int i = 0; i < Mathf.Max(skillList.childCount, enemy.actSkills.Count); i++)
        {
            var row = i < skillList.childCount ? skillList.GetChild(i) :
                CreateSkillSlot();
            bool used = i < enemy.actSkills.Count && enemy.actSkills[i]?.dbObj != null;
            if (row.gameObject.activeSelf != used) row.gameObject.SetActive(used);
            if (!used) continue;
            var skill = enemy.actSkills[i];
            row.Find("level").GetComponent<TMP_Text>().SetText("Lv.{0}", Mathf.Max(1f, skill.GetPar("level")));
            string id = skill.dbObj.ID;
            if (row.name == id) continue;
            row.name = id;
            var icon = row.GetComponent<Image>();
            icon.sprite = SkillIcon(id, resources);
            icon.enabled = icon.sprite != null;
        }
    }

    private RectTransform CreateSkillSlot()
    {
        var row = Instantiate(skillPrefab, skillList, false);
        var size = ((RectTransform)skillList.GetChild(0)).rect.size;
        row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        row.Find("disabled").gameObject.SetActive(false);
        return row;
    }

    private Sprite SkillIcon(string id, ResourceHolder resources)
    {
        foreach (var art in skillArt)
            if (art.id == id) return art.sprite;
        if (resources != null && resources.skills.TryGetValue(id, out var sprite) && sprite != null &&
            sprite.name.StartsWith("SpellBook07_", System.StringComparison.Ordinal))
            return sprite;
        return defaultSkillArt;
    }

    private static string DisplayName(string id)
    {
        var config = ConfigLoader.Instance;
        if (config != null && config.doctLoc.ContainsKey(id))
        {
            string name = config.GetMeLocale(id);
            if (!string.IsNullOrWhiteSpace(name) && name != id) return name;
        }
        if (id == "summon_zombie1") return "Summon Zombie";
        if (id == "basic_aoe") return "Basic AOE";
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' '));
    }
}
