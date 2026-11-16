using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ObjHolder), typeof(Button))]
public sealed class BattleTurnSkill : MonoBehaviour
{
    [SerializeField] private GameObject unavailable;
    [SerializeField] private GameObject readyFrame;
    [SerializeField] private GameObject unavailableFrame;
    private ObjHolder holder;
    private Button button;
    private void Awake()
    {
        holder = GetComponent<ObjHolder>();
        button = GetComponent<Button>();
    }
    private void LateUpdate()
    {
        var skill = holder.obj;
        var owner = skill?.owner;
        var state = MainStates.instance;
        bool ready = skill != null && owner != null && state != null && state.inBattle &&
            owner.GetPar("can_cast") > 0 && owner.GetPar("health") > 0 &&
            state.awaitUnits.TryGetValue(owner.RID, out var waiting) && waiting > 0 &&
            skill.GetPar("cd") <= 0 && owner.GetPar("mana") >= skill.GetPar("manacost");
        button.interactable = ready;
        Show(unavailable, !ready);
        Show(readyFrame, ready);
        Show(unavailableFrame, !ready);
    }
    private static void Show(GameObject target, bool visible)
    {
        if (target != null && target.activeSelf != visible) target.SetActive(visible);
    }
}
