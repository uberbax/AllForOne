using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LayerLab
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("LayerLab/Rendering/Foliage Wind Controller")]
    public sealed class FoliageWindController : MonoBehaviour
    {
        private static readonly int FoliageWindEnabledId = Shader.PropertyToID("_FoliageWindEnabled");
        private static readonly int FoliageWindStrengthId = Shader.PropertyToID("_FoliageWindStrength");
        private static readonly int FoliageWindSpeedId = Shader.PropertyToID("_FoliageWindSpeed");
        private static readonly int FoliageWindScaleId = Shader.PropertyToID("_FoliageWindScale");
        private static readonly int FoliageWindPhaseOffsetId = Shader.PropertyToID("_FoliageWindPhaseOffset");
        private static readonly int FoliageWindBendStartId = Shader.PropertyToID("_FoliageWindBendStart");
        private static readonly int FoliageWindBendPowerId = Shader.PropertyToID("_FoliageWindBendPower");
        private static readonly int FoliageWindShadowStrengthId = Shader.PropertyToID("_FoliageWindShadowStrength");
        private static readonly int FoliageWindShadowColorId = Shader.PropertyToID("_FoliageWindShadowColor");
        private static readonly int FoliageWindShadowAlphaThresholdId = Shader.PropertyToID("_FoliageWindShadowAlphaThreshold");

        [SerializeField] private FoliageWindProfileSO profile;
        [SerializeField] private bool applyEveryFrame = true;

        public FoliageWindProfileSO Profile => profile;

        private void Reset()
        {
#if UNITY_EDITOR
            profile = AssetDatabase.LoadAssetAtPath<FoliageWindProfileSO>(
                "Assets/Layer Lab/2D Minimal World/Common/FoliageWindProfiles/DefaultFoliageWindProfile.asset");
#endif
            ApplyProfile();
        }

        private void OnEnable()
        {
            ApplyProfile();
        }

        private void OnValidate()
        {
            ApplyProfile();
        }

        private void LateUpdate()
        {
            if (applyEveryFrame)
                ApplyProfile();
        }

        [ContextMenu("Apply Profile")]
        public void ApplyProfile()
        {
            if (profile == null) return;

            // Publish wind settings as global shader properties so all foliage shares one wind phase and strength.
            Shader.SetGlobalFloat(FoliageWindEnabledId, profile.WindEnabled ? 1f : 0f);
            Shader.SetGlobalFloat(FoliageWindStrengthId, profile.WindStrength);
            Shader.SetGlobalFloat(FoliageWindSpeedId, profile.WindSpeed);
            Shader.SetGlobalFloat(FoliageWindScaleId, profile.WindScale);
            Shader.SetGlobalFloat(FoliageWindPhaseOffsetId, profile.PhaseOffset);
            Shader.SetGlobalFloat(FoliageWindBendStartId, profile.BendStart);
            Shader.SetGlobalFloat(FoliageWindBendPowerId, profile.BendPower);
            Shader.SetGlobalFloat(FoliageWindShadowStrengthId, profile.ShadowStrength);
            Shader.SetGlobalColor(FoliageWindShadowColorId, profile.ShadowColor);
            Shader.SetGlobalFloat(FoliageWindShadowAlphaThresholdId, profile.ShadowAlphaThreshold);
        }
    }
}
