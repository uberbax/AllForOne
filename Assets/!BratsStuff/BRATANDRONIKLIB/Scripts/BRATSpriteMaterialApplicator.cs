using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BRATSpriteMaterialApplicator : MonoBehaviour
{
#if UNITY_EDITOR
    private const int EditorApplyPassCount = 16;
#endif

    [SerializeField] private Material material;
    [SerializeField] private Transform[] targetRoots;

#if UNITY_EDITOR
    private int _remainingEditorApplyPasses;
#endif

    private void OnEnable()
    {
        Apply();
#if UNITY_EDITOR
        ScheduleEditorApply();
#endif
    }

    private void Start()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
#if UNITY_EDITOR
        ScheduleEditorApply();
#endif
    }

#if UNITY_EDITOR
    private void OnDisable()
    {
        EditorApplication.update -= ApplyDelayed;
    }

    private void ScheduleEditorApply()
    {
        _remainingEditorApplyPasses = EditorApplyPassCount;
        EditorApplication.update -= ApplyDelayed;
        EditorApplication.update += ApplyDelayed;
    }

    private void ApplyDelayed()
    {
        if (this == null || !isActiveAndEnabled)
        {
            EditorApplication.update -= ApplyDelayed;
            return;
        }

        Apply();

        _remainingEditorApplyPasses--;
        if (_remainingEditorApplyPasses <= 0)
        {
            EditorApplication.update -= ApplyDelayed;
        }
    }
#endif

    private void Apply()
    {
        if (material == null || targetRoots == null)
        {
            return;
        }

        for (int rootIndex = 0; rootIndex < targetRoots.Length; rootIndex++)
        {
            Transform targetRoot = targetRoots[rootIndex];
            if (targetRoot == null)
            {
                continue;
            }

            SpriteRenderer[] renderers = targetRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                SpriteRenderer renderer = renderers[rendererIndex];
                if (renderer.sharedMaterial != material)
                {
                    renderer.sharedMaterial = material;
                }
            }
        }
    }
}
