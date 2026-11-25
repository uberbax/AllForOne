using UnityEngine;
using UnityEngine.UI;

namespace BRATANDRONIKLIB
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class BRATGUIBattleWalkTest : MonoBehaviour
    {
        [SerializeField] private BRATMotionBattleWalkTest movement;
        [SerializeField] private Button restartButton;
        [SerializeField] private RectTransform controls;
        private Button startButton;
        private Canvas canvas;
        private Vector2 initialOffset;

        private void OnEnable()
        {
            startButton = GetComponent<Button>();
            startButton.onClick.AddListener(StartWalk);
            if (restartButton && movement)
                restartButton.onClick.AddListener(movement.RestartWalk);

            if (controls)
            {
                canvas = controls.GetComponentInParent<Canvas>();
                initialOffset = controls.anchoredPosition;
                Canvas.preWillRenderCanvases += PlaceBelowCounter;
            }

            UpdateButtons();
        }

        private void StartWalk()
        {
            if (movement)
                movement.enabled = true;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool running = movement && movement.isActiveAndEnabled;
            startButton.interactable = movement && !running;
            if (restartButton)
                restartButton.interactable = running;
        }

        private void PlaceBelowCounter()
        {
            if (!canvas || canvas.scaleFactor <= 0f)
                return;

            // FPSCounter occupies the first 58 physical pixels of the safe area.
            var safe = Screen.safeArea;
            var top = (Screen.height - safe.yMax + 70f) / canvas.scaleFactor;
            var left = safe.xMin / canvas.scaleFactor + initialOffset.x;
            var offset = new Vector2(left, -Mathf.Max(-initialOffset.y, top));
            if (controls.anchoredPosition != offset)
                controls.anchoredPosition = offset;
        }

        private void OnDisable()
        {
            Canvas.preWillRenderCanvases -= PlaceBelowCounter;
            if (controls)
                controls.anchoredPosition = initialOffset;
            startButton.onClick.RemoveListener(StartWalk);
            if (restartButton && movement)
                restartButton.onClick.RemoveListener(movement.RestartWalk);
        }
    }
}
