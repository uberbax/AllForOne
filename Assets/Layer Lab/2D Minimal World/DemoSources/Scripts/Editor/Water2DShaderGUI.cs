using UnityEditor;
using UnityEngine;

namespace LayerLab
{
    // Custom material inspector for the Water2D shader.
    // Groups properties into collapsible sections and gates each feature behind its toggle.
    public sealed class Water2DShaderGUI : ShaderGUI
    {
        private bool showBase = true;
        private bool showDepth = true;
        private bool showShadow = true;
        private bool showHighlight = true;
        private bool showFoam = true;
        private bool showLighting = true;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            DrawBaseSection(materialEditor, properties);
            DrawDepthSection(materialEditor, properties);
            DrawShadowSection(materialEditor, properties);
            DrawHighlightSection(materialEditor, properties);
            DrawFoamSection(materialEditor, properties);
            DrawLightingSection(materialEditor, properties);

            EditorGUILayout.Space(8f);
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
        }

        private void DrawBaseSection(MaterialEditor editor, MaterialProperty[] properties)
        {
            DrawFoldout("Base", ref showBase, () =>
            {
                DrawProperty(editor, properties, "Sprite Color", "_Color");
                DrawProperty(editor, properties, "Water Alpha", "_WaterAlpha");
            });
        }

        private void DrawDepthSection(MaterialEditor editor, MaterialProperty[] properties)
        {
            DrawFeatureFoldout(editor, properties, "Depth Color", ref showDepth, () =>
            {
                DrawProperty(editor, properties, "Shallow Water Color", "_TopColor");
                DrawProperty(editor, properties, "Deep Water Color", "_DeepColor");
                DrawProperty(editor, properties, "Deep Color Start", "_DeepDepthStart");
                DrawProperty(editor, properties, "Deep Color End", "_DeepDepthEnd");
                DrawProperty(editor, properties, "Depth Color Y Offset", "_DepthColorOffsetY");
            }, "_DepthColorEnabled");
        }

        private void DrawShadowSection(MaterialEditor editor, MaterialProperty[] properties)
        {
            DrawFeatureFoldout(editor, properties, "Shore Shadow", ref showShadow, () =>
            {
                DrawProperty(editor, properties, "Shore Shadow Color", "_ShoreShadowColor", "_ShoreReflectionColor");
                DrawProperty(editor, properties, "Shore Shadow Strength", "_ShoreShadowStrength", "_ShoreReflectionStrength");
                DrawProperty(editor, properties, "Shore Shadow Width", "_ShoreShadowWidth", "_ShoreReflectionWidth");
                DrawProperty(editor, properties, "Shore Shadow Softness", "_ShoreShadowSoftness", "_ShoreReflectionSoftness");
                DrawProperty(editor, properties, "Shore Shadow Y Offset", "_ShoreShadowOffsetY", "_ShoreReflectionOffsetY");
                DrawProperty(editor, properties, "Shore Shadow Warp", "_ShoreShadowWarpStrength", "_ShoreReflectionWarpStrength");
                DrawProperty(editor, properties, "Shore Shadow Motion Strength", "_ShoreShadowMotionStrength", "_ShoreReflectionMotionStrength");
                DrawProperty(editor, properties, "Shore Shadow Motion Scale", "_ShoreShadowMotionScale", "_ShoreReflectionMotionScale");
                DrawProperty(editor, properties, "Shore Shadow Motion Speed", "_ShoreShadowMotionSpeed", "_ShoreReflectionMotionSpeed");
            }, "_ShoreShadowEnabled", "_ShoreReflectionEnabled");
        }

        private void DrawHighlightSection(MaterialEditor editor, MaterialProperty[] properties)
        {
            DrawFeatureFoldout(editor, properties, "Wave Highlight", ref showHighlight, () =>
            {
                DrawProperty(editor, properties, "Highlight Color", "_LineColor");
                DrawProperty(editor, properties, "Highlight Scale", "_LineScale");
                DrawProperty(editor, properties, "Highlight Density", "_LineDensity");
                DrawProperty(editor, properties, "Highlight Width", "_LineWidth");
                DrawProperty(editor, properties, "Highlight Length", "_LineLength");
                DrawProperty(editor, properties, "Highlight Strength", "_LineStrength");
                DrawProperty(editor, properties, "Highlight Cycle Speed", "_LineSpeed");
            }, "_LineHighlightEnabled");
        }

