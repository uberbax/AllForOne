namespace Animpic.Local.POLYFantasyCharacter.Camp
{
using UnityEditor;
using UnityEngine;

public sealed class FantasyCampNatureShaderGUI : ShaderGUI
{
    static bool appearanceOpen = true;
    static bool gradientOpen = true;
    static bool windOpen = true;
    static bool advancedOpen;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        appearanceOpen = EditorGUILayout.BeginFoldoutHeaderGroup(appearanceOpen, "Appearance");
        if (appearanceOpen)
        {
            editor.TexturePropertySingleLine(new GUIContent("Base Texture"),
                Property(properties, "_BaseColor"), Property(properties, "_Tint"));
            Draw(editor, properties, "_Alpha", "Alpha Multiplier");
            Draw(editor, properties, "_Cutoff", "Alpha Cutoff");
            Draw(editor, properties, "_Metallic", "Metallic");
            Draw(editor, properties, "_Smoothness", "Smoothness");
            Draw(editor, properties, "_Occlusion", "Occlusion");
            Draw(editor, properties, "_NormalStrength", "Normal From Color");
            Draw(editor, properties, "_Cull", "Cull Mode");
            EditorGUILayout.HelpBox("The texture alpha channel defines the blade or leaf silhouette. Cull Off is recommended for crossed grass cards.", MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        gradientOpen = EditorGUILayout.BeginFoldoutHeaderGroup(gradientOpen, "Color Gradient");
        if (gradientOpen)
        {
            MaterialProperty enabled = Property(properties, "_IfGradient");
            editor.ShaderProperty(enabled, "Enabled");
            if (enabled.floatValue > 0.5f)
            {
                Draw(editor, properties, "_BotColor", "Bottom Color");
                Draw(editor, properties, "_TopColor", "Top Color");
                MaterialProperty source = Property(properties, "_GradientSource");
                editor.ShaderProperty(source, "Height Source");
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

        windOpen = EditorGUILayout.BeginFoldoutHeaderGroup(windOpen, "Wind Animation");
        if (windOpen)
        {
            bool usesGlobal = FantasyCampWindShaderGUIUtility.DrawGlobalWind(
                editor, Property(properties, "_UseGlobalWind"));
            if (usesGlobal)
            {
                Draw(editor, properties, "_WindResponse", "Nature Response");
                EditorGUILayout.HelpBox(
                    "Global strength is multiplied by Nature Response. The default 0.4 preserves the lighter motion of grass while sharing direction, speed and gusts with trees.",
                    MessageType.Info);
            }
            else
            {
                MaterialProperty enabled = Property(properties, "_Wind_Enabled");
                editor.ShaderProperty(enabled, "Local Wind Enabled");
                if (enabled.floatValue > 0.5f)
                {
                    DrawWindDirection(Property(properties, "_WindDirection"));
                    Draw(editor, properties, "_WindPower", "Main Strength");
                    Draw(editor, properties, "_LeaveSpeed", "Speed");
                    Draw(editor, properties, "_WindScale", "Wave Scale");
                    Draw(editor, properties, "_WindChaotic", "Turbulence");

                    EditorGUILayout.Space(3f);
                    EditorGUILayout.LabelField("Small Blade / Leaf Motion",
                        EditorStyles.boldLabel);
                    Draw(editor, properties, "_LeafWindSwing", "Strength");
                    Draw(editor, properties, "_LeafWindScale", "Scale");
                    Draw(editor, properties, "_LeafWindPower", "Speed Multiplier");
                }
            }

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Geometry Root Mask", EditorStyles.boldLabel);
            MaterialProperty source = Property(properties, "_WindMaskSource");
            editor.ShaderProperty(source, "Source");
            Draw(editor, properties, "_WindMaskInvert", "Invert Mask");
            Draw(editor, properties, "_WindMaskPower", "Root Stiffness");
            if (source.floatValue > 2.5f)
            {
                Draw(editor, properties, "_WindObjectBottom", "Object Bottom");
                Draw(editor, properties, "_WindObjectTop", "Object Top");
            }
            EditorGUILayout.HelpBox(
                "The root mask stays material-specific because it describes this mesh, not the weather. UV0 V is the safest default for grass cards.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        advancedOpen = EditorGUILayout.BeginFoldoutHeaderGroup(advancedOpen, "Advanced");
        if (advancedOpen)
        {
            DrawVector3(Property(properties, "_Offset"), "Geometry Offset");
            editor.EnableInstancingField();
            editor.RenderQueueField();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
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
            FantasyCampShaderSliderGUI.Draw(editor, property, label, "Nature",
                property.rangeLimits.x, property.rangeLimits.y);
            return;
        }

        if (property.propertyType == UnityEngine.Rendering.ShaderPropertyType.Float)
        {
            switch (name)
            {
                case "_GradientObjectBottom":
                case "_GradientObjectTop":
                case "_WindObjectBottom":
                case "_WindObjectTop":
                    FantasyCampShaderSliderGUI.Draw(editor, property, label, "Nature",
                        -10f, 10f);
                    return;
                case "_GradientNoiseScale":
                    FantasyCampShaderSliderGUI.Draw(editor, property, label, "Nature",
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
        Vector2 direction = EditorGUILayout.Vector2Field("Direction (World X / Z)",
            new Vector2(value.x, value.y));
        if (EditorGUI.EndChangeCheck())
            property.vectorValue = new Vector4(direction.x, direction.y, 0f, 0f);
        EditorGUI.showMixedValue = false;
    }

    static void DrawVector3(MaterialProperty property, string label)
    {
        EditorGUI.showMixedValue = property.hasMixedValue;
        Vector4 value = property.vectorValue;
        EditorGUI.BeginChangeCheck();
        Vector3 result = EditorGUILayout.Vector3Field(label,
            new Vector3(value.x, value.y, value.z));
        if (EditorGUI.EndChangeCheck())
            property.vectorValue = new Vector4(result.x, result.y, result.z, 0f);
        EditorGUI.showMixedValue = false;
    }
}

}
