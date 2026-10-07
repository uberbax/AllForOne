using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace Paperbound.Traveller
{
    [DefaultExecutionOrder(-100)]
    public sealed class TravellerDemo : MonoBehaviour
    {
        public TravellerMotor motor;
        public TravellerPresentation presentation;
        public ParticleSystem dust, snow;
        public bool showControls = true;
        bool walkPreview;
        Vector3 start;
        int direction = 1;
        void Start() { if (motor) start = motor.transform.position; }
        void Update()
        {
            if (!motor || !presentation) return;
            Vector2 pointer = Vector2.zero;
            bool toggleFold = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) pointer = Mouse.current.position.ReadValue();
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) pointer = Touchscreen.current.primaryTouch.position.ReadValue();
            toggleFold = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            pointer = Input.mousePosition; toggleFold = Input.GetKeyDown(KeyCode.F);
#endif
            motor.BlockPointerInput = showControls && new Rect(18, 18, 290, 240).Contains(new Vector2(pointer.x, Screen.height - pointer.y));
            if (toggleFold) ToggleFold();
            if (walkPreview && !motor.HasDestination && presentation.CanMove)
            { direction *= -1; motor.MoveTo(start + new Vector3(direction * 1.7f, 0, 0)); }
        }
        void ToggleFold() { if (!presentation) return; if (motor) motor.Stop(); presentation.SetFolded(!presentation.IsFolded); }
        void OnGUI()
        {
            if (!showControls || !motor || !presentation) return;
            GUILayout.BeginArea(new Rect(18, 18, 290, 240), GUI.skin.box);
            GUILayout.Label("PAPERBOUND / TRAVELLER");
            GUILayout.Label("WASD / arrows / click to walk");
            GUILayout.Label("F to fold or unfold");
            bool preview = GUILayout.Toggle(walkPreview, "Walk preview");
            if (preview != walkPreview) { walkPreview = preview; motor.Stop(); motor.readPlayerInput = !preview; }
            presentation.soundEnabled = GUILayout.Toggle(presentation.soundEnabled, "Paper footsteps & folding sound");
            if (GUILayout.Button(presentation.IsFolded ? "Unfold" : "Fold")) ToggleFold();
            if (GUILayout.Button("Dust / snow"))
            { if (dust && snow) { bool wasDust = dust.gameObject.activeSelf; dust.gameObject.SetActive(!wasDust); snow.gameObject.SetActive(wasDust); } }
            if (GUILayout.Button("Reset"))
            { walkPreview = false; motor.readPlayerInput = true; motor.Teleport(start); presentation.SetFolded(false); }
            GUILayout.EndArea();
        }
        void OnDisable() { if (motor) { motor.BlockPointerInput = false; motor.Stop(); } }
    }
}
