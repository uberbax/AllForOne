namespace Animpic.Local.POLYFantasyCharacter.Camp
{
using UnityEditor;
using UnityEngine;

public sealed class FantasyCampWaterShaderGUI : ShaderGUI
{
    const string FoldoutStatePrefix = "POLY-FantasyCamp.WaterShaderGUI.Foldout.";

    sealed class SliderSpec
    {
        public readonly string PropertyName;
        public readonly string Label;
        public readonly Vector2 DefaultRange;
        public Vector2 DraftRange;
        public bool DraftInitialized;

        public SliderSpec(string propertyName, string label, float min, float max)
        {
            PropertyName = propertyName;
            Label = label;
            DefaultRange = new Vector2(min, max);
        }

        public string PreferenceKey
        {
            get
            {
                return "POLY-FantasyCamp.WaterShaderGUI.SliderRange." +
                    Application.dataPath.Replace('\\', '/') + "." + PropertyName;
            }
        }
    }

    static readonly SliderSpec[] Sliders =
    {
        new SliderSpec("_Distance", "Depth Color Distance", 0.01f, 20f),
        new SliderSpec("_RefractionSpeed", "Normal Scroll Speed", -1f, 1f),
        new SliderSpec("_NormalPower", "Normal Strength", 0f, 4f),
        new SliderSpec("_RefractionPower", "Refraction Strength", 0f, 1f),
        new SliderSpec("_FoamAmount", "Foam Depth Distance", 0f, 10f),
        new SliderSpec("_FoamSpeed", "Foam Scroll Speed", -5f, 5f),
        new SliderSpec("_FoamScale", "Foam Pattern Scale", 0f, 150f),
        new SliderSpec("_FoamCuttoff", "Foam Threshold", 0f, 30f),
        new SliderSpec("_HightFrequency", "Wave Tiling / Density", 0f, 10f),
        new SliderSpec("_WaveSpeed", "Wave Speed", -10f, 10f),
        new SliderSpec("_WaveAmplitude", "Wave Amplitude", 0f, 1f)
    };

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        MaterialProperty depthEnabled = Property(properties, "_DepthEnabled");
        MaterialProperty refractionEnabled = Property(properties, "_RefractionEnabled");
        MaterialProperty foamEnabled = Property(properties, "_FoamEnabled");
        MaterialProperty wavesEnabled = Property(properties, "_WavesEnabled");

        if (BeginSection("Appearance", "Water Color & Depth", true))
        {
            editor.ShaderProperty(depthEnabled, "Depth Color Enabled");
            using (new EditorGUI.DisabledScope(!IsEnabledOrMixed(depthEnabled)))
            {
                Draw(editor, properties, "_SurfaceColor", "Shallow Color (A = Density)");
                Draw(editor, properties, "_Distance", "Depth Color Distance");
            }
            Draw(editor, properties, "_DeepColor", "Deep Color (A = Density)");
            Draw(editor, properties, "_Smoothness", "Smoothness");
            EditorGUILayout.HelpBox(
                "Color alpha controls density within scene refraction, not the final pass opacity. " +
                "Alpha 1 keeps the water color; lower Shallow / Deep alpha to show the captured background. " +
                "Both default to 1. With refraction disabled, alpha does not reveal the scene.",
                MessageType.Info);
            if (!IsEnabledOrMixed(depthEnabled))
                EditorGUILayout.HelpBox(
                    "Depth color is disabled: Deep Color is used everywhere and shore foam is suspended. " +
                    "Normal animation, scene refraction and vertex waves keep their own settings.",
                    MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("NormalsRefraction", "Normals & Refraction"))
        {
            MaterialProperty normalMap = Property(properties, "_RefreactionNormal");
            editor.TexturePropertySingleLine(new GUIContent("Water Normal Map"), normalMap);
            editor.TextureScaleOffsetProperty(normalMap);
            DrawVector2(editor, Property(properties, "_RefractionScale"), "Normal Tiling");
            Draw(editor, properties, "_RefractionSpeed", "Normal Scroll Speed");
            Draw(editor, properties, "_NormalPower", "Normal Strength");
            EditorGUILayout.HelpBox(
                "Use a texture imported as a Normal Map. The empty slot is flat: assign the original " +
                "Textures/Water/images.jpg or your own normal map for moving ripples. Normal Tiling " +
                "multiplies the texture tiling above. Depth color softens normals near intersections.",
                MessageType.Info);

            editor.ShaderProperty(refractionEnabled, "Scene Refraction Enabled");
            using (new EditorGUI.DisabledScope(!IsEnabledOrMixed(refractionEnabled)))
                Draw(editor, properties, "_RefractionPower", "Refraction Strength");
            EditorGUILayout.HelpBox(
                "Refraction needs a captured scene color: Built-in uses a GrabPass; URP needs Opaque " +
                "Texture; HDRP needs the transparent refraction / color-pyramid settings. Use lower " +
                "color alpha to see the effect. Screen-space refraction cannot reveal off-screen objects.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("Foam", "Shore Foam"))
        {
            editor.ShaderProperty(foamEnabled, "Enabled");
            using (new EditorGUI.DisabledScope(
                !IsEnabledOrMixed(foamEnabled) || !IsEnabledOrMixed(depthEnabled)))
            {
                Draw(editor, properties, "_FoamColor", "Foam Color (A = Coverage)");
                Draw(editor, properties, "_FoamAmount", "Foam Depth Distance");
                Draw(editor, properties, "_FoamSpeed", "Scroll Speed");
                Draw(editor, properties, "_FoamScale", "Pattern Scale");
                Draw(editor, properties, "_FoamCuttoff", "Threshold");
            }
            EditorGUILayout.HelpBox(
                "Foam uses scene-depth intersections and procedural noise; no foam texture is needed. " +
                "Depth Color must be enabled and a valid camera depth texture must be available. " +
                "Turning Depth Color off preserves the foam settings. Foam color alpha controls coverage " +
                "and also participates in the final water density.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("Waves", "Vertex Waves"))
        {
            editor.ShaderProperty(wavesEnabled, "Enabled");
            using (new EditorGUI.DisabledScope(!IsEnabledOrMixed(wavesEnabled)))
            {
                MaterialProperty density = Property(properties, "_HightFrequency");
                DrawSlider(editor, density, new GUIContent("Wave Tiling / Density",
                    "Repeats along object X per object-space unit. Higher values create closer crests; " +
                    "lower values spread them apart. This does not change mesh resolution."),
                    FindSlider("_HightFrequency"));
                if (!density.hasMixedValue)
                {
                    float magnitude = Mathf.Abs(density.floatValue);
                    string spacing = magnitude > 0f
                        ? (2f * Mathf.PI / (6.28f * magnitude)).ToString("0.###") + " object units"
                        : "Uniform motion (no repeating crests)";
                    EditorGUILayout.LabelField("Approx. Crest Spacing", spacing);
                }
                Draw(editor, properties, "_WaveSpeed", "Speed");
                Draw(editor, properties, "_WaveAmplitude", "Amplitude (Object Units)");
            }
            EditorGUILayout.HelpBox(
                "Enable Vertex Waves to animate geometry. Tiling controls crest density along object X: " +
                "0.3 is about 3.3 object units between crests, 1 is about 1, and 3 is about 0.33. " +
                "Speed controls animation and Amplitude controls height. A subdivided mesh is needed " +
                "for dense waves; the shader does not generate geometry. " +
                "Large amplitudes can leave the mesh's original renderer bounds.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("Advanced", "Advanced"))
        {
            Draw(editor, properties, "_Cull", "Cull Mode");
            Draw(editor, properties, "_ZWrite", "Write Depth");
            editor.EnableInstancingField();
            editor.RenderQueueField();
            EditorGUILayout.HelpBox(
                "Defaults are Cull Off, Write Depth On and the Transparent queue. The final pass " +
                "alpha is always 1 because refraction is already composited in the shader. There is " +
                "no shadow-caster or depth-prepass pass. Keep the queue transparent; changing depth " +
                "writes or ordering can affect overlapping transparent objects.",
                MessageType.Info);
            EditorGUILayout.HelpBox(
                "Built-in depth color and foam need camera depth. You can manually add the optional " +
                "FantasyCampWaterDepth component to the camera, including Scene View support. " +
                "URP needs Depth Texture and Opaque Texture. No camera, material or pipeline settings " +
                "are changed automatically by this inspector. CPU batching is disabled to preserve waves.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("SliderRanges", "Slider Ranges"))
        {
            EditorGUILayout.HelpBox(
                "Min / Max set the dragging range only. You can still type exact values outside it. " +
                "Use the Min/Max button beside a slider to edit the same limits directly below it. " +
                "These limits are saved for this project on this computer, not in materials. " +
                "Changing limits or pressing Reset never changes material values. " +
                "Smoothness keeps its standard 0-1 range.", MessageType.Info);
            foreach (SliderSpec spec in Sliders)
                if (DrawSliderRange(spec))
                    editor.Repaint();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    static bool BeginSection(string key, string label, bool defaultOpen = false)
    {
        string stateKey = FoldoutStatePrefix + key;
        bool wasOpen = SessionState.GetBool(stateKey, defaultOpen);
        bool isOpen = EditorGUILayout.BeginFoldoutHeaderGroup(wasOpen, label);
        if (isOpen != wasOpen)
            SessionState.SetBool(stateKey, isOpen);
        return isOpen;
    }

    static bool IsEnabledOrMixed(MaterialProperty property)
    {
        return property.hasMixedValue || property.floatValue > 0.5f;
    }

    static MaterialProperty Property(MaterialProperty[] properties, string name)
    {
        return FindProperty(name, properties);
    }

    static void Draw(MaterialEditor editor, MaterialProperty[] properties,
        string name, string label)
    {
        SliderSpec spec = FindSlider(name);
        MaterialProperty property = Property(properties, name);
        if (spec != null)
            DrawSlider(editor, property, new GUIContent(label), spec);
        else
            editor.ShaderProperty(property, label);
    }

    static SliderSpec FindSlider(string name)
    {
        foreach (SliderSpec spec in Sliders)
            if (spec.PropertyName == name)
                return spec;
        return null;
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

    static Vector2 ReadSliderRange(SliderSpec spec)
    {
        string key = spec.PreferenceKey;
        Vector2 range = new Vector2(
            EditorPrefs.GetFloat(key + ".Min", spec.DefaultRange.x),
            EditorPrefs.GetFloat(key + ".Max", spec.DefaultRange.y));
        return IsValidRange(range) ? range : spec.DefaultRange;
    }

    static void DrawSlider(MaterialEditor editor, MaterialProperty property,
        GUIContent label, SliderSpec spec)
    {
        Vector2 range = ReadSliderRange(spec);
        GUIContent controlLabel = new GUIContent(label);
        controlLabel.tooltip += " Click Min/Max to edit the drag limits, or type an exact value.";
        string inlineStateKey = spec.PreferenceKey + ".InlineOpen";
        bool rangeOpen = SessionState.GetBool(inlineStateKey, false);
        float numberWidth = Mathf.Max(64f, EditorGUIUtility.fieldWidth);
        const float rangeButtonWidth = 62f;
        const float gap = 5f;
        // GetControlRect returns a placeholder during Layout. Keep control IDs consistent
        // by choosing the narrow-inspector fallback from the view width instead.
        float availableWidth = EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth -
            40f - rangeButtonWidth - gap;
        bool drawTrack = availableWidth >= numberWidth + gap + 30f;
        Rect controls = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), controlLabel);
        Rect rangeButtonRect = new Rect(controls.xMax - rangeButtonWidth, controls.y,
            rangeButtonWidth, controls.height);
        Rect numberRect = controls;
        numberRect.xMax = rangeButtonRect.xMin - gap;
        if (drawTrack)
        {
            numberRect.xMin = numberRect.xMax - numberWidth;
        }
        numberRect.width = Mathf.Max(1f, numberRect.width);

        bool previousMixedValue = EditorGUI.showMixedValue;
        int previousIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        try
        {
            EditorGUI.showMixedValue = property.hasMixedValue;
            if (drawTrack)
            {
                Rect trackRect = controls;
                trackRect.xMax = numberRect.xMin - gap;
                trackRect.width = Mathf.Max(1f, trackRect.width);
                float value = property.floatValue;
                // Clamp the thumb only. Repainting must not rewrite existing material values.
                float thumb = IsFinite(value) ? Mathf.Clamp(value, range.x, range.y) : range.x;
                EditorGUI.BeginChangeCheck();
                float draggedValue = GUI.HorizontalSlider(trackRect, thumb, range.x, range.y);
                if (EditorGUI.EndChangeCheck() && IsFinite(draggedValue))
                {
                    editor.RegisterPropertyChangeUndo(label.text);
                    property.floatValue = draggedValue;
                }
                // HorizontalSlider has no mixed-value presentation of its own.
                if (property.hasMixedValue)
                    GUI.Label(trackRect, "Mixed", EditorStyles.centeredGreyMiniLabel);
            }

            // A separate field allows values outside the drag range, including negative speeds.
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            float typedValue = EditorGUI.FloatField(numberRect, property.floatValue);
            if (EditorGUI.EndChangeCheck() && IsFinite(typedValue))
            {
                editor.RegisterPropertyChangeUndo(label.text);
                property.floatValue = typedValue;
            }
        }
        finally
        {
            EditorGUI.showMixedValue = previousMixedValue;
            EditorGUI.indentLevel = previousIndent;
        }

        // Limits are editor preferences, so they remain editable when the effect is disabled.
        bool previousEnabled = GUI.enabled;
        bool previousChanged = GUI.changed;
        bool toggleRange = false;
        try
        {
            GUI.enabled = true;
            EditorGUI.showMixedValue = false;
            bool requestedOpen = GUI.Toggle(rangeButtonRect, rangeOpen,
                new GUIContent("Min/Max", "Show or hide this slider's minimum and maximum."),
                EditorStyles.miniButton);
            toggleRange = requestedOpen != rangeOpen;
            // Do not add/remove GUILayout entries in the event that changes expansion.
            if (rangeOpen && !toggleRange)
                if (DrawSliderRange(spec, false))
                    editor.Repaint();
        }
        finally
        {
            GUI.enabled = previousEnabled;
            GUI.changed = previousChanged;
            EditorGUI.showMixedValue = previousMixedValue;
        }
        if (toggleRange)
        {
            SessionState.SetBool(inlineStateKey, !rangeOpen);
            editor.Repaint();
            GUIUtility.ExitGUI();
        }
    }

    static bool DrawSliderRange(SliderSpec spec, bool showLabel = true)
    {
        if (!spec.DraftInitialized)
        {
            spec.DraftRange = ReadSliderRange(spec);
            spec.DraftInitialized = true;
        }

        // Use the same layout entries on Layout and Repaint, including narrow inspectors.
        if (showLabel)
            EditorGUILayout.LabelField(spec.Label);
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
        bool previousMixedValue = EditorGUI.showMixedValue;
        bool rangeEdited = false;
        EditorGUIUtility.labelWidth = 28f;
        EditorGUI.indentLevel = 0;
        EditorGUI.showMixedValue = false;
        try
        {
            EditorGUI.BeginChangeCheck();
            float min = EditorGUI.FloatField(minRect, new GUIContent("Min"), spec.DraftRange.x);
            float max = EditorGUI.FloatField(maxRect, new GUIContent("Max"), spec.DraftRange.y);
            if (EditorGUI.EndChangeCheck())
            {
                // Retain invalid drafts so both endpoints can be edited in either order.
                rangeEdited = true;
                spec.DraftRange = new Vector2(min, max);
                if (IsValidRange(spec.DraftRange))
                {
                    EditorPrefs.SetFloat(spec.PreferenceKey + ".Min", min);
                    EditorPrefs.SetFloat(spec.PreferenceKey + ".Max", max);
                }
            }
            if (GUI.Button(resetRect, "Reset", EditorStyles.miniButton))
            {
                rangeEdited = true;
                EditorPrefs.DeleteKey(spec.PreferenceKey + ".Min");
                EditorPrefs.DeleteKey(spec.PreferenceKey + ".Max");
                spec.DraftRange = spec.DefaultRange;
            }
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUI.indentLevel = previousIndent;
            GUI.changed = previousChanged;
            EditorGUI.showMixedValue = previousMixedValue;
        }
        if (!IsValidRange(spec.DraftRange))
            EditorGUILayout.HelpBox(
                "Min must be less than Max. Both values and their difference must be finite. " +
                "The slider keeps the last valid limits until this range is corrected.", MessageType.Warning);
        return rangeEdited;
    }

    static void DrawVector2(MaterialEditor editor, MaterialProperty property, string label)
    {
        bool previousMixedValue = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = property.hasMixedValue;
        Vector4 value = property.vectorValue;
        EditorGUI.BeginChangeCheck();
        Vector2 result = EditorGUILayout.Vector2Field(label, new Vector2(value.x, value.y));
        if (EditorGUI.EndChangeCheck())
        {
            editor.RegisterPropertyChangeUndo(label);
            property.vectorValue = new Vector4(result.x, result.y, value.z, value.w);
        }
        EditorGUI.showMixedValue = previousMixedValue;
    }
}

}
