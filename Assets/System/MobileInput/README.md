# Mobile joystick

Drag `MobileJoystickCanvas.prefab` into a scene. Read the MobileJoystick component on SafeArea/Joystick:

```csharp
[SerializeField] private MobileJoystick joystick;

void Update()
{
    Vector2 input = joystick.Value;
    float horizontal = joystick.Horizontal;
    float vertical = joystick.Vertical;
}
```

Value is analog, with X/right and Y/up, a circular magnitude limit of 1 and a configurable radial dead zone (default 0.1). The dead zone is remapped smoothly to the remaining range. IsPressed reports whether the joystick owns a pointer. ResetInput() clears it manually. Release, disable, loss of focus and application pause reset both the value and handle. A second finger cannot take over an active drag. Mouse dragging works in the Editor.

The prefab contains an overlay Canvas, CanvasScaler, GraphicRaycaster, safe-area container and vector UI graphics. The invisible SafeArea surface receives presses and places the joystick centre at the press position. Interactive UI keeps priority. The joystick starts hidden, appears immediately on press, and fades to alpha 0 over Fade Duration (default 0.25 s) on release, using unscaled time. Input resets immediately on release. A new press interrupts the fade. The layout adapts to screen safe area/orientation.

An EventSystem is created at runtime only if no enabled EventSystem exists. The fallback uses StandaloneInputModule and requires Active Input Handling = Input Manager or Both (this project already uses Both). With an existing EventSystem, its current input module is used.

No player movement is attached. All runtime scripts, Editor builder and prefab are in this folder. The Editor menu Tools/Mobile Input/Create Joystick Canvas creates the prefab if missing and validates an existing prefab without overwriting it.
