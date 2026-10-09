namespace Animpic.Local.POLYFantasyCharacter.Camp
{
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FantasyCampGlobalWindSettings))]
public sealed class FantasyCampGlobalWindSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "These values drive every POLY-FantasyCamp Tree, Leaf and Nature material that has Use Global Wind enabled. Changes are applied immediately in Scene View and in Play Mode.",
            MessageType.Info);

        EditorGUILayout.LabelField("Main Wind", EditorStyles.boldLabel);
        Draw("windEnabled", "Enabled");
        Draw("direction", "Direction (World X / Z)");
        Draw("strength", "Strength");
        Draw("speed", "Speed");
        Draw("worldWaveScale", "World Wave Scale (X / Z)");
        Draw("turbulence", "Turbulence");

        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("Tree and Leaf Bend", EditorStyles.boldLabel);
        Draw("rootHeight", "Root Height (Object Y)");
        Draw("flexibility", "Flexibility per Meter");

        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("Small Leaf and Grass Motion",
            EditorStyles.boldLabel);
        Draw("flutterStrength", "Flutter Strength");
        Draw("flutterScale", "Flutter Scale");
        Draw("flutterSpeed", "Flutter Speed");

        bool changed = serializedObject.ApplyModifiedProperties();
        FantasyCampGlobalWindSettings settings =
            (FantasyCampGlobalWindSettings)target;
        if (changed && FantasyCampGlobalWindSettings.LoadDefault() == settings)
            settings.Apply();

        EditorGUILayout.Space(5f);
        if (GUILayout.Button("Apply Global Wind Now"))
        {
            settings.Apply();
            SceneView.RepaintAll();
        }
    }

    void Draw(string propertyName, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        float min;
        float max;
        // Defaults match the existing Range attributes; validation and Apply stay unchanged.
        switch (propertyName)
        {
            case "strength": min = 0f; max = 20f; break;
            case "speed": min = 0f; max = 10f; break;
            case "turbulence": min = 0f; max = 3f; break;
            case "rootHeight": min = -10f; max = 10f; break;
            case "flexibility": min = 0.01f; max = 2f; break;
            case "flutterStrength": min = 0f; max = 0.5f; break;
            case "flutterScale": min = 0.01f; max = 20f; break;
            case "flutterSpeed": min = 0.01f; max = 5f; break;
            default:
                EditorGUILayout.PropertyField(property, new GUIContent(label));
                return;
        }
        FantasyCampShaderSliderGUI.DrawSerialized(this, property, label, "GlobalWind", min, max);
    }
}

[InitializeOnLoad]
static class FantasyCampGlobalWindEditorBridge
{
    static FantasyCampGlobalWindEditorBridge()
    {
        EditorApplication.delayCall += Apply;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Apply();
    }

    static void Apply()
    {
        FantasyCampGlobalWindSettings settings =
            FantasyCampGlobalWindSettings.LoadDefault();
        if (settings != null)
            settings.Apply();
        else
            FantasyCampGlobalWindSettings.ClearGlobals();
        SceneView.RepaintAll();
    }
}

internal static class FantasyCampWindShaderGUIUtility
{
    public static bool DrawGlobalWind(MaterialEditor editor,
        MaterialProperty useGlobalWind)
    {
        editor.ShaderProperty(useGlobalWind, "Use Global Wind");
        if (useGlobalWind.floatValue <= 0.5f)
            return false;

        FantasyCampGlobalWindSettings settings =
            FantasyCampGlobalWindSettings.LoadDefault();
        if (settings == null)
        {
            EditorGUILayout.HelpBox(
                "No FantasyCampGlobalWindSettings asset was found in a Resources folder. The material is using its legacy local fallback values.",
                MessageType.Warning);
            return true;
        }

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Global Settings", settings,
                typeof(FantasyCampGlobalWindSettings), false);
        if (GUILayout.Button("Select Global Wind Settings"))
            Selection.activeObject = settings;
        return true;
    }
}

}
