using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ObjHolder))]
public sealed class PartyCampHeroCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image portrait, frame;
    [SerializeField] private GameObject checkmark;
    [SerializeField] private TMP_Text nameLabel, levelLabel;
    [SerializeField] private Sprite missingPortrait;
    private PartyCampController controller;
    private RObj hero;
    private ObjHolder holder;
    private void Awake() { button.onClick.AddListener(Clicked); }
    private void Clicked() { if (controller != null) controller.ToggleHero(hero); }
    public void Bind(PartyCampController owner, RObj value, bool assigned)
    {
        controller = owner; hero = value;
        if (holder == null) holder = GetComponent<ObjHolder>();
        holder.noTrack = true; holder.asDBObj = ""; holder.obj = value;
        var resources = ResourceHolder.instance;
        portrait.sprite = resources != null && resources.avas.TryGetValue(value.dbObj.ID, out var sprite) ? sprite : null;
        if (portrait.sprite == null) portrait.sprite = missingPortrait;
        portrait.enabled = portrait.sprite != null;
        nameLabel.text = value.dbObj.ID.Replace('_', ' ');
        levelLabel.SetText("Lv.{0}  A.{1}", Mathf.Max(1, value.GetPar("level")), HeroAscension.Level(value));
        checkmark.SetActive(assigned);
        frame.color = assigned ? new Color(.2f,.72f,1) : Color.white;
        button.interactable = true;
    }
}
