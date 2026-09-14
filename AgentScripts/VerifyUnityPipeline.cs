using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run through Unity Pipeline run_script. Lives outside Assets to avoid project recompilation.
public static class VerifyUnityPipeline
{
    public static Dictionary<string, object> Run()
    {
        var activeScene = SceneManager.GetActiveScene();
        var wasDirty = activeScene.isDirty;
        var sceneCount = SceneManager.sceneCount;
        var report = new Dictionary<string, object>();
        var preview = EditorSceneManager.NewPreviewScene();
        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Unity Pipeline isolated verification");
        try
        {
            var probe = new GameObject("PipelineVerificationProbe");
            SceneManager.MoveGameObjectToScene(probe, preview);
            var collider = probe.AddComponent<BoxCollider>();
            var serialized = new SerializedObject(collider);
            serialized.FindProperty("m_IsTrigger").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(collider.isTrigger, "Serialized component edit failed");
            report["componentSerialization"] = true;

            Undo.RecordObject(probe.transform, "Verify local Undo");
            probe.transform.localPosition = new Vector3(1, 2, 3);
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(undoGroup);
            Require(probe.transform.localPosition == Vector3.zero, "Undo failed");
            report["undo"] = true;

            const string prefabPath = "Assets/!Prefabs/UpgradeItem.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Require(prefab != null, "Verification prefab missing: " + prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            Require(PrefabUtility.IsPartOfPrefabInstance(instance), "Prefab connection missing");
            Require(instance.scene == preview, "Prefab was instantiated into the wrong scene");
            report["prefabInstantiation"] = true;
            report["assetDatabase"] = AssetDatabase.AssetPathToGUID(prefabPath).Length == 32;
            foreach (var component in instance.GetComponentsInChildren<Component>(true))
                Require(component != null, "Missing component in verification prefab");
            report["prefabComponents"] = true;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }

        Require(SceneManager.GetActiveScene() == activeScene, "Active scene changed");
        Require(activeScene.isDirty == wasDirty, "Active scene dirty state changed");
        Require(SceneManager.sceneCount == sceneCount, "Scene count changed");
        report["activeScenePreserved"] = true;
        report["unityVersion"] = Application.unityVersion;
        return report;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
