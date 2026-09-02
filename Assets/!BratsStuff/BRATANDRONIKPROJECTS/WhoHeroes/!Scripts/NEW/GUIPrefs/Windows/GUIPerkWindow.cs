using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIPerkWindow : MonoBehaviour
{
    private const string TradeDynamicId = "whoheroes_trade_runtime";

    public string winType = "perk";
    public Transform holder;
    public GameObject afterAppear;
    public GUIButtUpgrade reroll;
    public Button skip;
    public Button accept;
    public TextMeshProUGUI tradeCostText;
    public List<Bon> rerollPrice = new List<Bon>();

    private readonly List<PerkAnimator> perks = new List<PerkAnimator>();
    private readonly List<GUIUnitFullInfo> unitWins = new List<GUIUnitFullInfo>();
    private readonly List<RObj> transientChoices = new List<RObj>();
    private List<RObj> choices = new List<RObj>();
    private FormatDynamic tradeDynamic;
    private RObj tradeSource;
    private bool perkChosen;
    private Coroutine appearanceRoutine;

    private bool IsTrade => winType == "trade";
    private bool IsPermanentPerk => winType == "perk";

    private void Awake()
    {
        if (holder != null)
        {
            for (var i = 0; i < holder.childCount; i++)
            {
                var child = holder.GetChild(i);
                perks.Add(child.GetComponent<PerkAnimator>());
                unitWins.Add(child.GetComponent<GUIUnitFullInfo>());
            }
        }

        if (IsTrade && tradeCostText == null)
            Debug.LogError("WhoHeroes trade window: tradeCostText is not assigned.", this);
    }

    private void OnEnable()
    {
        if (ConfigLoader.parseEnded && (!IsPermanentPerk || HasPendingPermanentPerk()))
            Fill();
    }

    private void OnDisable()
    {
        if (appearanceRoutine != null)
            StopCoroutine(appearanceRoutine);
        appearanceRoutine = null;
        CleanupTransientChoices();
    }

    private void OnDestroy() => CleanupTransientChoices();

    private void Start()
    {
        if (IsPermanentPerk && !HasPendingPermanentPerk())
            gameObject.SetActive(false);
        if (IsTrade)
            skip?.onClick.AddListener(DeclineTrade);
        else
            skip?.onClick.AddListener(() => gameObject.SetActive(false));
        reroll?.upgrade?.buy?.onClick.AddListener(() =>
        {
            if (!GUILIB.CanAfford(rerollPrice))
                return;

            MainStates.instance.DelItems(rerollPrice);
            Fill();
        });

        if (IsTrade)
        {
            accept?.onClick.AddListener(TryCompleteTradePurchase);
            return;
        }

        accept?.onClick.AddListener(() =>
        {
            foreach (var choice in choices)
                GUILIB.CoreAction(choice, choice.it == ItemType.projectile ? "take_skill" : "buy");
            gameObject.SetActive(false);
        });
    }

    public void Fill()
    {
        if (appearanceRoutine != null)
            StopCoroutine(appearanceRoutine);
        appearanceRoutine = null;
        CleanupTransientChoices();
        perkChosen = false;
        afterAppear?.SetActive(false);
        choices = IsTrade ? BuildTradeChoices() : BuildPerkChoices();

        if (IsPermanentPerk)
        {
            reroll?.gameObject.SetActive(false);
            skip?.gameObject.SetActive(false);
            accept?.gameObject.SetActive(false);
        }

        for (var i = 0; i < unitWins.Count; i++)
        {
            unitWins[i].gameObject.SetActive(i < choices.Count);
            if (i < choices.Count)
            {
                perks[i]?.ResetState();
                unitWins[i].Fill(choices[i]);
                if (IsPermanentPerk)
                    FillPermanentPerkCard(unitWins[i], choices[i]);
            }
        }

        if (IsTrade)
            ConfigureTradePurchase();
        else
            reroll?.Fill(rerollPrice, false, false, true, "reroll");

        if (choices.Count > 0)
            appearanceRoutine = StartCoroutine(ShowPerksSequence());
        else
            afterAppear?.SetActive(true);
    }

    private List<RObj> BuildPerkChoices()
    {
        var result = new List<RObj>();
        if (!GUILIB.CoreReady)
            return result;

        var pool = ModelSet.GetMeItemsAll(MainCycle_WhoHeroes.PermanentPerkSetId)
            .Where(MainCycle_WhoHeroes.PermanentPerkIds.Contains)
            .Distinct()
            .ToList();
        var count = Mathf.Min(3, Mathf.Min(unitWins.Count, pool.Count));
        foreach (var id in ModelSet.GetMeNonRepeat(pool, count))
        {
            var choice = DatabaseAll.instance.CreateItem(id, 1, false, false);
            choice.dynamic = ConfigLoader.Instance.allDynamic[id];
            transientChoices.Add(choice);
            result.Add(choice);
        }
        return result;
    }

    public bool SelectPermanentPerk(RObj choice)
    {
        if (!IsPermanentPerk || perkChosen || choice?.dbObj == null ||
            !choices.Contains(choice) || choice.dynamic == null || MainStates.instance == null)
            return false;

        perkChosen = true;
        MainStates.instance.ExecuteDone(choice.dynamic);
        MainCycle_WhoHeroes.Instance?.OnPermanentPerkChosen(choice.dbObj.ID);
        EventManager.INV(WhoHeroesEvents.PermanentPerkChosen,
            new ArgPass { who = choice, what = choice.dbObj.ID });
        gameObject.SetActive(false);
        return true;
    }

    private static void FillPermanentPerkCard(GUIUnitFullInfo window, RObj choice)
    {
        if (window?.unitgui?.general == null || choice?.dbObj == null)
            return;

        var id = choice.dbObj.ID;
        var level = ModelStatistics.instance == null ? 0 : ModelStatistics.instance.GetStatValue(id, false);
        var general = window.unitgui.general;
        if (general.name != null)
            general.name.text = MainCycle_WhoHeroes.PermanentPerkTitle(id);
        if (general.description != null)
            general.description.text = MainCycle_WhoHeroes.PermanentPerkDescription(id, level + 1);
        if (general.number != null)
            general.number.text = (level + 1).ToString();
        var icon = GUILIB.Icon(MainCycle_WhoHeroes.PermanentPerkIcon(id));
        if (general.icon != null && icon != null)
            general.icon.sprite = icon;
        window.unitgui.hire?.gameObject.SetActive(false);
    }

    private List<RObj> BuildTradeChoices()
    {
        tradeSource = null;
        var result = new List<RObj>();
        var cycle = MainCycle_WhoHeroes.Instance;
        if (!GUILIB.CoreReady || cycle == null || !cycle.TraderAvailableToday ||
            !MainStates.instance.all.TryGetValue("main_player", out var player))
            return result;

        if (!ConfigLoader.Instance.dictSets.ContainsKey(MainCycle_WhoHeroes.TraderSetId))
            return result;

        var roster = player.inventory
            .Where(value => value != null && value.it == ItemType.monster && value.dbObj != null &&
                            value.GetPar("amount") > 0f)
            .ToList();
        var sourceIds = roster.Select(value => value.dbObj.ID).Distinct().ToList();
        if (sourceIds.Count == 0)
            return result;

        var sourceId = ModelSet.GetMeNonRepeat(sourceIds, 1)[0];
        tradeSource = roster.First(value => value.dbObj.ID == sourceId);

        var targetIds = ModelSet.GetMeItemsAll(MainCycle_WhoHeroes.TraderSetId)
            .Where(id => id != sourceId && DatabaseAll.instance.heroes.ContainsKey(id))
            .Distinct()
            .ToList();
        if (targetIds.Count == 0)
        {
            tradeSource = null;
            return result;
        }

        var targetId = ModelSet.GetMeNonRepeat(targetIds, 1)[0];
        var sourceAmount = Mathf.Max(1, Mathf.RoundToInt(tradeSource.GetPar("amount")));
        var sourcePower = Mathf.Max(1f, tradeSource.GetMainPar("battle_power")) * sourceAmount;
        var targetTemplate = DatabaseAll.instance.CreateMonster(targetId, 1, false, false);
        var targetPower = Mathf.Max(1f, targetTemplate.GetMainPar("battle_power"));
        DestroyTransient(targetTemplate);
        var targetAmount = Mathf.Max(1,
            Mathf.CeilToInt(sourcePower * MainCycle_WhoHeroes.TraderPowerMultiplier() / targetPower));
        var target = DatabaseAll.instance.CreateMonster(targetId, targetAmount, false, false);
        target.SetPar("used_slot", -1f);
        transientChoices.Add(target);

        result.Add(tradeSource);
        result.Add(target);
        return result;
    }

    private void ConfigureTradePurchase()
    {
        var valid = tradeSource != null && choices.Count == 2;
        if (!valid)
        {
            tradeDynamic = null;
            if (accept != null)
                accept.interactable = false;
            if (tradeCostText != null)
                tradeCostText.text = MainCycle_WhoHeroes.Text("no_trade");
            return;
        }

        var sourceAmount = Mathf.Max(1, Mathf.RoundToInt(tradeSource.GetPar("amount")));
        tradeDynamic = new FormatDynamic
        {
            id = TradeDynamicId,
            multi = 1,
            price = new List<Bon>
            {
                new Bon { Key = tradeSource.dbObj.ID, Value = sourceAmount },
                new Bon { Key = MainCycle_WhoHeroes.GoldResourceId, Value = MainCycle_WhoHeroes.TraderGoldSurcharge() }
            },
            itemsGet = new List<Bon>
            {
                new Bon
                {
                    Key = choices[1].dbObj.ID,
                    Value = Mathf.Max(1, Mathf.RoundToInt(choices[1].GetPar("amount")))
                }
            }
        };

        if (accept != null)
            accept.interactable = GUILIB.CanAfford(tradeDynamic.price);
        if (tradeCostText != null)
            tradeCostText.text = MainCycle_WhoHeroes.Text("gold_amount")
                .Replace("{amount}", MainCycle_WhoHeroes.TraderGoldSurcharge().ToString());
    }

    private void TryCompleteTradePurchase()
    {
        if (tradeDynamic == null || choices.Count != 2 || MainStates.instance == null ||
            !MainStates.instance.all.TryGetValue("main_player", out var player) ||
            !GUILIB.CanAfford(tradeDynamic.price))
            return;

        var paid = false;
        MainStates.instance.Buy(tradeDynamic.price, null, () => paid = true);
        if (!paid)
            return;

        var result = choices[1];
        result.SetPar("used_slot", -1f);
        MainCycle_WhoHeroes.ApplyPermanentPerksToUnit(result, false);
        MainCycle_WhoHeroes.AddOrMergeCityStack(result);
        transientChoices.Remove(result);
        CompleteTrade();
    }

    private void CompleteTrade()
    {
        if (tradeSource != null)
        {
            tradeSource.SetPar("used_slot", -1f);
            if (tradeSource.GetPar("amount") <= 0f)
                MainCycle_WhoHeroes.DisposeRuntimeObject(tradeSource);
        }

        RObj player = null;
        if (MainStates.instance != null && MainStates.instance.all.TryGetValue("main_player", out player))
            player.RecalcPars();
        EventManager.INV(WhoHeroesEvents.Refresh, new ArgPass { who = player });
        MainCycle_WhoHeroes.Instance?.CompleteTraderForToday();
        gameObject.SetActive(false);
    }

    private void DeclineTrade()
    {
        MainCycle_WhoHeroes.Instance?.CompleteTraderForToday();
        gameObject.SetActive(false);
    }

    private static bool HasPendingPermanentPerk()
    {
        return ModelStatistics.instance != null &&
               ModelStatistics.instance.GetStatValue(MainCycle_WhoHeroes.PendingPerkNightStat, false) > 0;
    }

    private IEnumerator ShowPerksSequence()
    {
        foreach (var perk in perks)
        {
            if (perk == null || !perk.gameObject.activeSelf)
                continue;
            yield return perk.PlayAppear();
            yield return new WaitForSeconds(0.1f);
        }
        afterAppear?.SetActive(true);
        appearanceRoutine = null;
    }

    private void CleanupTransientChoices()
    {
        foreach (var value in transientChoices.ToArray())
            if (value != null && value.owner == null)
                DestroyTransient(value);
        transientChoices.Clear();
        choices.Clear();
        tradeSource = null;
        tradeDynamic = null;
    }

    private static void DestroyTransient(RObj value)
    {
        if (value == null || MainStates.instance == null)
            return;
        foreach (var skill in value.actSkills.ToArray())
            skill?.Destroy();
        value.actSkills.Clear();
        value.Destroy();
    }
}
