using System;
using TMPro;
using UnityEngine;

public sealed class PrisonDialogue : MonoBehaviour
{
    [SerializeField] private TMP_Text currentLine;
    [SerializeField] private TMP_Text recentLine;
    [SerializeField] private TMP_Text oldestLine;
    [SerializeField] private TMP_Text continueLabel;
    [SerializeField] private TMP_Text leaveLabel;
    [SerializeField] private GameObject prison;
    [SerializeField] private GameObject cages;

    private static readonly (string Line, string Continue, string Leave)[] Pages =
    {
        ("I'm Morden, keeper of these cells. If you hear scratching, leave it be. If it says your name, come get me.",
            "Who's in the cells?", "I'll come back later"),
        ("We keep the creatures we capture down here. We feed them, train them, and learn what each one will put up with. That little beast in the corner? It'll bite your hand for a scrap of meat, then turn its nose up at the bowl.",
            "Can they fight with me?", "That's enough for now"),
        ("Once a creature trusts you enough, you can take it into battle. Keep showing up. Bring food it likes. Train with it. You'll know you're getting somewhere when it starts waiting by the door for you.",
            "Show me the prisoners", "Leave"),
    };

    private int page;

    private Action externalAdvance;
    private Action externalLeave;

    private void OnEnable()
    {
        if (externalAdvance == null) ResetDialogue();
    }

    public void ShowExternalPage(string line, string choice, string decline, string previousLine,
        string playerReply, Action onAdvance, Action onLeave)
    {
        externalAdvance = onAdvance;
        externalLeave = onLeave;
        currentLine.text = line;
        continueLabel.text = choice;
        leaveLabel.text = decline;
        leaveLabel.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(decline));
        ShowHistory(recentLine, previousLine);
        ShowHistory(oldestLine, playerReply);
        gameObject.SetActive(true);
    }

    public void ResetDialogue()
    {
        page = 0;
        Refresh();
    }

    public void Advance()
    {
        if (externalAdvance != null)
        {
            externalAdvance();
            return;
        }

        if (page == Pages.Length - 1)
        {
            cages.SetActive(true);
            Leave();
            return;
        }

        page++;
        Refresh();
    }

    public void Leave()
    {
        if (externalLeave != null)
            externalLeave();
        else
            prison.SetActive(false);
    }

    private void Refresh()
    {
        var entry = Pages[page];
        currentLine.text = entry.Line;
        continueLabel.text = entry.Continue;
        leaveLabel.text = entry.Leave;
        var previous = page > 0 ? Pages[page - 1] : default;
        ShowHistory(recentLine, previous.Line);      // dialogueNpc
        ShowHistory(oldestLine, previous.Continue); // dialogueMe
    }

    private static void ShowHistory(TMP_Text label, string line)
    {
        label.text = line ?? string.Empty;
        label.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(line));
    }
}
