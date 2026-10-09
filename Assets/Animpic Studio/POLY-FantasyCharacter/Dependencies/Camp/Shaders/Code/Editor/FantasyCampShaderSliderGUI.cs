namespace Animpic.Local.POLYFantasyCharacter.Camp
{
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor-only controls. Limits never become shader properties or runtime settings.
internal static class FantasyCampShaderSliderGUI
{
    sealed class RangeState
    {
        public string Key;
        public Vector2 Defaults;
        public Vector2 Draft;
        public bool Open;
        public bool? PendingOpen;
    }

    static readonly Dictionary<string, RangeState> States = new Dictionary<string, RangeState>();

    public static void Draw(MaterialEditor editor, MaterialProperty property, string label,
        string family, float defaultMin, float defaultMax)
    {
        if (property.propertyType != UnityEngine.Rendering.ShaderPropertyType.Float &&
            property.propertyType != UnityEngine.Rendering.ShaderPropertyType.Range)
        {
            editor.ShaderProperty(property, label);
            return;
        }

        RangeState state = GetState(family, property.name, defaultMin, defaultMax);
        DrawControl(editor, label, property.floatValue, property.hasMixedValue, state,
            value =>
            {
                editor.RegisterPropertyChangeUndo(label);
                property.floatValue = value;
            });
    }

    public static void DrawSerialized(Editor editor, SerializedProperty property, string label,
        string family, float defaultMin, float defaultMax)
    {
        if (property.propertyType != SerializedPropertyType.Float)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            return;
        }

        RangeState state = GetState(family, property.propertyPath, defaultMin, defaultMax);
        // The owning inspector retains Update/ApplyModifiedProperties and its existing
        // validation/Scene View callbacks. SerializedProperty supplies Undo support.
        DrawControl(editor, label, property.floatValue, property.hasMultipleDifferentValues,
            state, value => property.floatValue = value);
    }

    static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    static bool IsValidRange(Vector2 range)
    {
        return IsFinite(range.x) && IsFinite(range.y) && range.x < range.y &&
            IsFinite(range.y - range.x);
    }

    static RangeState GetState(string family, string propertyName, float min, float max)
    {
        string key = "POLY-FantasyCamp." + family + "ShaderGUI.SliderRange." +
            Application.dataPath.Replace('\\', '/') + "." + propertyName;
        RangeState state;
        if (!States.TryGetValue(key, out state))
        {
            Vector2 defaults = new Vector2(min, max);
            if (!IsValidRange(defaults))
                defaults = new Vector2(0f, 1f);
            state = new RangeState
            {
                Key = key,
                Defaults = defaults,
                Open = SessionState.GetBool(key + ".InlineOpen", false)
            };
            state.Draft = ReadRange(state);
            States.Add(key, state);
        }

        // Defer expansion until Layout: never change the number of GUILayout entries
        // mid-event, and never abort the owner's ApplyModifiedProperties with ExitGUI.
        if (Event.current.type == EventType.Layout && state.PendingOpen.HasValue)
        {
            state.Open = state.PendingOpen.Value;
            state.PendingOpen = null;
            SessionState.SetBool(key + ".InlineOpen", state.Open);
        }
        return state;
    }

    static Vector2 ReadRange(RangeState state)
    {
        Vector2 range = new Vector2(
            EditorPrefs.GetFloat(state.Key + ".Min", state.Defaults.x),
            EditorPrefs.GetFloat(state.Key + ".Max", state.Defaults.y));
        return IsValidRange(range) ? range : state.Defaults;
    }

    static void DrawControl(Editor editor, string label, float value, bool mixed,
        RangeState state, Action<float> writeValue)
    {
        Vector2 range = ReadRange(state);
        float numberWidth = Mathf.Max(64f, EditorGUIUtility.fieldWidth);
        const float buttonWidth = 62f;
        const float gap = 5f;
        // The view width is stable on Layout; GetControlRect's placeholder is not.
        float availableWidth = EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth -
            40f - buttonWidth - gap - EditorGUI.indentLevel * 15f;
        bool drawTrack = availableWidth >= numberWidth + gap + 30f;
        Rect controls = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(),
            new GUIContent(label, "Click Min/Max to edit the drag limits. Exact numeric input may exceed them."));
        Rect buttonRect = new Rect(controls.xMax - buttonWidth, controls.y, buttonWidth, controls.height);
        Rect numberRect = controls;
        numberRect.xMax = buttonRect.xMin - gap;
        if (drawTrack)
            numberRect.xMin = numberRect.xMax - numberWidth;
        numberRect.width = Mathf.Max(1f, numberRect.width);

        bool previousMixed = EditorGUI.showMixedValue;
        int previousIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        try
        {
            EditorGUI.showMixedValue = mixed;
            if (drawTrack)
            {
                Rect trackRect = controls;
                trackRect.xMax = numberRect.xMin - gap;
                trackRect.width = Mathf.Max(1f, trackRect.width);
                // Clamp only the displayed thumb, never the stored value on repaint.
                float thumb = IsFinite(value) ? Mathf.Clamp(value, range.x, range.y) : range.x;
                EditorGUI.BeginChangeCheck();
                float dragged = GUI.HorizontalSlider(trackRect, thumb, range.x, range.y);
                if (EditorGUI.EndChangeCheck() && IsFinite(dragged))
                {
                    writeValue(dragged);
                    value = dragged;
                    mixed = false;
                }
                if (mixed)
                    GUI.Label(trackRect, "Mixed", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            float typed = EditorGUI.FloatField(numberRect, value);
            if (EditorGUI.EndChangeCheck() && IsFinite(typed))
                writeValue(typed);
        }
        finally
        {
            EditorGUI.showMixedValue = previousMixed;
            EditorGUI.indentLevel = previousIndent;
        }

        // Preferences remain accessible even if the effect's material controls are disabled.
        bool previousEnabled = GUI.enabled;
        bool previousChanged = GUI.changed;
        try
        {
            GUI.enabled = true;
            EditorGUI.showMixedValue = false;
            bool requestedOpen = GUI.Toggle(buttonRect, state.Open,
                new GUIContent("Min/Max", "Show or hide the slider limits. Reset restores limits only."),
                EditorStyles.miniButton);
            if (requestedOpen != state.Open)
            {
                state.PendingOpen = requestedOpen;
                editor.Repaint();
            }
            if (state.Open && DrawLimits(state))
                editor.Repaint();
        }
        finally
        {
            GUI.enabled = previousEnabled;
            GUI.changed = previousChanged;
            EditorGUI.showMixedValue = previousMixed;
        }
    }

    static bool DrawLimits(RangeState state)
    {
        Rect controls = EditorGUILayout.GetControlRect();
        const float gap = 4f;
        const float resetWidth = 45f;
        float fieldWidth = Mathf.Max(1f, (controls.width - resetWidth - gap * 2f) * 0.5f);
        Rect minRect = new Rect(controls.x, controls.y, fieldWidth, controls.height);
        Rect maxRect = new Rect(minRect.xMax + gap, controls.y, fieldWidth, controls.height);
        Rect resetRect = new Rect(maxRect.xMax + gap, controls.y, resetWidth, controls.height);
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        int previousIndent = EditorGUI.indentLevel;
        bool previousChanged = GUI.changed;
        bool previousMixed = EditorGUI.showMixedValue;
        bool edited = false;
        EditorGUIUtility.labelWidth = 28f;
        EditorGUI.indentLevel = 0;
        EditorGUI.showMixedValue = false;
        try
        {
            EditorGUI.BeginChangeCheck();
            float min = EditorGUI.FloatField(minRect, new GUIContent("Min"), state.Draft.x);
            float max = EditorGUI.FloatField(maxRect, new GUIContent("Max"), state.Draft.y);
            if (EditorGUI.EndChangeCheck())
            {
                edited = true;
                // Keep invalid drafts visible; only the last valid pair drives the slider.
                state.Draft = new Vector2(min, max);
                if (IsValidRange(state.Draft))
                {
                    EditorPrefs.SetFloat(state.Key + ".Min", min);
                    EditorPrefs.SetFloat(state.Key + ".Max", max);
                }
            }
            if (GUI.Button(resetRect, "Reset", EditorStyles.miniButton))
            {
                edited = true;
                EditorPrefs.DeleteKey(state.Key + ".Min");
                EditorPrefs.DeleteKey(state.Key + ".Max");
                state.Draft = state.Defaults;
            }
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUI.indentLevel = previousIndent;
            GUI.changed = previousChanged;
            EditorGUI.showMixedValue = previousMixed;
        }
        // Keep the same layout entries when typing switches between valid/invalid drafts.
        bool valid = IsValidRange(state.Draft);
        EditorGUILayout.HelpBox(valid
            ? "Limits affect dragging only. Exact numbers may exceed them; Reset changes limits, not values."
            : "Min must be less than Max. Both values and their difference must be finite. " +
                "The last valid limits remain active.", valid ? MessageType.Info : MessageType.Warning);
        return edited;
    }
}

}
