#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ExactReferenceFinder : EditorWindow
{
    private UnityEngine.Object target;
    private Vector2 scroll;

    private readonly List<Result> results = new();

    private class Result
    {
        public GameObject gameObject;
        public Component component;
        public string propertyPath;
        public string displayName;
    }

    [MenuItem("Tools/Find Exact References In Scene")]
    public static void Open()
    {
        var window = GetWindow<ExactReferenceFinder>();
        window.titleContent = new GUIContent("Reference Finder");
        window.minSize = new Vector2(550, 300);

        // Automatically use current selection.
        window.target = Selection.activeObject;
        window.FindReferences();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Find Exact References In Scene",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();

        target = EditorGUILayout.ObjectField(
            "Target",
            target,
            typeof(UnityEngine.Object),
            true);

        if (EditorGUI.EndChangeCheck())
            FindReferences();

        EditorGUILayout.Space(5);

        using (new EditorGUI.DisabledScope(target == null))
        {
            if (GUILayout.Button("Find References", GUILayout.Height(28)))
                FindReferences();
        }

        EditorGUILayout.Space(8);

        if (target == null)
        {
            EditorGUILayout.HelpBox(
                "Select or drag an object to search for.",
                MessageType.Info);

            return;
        }

        EditorGUILayout.LabelField(
            $"Found {results.Count} reference(s)",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(4);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var result in results)
            DrawResult(result);

        EditorGUILayout.EndScrollView();
    }

    private void DrawResult(Result result)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
                result.gameObject.name,
                EditorStyles.linkLabel,
                GUILayout.Width(160)))
        {
            SelectResult(result);
        }

        EditorGUILayout.LabelField(
            result.component.GetType().Name,
            GUILayout.Width(180));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            "Field:",
            GUILayout.Width(40));

        EditorGUILayout.SelectableLabel(
            result.propertyPath,
            EditorStyles.textField,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));

        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(result.displayName))
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                "Name:",
                GUILayout.Width(40));

            EditorGUILayout.LabelField(result.displayName);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(2);

        if (GUILayout.Button("Select & Ping", GUILayout.Height(22)))
            SelectResult(result);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(3);
    }

    private void FindReferences()
    {
        results.Clear();

        if (target == null)
        {
            Repaint();
            return;
        }

        Component[] components =
            FindObjectsByType<Component>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            try
            {
                SearchComponent(component);
            }
            catch
            {
                // Some internal/native Unity components can fail
                // SerializedObject creation. Ignore them.
            }
        }

        Repaint();
    }

    private void SearchComponent(Component component)
    {
        SerializedObject serializedObject =
            new SerializedObject(component);

        SerializedProperty property =
            serializedObject.GetIterator();

        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = true;

            if (property.propertyType !=
                SerializedPropertyType.ObjectReference)
                continue;

            if (property.objectReferenceValue != target)
                continue;

            results.Add(new Result
            {
                gameObject = component.gameObject,
                component = component,
                propertyPath = property.propertyPath,
                displayName = property.displayName
            });
        }
    }

    private void SelectResult(Result result)
    {
        if (result.component == null)
            return;

        Selection.activeGameObject = result.gameObject;

        EditorGUIUtility.PingObject(result.component);

        // Make sure Inspector updates.
        Selection.activeObject = result.component;

        // Attempt to expand the component in Inspector.
        InternalEditorUtilityBridge.SetExpanded(
            result.component,
            true);
    }

    /// <summary>
    /// Keeps internal Unity API usage isolated so if Unity changes it,
    /// the finder itself still works.
    /// </summary>
    private static class InternalEditorUtilityBridge
    {
        public static void SetExpanded(
            UnityEngine.Object obj,
            bool expanded)
        {
            try
            {
                UnityEditorInternal.InternalEditorUtility
                    .SetIsInspectorExpanded(obj, expanded);
            }
            catch
            {
                // Not critical.
            }
        }
    }
}

#endif