        private void DrawFoamSection(MaterialEditor editor, MaterialProperty[] properties)
        {
            DrawFeatureFoldout(editor, properties, "Shore Foam", ref showFoam, () =>
            {
                DrawProperty(editor, properties, "Shore Foam Color", "_ShoreFoamColor");
                DrawProperty(editor, properties, "Shore Foam Strength", "_ShoreFoamStrength");
                DrawProperty(editor, properties, "Shore Foam Width", "_ShoreFoamWidth");
                DrawProperty(editor, properties, "Shore Foam Softness", "_ShoreFoamSoftness");
                DrawProperty(editor, properties, "Shore Foam Noise", "_ShoreFoamNoiseStrength");
                DrawProperty(editor, properties, "Shore Foam Noise Scale", "_ShoreFoamNoiseScale");
                DrawProperty(editor, properties, "Shore Foam Warp", "_ShoreFoamWarpStrength");
                DrawProperty(editor, properties, "Shore Foam Warp Scale", "_ShoreFoamWarpScale");
                DrawProperty(editor, properties, "Shore Foam Motion Strength", "_ShoreFoamMotionStrength");
                DrawProperty(editor, properties, "Shore Foam Motion Scale", "_ShoreFoamMotionScale");
                DrawProperty(editor, properties, "Shore Foam Motion Speed", "_ShoreFoamMotionSpeed");
                DrawProperty(editor, properties, "Shore Foam Y Offset", "_ShoreFoamOffsetY");
            }, "_ShoreFoamEnabled");
        }

        private void DrawLightingSection(MaterialEditor editor, MaterialProperty[] properties)
        {
            DrawFeatureFoldout(editor, properties, "DayNight Lighting", ref showLighting, () =>
            {
                DrawProperty(editor, properties, "Light Influence", "_LightInfluence");
                DrawProperty(editor, properties, "Min Brightness", "_MinBrightness");
            }, "_DayNightLightingEnabled");
        }

        // Foldout whose body is enabled only when at least one of the feature toggle properties is on.
        private void DrawFeatureFoldout(
            MaterialEditor editor,
            MaterialProperty[] properties,
            string title,
            ref bool show,
            System.Action drawBody,
            params string[] enabledPropertyNames)
        {
            DrawFoldout(title, ref show, () =>
            {
                MaterialProperty enabledProperty = FindFirstProperty(properties, enabledPropertyNames);
                bool enabled = DrawToggle(editor, enabledProperty);
                using (new EditorGUI.DisabledScope(!enabled))
                {
                    drawBody();
                }
            });
        }

        private bool DrawToggle(MaterialEditor editor, MaterialProperty property)
        {
            if (property == null)
            {
                return true;
            }

            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle("Enabled", property.floatValue > 0.5f);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo(property.displayName);
                property.floatValue = enabled ? 1f : 0f;
            }

            return enabled;
        }

        private void DrawFoldout(string title, ref bool show, System.Action drawBody)
        {
            EditorGUILayout.Space(4f);
            show = EditorGUILayout.BeginFoldoutHeaderGroup(show, title);
            if (show)
            {
                EditorGUI.indentLevel++;
                drawBody();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawProperty(MaterialEditor editor, MaterialProperty[] properties, string label, params string[] propertyNames)
        {
            MaterialProperty property = FindFirstProperty(properties, propertyNames);
            if (property == null)
            {
                return;
            }

            editor.ShaderProperty(property, new GUIContent(label));
        }

        // Returns the first existing property among the given names, or null if none are present.
        private MaterialProperty FindFirstProperty(MaterialProperty[] properties, params string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                MaterialProperty property = FindProperty(propertyNames[i], properties, false);
                if (property != null)
                {
                    return property;
                }
            }

            return null;
        }
    }
}
