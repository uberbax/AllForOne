namespace Animpic.Local.POLYFantasyCharacter.Camp
{
using UnityEditor;
using UnityEngine;

public sealed class FantasyCampLeafShaderGUI : ShaderGUI
{
    static bool appearanceOpen = true;
    static bool transmissionOpen = true;
    static bool gradientOpen = true;
    static bool mainWindOpen = true;
    static bool flutterOpen = true;
    static bool advancedOpen;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        appearanceOpen = EditorGUILayout.BeginFoldoutHeaderGroup(appearanceOpen,
            "Leaf Appearance");
        if (appearanceOpen)
        {
            editor.TexturePropertySingleLine(new GUIContent("Leaf Texture"),
                Property(properties, "_BaseColor"), Property(properties, "_Tint"));
            Draw(editor, properties, "_TextureColorStrength",
                "Keep Texture Color");
            Draw(editor, properties, "_TintStrength", "Tint Strength");
            Draw(editor, properties, "_Alpha", "Alpha Multiplier");
            Draw(editor, properties, "_Cutoff", "Alpha Cutoff");
            Draw(editor, properties, "_Metallic", "Metallic");
            Draw(editor, properties, "_Smoothness", "Smoothness");
            Draw(editor, properties, "_Occlusion", "Occlusion");
            Draw(editor, properties, "_NormalStrength", "Normal From Color");
            Draw(editor, properties, "_ReceiveShadowStrength",
                "Received Shadow Strength");
            Draw(editor, properties, "_BackfaceLighting",
                "Back Side Direct Light");
            Draw(editor, properties, "_Cull", "Cull Mode");
            EditorGUILayout.HelpBox(
                "The texture alpha defines the leaf silhouette. Keep Texture Color = 1 preserves a colored texture; 0 uses it only as an alpha mask. Received Shadow Strength controls self-shadowing and shadows from the scene. Back Side Direct Light darkens rear faces while indirect light remains available, preventing black leaves. Cull Off is recommended for two-sided leaf cards.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        transmissionOpen = EditorGUILayout.BeginFoldoutHeaderGroup(
            transmissionOpen, "Leaf Transmission");
        if (transmissionOpen)
        {
            Draw(editor, properties, "_TransmissionStrength", "Strength");
            if (Property(properties, "_TransmissionStrength").floatValue > 0.001f)
            {
                Draw(editor, properties, "_TransmissionColor", "Tint");
                Draw(editor, properties, "_TransmissionPower", "Sharpness");
                Draw(editor, properties, "_TransmissionAmbient",
                    "Indirect Fill");
                EditorGUILayout.HelpBox(
                    "Lets scene light pass through thin leaf cards. It follows the lighting available in the active render-pipeline adapter and does not stay fully lit like constant emission.",
                    MessageType.Info);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        gradientOpen = EditorGUILayout.BeginFoldoutHeaderGroup(gradientOpen,
            "Leaf Color Gradient");
        if (gradientOpen)
        {
            MaterialProperty enabled = Property(properties, "_IfGradient");
            editor.ShaderProperty(enabled, "Enabled");
            if (enabled.floatValue > 0.5f)
            {
                Draw(editor, properties, "_BotColor", "Root Color");
                Draw(editor, properties, "_TopColor", "Tip Color");
                MaterialProperty source = Property(properties, "_GradientSource");
                editor.ShaderProperty(source, "Gradient Source");
                Draw(editor, properties, "_GradientPosition", "Gradient Height");
                Draw(editor, properties, "_GradientOffset", "Gradient Offset");
                Draw(editor, properties, "_GradientInvert", "Invert Gradient");
                if (source.floatValue > 2.5f)
                {
                    Draw(editor, properties, "_GradientObjectBottom", "Object Bottom");
                    Draw(editor, properties, "_GradientObjectTop", "Object Top");
                }
                Draw(editor, properties, "_GradientNoiseAmount", "Edge Breakup");
                if (Property(properties, "_GradientNoiseAmount").floatValue > 0.001f)
                    Draw(editor, properties, "_GradientNoiseScale", "Breakup Scale");
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        mainWindOpen = EditorGUILayout.BeginFoldoutHeaderGroup(mainWindOpen,
            "Main Wind");
        if (mainWindOpen)
        {
            bool usesGlobal = FantasyCampWindShaderGUIUtility.DrawGlobalWind(
                editor, Property(properties, "_UseGlobalWind"));
            if (usesGlobal)
            {
                Draw(editor, properties, "_WindResponse", "Leaf Response");
                EditorGUILayout.HelpBox(
                    "Main bend and flutter are controlled by the global settings asset. Response scales this material while keeping its phase synchronized with Tree.",
                    MessageType.Info);
            }
            else
            {
                MaterialProperty enabled = Property(properties, "_Wind_Enabled");
                editor.ShaderProperty(enabled, "Local Wind Enabled");
                if (enabled.floatValue > 0.5f)
                {
                    DrawWindDirection(Property(properties, "_WindDirection"));
                    Draw(editor, properties, "_WindPower", "Bend Strength");
                    Draw(editor, properties, "_WindSpeed", "Wind Speed");
                    DrawVector2(Property(properties, "_UV"),
                        "World Wave Scale (X / Z)");
                    Draw(editor, properties, "_WindTurbulence", "Turbulence");
                    Draw(editor, properties, "_WindRootHeight",
                        "Root Height (Object Y)");
                    Draw(editor, properties, "_WindFlexibility",
                        "Flexibility per Meter");
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        flutterOpen = EditorGUILayout.BeginFoldoutHeaderGroup(flutterOpen,
            "Leaf Flutter");
        if (flutterOpen)
        {
            if (Property(properties, "_UseGlobalWind").floatValue > 0.5f)
            {
                EditorGUILayout.HelpBox(
                    "Flutter Strength, Scale and Speed are stored in FantasyCampGlobalWindSettings.",
                    MessageType.Info);
            }
            else if (Property(properties, "_Wind_Enabled").floatValue > 0.5f)
            {
                Draw(editor, properties, "_LeafWindSwing", "Flutter Strength");
                Draw(editor, properties, "_LeafWindScale", "Flutter Scale");
                Draw(editor, properties, "_LeafWindPower", "Flutter Speed");
                EditorGUILayout.HelpBox(
                    "Flutter adds small cross-wind motion on top of the main branch bend.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Enable Main Wind to use leaf flutter.",
                    MessageType.Info);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        advancedOpen = EditorGUILayout.BeginFoldoutHeaderGroup(advancedOpen,
            "Advanced");
        if (advancedOpen)
        {
            editor.EnableInstancingField();
            editor.RenderQueueField();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        SynchronizeLegacyMainTexture(editor.targets);
    }

    static MaterialProperty Property(MaterialProperty[] properties, string name)
    {
        return FindProperty(name, properties);
    }

    static void Draw(MaterialEditor editor, MaterialProperty[] properties,
        string name, string label)
    {
        MaterialProperty property = Property(properties, name);
        if (property.propertyType == UnityEngine.Rendering.ShaderPropertyType.Range)
        {
            FantasyCampShaderSliderGUI.Draw(editor, property, label, "Leaf",
                property.rangeLimits.x, property.rangeLimits.y);
            return;
        }

        if (property.propertyType == UnityEngine.Rendering.ShaderPropertyType.Float)
        {
            switch (name)
            {
                case "_GradientObjectBottom":
                case "_GradientObjectTop":
                case "_WindRootHeight":
                    FantasyCampShaderSliderGUI.Draw(editor, property, label, "Leaf",
                        -10f, 10f);
                    return;
                case "_GradientNoiseScale":
                    FantasyCampShaderSliderGUI.Draw(editor, property, label, "Leaf",
                        0.01f, 20f);
                    return;
            }
        }

        // Toggle and enum Float properties keep their native material drawers.
        editor.ShaderProperty(property, label);
    }

    static void DrawWindDirection(MaterialProperty property)
    {
        EditorGUI.showMixedValue = property.hasMixedValue;
        Vector4 value = property.vectorValue;
        EditorGUI.BeginChangeCheck();
        Vector2 direction = EditorGUILayout.Vector2Field(
            "Direction (World X / Z)", new Vector2(value.x, value.y));
        if (EditorGUI.EndChangeCheck())
            property.vectorValue = new Vector4(direction.x, direction.y, 0f, 0f);
        EditorGUI.showMixedValue = false;
    }

    static void DrawVector2(MaterialProperty property, string label)
    {
        EditorGUI.showMixedValue = property.hasMixedValue;
        Vector4 value = property.vectorValue;
        EditorGUI.BeginChangeCheck();
        Vector2 result = EditorGUILayout.Vector2Field(label,
            new Vector2(value.x, value.y));
        if (EditorGUI.EndChangeCheck())
            property.vectorValue = new Vector4(result.x, result.y, 0f, 0f);
        EditorGUI.showMixedValue = false;
    }

    static void SynchronizeLegacyMainTexture(Object[] targets)
    {
        foreach (Object target in targets)
        {
            Material material = target as Material;
            if (material == null || !material.HasProperty("_BaseColor")
                || !material.HasProperty("_MainTex"))
                continue;

            Texture texture = material.GetTexture("_BaseColor");
            Vector2 scale = material.GetTextureScale("_BaseColor");
            Vector2 offset = material.GetTextureOffset("_BaseColor");
            bool changed = material.GetTexture("_MainTex") != texture
                || material.GetTextureScale("_MainTex") != scale
                || material.GetTextureOffset("_MainTex") != offset
                || !material.doubleSidedGI;
            if (!changed)
                continue;

            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", scale);
            material.SetTextureOffset("_MainTex", offset);
            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);
        }
    }

}

}
