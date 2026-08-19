using UnityEngine;

public class GUIUnitFrameBehav : MonoBehaviour
{
    public GUIUnitFrame unitgui;

    public void Fill(RObj value = null)
    {
        unitgui?.Fill(value);
    }

    public void SetUpSlot(bool hasInfo = true, bool hasAction = true, string actionType = "add", string actionColor = "butgreen")
    {
        unitgui?.SetUpActions(hasInfo, hasAction, actionType, actionColor);
    }

    public void ChangeActionState(bool state = true, string color = "butgreen", string disabledColor = "butgrey")
    {
        unitgui?.ChangeAction(state, color, disabledColor);
    }
}
