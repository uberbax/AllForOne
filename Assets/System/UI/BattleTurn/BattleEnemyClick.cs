using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleEnemyClick : MonoBehaviour, IPointerClickHandler
{
    private RObj unit;

    public static void Attach(RObj target)
    {
        var image = target.visMain.GetComponent<RawImage>();
        if (image == null) return;
        var canvas = image.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        image.raycastTarget = true;
        canvas.worldCamera = Camera.main;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        var click = image.GetComponent<BattleEnemyClick>() ?? image.gameObject.AddComponent<BattleEnemyClick>();
        click.unit = target;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var state = MainStates.instance;
        if (eventData.button != PointerEventData.InputButton.Left || state == null || !state.inBattle ||
            unit == null || unit.main == null || !unit.main.activeInHierarchy ||
            !unit.tags.Contains("enemy") || unit.GetPar("health") <= 0) return;
        state.lastTargetSelected = unit;
    }
}
