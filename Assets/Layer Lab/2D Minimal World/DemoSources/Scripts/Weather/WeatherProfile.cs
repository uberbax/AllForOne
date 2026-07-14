using UnityEngine;

namespace LayerLab
{
    // ScriptableObject describing a single weather state (Clear/Rain/Snow/Fog)
    // and all the visual/timing parameters used to drive it at runtime.
    [CreateAssetMenu(fileName = "WeatherProfile", menuName = "LayerLab/Weather/Profile")]
    public class WeatherProfile : ScriptableObject
    {
        // General
        [Header("General")]
        [SerializeField] private WeatherType weatherType = WeatherType.Clear;
        [SerializeField] private string displayName = "Clear";

        // Auto Transition: weighting and timing used by the automatic weather scheduler.
        [Header("Auto Transition")]
        [SerializeField, Min(0f)] private float selectionWeight = 1f;
        [SerializeField, Min(1f)] private float minDurationSeconds = 60f;
        [SerializeField, Min(1f)] private float maxDurationSeconds = 120f;
        [SerializeField, Min(0f)] private float transitionDurationSeconds = 6f;

        // Particles
        [Header("Particles")]
        [SerializeField, Range(0f, 1f)] private float particleIntensity = 0f;
        [SerializeField, Min(0f)] private float rainEmissionRate = 0f;
        [SerializeField, Min(0f)] private float rainSplashEmissionRate = 0f;
        [SerializeField, Min(0f)] private float snowEmissionRate = 0f;
        [SerializeField, Min(0f)] private float snowParticleMinSize = 0.12f;
        [SerializeField, Min(0f)] private float snowParticleMaxSize = 0.26f;

        // Rain Rendering
        [Header("Rain Rendering")]
        [SerializeField] private Color rainColor = new Color(0.72f, 0.88f, 1f, 0.62f);
        [SerializeField, Min(0f)] private float rainStreakMinSize = 0.045f;
        [SerializeField, Min(0f)] private float rainStreakMaxSize = 0.12f;
        [SerializeField, Min(0f)] private float rainLengthScale = 2.2f;
        [SerializeField, Min(0f)] private float rainVelocityLengthScale = 0.02f;
        [SerializeField] private Color rainSplashColor = new Color(0.72f, 0.9f, 1f, 0.46f);

        // Rain Color Auto Adjustment
        [Header("Rain Color Auto Adjustment")]
        [SerializeField] private bool adaptRainColorToDayNight = true;
        [SerializeField, Range(0f, 2f)] private float rainDayAlphaMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float rainNightAlphaMultiplier = 0.52f;
        [SerializeField, Range(0f, 2f)] private float rainFogAlphaMultiplier = 0.72f;
        [SerializeField, Range(0f, 1f)] private float rainMinAlpha = 0.18f;
        [SerializeField, Range(0f, 1f)] private float rainMaxAlpha = 0.68f;
        [SerializeField, Range(0f, 1f)] private float rainNightBrightnessBoost = 0.08f;
        [SerializeField, Range(0f, 2f)] private float rainSplashDayAlphaMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float rainSplashNightAlphaMultiplier = 0.55f;

        // Snow Rendering
        [Header("Snow Rendering")]
        [SerializeField] private Color snowColor = new Color(0.92f, 0.97f, 1f, 0.75f);
        [SerializeField, Range(0f, 2f)] private float snowDayAlphaMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float snowNightAlphaMultiplier = 0.66f;
        [SerializeField] private Color snowNightTint = new Color(0.58f, 0.68f, 0.9f, 1f);
        [SerializeField, Range(0f, 1f)] private float snowNightTintBlend = 0.35f;

        // Fog / Post-processing
        [Header("Fog / Post-processing")]
        [SerializeField, Range(0f, 1f)] private float fogOverlayAlpha = 0f;
        [SerializeField] private Color fogOverlayColor = new Color(0.75f, 0.78f, 0.8f, 1f);
        [SerializeField, Range(0f, 1f)] private float weatherVolumeWeight = 0f;

        // Fog Day/Night Adjustment
        [Header("Fog Day/Night Adjustment")]
        [SerializeField, Range(0f, 2f)] private float fogDayAlphaMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float fogNightAlphaMultiplier = 0.5f;
        [SerializeField] private Color fogNightColor = new Color(0.18f, 0.22f, 0.32f, 1f);
        [SerializeField, Range(0f, 1f)] private float fogNightColorBlend = 0.65f;

        // Post-processing Day/Night Adjustment
        [Header("Post-processing Day/Night Adjustment")]
        [SerializeField, Range(0f, 2f)] private float weatherVolumeDayMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float weatherVolumeNightMultiplier = 0.65f;

        // Global Light Adjustment
        [Header("Global Light Adjustment")]
        [SerializeField] private Color lightColorMultiplier = Color.white;
        [SerializeField, Min(0f)] private float lightIntensityMultiplier = 1f;

