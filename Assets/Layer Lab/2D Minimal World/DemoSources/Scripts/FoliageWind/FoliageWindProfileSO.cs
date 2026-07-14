using UnityEngine;

namespace LayerLab
{
    [CreateAssetMenu(fileName = "FoliageWindProfile", menuName = "LayerLab/Foliage Wind/Profile")]
    public sealed class FoliageWindProfileSO : ScriptableObject
    {
        [Header("Wind")]
        [SerializeField] private bool windEnabled = true;
        [SerializeField, Min(0f)] private float windStrength = 0.06f;
        [SerializeField, Min(0f)] private float windSpeed = 1.2f;
        [SerializeField, Min(0f)] private float windScale = 0.75f;
        [SerializeField] private float phaseOffset;

        [Header("Bend")]
        [SerializeField, Range(0f, 1f)] private float bendStart = 0.08f;
        [SerializeField, Range(0.2f, 4f)] private float bendPower = 1.7f;

        [Header("Shadow")]
        [SerializeField, Min(0f)] private float shadowStrength = 0.06f;
        [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.26666668f);
        [SerializeField, Range(0f, 1f)] private float shadowAlphaThreshold = 0.748f;

        public bool WindEnabled => windEnabled;
        public float WindStrength => windStrength;
        public float WindSpeed => windSpeed;
        public float WindScale => windScale;
        public float PhaseOffset => phaseOffset;
        public float BendStart => bendStart;
        public float BendPower => bendPower;
        public float ShadowStrength => shadowStrength;
        public Color ShadowColor => shadowColor;
        public float ShadowAlphaThreshold => shadowAlphaThreshold;

        private void OnValidate()
        {
            // Clamp authoring values to shader-safe ranges before the controller publishes them globally.
            windStrength = Mathf.Max(0f, windStrength);
            windSpeed = Mathf.Max(0f, windSpeed);
            windScale = Mathf.Max(0f, windScale);
            bendStart = Mathf.Clamp01(bendStart);
            bendPower = Mathf.Clamp(bendPower, 0.2f, 4f);
            shadowStrength = Mathf.Max(0f, shadowStrength);
            shadowAlphaThreshold = Mathf.Clamp01(shadowAlphaThreshold);
        }
    }
}
