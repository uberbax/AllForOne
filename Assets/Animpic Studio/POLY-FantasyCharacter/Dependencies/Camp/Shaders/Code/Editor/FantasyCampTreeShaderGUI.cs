namespace Animpic.Local.POLYFantasyCharacter.Camp
{
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class FantasyCampTreeShaderGUI : ShaderGUI
{
    const string MossFoldoutStatePrefix = "POLY-FantasyCamp.TreeShaderGUI.Foldout.";

    static bool barkOpen = true;
    static bool gradientOpen = true;
    static bool windOpen = true;
    static bool advancedOpen;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        barkOpen = EditorGUILayout.BeginFoldoutHeaderGroup(barkOpen,
            "Bark Surface");
        if (barkOpen)
        {
            editor.TexturePropertySingleLine(new GUIContent("Bark Texture"),
                Property(properties, "_BaseColor"), Property(properties, "_Tint"));
            MaterialProperty alphaClip = Property(properties, "_AlphaClip");
            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(alphaClip, "Alpha Clipping");
            if (EditorGUI.EndChangeCheck())
                ApplyAlphaMode(editor.targets, alphaClip.floatValue > 0.5f);

            if (alphaClip.floatValue > 0.5f)
            {
                Draw(editor, properties, "_Alpha", "Alpha Multiplier");
                Draw(editor, properties, "_Cutoff", "Alpha Cutoff");
            }

            Draw(editor, properties, "_Metallic", "Metallic");
            Draw(editor, properties, "_Smoothness", "Smoothness");
            Draw(editor, properties, "_Occlusion", "Occlusion");
            Draw(editor, properties, "_NormalStrength", "Normal From Color");
            Draw(editor, properties, "_Cull", "Cull Mode");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        gradientOpen = EditorGUILayout.BeginFoldoutHeaderGroup(gradientOpen,
            "Trunk Color Gradient");
        if (gradientOpen)
        {
            MaterialProperty enabled = Property(properties, "_TreeGradientEnabled");
            editor.ShaderProperty(enabled, "Enabled");
            if (enabled.floatValue > 0.5f)
            {
                Draw(editor, properties, "_TreeBottomColor", "Root Color");
                Draw(editor, properties, "_TreeTopColor", "Crown Color");
                MaterialProperty source = Property(properties, "_TreeGradientSource");
                editor.ShaderProperty(source, "Gradient Source");
                Draw(editor, properties, "_TreeGradientScale", "Gradient Height");
                Draw(editor, properties, "_TreeGradientOffset", "Gradient Offset");
                Draw(editor, properties, "_TreeGradientInvert", "Invert Gradient");
                if (source.floatValue > 2.5f)
                {
                    Draw(editor, properties, "_TreeGradientObjectBottom",
                        "Object Bottom");
                    Draw(editor, properties, "_TreeGradientObjectTop",
                        "Object Top");
                }
                Draw(editor, properties, "_TreeGradientBreakup", "Edge Breakup");
                if (Property(properties, "_TreeGradientBreakup").floatValue > 0.001f)
                    Draw(editor, properties, "_TreeGradientBreakupScale",
                        "Breakup Scale");
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginMossSection("Moss", "Moss — Ground Up"))
        {
            MaterialProperty enabled = Property(properties, "_MossEnabled");
            editor.ShaderProperty(enabled, "Enabled");
            if (IsEnabledOrMixed(enabled))
            {
                editor.TexturePropertySingleLine(new GUIContent("Texture"),
                    Property(properties, "_MossTexture"));
                Draw(editor, properties, "_MossColor", "Color");
                Draw(editor, properties, "_MossTiling", "Object-space Tiling");
                Draw(editor, properties, "_MossGroundHeight", "Ground Offset (World Y)");
                Draw(editor, properties, "_MossHeight", "Coverage Height");
                Draw(editor, properties, "_MossIntensity", "Intensity");
                Draw(editor, properties, "_MossEdgeSoftness", "Top Edge Softness");
                EditorGUILayout.HelpBox(
                    "Ground Offset is a world-Y distance from this object's pivot, not an absolute world height. " +
                    "Coverage and softness use fixed world units. The mask follows the wind-deformed position " +
                    "and stays world-horizontal; coverage can move over the bark during bending. " +
                    "The texture uses the original object-space position before wind, so its pattern stays attached. " +
                    "Object bounds are not detected automatically.",
                    MessageType.Info);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginMossSection("TopMoss", "Moss — Top Down"))
        {
            MaterialProperty enabled = Property(properties, "_TopMossEnabled");
            editor.ShaderProperty(enabled, "Enabled");
            if (IsEnabledOrMixed(enabled))
            {
                editor.TexturePropertySingleLine(new GUIContent("Texture"),
                    Property(properties, "_TopMossTexture"));
                Draw(editor, properties, "_TopMossColor", "Color");
                Draw(editor, properties, "_TopMossTiling", "Object-space Tiling");
                Draw(editor, properties, "_TopMossTopHeight", "Top Offset (World Y)");
                Draw(editor, properties, "_TopMossHeight", "Coverage Depth");
                Draw(editor, properties, "_TopMossIntensity", "Intensity");
                Draw(editor, properties, "_TopMossEdgeSoftness", "Bottom Edge Softness");
                EditorGUILayout.HelpBox(
                    "Top Offset is a world-Y distance from the pivot. The lower boundary is Top Offset minus " +
                    "Coverage Depth; coverage and softness use world units. The wind-deformed height determines " +
                    "coverage while the texture stays attached to the original mesh. Depth of zero or less " +
                    "disables moss. Both moss layers are independent and blend RGB only, ignoring texture alpha.",
                    MessageType.Info);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginMossSection("MossProjection", "Moss Projection"))
        {
            Draw(editor, properties, "_TriplanarSharpness", "Sharpness");
            Draw(editor, properties, "_TriplanarSeamSmoothing", "Seam Smoothing");
            EditorGUILayout.HelpBox(
                "Shared by both moss layers. Texture coordinates and projection weights use the original " +
                "object-space mesh before wind. Smoothing cannot align separate modules with different pivots " +
                "or local spaces. Bark texture, gradient and wind settings are unchanged.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        windOpen = EditorGUILayout.BeginFoldoutHeaderGroup(windOpen,
            "Tree Wind");
        if (windOpen)
        {
            bool usesGlobal = FantasyCampWindShaderGUIUtility.DrawGlobalWind(
                editor, Property(properties, "_UseGlobalWind"));
            if (usesGlobal)
            {
                Draw(editor, properties, "_WindResponse", "Tree Response");
                EditorGUILayout.HelpBox(
                    "Direction, strength, speed, wave shape and bend are controlled by the global settings asset. Response scales only this material without desynchronizing the wind phase.",
                    MessageType.Info);
            }
            else
            {
                MaterialProperty enabled = Property(properties, "_Wind_Enabled");
                editor.ShaderProperty(enabled, "Local Wind Enabled");
                if (enabled.floatValue > 0.5f)
                {
                    DrawVector2(Property(properties, "_WindDirection"),
                        "Direction (World X / Z)");
                    Draw(editor, properties, "_WindPower", "Wind Strength");
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

        advancedOpen = EditorGUILayout.BeginFoldoutHeaderGroup(advancedOpen,
            "Advanced");
        if (advancedOpen)
        {
            editor.EnableInstancingField();
            editor.RenderQueueField();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // New moss sections remember their UI state without writing to materials.
    static bool BeginMossSection(string key, string label)
    {
        string stateKey = MossFoldoutStatePrefix + key;
        bool wasOpen = SessionState.GetBool(stateKey, false);
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
        MaterialProperty property = Property(properties, name);
        if (TryGetSliderRange(property, out Vector2 range))
            FantasyCampShaderSliderGUI.Draw(editor, property, label, "Tree", range.x, range.y);
        else
            editor.ShaderProperty(property, label);
    }

    static bool TryGetSliderRange(MaterialProperty property, out Vector2 range)
    {
        range = Vector2.zero;
        if (property.propertyType == ShaderPropertyType.Range)
        {
            range = property.rangeLimits;
            return true;
        }
        if (property.propertyType != ShaderPropertyType.Float)
            return false;

        // Only continuous Float properties use sliders; toggles and enums keep their native drawers.
        switch (property.name)
        {
            case "_MossTiling":
            case "_TopMossTiling":
            case "_TreeGradientBreakupScale":
                range = new Vector2(0.01f, 20f);
                return true;
            case "_MossGroundHeight":
            case "_TopMossTopHeight":
                range = new Vector2(-10f, 10f);
                return true;
            case "_MossHeight":
            case "_TopMossHeight":
                range = new Vector2(0f, 10f);
                return true;
            case "_TreeGradientObjectBottom":
            case "_TreeGradientObjectTop":
            case "_WindRootHeight":
                range = new Vector2(-10f, 20f);
                return true;
            default:
                return false;
        }
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

    static void ApplyAlphaMode(Object[] targets, bool alphaClip)
    {
        foreach (Object target in targets)
        {
            Material material = target as Material;
            if (material == null)
                continue;

            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry;
            }
            EditorUtility.SetDirty(material);
        }
    }
}

}
