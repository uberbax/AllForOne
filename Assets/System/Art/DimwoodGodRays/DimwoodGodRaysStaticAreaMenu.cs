
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DimwoodGodRaysStaticAreaMenu
{
    [MenuItem("GameObject/Dimwood VFX/Static God Rays Area", false, 10)]
    public static void Create()
    {
        GameObject go = new GameObject("Static God Rays Area");

        Undo.RegisterCreatedObjectUndo(
            go,
            "Create Static God Rays Area"
        );

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        go.AddComponent<DimwoodGodRaysStaticArea>();

        var main = ps.main;
        main.duration = 10f;

        if (SceneView.lastActiveSceneView != null)
        {
            Camera cam = SceneView.lastActiveSceneView.camera;

            if (cam != null)
            {
                go.transform.position =
                    cam.transform.position +
                    cam.transform.forward * 5f;
            }
        }

        Selection.activeGameObject = go;
    }
}
#endif