        // Wind
        [Header("Wind")]
        [SerializeField] private Vector2 windDirection = new Vector2(-0.35f, -1f);
        [SerializeField, Min(0f)] private float windStrength = 1f;

        public WeatherType WeatherType => weatherType;
        public string DisplayName => displayName;
        public float SelectionWeight => selectionWeight;
        public float MinDurationSeconds => minDurationSeconds;
        public float MaxDurationSeconds => Mathf.Max(minDurationSeconds, maxDurationSeconds);
        public float TransitionDurationSeconds => transitionDurationSeconds;
        public float ParticleIntensity => particleIntensity;
        public float RainEmissionRate => rainEmissionRate;
        public float RainSplashEmissionRate => rainSplashEmissionRate;
        public float SnowEmissionRate => snowEmissionRate;
        public float SnowParticleMinSize => snowParticleMinSize;
        public float SnowParticleMaxSize => Mathf.Max(snowParticleMinSize, snowParticleMaxSize);
        public Color RainColor => rainColor;
        public float RainStreakMinSize => rainStreakMinSize;
        public float RainStreakMaxSize => Mathf.Max(rainStreakMinSize, rainStreakMaxSize);
        public float RainLengthScale => rainLengthScale;
        public float RainVelocityLengthScale => rainVelocityLengthScale;
        public Color RainSplashColor => rainSplashColor;
        public bool AdaptRainColorToDayNight => adaptRainColorToDayNight;
        public float RainDayAlphaMultiplier => rainDayAlphaMultiplier;
        public float RainNightAlphaMultiplier => rainNightAlphaMultiplier;
        public float RainFogAlphaMultiplier => rainFogAlphaMultiplier;
        public float RainMinAlpha => rainMinAlpha;
        public float RainMaxAlpha => rainMaxAlpha;
        public float RainNightBrightnessBoost => rainNightBrightnessBoost;
        public float RainSplashDayAlphaMultiplier => rainSplashDayAlphaMultiplier;
        public float RainSplashNightAlphaMultiplier => rainSplashNightAlphaMultiplier;
        public Color SnowColor => snowColor;
        public float SnowDayAlphaMultiplier => snowDayAlphaMultiplier;
        public float SnowNightAlphaMultiplier => snowNightAlphaMultiplier;
        public Color SnowNightTint => snowNightTint;
        public float SnowNightTintBlend => snowNightTintBlend;
        public float FogOverlayAlpha => fogOverlayAlpha;
        public Color FogOverlayColor => fogOverlayColor;
        public float WeatherVolumeWeight => weatherVolumeWeight;
        public float FogDayAlphaMultiplier => fogDayAlphaMultiplier;
        public float FogNightAlphaMultiplier => fogNightAlphaMultiplier;
        public Color FogNightColor => fogNightColor;
        public float FogNightColorBlend => fogNightColorBlend;
        public float WeatherVolumeDayMultiplier => weatherVolumeDayMultiplier;
        public float WeatherVolumeNightMultiplier => weatherVolumeNightMultiplier;
        public Color LightColorMultiplier => lightColorMultiplier;
        public float LightIntensityMultiplier => lightIntensityMultiplier;
        public Vector2 WindDirection => windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector2.down;
        public float WindStrength => windStrength;

        private void OnValidate()
        {
            minDurationSeconds = Mathf.Max(1f, minDurationSeconds);
            maxDurationSeconds = Mathf.Max(minDurationSeconds, maxDurationSeconds);
            transitionDurationSeconds = Mathf.Max(0f, transitionDurationSeconds);
            selectionWeight = Mathf.Max(0f, selectionWeight);
            lightIntensityMultiplier = Mathf.Max(0f, lightIntensityMultiplier);
            rainStreakMinSize = Mathf.Max(0f, rainStreakMinSize);
            rainStreakMaxSize = Mathf.Max(rainStreakMinSize, rainStreakMaxSize);
            snowParticleMinSize = Mathf.Max(0f, snowParticleMinSize);
            snowParticleMaxSize = Mathf.Max(snowParticleMinSize, snowParticleMaxSize);
            rainLengthScale = Mathf.Max(0f, rainLengthScale);
            rainVelocityLengthScale = Mathf.Max(0f, rainVelocityLengthScale);
            rainMinAlpha = Mathf.Clamp01(rainMinAlpha);
            rainMaxAlpha = Mathf.Clamp01(Mathf.Max(rainMinAlpha, rainMaxAlpha));
            snowNightTintBlend = Mathf.Clamp01(snowNightTintBlend);
            fogNightColorBlend = Mathf.Clamp01(fogNightColorBlend);
            windStrength = Mathf.Max(0f, windStrength);
        }
    }
}
