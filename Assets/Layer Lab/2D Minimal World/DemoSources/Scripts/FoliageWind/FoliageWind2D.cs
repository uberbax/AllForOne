using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LayerLab
{
    /// <summary>
    /// Feeds wind values to the SpriteRenderer so the foliage body and its optional shadow sway in the same wind phase.
    /// The Transform never moves (only the shader vertex offset is used), so click hit-testing and collider positions stay intact.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("LayerLab/Rendering/Foliage Wind 2D")]
    public sealed class FoliageWind2D : MonoBehaviour
    {
        private readonly int _windAnchorId = Shader.PropertyToID("_WindAnchor");
        private readonly int _windPivotYId = Shader.PropertyToID("_WindPivotY");
        private readonly int _windHeightId = Shader.PropertyToID("_WindHeight");

        [Header("Target")]
        [Tooltip("If empty, this object's own SpriteRenderer is used as the body.")]
        [FormerlySerializedAs("treeRenderer")]
        [SerializeField] private SpriteRenderer targetRenderer;

        [Tooltip("If empty, child SpriteRenderers whose name contains \"shadow\" are collected automatically.")]
        [SerializeField] private SpriteRenderer[] shadowRenderers;

        [Tooltip("When enabled, the body and shadow SpriteRenderers are auto-collected in OnEnable/OnValidate.")]
        [SerializeField] private bool autoCollectRenderers = true;

        [Header("Materials")]
        [Tooltip("If empty, the existing material is kept and only the properties are fed.")]
        [FormerlySerializedAs("treeMaterial")]
        [SerializeField] private Material foliageMaterial;

        [Tooltip("If empty, the existing shadow material is kept and only the properties are fed.")]
        [SerializeField] private Material shadowMaterial;

        [Tooltip("Applies the assigned material to SpriteRenderer.sharedMaterial.")]
        [SerializeField] private bool applyMaterials = true;

        private MaterialPropertyBlock _targetBlock;
        private MaterialPropertyBlock _shadowBlock;
        private Material _runtimeFoliageMaterial;
        private Material _runtimeShadowMaterial;

        private void Reset()
        {
            CollectRenderers();
            EnsureDefaultMaterials();
            Apply();
        }

        private void OnEnable()
        {
            if (autoCollectRenderers) CollectRenderers();
            EnsureDefaultMaterials();
            Apply();
        }

        private void OnValidate()
        {
            if (autoCollectRenderers) CollectRenderers();
            EnsureDefaultMaterials();
            Apply();
        }

        private void LateUpdate()
        {
            if (transform.hasChanged)
            {
                Apply();
                transform.hasChanged = false;
            }
        }

        private void OnDestroy()
        {
            DestroyRuntimeMaterial(ref _runtimeFoliageMaterial);
            DestroyRuntimeMaterial(ref _runtimeShadowMaterial);
        }

        [ContextMenu("Recollect Targets")]
        private void CollectRenderers()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<SpriteRenderer>();

            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            int shadowCount = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr != null && sr != targetRenderer && IsShadowRenderer(sr))
                    shadowCount++;
            }

            if (shadowRenderers == null || shadowRenderers.Length != shadowCount)
                shadowRenderers = new SpriteRenderer[shadowCount];

            int index = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr != null && sr != targetRenderer && IsShadowRenderer(sr))
                    shadowRenderers[index++] = sr;
            }
        }

        [ContextMenu("Apply Materials/Defaults")]
        private void Apply()
        {
            // Property blocks carry per-sprite bend data without instantiating unique materials.
            EnsureDefaultMaterials();
            Vector3 anchor = transform.position;
            ApplyTarget(anchor);
            ApplyShadows(anchor);
        }

        private void EnsureDefaultMaterials()
        {
            if (!applyMaterials) return;

            if (foliageMaterial == null)
                foliageMaterial = LoadDefaultMaterial(
                    "Assets/Layer Lab/2D Minimal World/DemoSources/Materials/FoliageWind2D.mat",
                    "Sprites/FoliageWind2D",
                    ref _runtimeFoliageMaterial);

            if (shadowMaterial == null && HasShadowRenderers())
                shadowMaterial = LoadDefaultMaterial(
                    "Assets/Layer Lab/2D Minimal World/DemoSources/Materials/FoliageWindShadow2D.mat",
                    "Sprites/FoliageWindShadow2D",
                    ref _runtimeShadowMaterial);
        }

        private bool HasShadowRenderers()
        {
            if (shadowRenderers == null) return false;

            for (int i = 0; i < shadowRenderers.Length; i++)
            {
                if (shadowRenderers[i] != null)
                    return true;
            }

            return false;
        }

        private static Material LoadDefaultMaterial(string assetPath, string shaderName, ref Material runtimeMaterial)
        {
#if UNITY_EDITOR
            Material assetMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (assetMaterial != null) return assetMaterial;
#endif
            if (runtimeMaterial != null) return runtimeMaterial;

            Shader shader = Shader.Find(shaderName);
            if (shader == null) return null;

            runtimeMaterial = new Material(shader)
            {
                name = $"{shaderName} (Runtime)",
                hideFlags = HideFlags.DontSave
            };
            return runtimeMaterial;
        }

        private static void DestroyRuntimeMaterial(ref Material runtimeMaterial)
        {
            if (runtimeMaterial == null) return;

            if (Application.isPlaying)
                Destroy(runtimeMaterial);
            else
                DestroyImmediate(runtimeMaterial);

            runtimeMaterial = null;
        }

        private void ApplyTarget(Vector3 anchor)
        {
            if (targetRenderer == null) return;

            if (applyMaterials && foliageMaterial != null && targetRenderer.sharedMaterial != foliageMaterial)
                targetRenderer.sharedMaterial = foliageMaterial;

            _targetBlock ??= new MaterialPropertyBlock();
            _targetBlock.Clear();
            _targetBlock.SetVector(_windAnchorId, new Vector4(anchor.x, anchor.y, anchor.z, 0f));

            Sprite sprite = targetRenderer.sprite;
            if (sprite != null)
            {
                // The shader bends from the sprite-local pivot and height, so refresh these bounds when applying.
                Bounds bounds = sprite.bounds;
                _targetBlock.SetFloat(_windPivotYId, bounds.min.y);
                _targetBlock.SetFloat(_windHeightId, Mathf.Max(bounds.size.y, 0.0001f));
            }

            targetRenderer.SetPropertyBlock(_targetBlock);
        }

        private void ApplyShadows(Vector3 anchor)
        {
            if (shadowRenderers == null) return;

            for (int i = 0; i < shadowRenderers.Length; i++)
            {
                SpriteRenderer shadowRenderer = shadowRenderers[i];
                if (shadowRenderer == null) continue;

                if (applyMaterials && shadowMaterial != null && shadowRenderer.sharedMaterial != shadowMaterial)
                    shadowRenderer.sharedMaterial = shadowMaterial;

                _shadowBlock ??= new MaterialPropertyBlock();
                _shadowBlock.Clear();
                _shadowBlock.SetVector(_windAnchorId, new Vector4(anchor.x, anchor.y, anchor.z, 0f));
                shadowRenderer.SetPropertyBlock(_shadowBlock);
            }
        }

        private static bool IsShadowRenderer(SpriteRenderer sr)
        {
            string objectName = sr.gameObject.name;
            return objectName.IndexOf("shadow", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
