using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class TavernDialogue : MonoBehaviour
{
    public const string WilmerContractStat = "tavern_wilmer_contract";
    public const int WilmerGoldShare = 10;

    private const string RolandQuestStat = "tavern_oathwake_search";
    private const string RolandId = "hero_warrior";
    private const string WilmerId = "hero_thief";

    [SerializeField] private PrisonDialogue view;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField, Min(0)] private int roomPrice = 25;
    [SerializeField, Min(0)] private int wilmerPrice = 150;

    private string currentLine;
    private string previousLine;
    private string playerReply;

    private void OnDisable() => Close();

    public void TalkToAnna()
    {
        Begin("Anna");
        Show("I'm Anna. If you're here for a quiet drink, you've picked the right night. Even the floorboards are behaving.",
            "How's business?", () => Show(
                "Quiet enough to hear the rain. You look like you've had your share of it. I've got a dry room upstairs, if you'd like a proper rest.",
                $"Rest for {roomPrice} gold", RentRoom, "Maybe later"));
    }

    public void TalkToRoland()
    {
        Begin("Roland");
        Show("Roland. I used to carry a knight's banner. These days, I carry something smaller.",
            "What's in the cloth?", () =>
            {
                if (OwnsHero(RolandId))
                {
                    End("The hilt of Oathwake. I'll keep it close. I can't wait to see what lies beyond these walls.");
                    return;
                }

                Show("The hilt of Oathwake. The blade vanished years ago, but I still have a lead. Help me find it, and my sword arm is yours.",
                    "We'll find Oathwake", RecruitRoland, "I can't promise that");
            });
    }

    public void TalkToWilmer()
    {
        Begin("Wilmer");
        Show("Wilmer. Bounty hunter, mercenary, whatever the contract calls for. I get paid when the job's done. And a little before.",
            "What's your price?", OfferWilmer);
    }

    private void OfferWilmer()
    {
        if (OwnsHero(WilmerId))
        {
            End("I'm still in. Here's hoping the next chest is so full we have to carry it out between us.");
            return;
        }

        Show($"I'll take {wilmerPrice} gold up front and {WilmerGoldShare}% of the gold from the monsters you bring down. The rest of the spoils are yours. Deal?",
            $"Hire for {wilmerPrice} gold", RecruitWilmer, "No deal");
    }

    private void RentRoom()
    {
        var state = MainStates.instance;
        if (state == null || !state.HasMain())
        {
            End("Give me a moment, would you? The room will still be here.");
            return;
        }

        if (!Pay(state, roomPrice))
        {
            End("You're a little short. Come back when you've got it. I'll keep a lamp lit.");
            return;
        }

        var player = state.mainPlayer;
        Restore(state, player);
        foreach (var hero in player.inventory)
            if (hero.it == ItemType.monster)
                Restore(state, hero);

        state.Save();
        End("Upstairs, second door on the left. Take your time. The rain can wait.");
    }

    private void RecruitRoland() => Recruit(RolandId, 0, RolandQuestStat, 1,
        "Then we have a road to walk. I haven't looked forward to an adventure like this in years.");

    private void RecruitWilmer() => Recruit(WilmerId, wilmerPrice, WilmerContractStat, WilmerGoldShare,
        "We have a deal. I can almost hear the treasure rattling already. Let's find something worth hauling home.");

    private void Recruit(string id, int price, string stat, int statValue, string joinedLine)
    {
        var state = MainStates.instance;
        if (state == null || !state.HasMain() || ModelStatistics.instance == null ||
            DatabaseAll.instance == null || !DatabaseAll.instance.heroes.ContainsKey(id))
        {
            End("Give me a moment. We'll talk again soon.");
            return;
        }

        if (OwnsHero(id))
        {
            End(joinedLine);
            return;
        }

        if (state.GetItemsCount("gold") < price)
        {
            End("You're short on gold. Come back when the purse is heavier.");
            return;
        }

        var hero = DatabaseAll.instance.CreateMonster(id, 1);
        if (!Pay(state, price)) return;
        state.AddItems(new List<RObj> { hero });
        ModelStatistics.instance.SetStatValue(stat, statValue);
        state.Save();
        End(joinedLine);
        UIfiller.GlobalRefresh();
    }

    private static bool OwnsHero(string id) => MainStates.instance != null && MainStates.instance.GetItemsCount(id) > 0;

    private static bool Pay(MainStates state, int price)
    {
        if (price <= 0) return true;
        if (state.GetItemsCount("gold") < price) return false;
        state.DelItems(new List<Bon> { new Bon { Key = "gold", Value = price } });
        return true;
    }

    private static void Restore(MainStates state, RObj unit)
    {
        float damage = unit.GetPar("registered_damage");
        if (damage > 0) state.DealHeal(unit, damage);
        float missingMana = unit.GetPar("max_mana") - unit.GetPar("mana");
        if (missingMana > 0) state.AddMana(unit, missingMana);
    }

    private void Begin(string speaker)
    {
        speakerLabel.text = speaker;
        currentLine = previousLine = playerReply = string.Empty;
    }

    private void Show(string line, string choice, Action next, string decline = "Leave")
    {
        currentLine = line;
        view.ShowExternalPage(line, choice, decline, previousLine, playerReply, () =>
        {
            previousLine = currentLine;
            playerReply = choice;
            next();
        }, Close);
    }

    private void End(string line) => Show(line, "Back", Close, string.Empty);

    private void Close()
    {
        if (view != null) view.gameObject.SetActive(false);
    }
}
