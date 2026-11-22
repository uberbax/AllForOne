using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class BattleTargetIndicators : MonoBehaviour
{
    [SerializeField] private BattleTurnPanel turns;
    [SerializeField] private RectTransform targetMy;
    [SerializeField] private RectTransform targetEnemy;
    [SerializeField] private Transform stageRoot;
    private Camera worldCamera;
    private RObj selectedEnemy;
    private bool castFocusActive;
    private (RectTransform Arrow, Vector3 Home) myArrow, enemyArrow;

    private void Awake()
    {
        myArrow = FindArrow(targetMy);
        enemyArrow = FindArrow(targetEnemy);
    }

    private static (RectTransform Arrow, Vector3 Home) FindArrow(RectTransform marker)
    {
        var arrow = marker != null ? marker.Find("Above - Arrow") as RectTransform : null;
        return (arrow, arrow != null ? arrow.localPosition : Vector3.zero);
    }

    public void SetCastFocusActive(bool active)
    {
        castFocusActive = active;
        if (!active) return;
        Show(targetMy, false);
        Show(targetEnemy, false);
    }

    private void OnEnable()
    {
        Show(targetMy, false);
        Show(targetEnemy, false);
    }

    private bool BelongsToStage(RObj unit) => unit != null && unit.main != null &&
        (stageRoot == null || unit.main.transform.IsChildOf(stageRoot));

    private void LateUpdate()
    {
        var state = MainStates.instance;
        if (state == null || !state.inBattle)
        {
            Show(targetMy, false);
            ClearEnemy();
            return;
        }
        if (castFocusActive) return;
        if (worldCamera == null || !worldCamera.isActiveAndEnabled) worldCamera = Camera.main;
        var ally = turns != null ? turns.CurrentAlly : null;
        Follow(targetMy, myArrow, BelongsToStage(ally) && ally.main.activeInHierarchy ? ally : null);
        var enemy = state.lastTargetSelected;
        if (BelongsToStage(enemy) && enemy.main.activeInHierarchy &&
            enemy.tags.Contains("enemy") && enemy.GetPar("health") > 0)
        {
            selectedEnemy = enemy;
            Follow(targetEnemy, enemyArrow, enemy);
        }
        else ClearEnemy();
    }

    private void Follow(RectTransform marker, (RectTransform Arrow, Vector3 Home) upper, RObj unit)
    {
        if (marker == null) return;
        if (unit == null || worldCamera == null)
        {
            Show(marker, false);
            return;
        }
        var parent = marker.parent as RectTransform;
        var canvas = marker.GetComponentInParent<Canvas>();
        if (parent == null || canvas == null) { Show(marker, false); return; }
        canvas = canvas.rootCanvas;
        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screen = worldCamera.WorldToScreenPoint(unit.main.transform.position);
        if (screen.z <= 0 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out var point))
        {
            Show(marker, false);
            return;
        }
        var position = marker.localPosition;
        position.x = point.x;
        marker.localPosition = position;
        if (upper.Arrow != null)
        {
            var head = unit.visMain != null ? unit.visMain.transform.Find("head") : null;
            if (head == null) head = unit.main.transform.Find("head");
            var arrowPosition = upper.Home;
            if (head != null)
            {
                var headScreen = worldCamera.WorldToScreenPoint(head.position);
                if (headScreen.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    marker, headScreen, uiCamera, out var headPoint))
                {
                    arrowPosition.x = headPoint.x;
                    arrowPosition.y = headPoint.y;
                }
            }
            upper.Arrow.localPosition = arrowPosition;
        }
        Show(marker, true);
    }

    private void ClearEnemy()
    {
        var state = MainStates.instance;
        if (state != null && state.lastTargetSelected != null &&
            (ReferenceEquals(state.lastTargetSelected, selectedEnemy) || BelongsToStage(state.lastTargetSelected)))
            state.lastTargetSelected = null;
        selectedEnemy = null;
        Show(targetEnemy, false);
    }

    private static void Show(RectTransform marker, bool visible)
    {
        if (marker != null && marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
    }

    private void OnDisable()
    {
        Show(targetMy, false);
        ClearEnemy();
    }
}
