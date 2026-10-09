namespace Animpic.Local.POLYFantasyCharacter.Camp
{
using UnityEditor;
using UnityEngine;

public sealed class FantasyCampSurfaceShaderGUI : ShaderGUI
{
    const string FoldoutStatePrefix = "POLY-FantasyCamp.SurfaceShaderGUI.Foldout.";

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        if (BeginSection("BaseSurface", "Base Surface", true))
        {
            MaterialProperty baseTexture = FindProperty("_BaseColor", properties);
            editor.TexturePropertySingleLine(new GUIContent("Base Texture"), baseTexture);
            editor.TextureScaleOffsetProperty(baseTexture);
            MaterialProperty alphaClip = FindProperty("_AlphaClip", properties);
            editor.ShaderProperty(alphaClip, "Alpha Clipping");
            if (IsEnabledOrMixed(alphaClip))
                Property(editor, properties, "_Cutoff", "Alpha Cutoff");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("Detail", "Detail Layer"))
        {
            MaterialProperty enabled = FindProperty("_DetailEnabled", properties);
            editor.ShaderProperty(enabled, "Enabled");
            if (IsEnabledOrMixed(enabled))
            {
                Texture(editor, properties, "_DetailTexture", "Texture");
                Property(editor, properties, "_DetailColor", "Tint");
                Property(editor, properties, "_DetailBlendMode", "Blend Mode");
                Property(editor, properties, "_DetailIntensity", "Intensity");
                Property(editor, properties, "_DetailTiling", "World-space Tiling");
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("Moss", "Moss — Ground Up"))
        {
            MaterialProperty enabled = FindProperty("_MossEnabled", properties);
            editor.ShaderProperty(enabled, "Enabled");
            if (IsEnabledOrMixed(enabled))
            {
                Texture(editor, properties, "_MossTexture", "Texture");
                Property(editor, properties, "_MossColor", "Color");
                Property(editor, properties, "_MossTiling", "Object-space Tiling");
                Property(editor, properties, "_MossGroundHeight", "Ground Offset (World Y)");
                Property(editor, properties, "_MossHeight", "Coverage Height");
                Property(editor, properties, "_MossIntensity", "Intensity");
                Property(editor, properties, "_MossEdgeSoftness", "Top Edge Softness");
                EditorGUILayout.HelpBox(
                    "Ground Offset is a world-Y distance from this object's pivot, not an absolute world height. " +
                    "Coverage and edge softness use fixed world units. Moving the object carries the mask with it; " +
                    "rotation or scale recalculates coverage while the boundary stays horizontal. " +
                    "Only the object-space texture stays on the same local mesh points through all transforms. " +
                    "Bounds are not detected automatically; previously saved local heights may need adjustment.",
                    MessageType.Info);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("TopMoss", "Moss — Top Down"))
        {
            MaterialProperty enabled = FindProperty("_TopMossEnabled", properties);
            editor.ShaderProperty(enabled, "Enabled");
            if (IsEnabledOrMixed(enabled))
            {
                Texture(editor, properties, "_TopMossTexture", "Texture");
                Property(editor, properties, "_TopMossColor", "Color");
                Property(editor, properties, "_TopMossTiling", "Object-space Tiling");
                Property(editor, properties, "_TopMossTopHeight", "Top Offset (World Y)");
                Property(editor, properties, "_TopMossHeight", "Coverage Depth");
                Property(editor, properties, "_TopMossIntensity", "Intensity");
                Property(editor, properties, "_TopMossEdgeSoftness", "Bottom Edge Softness");
                EditorGUILayout.HelpBox(
                    "Top Offset is a world-Y distance from the pivot, not an absolute world height. " +
                    "The lower boundary is Top Offset minus Coverage Depth; coverage and softness use world units. " +
                    "The mask follows translation, but rotation or scale changes coverage and keeps the edge horizontal. " +
                    "The texture remains object-space. Depth of zero or less disables moss. " +
                    "Bounds are not detected automatically, and moss texture alpha is ignored.",
                    MessageType.Info);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("Rust", "Rust"))
        {
            MaterialProperty enabled = FindProperty("_RustEnabled", properties);
            editor.ShaderProperty(enabled, "Enabled");
            if (IsEnabledOrMixed(enabled))
            {
                Property(editor, properties, "_RustColor", "Color");
                MaterialProperty useTexture = FindProperty("_RustUseTexture", properties);
                editor.ShaderProperty(useTexture, "Use Texture");
                if (IsEnabledOrMixed(useTexture))
                {
                    Texture(editor, properties, "_RustTexture", "Texture");
                    Property(editor, properties, "_RustTiling", "World-space Tiling");
                }

                if (Subsection("Rust.Distribution", "Top-down Distribution"))
                {
                    EditorGUI.indentLevel++;
                    Property(editor, properties, "_RustHeightSource", "Height Source");
                    Property(editor, properties, "_RustHeightInvert", "Invert Height");
                    Property(editor, properties, "_RustDownwardCoverage", "Coverage From Top");
                    Property(editor, properties, "_RustTopDownSoftness", "Bottom Edge Softness");
                    Property(editor, properties, "_RustTopDownIntensity", "Intensity");
                    EditorGUILayout.HelpBox(
                        "Rust starts at the top. UV0/UV1 expect V=0 at the bottom and V=1 at the top. " +
                        "Vertex Color expects black at the bottom and red at the top.",
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                }

                if (Subsection("Rust.Breakup", "Procedural Breakup"))
                {
                    EditorGUI.indentLevel++;
                    MaterialProperty procedural = FindProperty("_RustProceduralEnabled", properties);
                    editor.ShaderProperty(procedural, "Enabled");
                    if (IsEnabledOrMixed(procedural))
                    {
                        Property(editor, properties, "_RustProceduralScale", "Cell Scale");
                        Property(editor, properties, "_RustProceduralCoverage", "Cell Coverage");
                        Property(editor, properties, "_RustProceduralSoftness", "Cell Softness");
                        Property(editor, properties, "_RustProceduralSeed", "Seed");
                        Property(editor, properties, "_RustProceduralInfluence", "Influence");
                    }
                    EditorGUI.indentLevel--;
                }

                if (Subsection("Rust.Flows", "Vertical Flows"))
                {
                    EditorGUI.indentLevel++;
                    MaterialProperty flow = FindProperty("_RustFlowEnabled", properties);
                    editor.ShaderProperty(flow, "Enabled");
                    if (IsEnabledOrMixed(flow))
                    {
                        Property(editor, properties, "_RustFlowScale", "Flow Scale");
                        Property(editor, properties, "_RustFlowStretch", "Vertical Stretch");
                        Property(editor, properties, "_RustFlowDistortion", "Distortion");
                        Property(editor, properties, "_RustFlowCoverage", "Coverage");
                        Property(editor, properties, "_RustFlowSoftness", "Softness");
                        Property(editor, properties, "_RustFlowInfluence", "Influence");
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("Projection", "Shared Projection"))
        {
            Property(editor, properties, "_TriplanarSharpness", "Sharpness");
            Property(editor, properties, "_TriplanarSeamSmoothing", "Seam Smoothing");
            EditorGUILayout.HelpBox(
                "Sharpness and smoothing are shared by all triplanar layers. Both moss layers use " +
                "object-space projection; Detail and Rust keep world-space projection. " +
                "Smoothing does not align separate meshes with different local pivots.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (BeginSection("Advanced", "Advanced"))
        {
            editor.EnableInstancingField();
            editor.DoubleSidedGIField();
            editor.RenderQueueField();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

    }

    static void Property(MaterialEditor editor, MaterialProperty[] properties, string name, string label)
    {
        MaterialProperty property = FindProperty(name, properties);
        if (TryGetSliderRange(property, out Vector2 range))
            FantasyCampShaderSliderGUI.Draw(editor, property, label, "Surface", range.x, range.y);
        else
            editor.ShaderProperty(property, label);
    }

    static bool TryGetSliderRange(MaterialProperty property, out Vector2 range)
    {
        range = Vector2.zero;
        if (property.propertyType == UnityEngine.Rendering.ShaderPropertyType.Range)
        {
            range = property.rangeLimits;
            return true;
        }
        if (property.propertyType != UnityEngine.Rendering.ShaderPropertyType.Float)
            return false;

        // Only continuous Float properties use sliders; toggles and enums keep their native drawers.
        switch (property.name)
        {
            case "_DetailTiling":
            case "_MossTiling":
            case "_TopMossTiling":
            case "_RustTiling":
                range = new Vector2(0.01f, 20f);
                return true;
            case "_RustProceduralScale":
            case "_RustFlowScale":
                range = new Vector2(0.01f, 30f);
                return true;
            case "_MossGroundHeight":
            case "_TopMossTopHeight":
                range = new Vector2(-10f, 10f);
                return true;
            case "_MossHeight":
            case "_TopMossHeight":
                range = new Vector2(0f, 10f);
                return true;
            case "_RustProceduralSeed":
                range = new Vector2(0f, 100f);
                return true;
            default:
                return false;
        }
    }

    static void Texture(MaterialEditor editor, MaterialProperty[] properties, string name, string label)
        => editor.TexturePropertySingleLine(new GUIContent(label), FindProperty(name, properties));

    // ShaderProperty draws the mixed-value toggle; retain its settings while any
    // selected material may use the layer instead of trusting only the first one.
    static bool IsEnabledOrMixed(MaterialProperty property)
        => property.hasMixedValue || property.floatValue > 0.5f;

    // SessionState survives assembly reloads, but never writes foldout state into materials.
    // Top-level header groups must be closed before the next group starts.
    static bool BeginSection(string key, string label, bool defaultOpen = false)
    {
        string stateKey = FoldoutStatePrefix + key;
        bool wasOpen = SessionState.GetBool(stateKey, defaultOpen);
        bool isOpen = EditorGUILayout.BeginFoldoutHeaderGroup(wasOpen, label);
        if (isOpen != wasOpen)
            SessionState.SetBool(stateKey, isOpen);
        return isOpen;
    }

    // Ordinary foldouts are safe inside a FoldoutHeaderGroup; header groups cannot nest.
    static bool Subsection(string key, string label)
    {
        EditorGUILayout.Space(4);
        string stateKey = FoldoutStatePrefix + key;
        bool wasOpen = SessionState.GetBool(stateKey, false);
        bool isOpen = EditorGUILayout.Foldout(wasOpen, label, true);
        if (isOpen != wasOpen)
            SessionState.SetBool(stateKey, isOpen);
        return isOpen;
    }
}

}
