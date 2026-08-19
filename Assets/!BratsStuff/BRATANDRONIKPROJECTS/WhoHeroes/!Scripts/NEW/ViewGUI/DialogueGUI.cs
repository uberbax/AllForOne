using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueGUI : MonoBehaviour
{
    public GUIDialogueItem dia;
    public int count;
    public bool slowMode = true;
    public string dialogueId = "";

    private void Start()
    {
        dia?.next?.onClick.AddListener(Next);
    }

    private void Next()
    {
        if (Dialoguer.instance != null)
            Dialoguer.instance.Hide();
        else
            gameObject.SetActive(false);
    }

    public void Fill(string id)
    {
        dialogueId = id;
        count = 0;
        if (Dialoguer.instance != null && ConfigLoader.Instance != null && ConfigLoader.Instance.dictDialogues.ContainsKey(id))
        {
            Dialoguer.instance.ShowDialogue(id);
            return;
        }
        EventManager.INV(WhoHeroesEvents.Dialogue, new ArgPass { what = id });
    }
}

[Serializable]
public class GUIDialogueItem
{
    public TextMeshProUGUI diaText;
    public TextMeshProUGUI leftTop;
    public TextMeshProUGUI rightTop;
    public GameObject leftChar;
    public GameObject rightChar;
    public Image leftCharAva;
    public Image rightCharAva;
    public GameObject leftTopObj;
    public GameObject rightTopObj;
    public Button next;
}
