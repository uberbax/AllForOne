#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ClickObjectSpawner
{
    private const string EnabledKey = "ClickObjectSpawner.Enabled";
    private const string ParentNameKey = "ClickObjectSpawner.ParentName";
    private const string RadiusKey = "ClickObjectSpawner.Radius";

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledKey, false);
        set => EditorPrefs.SetBool(EnabledKey, value);
    }

    public static string ParentName
    {
        get => EditorPrefs.GetString(ParentNameKey, "Tier1");
        set => EditorPrefs.SetString(ParentNameKey, value);
    }

    public static float Radius
    {
        get => EditorPrefs.GetFloat(RadiusKey, 0.5f);
        set => EditorPrefs.SetFloat(RadiusKey, value);
    }

    // =====================================================
    // TIER COLORS
    // =====================================================

    private static readonly Color[] DefaultColors =
    {
        new Color(0.20f, 0.80f, 1.00f, 0.30f), // Tier1
        new Color(0.20f, 1.00f, 0.40f, 0.30f), // Tier2
        new Color(1.00f, 0.85f, 0.20f, 0.30f), // Tier3
        new Color(1.00f, 0.50f, 0.20f, 0.30f), // Tier4
        new Color(1.00f, 0.20f, 0.20f, 0.30f), // Tier5
        new Color(0.80f, 0.20f, 1.00f, 0.30f), // Tier6
        new Color(1.00f, 0.20f, 0.70f, 0.30f), // Tier7
        new Color(0.20f, 1.00f, 0.90f, 0.30f), // Tier8
        new Color(0.60f, 0.60f, 1.00f, 0.30f), // Tier9
        new Color(1.00f, 1.00f, 1.00f, 0.30f), // Tier10
    };

    public static Color GetTierColor(int tier)
    {
        tier = Mathf.Clamp(tier, 1, 10);

        string key = $"ClickSpawner.Tier{tier}";

        Color defaultColor = DefaultColors[tier - 1];

        return new Color(
            EditorPrefs.GetFloat(key + ".R", defaultColor.r),
            EditorPrefs.GetFloat(key + ".G", defaultColor.g),
            EditorPrefs.GetFloat(key + ".B", defaultColor.b),
            EditorPrefs.GetFloat(key + ".A", defaultColor.a)
        );
    }

    public static void SetTierColor(int tier, Color color)
    {
        tier = Mathf.Clamp(tier, 1, 10);

        string key = $"ClickSpawner.Tier{tier}";

        EditorPrefs.SetFloat(key + ".R", color.r);
        EditorPrefs.SetFloat(key + ".G", color.g);
        EditorPrefs.SetFloat(key + ".B", color.b);
        EditorPrefs.SetFloat(key + ".A", color.a);
    }

    static ClickObjectSpawner()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    // =====================================================
    // SPAWNING
    // =====================================================

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!Enabled)
            return;

        Event e = Event.current;

        if (e.type != EventType.MouseDown || e.button != 0)
            return;

        // Keep Alt + LMB available for Scene View navigation.
        if (e.alt)
            return;

        GameObject parent = GameObject.Find(ParentName);

        if (parent == null)
        {
            Debug.LogWarning(
                $"Can't find GameObject called '{ParentName}'."
            );

            return;
        }

        float radius = Mathf.Max(0.01f, Radius);

        // obj_0.5, obj_1, obj_1.25 etc.
        string objectName =
            "obj_" + radius.ToString("0.##");

        GameObject obj = new GameObject(objectName);

        Undo.RegisterCreatedObjectUndo(
            obj,
            $"Create {objectName}"
        );

        obj.transform.SetParent(parent.transform);

        // =================================================
        // MOUSE -> XY WORLD POSITION
        // =================================================

        Ray ray =
            HandleUtility.GUIPointToWorldRay(e.mousePosition);

        Plane plane = new Plane(
            Vector3.forward,
            parent.transform.position
        );

        if (plane.Raycast(ray, out float distance))
        {
            obj.transform.position = ray.GetPoint(distance);
        }

        // =================================================
        // DETERMINE TIER
        // =================================================

        int tier = GetTierFromName(ParentName);

        Color color = GetTierColor(tier);

        // =================================================
        // MARKER
        // =================================================

        ClickSpawnMarker marker =
            obj.AddComponent<ClickSpawnMarker>();

        marker.radius = radius;
        marker.color = color;
        marker.text = ParentName;

        Selection.activeGameObject = obj;

        EditorUtility.SetDirty(obj);

        e.Use();
    }

    private static int GetTierFromName(string parentName)
    {
        for (int i = 10; i >= 1; i--)
        {
            if (parentName.Equals(
                    $"Tier{i}",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 1;
    }
}


// =====================================================
// MARKER COMPONENT
// =====================================================

[ExecuteAlways]
public class ClickSpawnMarker : MonoBehaviour
{
    public float radius = 0.5f;

    public Color color =
        new Color(0.2f, 0.8f, 1f, 0.3f);

    public string text = "Tier1";
}


// =====================================================
// MARKER DRAWER
// =====================================================

[InitializeOnLoad]
public static class ClickSpawnMarkerDrawer
{
    private static GUIStyle labelStyle;

    static ClickSpawnMarkerDrawer()
    {
        SceneView.duringSceneGui += DrawMarkers;
    }

    private static void DrawMarkers(SceneView sceneView)
    {
        ClickSpawnMarker[] markers =
            Object.FindObjectsByType<ClickSpawnMarker>(
                FindObjectsSortMode.None
            );

        if (labelStyle == null)
        {
            labelStyle =
                new GUIStyle(EditorStyles.boldLabel);

            labelStyle.alignment =
                TextAnchor.MiddleCenter;

            labelStyle.normal.textColor =
                Color.white;
        }

        foreach (ClickSpawnMarker marker in markers)
        {
            if (marker == null)
                continue;

            Vector3 position =
                marker.transform.position;

            // =============================
            // FILLED CIRCLE
            // =============================

            Handles.color = marker.color;

            Handles.DrawSolidDisc(
                position,
                Vector3.forward,
                marker.radius
            );

            // =============================
            // OUTLINE
            // =============================

            Color outlineColor = marker.color;

            outlineColor.a =
                Mathf.Min(1f, marker.color.a * 2.5f);

            Handles.color = outlineColor;

            Handles.DrawWireDisc(
                position,
                Vector3.forward,
                marker.radius
            );

            // =============================
            // TEXT
            // =============================

            Handles.Label(
                position,
                marker.text,
                labelStyle
            );
        }
    }
}


// =====================================================
// SETTINGS WINDOW
// =====================================================

public class ClickObjectSpawnerWindow : EditorWindow
{
    private Vector2 scroll;

    [MenuItem("Tools/Click Object Spawner")]
    public static void ShowWindow()
    {
        GetWindow<ClickObjectSpawnerWindow>(
            "Click Spawner"
        );
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label(
            "Click Object Spawner",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space();

        // =================================================
        // GENERAL
        // =================================================

        ClickObjectSpawner.Enabled =
            EditorGUILayout.Toggle(
                "Active",
                ClickObjectSpawner.Enabled
            );

        ClickObjectSpawner.ParentName =
            EditorGUILayout.TextField(
                "Parent Name",
                ClickObjectSpawner.ParentName
            );

        ClickObjectSpawner.Radius =
            EditorGUILayout.FloatField(
                "Circle Radius",
                ClickObjectSpawner.Radius
            );

        ClickObjectSpawner.Radius =
            Mathf.Max(
                0.01f,
                ClickObjectSpawner.Radius
            );

        EditorGUILayout.Space(10);

        // =================================================
        // TIER COLORS
        // =================================================

        GUILayout.Label(
            "Tier Colors",
            EditorStyles.boldLabel
        );

        for (int tier = 1; tier <= 10; tier++)
        {
            Color current =
                ClickObjectSpawner.GetTierColor(tier);

            Color newColor =
                EditorGUILayout.ColorField(
                    $"Tier {tier}",
                    current
                );

            if (newColor != current)
            {
                ClickObjectSpawner.SetTierColor(
                    tier,
                    newColor
                );
            }
        }

        EditorGUILayout.Space(10);

        // =================================================
        // INFO
        // =================================================

        if (ClickObjectSpawner.Enabled)
        {
            int tier = GetCurrentTier();

            EditorGUILayout.HelpBox(
                $"Parent: {ClickObjectSpawner.ParentName}\n" +
                $"Tier Color: Tier {tier}\n" +
                $"Object: obj_{ClickObjectSpawner.Radius:0.##}",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Spawner is disabled.",
                MessageType.None
            );
        }

        EditorGUILayout.EndScrollView();

        SceneView.RepaintAll();
    }

    private int GetCurrentTier()
    {
        string name = ClickObjectSpawner.ParentName;

        for (int i = 10; i >= 1; i--)
        {
            if (name.Equals(
                    $"Tier{i}",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 1;
    }
}
#endif