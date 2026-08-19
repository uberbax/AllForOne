using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUIPerkWindow : MonoBehaviour
{
    public string winType = "perk";
    public Transform holder;
    public GameObject afterAppear;
    public GUIButtUpgrade reroll;
    public Button skip;
    public Button accept;
    public List<Bon> rerollPrice = new List<Bon>();
    private readonly List<PerkAnimator> perks = new List<PerkAnimator>();
    private readonly List<GUIUnitFullInfo> unitWins = new List<GUIUnitFullInfo>();
    private List<RObj> choices = new List<RObj>();

    private void Awake()
    {
        if (holder == null) return;
        for (var i = 0; i < holder.childCount; i++)
        {
            var child = holder.GetChild(i);
            perks.Add(child.GetComponent<PerkAnimator>());
            unitWins.Add(child.GetComponent<GUIUnitFullInfo>());
        }
    }

    private void Start()
    {
        skip?.onClick.AddListener(() => gameObject.SetActive(false));
        reroll?.upgrade?.buy?.onClick.AddListener(() =>
        {
            if (!GUILIB.CanAfford(rerollPrice)) return;
            MainStates.instance.DelItems(rerollPrice);
            Fill();
        });
        accept?.onClick.AddListener(() =>
        {
            foreach (var choice in choices)
                GUILIB.CoreAction(choice, choice.it == ItemType.projectile ? "take_skill" : "buy");
            gameObject.SetActive(false);
        });
    }

    public void Fill()
    {
        afterAppear?.SetActive(false);
        choices = BuildChoices();
        for (var i = 0; i < unitWins.Count; i++)
        {
            unitWins[i].gameObject.SetActive(i < choices.Count);
            if (i < choices.Count) unitWins[i].Fill(choices[i]);
        }
        reroll?.Fill(rerollPrice, false, false, true, "reroll");
        StartCoroutine(ShowPerksSequence());
    }

    private List<RObj> BuildChoices()
    {
        var result = new List<RObj>();
        if (!GUILIB.CoreReady || !MainStates.instance.all.TryGetValue("main_player", out var player))
            return result;
        if (winType == "perk")
        {
            foreach (var id in DatabaseAll.instance.skills.Keys.Take(unitWins.Count))
                result.Add(DatabaseAll.instance.CreateProjectile(player, id, Vector3.zero, false, false));
        }
        else
        {
            foreach (var id in DatabaseAll.instance.items.Keys.Take(unitWins.Count))
                result.Add(DatabaseAll.instance.CreateItem(id, 1, false, false));
        }
        return result;
    }

    private IEnumerator ShowPerksSequence()
    {
        foreach (var perk in perks)
        {
            if (perk == null || !perk.gameObject.activeSelf) continue;
            yield return perk.PlayAppear();
            yield return new WaitForSeconds(0.1f);
        }
        afterAppear?.SetActive(true);
    }
}
