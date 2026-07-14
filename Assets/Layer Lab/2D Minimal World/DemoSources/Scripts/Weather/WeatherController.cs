using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace LayerLab
{
    [ExecuteAlways]
    // Drives weather state (clear/rain/snow/fog) by blending WeatherProfiles, and applies the
    // result to particles, fog overlay, post-process volume, and time-of-day color adjustments.
    public class WeatherController : MonoBehaviour
    {
        [Header("Weather")]
        [SerializeField] private bool autoWeather = true;
        [SerializeField] private WeatherProfile initialProfile;
        [SerializeField] private WeatherProfile[] profiles;

        [Header("Weather Rig")]
        [SerializeField] private Transform weatherRig;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool autoFindMainCamera = true;
        [SerializeField] private bool followCamera = true;
        [SerializeField] private Vector3 rigOffset = new Vector3(0f, 0f, 5f);
        [SerializeField, Min(0f)] private float screenPadding = 3f;
        [SerializeField] private bool autoScaleParticleShapes = true;
        [SerializeField] private bool autoScaleFogOverlay = true;

        [Header("Effect References")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem rainSplashParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private SpriteRenderer fogOverlay;
        [SerializeField] private Volume weatherVolume;

        [Header("Fog Distance")]
        [SerializeField] private Transform fogFocusTarget;
        [SerializeField] private bool autoFindFogFocusTarget = true;
        [SerializeField, Min(0f)] private float fogClearRadius = 2.5f;
        [SerializeField, Min(0f)] private float fogFullRadius = 8f;
        [SerializeField, Min(0.1f)] private float fogGradientPower = 1.15f;

        [Header("Time-of-Day Color Adjustment")]
        [SerializeField] private DayNightController dayNightController;
        [SerializeField] private bool autoFindDayNightController = true;

        [Header("Rendering Order")]
        [SerializeField] private string weatherSortingLayer = "Default";
        [SerializeField] private int weatherSortingOrder = 500;

        private WeatherProfile _currentProfile;
        private WeatherProfile _sourceProfile;
        private WeatherProfile _targetProfile;
        private float _transitionTimer;
        private float _transitionDuration;
        private float _weatherTimer;
        private float _currentRainRate;
        private float _currentRainSplashRate;
        private float _currentSnowRate;
        private float _currentFogAlpha;
        private Color _currentFogColor = Color.white;
        private float _currentVolumeWeight;
        private ParticleSystem _cachedRainParticles;
        private ParticleSystemRenderer _rainParticleRenderer;
        private Texture2D _rainStreakTexture;
        private Material _rainStreakMaterial;
        private Material _fogOverlayMaterial;
        private MaterialPropertyBlock _fogPropertyBlock;
        private bool _searchedFogFocusTarget;
        private bool _pendingSnowWarmStart;

        private const string FogOverlayMaterialResourcePath = "Weather/WeatherFogOverlay2D";
        private const float ParticleEmissionThreshold = 0.01f;
        private const float SnowWarmStartSeconds = 0.7f;

        private static readonly int FogFocusPositionId = Shader.PropertyToID("_FogFocusPosition");
        private static readonly int FogClearRadiusId = Shader.PropertyToID("_FogClearRadius");
        private static readonly int FogFullRadiusId = Shader.PropertyToID("_FogFullRadius");
        private static readonly int FogGradientPowerId = Shader.PropertyToID("_FogGradientPower");

        public Color LightColorMultiplier { get; private set; } = Color.white;
        public float LightIntensityMultiplier { get; private set; } = 1f;
        public WeatherType CurrentWeatherType => _targetProfile != null ? _targetProfile.WeatherType : WeatherType.Clear;
        public WeatherProfile CurrentProfile => _targetProfile;

        private void Reset()
        {
            weatherRig = transform;
#if UNITY_EDITOR
            AutoAssignProfilesFromProject();
#endif
            ResolveCameraIfNeeded();
            FindDayNightControllerIfNeeded();
            ResolveFogFocusTargetIfNeeded();
            ApplySorting();
            SetInitialProfile();
            ApplyCurrentVisualState();
        }

        private void Awake()
        {
            ResolveCameraIfNeeded();
            FindDayNightControllerIfNeeded();
            ResolveFogFocusTargetIfNeeded();
            ApplySorting();
            SetInitialProfile();
            ApplyCurrentVisualState();
        }

        private void OnEnable()
        {
            ResolveCameraIfNeeded();
            FindDayNightControllerIfNeeded();
            ApplySorting();
            SetInitialProfile();
            ApplyCurrentVisualState();
        }

        private void Update()
        {
            ResolveCameraIfNeeded();
            FollowAndResizeRig();
            ResolveFogFocusTargetIfNeeded();

            if (Application.isPlaying)
            {
                TickAutoWeather(Time.deltaTime);
                TickTransition(Time.deltaTime);
            }
            else
            {
                ApplyCurrentVisualState();
            }
        }

        private void OnValidate()
        {
            screenPadding = Mathf.Max(0f, screenPadding);
            fogClearRadius = Mathf.Max(0f, fogClearRadius);
            fogFullRadius = Mathf.Max(fogClearRadius + 0.01f, fogFullRadius);
            fogGradientPower = Mathf.Max(0.1f, fogGradientPower);
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                if (profiles == null || profiles.Length == 0)
                {
                    AutoAssignProfilesFromProject();
                }
#endif
                ResolveCameraIfNeeded();
                FindDayNightControllerIfNeeded();
                _searchedFogFocusTarget = false;
                ResolveFogFocusTargetIfNeeded();
                ApplySorting();
                SetInitialProfile();
                ApplyCurrentVisualState();
                FollowAndResizeRig();
            }
        }

        [ContextMenu("Apply Clear")]
        public void ApplyClear()
        {
            autoWeather = false;
            SetWeather(WeatherType.Clear, false);
        }

        [ContextMenu("Apply Rain")]
        public void ApplyRain()
        {
            autoWeather = false;
            SetWeather(WeatherType.Rain, false);
        }

        [ContextMenu("Apply Snow")]
        public void ApplySnow()
        {
            autoWeather = false;
            SetWeather(WeatherType.Snow, false);
        }

        [ContextMenu("Apply Fog")]
        public void ApplyFog()
        {
            autoWeather = false;
            SetWeather(WeatherType.Fog, false);
        }

        public void SetWeather(WeatherType weatherType, bool instant)
        {
            SetWeather(weatherType, instant, -1f);
        }

        public void SetWeather(WeatherType weatherType, bool instant, float transitionDurationOverride)
        {
            WeatherProfile profile = FindProfile(weatherType);
            if (profile != null)
            {
                SetProfile(profile, instant, transitionDurationOverride);
            }
        }

        public void SetProfile(WeatherProfile profile, bool instant)
        {
            SetProfile(profile, instant, -1f);
        }

        public void SetProfile(WeatherProfile profile, bool instant, float transitionDurationOverride)
        {
            if (profile == null)
            {
                return;
            }

            // Snow needs a warm start when emission begins from zero; otherwise the screen stays empty until particles spawn naturally.
            bool shouldWarmStartSnow = Application.isPlaying &&
                profile.WeatherType == WeatherType.Snow &&
                (CurrentWeatherType != WeatherType.Snow || _currentSnowRate <= ParticleEmissionThreshold);

            // Keep transition state explicit so instant changes and blended changes share the same apply path.
            EnsureCurrentProfile();
            _sourceProfile = instant ? profile : _currentProfile;
            _targetProfile = profile;
            _transitionTimer = 0f;
            _transitionDuration = instant
                ? 0f
                : transitionDurationOverride >= 0f
                    ? Mathf.Max(0f, transitionDurationOverride)
                    : profile.TransitionDurationSeconds;
            _pendingSnowWarmStart = shouldWarmStartSnow;

            if (instant || _transitionDuration <= 0f)
            {
                _currentProfile = profile;
                ApplyProfileBlend(profile, profile, 1f);
            }

            ResetWeatherTimer(profile);
        }

        private void TickAutoWeather(float deltaTime)
        {
            if (!autoWeather || profiles == null || profiles.Length <= 1)
            {
                return;
            }

            _weatherTimer -= deltaTime;
            if (_weatherTimer > 0f)
            {
                return;
            }

            WeatherProfile nextProfile = PickWeightedProfile(_targetProfile);
            if (nextProfile != null)
            {
                SetProfile(nextProfile, false);
            }
        }

        private void TickTransition(float deltaTime)
        {
            EnsureCurrentProfile();

            if (_targetProfile == null)
            {
                ApplyClearState();
                return;
            }

            if (_transitionDuration <= 0f)
            {
                _currentProfile = _targetProfile;
                ApplyProfileBlend(_targetProfile, _targetProfile, 1f);
                return;
            }

            _transitionTimer += deltaTime;
            float t = Mathf.Clamp01(_transitionTimer / _transitionDuration);
            ApplyProfileBlend(_sourceProfile, _targetProfile, Smooth01(t));

            if (t >= 1f)
            {
                _currentProfile = _targetProfile;
                _sourceProfile = _targetProfile;
                _transitionDuration = 0f;
            }
        }

        private void SetInitialProfile()
        {
            if (_targetProfile != null)
            {
                return;
            }

            WeatherProfile profile = initialProfile != null ? initialProfile : FindProfile(WeatherType.Clear);
            if (profile == null && profiles != null && profiles.Length > 0)
            {
                profile = profiles[0];
            }

            if (profile != null)
            {
                _currentProfile = profile;
                _sourceProfile = profile;
                _targetProfile = profile;
                ResetWeatherTimer(profile);
            }
        }

        private void EnsureCurrentProfile()
        {
            if (_targetProfile == null)
            {
                SetInitialProfile();
            }

            if (_sourceProfile == null)
            {
                _sourceProfile = _targetProfile;
            }

            if (_currentProfile == null)
            {
                _currentProfile = _sourceProfile != null ? _sourceProfile : _targetProfile;
            }
        }

        private WeatherProfile FindProfile(WeatherType weatherType)
        {
            if (profiles == null)
            {
                return null;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                WeatherProfile profile = profiles[i];
                if (profile != null && profile.WeatherType == weatherType)
                {
                    return profile;
                }
            }

            return null;
        }

        private WeatherProfile PickWeightedProfile(WeatherProfile excludeProfile)
        {
            float totalWeight = 0f;
            for (int i = 0; i < profiles.Length; i++)
            {
                WeatherProfile profile = profiles[i];
                if (profile == null || profile == excludeProfile)
                {
                    continue;
                }

                totalWeight += profile.SelectionWeight;
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < profiles.Length; i++)
            {
                WeatherProfile profile = profiles[i];
                if (profile == null || profile == excludeProfile)
                {
                    continue;
                }

                roll -= profile.SelectionWeight;
                if (roll <= 0f)
                {
                    return profile;
                }
            }

            return null;
        }

        private void ResetWeatherTimer(WeatherProfile profile)
        {
            if (profile == null)
            {
                _weatherTimer = 60f;
                return;
            }

            _weatherTimer = Random.Range(profile.MinDurationSeconds, profile.MaxDurationSeconds);
        }

        private void ApplyCurrentVisualState()
        {
            EnsureCurrentProfile();
            if (_targetProfile == null)
            {
                ApplyClearState();
                return;
            }

            ApplyProfileBlend(_targetProfile, _targetProfile, 1f);
        }

        private void ApplyProfileBlend(WeatherProfile from, WeatherProfile to, float t)
        {
            if (from == null && to == null)
            {
                ApplyClearState();
                return;
            }

            from ??= to;
            to ??= from;

            // Blend every visible weather output from the same transition value so particles, fog, and lighting stay synchronized.
            float nightFactor = GetWeatherNightFactor();
            _currentRainRate = Mathf.Lerp(from.RainEmissionRate, to.RainEmissionRate, t) * Mathf.Lerp(from.ParticleIntensity, to.ParticleIntensity, t);
            _currentRainSplashRate = Mathf.Lerp(from.RainSplashEmissionRate, to.RainSplashEmissionRate, t) * Mathf.Lerp(from.ParticleIntensity, to.ParticleIntensity, t);
            _currentSnowRate = Mathf.Lerp(from.SnowEmissionRate, to.SnowEmissionRate, t) * Mathf.Lerp(from.ParticleIntensity, to.ParticleIntensity, t);
            _currentFogAlpha = EvaluateFogAlpha(from, to, t, nightFactor);
            _currentFogColor = EvaluateFogColor(from, to, t, nightFactor);
            _currentVolumeWeight = EvaluateWeatherVolumeWeight(from, to, t, nightFactor);
            LightColorMultiplier = MultiplyColor(Color.white, Color.LerpUnclamped(from.LightColorMultiplier, to.LightColorMultiplier, t));
            LightIntensityMultiplier = Mathf.Lerp(from.LightIntensityMultiplier, to.LightIntensityMultiplier, t);

            ApplyRainParticleColor(from, to, t);
            ApplyRainSplashParticleColor(from, to, t);
            ApplySnowParticleColor(from, to, t);
            ApplyRainParticleSize(from, to, t);
            ApplySnowParticleSize(from, to, t);
            ApplyRainRendererLength(from, to, t);
            ApplyWind(rainParticles, from, to, t);
            ApplyWind(snowParticles, from, to, t);
            ApplyParticleSystem(rainParticles, _currentRainRate, false);
            ApplyParticleSystem(rainSplashParticles, _currentRainSplashRate, false);
            _pendingSnowWarmStart = ApplyParticleSystem(snowParticles, _currentSnowRate, _pendingSnowWarmStart);
            ApplyFogOverlay();
            ApplyWeatherVolume();
        }

        private void ApplyClearState()
        {
            _currentRainRate = 0f;
            _currentRainSplashRate = 0f;
            _currentSnowRate = 0f;
            _currentFogAlpha = 0f;
            _currentFogColor = Color.white;
            _currentVolumeWeight = 0f;
            LightColorMultiplier = Color.white;
            LightIntensityMultiplier = 1f;

            ApplyParticleSystem(rainParticles, 0f);
            ApplyParticleSystem(rainSplashParticles, 0f);
            ApplyParticleSystem(snowParticles, 0f);
            ApplyFogOverlay();
            ApplyWeatherVolume();
        }

        private void ApplyParticleSystem(ParticleSystem particleSystem, float emissionRate)
        {
            ApplyParticleSystem(particleSystem, emissionRate, false);
        }

        private bool ApplyParticleSystem(ParticleSystem particleSystem, float emissionRate, bool warmStart)
        {
            if (particleSystem == null)
            {
                return warmStart;
            }

            float safeEmissionRate = Mathf.Max(0f, emissionRate);
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = safeEmissionRate;

            if (!Application.isPlaying)
            {
                return warmStart;
            }

            bool shouldEmit = safeEmissionRate > ParticleEmissionThreshold;
            if (shouldEmit)
            {
                if (!particleSystem.isPlaying)
                {
                    particleSystem.Play();
                }

                if (warmStart)
                {
                    WarmStartSnowParticles(particleSystem, safeEmissionRate);
                    warmStart = false;
                }
            }
            else if (!IsWeatherTransitionActive() && particleSystem.isPlaying && particleSystem.particleCount == 0)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            return warmStart;
        }

        private bool IsWeatherTransitionActive()
        {
            return _transitionDuration > 0f && _transitionTimer < _transitionDuration;
        }

        private void WarmStartSnowParticles(ParticleSystem particleSystem, float emissionRate)
        {
            if (particleSystem == null || emissionRate <= ParticleEmissionThreshold)
            {
                return;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            int maxParticles = Mathf.Max(1, main.maxParticles);
            int emitCount = Mathf.Clamp(Mathf.RoundToInt(emissionRate * SnowWarmStartSeconds), 12, Mathf.Min(96, maxParticles));
            if (emitCount <= 0)
            {
                return;
            }

            particleSystem.Emit(emitCount);
        }

        private void ApplyFogOverlay()
        {
            if (fogOverlay == null)
            {
                return;
            }

            Color color = _currentFogColor;
            color.a *= _currentFogAlpha;
            fogOverlay.color = color;
            fogOverlay.enabled = color.a > 0.001f;
            ApplyFogDistanceProperties();
        }

        private void ApplyWeatherVolume()
        {
            if (weatherVolume != null)
            {
                weatherVolume.weight = _currentVolumeWeight;
            }
        }

        private void ApplyWind(ParticleSystem particleSystem, WeatherProfile from, WeatherProfile to, float t)
        {
            if (particleSystem == null || from == null || to == null)
            {
                return;
            }

            Vector2 direction = Vector2.Lerp(from.WindDirection, to.WindDirection, t);
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.down;
            }

            direction.Normalize();
            float strength = Mathf.Lerp(from.WindStrength, to.WindStrength, t);
            ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = strength > 0.001f;
            velocity.x = direction.x * strength;
            velocity.y = direction.y * strength;
        }

        private void ApplyRainParticleColor(WeatherProfile from, WeatherProfile to, float t)
        {
            if (rainParticles == null || from == null || to == null)
            {
                return;
            }

            ParticleSystem.MainModule main = rainParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(EvaluateRainParticleColor(from, to, t));
        }

        private void ApplyRainSplashParticleColor(WeatherProfile from, WeatherProfile to, float t)
        {
            if (rainSplashParticles == null || from == null || to == null)
            {
                return;
            }

            ParticleSystem.MainModule main = rainSplashParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(EvaluateRainSplashParticleColor(from, to, t));
        }

        private void ApplySnowParticleColor(WeatherProfile from, WeatherProfile to, float t)
        {
            if (snowParticles == null || from == null || to == null)
            {
                return;
            }

            ParticleSystem.MainModule main = snowParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(EvaluateSnowParticleColor(from, to, t));
        }

        private Color EvaluateRainParticleColor(WeatherProfile from, WeatherProfile to, float t)
        {
            Color color = Color.LerpUnclamped(from.RainColor, to.RainColor, t);
            if (!from.AdaptRainColorToDayNight && !to.AdaptRainColorToDayNight)
            {
                return color;
            }

            float nightFactor = GetWeatherNightFactor();
            float dayAlphaMultiplier = Mathf.Lerp(from.RainDayAlphaMultiplier, to.RainDayAlphaMultiplier, t);
            float nightAlphaMultiplier = Mathf.Lerp(from.RainNightAlphaMultiplier, to.RainNightAlphaMultiplier, t);
            float fogAlphaMultiplier = Mathf.Lerp(from.RainFogAlphaMultiplier, to.RainFogAlphaMultiplier, t);
            float minAlpha = Mathf.Lerp(from.RainMinAlpha, to.RainMinAlpha, t);
            float maxAlpha = Mathf.Max(minAlpha, Mathf.Lerp(from.RainMaxAlpha, to.RainMaxAlpha, t));
            float brightnessBoost = Mathf.Lerp(from.RainNightBrightnessBoost, to.RainNightBrightnessBoost, t) * nightFactor;

            float alphaMultiplier = Mathf.Lerp(dayAlphaMultiplier, nightAlphaMultiplier, nightFactor);
            alphaMultiplier = Mathf.Lerp(alphaMultiplier, alphaMultiplier * fogAlphaMultiplier, _currentFogAlpha);
            color.a = Mathf.Clamp(color.a * alphaMultiplier, minAlpha, maxAlpha);

            color.r = Mathf.Lerp(color.r, 1f, brightnessBoost);
            color.g = Mathf.Lerp(color.g, 1f, brightnessBoost);
            color.b = Mathf.Lerp(color.b, 1f, brightnessBoost);
            return color;
        }

        private Color EvaluateRainSplashParticleColor(WeatherProfile from, WeatherProfile to, float t)
        {
            Color color = Color.LerpUnclamped(from.RainSplashColor, to.RainSplashColor, t);
            float nightFactor = GetWeatherNightFactor();
            float dayAlphaMultiplier = Mathf.Lerp(from.RainSplashDayAlphaMultiplier, to.RainSplashDayAlphaMultiplier, t);
            float nightAlphaMultiplier = Mathf.Lerp(from.RainSplashNightAlphaMultiplier, to.RainSplashNightAlphaMultiplier, t);
            color.a *= Mathf.Lerp(dayAlphaMultiplier, nightAlphaMultiplier, nightFactor);
            return color;
        }

        private Color EvaluateSnowParticleColor(WeatherProfile from, WeatherProfile to, float t)
        {
            Color color = Color.LerpUnclamped(from.SnowColor, to.SnowColor, t);
            float nightFactor = GetWeatherNightFactor();
            float dayAlphaMultiplier = Mathf.Lerp(from.SnowDayAlphaMultiplier, to.SnowDayAlphaMultiplier, t);
            float nightAlphaMultiplier = Mathf.Lerp(from.SnowNightAlphaMultiplier, to.SnowNightAlphaMultiplier, t);
            Color nightTint = Color.LerpUnclamped(from.SnowNightTint, to.SnowNightTint, t);
            float nightTintBlend = Mathf.Lerp(from.SnowNightTintBlend, to.SnowNightTintBlend, t) * nightFactor;

            float alpha = color.a * Mathf.Lerp(dayAlphaMultiplier, nightAlphaMultiplier, nightFactor);
            color = Color.LerpUnclamped(color, nightTint, nightTintBlend);
            color.a = alpha;
            return color;
        }

        private float EvaluateFogAlpha(WeatherProfile from, WeatherProfile to, float t, float nightFactor)
        {
            float baseAlpha = Mathf.Lerp(from.FogOverlayAlpha, to.FogOverlayAlpha, t);
            float dayAlphaMultiplier = Mathf.Lerp(from.FogDayAlphaMultiplier, to.FogDayAlphaMultiplier, t);
            float nightAlphaMultiplier = Mathf.Lerp(from.FogNightAlphaMultiplier, to.FogNightAlphaMultiplier, t);
            return baseAlpha * Mathf.Lerp(dayAlphaMultiplier, nightAlphaMultiplier, nightFactor);
        }

        private Color EvaluateFogColor(WeatherProfile from, WeatherProfile to, float t, float nightFactor)
        {
            Color color = Color.LerpUnclamped(from.FogOverlayColor, to.FogOverlayColor, t);
            Color nightColor = Color.LerpUnclamped(from.FogNightColor, to.FogNightColor, t);
            float nightColorBlend = Mathf.Lerp(from.FogNightColorBlend, to.FogNightColorBlend, t) * nightFactor;
            return Color.LerpUnclamped(color, nightColor, nightColorBlend);
        }

        private float EvaluateWeatherVolumeWeight(WeatherProfile from, WeatherProfile to, float t, float nightFactor)
        {
            float baseWeight = Mathf.Lerp(from.WeatherVolumeWeight, to.WeatherVolumeWeight, t);
            float dayMultiplier = Mathf.Lerp(from.WeatherVolumeDayMultiplier, to.WeatherVolumeDayMultiplier, t);
            float nightMultiplier = Mathf.Lerp(from.WeatherVolumeNightMultiplier, to.WeatherVolumeNightMultiplier, t);
            return baseWeight * Mathf.Lerp(dayMultiplier, nightMultiplier, nightFactor);
        }

        private float GetWeatherNightFactor()
        {
            if (dayNightController == null)
            {
                return 0f;
            }

            float hour = dayNightController.TimeOfDay;
            if (hour >= 7f && hour < 17f)
            {
                return 0f;
            }

            if (hour >= 19f || hour < 5f)
            {
                return 1f;
            }

            if (hour < 7f)
            {
                return 1f - Smooth01((hour - 5f) / 2f);
            }

            return Smooth01((hour - 17f) / 2f);
        }

        private void ApplyRainParticleSize(WeatherProfile from, WeatherProfile to, float t)
        {
            if (rainParticles == null || from == null || to == null)
            {
                return;
            }

            float minSize = Mathf.Lerp(from.RainStreakMinSize, to.RainStreakMinSize, t);
            float maxSize = Mathf.Lerp(from.RainStreakMaxSize, to.RainStreakMaxSize, t);
            ParticleSystem.MainModule main = rainParticles.main;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, Mathf.Max(minSize, maxSize));
        }

        private void ApplySnowParticleSize(WeatherProfile from, WeatherProfile to, float t)
        {
            if (snowParticles == null || from == null || to == null)
            {
                return;
            }

            float minSize = Mathf.Lerp(from.SnowParticleMinSize, to.SnowParticleMinSize, t);
            float maxSize = Mathf.Lerp(from.SnowParticleMaxSize, to.SnowParticleMaxSize, t);
            ParticleSystem.MainModule main = snowParticles.main;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, Mathf.Max(minSize, maxSize));
        }

        private void ApplyRainRendererLength(WeatherProfile from, WeatherProfile to, float t)
        {
            if (from == null || to == null)
            {
                return;
            }

            ParticleSystemRenderer particleRenderer = GetRainParticleRenderer();
            if (particleRenderer == null)
            {
                return;
            }

            particleRenderer.lengthScale = Mathf.Lerp(from.RainLengthScale, to.RainLengthScale, t);
            particleRenderer.velocityScale = Mathf.Lerp(from.RainVelocityLengthScale, to.RainVelocityLengthScale, t);
        }

        private ParticleSystemRenderer GetRainParticleRenderer()
        {
            if (rainParticles == null)
            {
                _cachedRainParticles = null;
                _rainParticleRenderer = null;
                return null;
            }

            if (_cachedRainParticles != rainParticles || _rainParticleRenderer == null)
            {
                _cachedRainParticles = rainParticles;
                _rainParticleRenderer = rainParticles.GetComponent<ParticleSystemRenderer>();
            }

            return _rainParticleRenderer;
        }

        private void FollowAndResizeRig()
        {
            if (weatherRig == null || targetCamera == null)
            {
                return;
            }

            if (followCamera)
            {
                Transform cameraTransform = targetCamera.transform;
                weatherRig.position = cameraTransform.position + rigOffset;
            }

            // Size the rig from the camera frustum so weather covers the visible screen while the rig follows the camera.
            float halfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 5f;
            float height = halfHeight * 2f + screenPadding * 2f;
            float width = height * targetCamera.aspect + screenPadding * 2f;

            if (autoScaleParticleShapes)
            {
                SetParticleShapeSize(rainParticles, width, height);
                SetParticleShapeSize(rainSplashParticles, width, height);
                SetParticleShapeSize(snowParticles, width, height);
            }

            if (autoScaleFogOverlay && fogOverlay != null)
            {
                fogOverlay.transform.localScale = new Vector3(width, height, 1f);
            }
        }

        private void SetParticleShapeSize(ParticleSystem particleSystem, float width, float height)
        {
            if (particleSystem == null)
            {
                return;
            }

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.scale = new Vector3(width, height, 0.1f);
        }

        private void ResolveCameraIfNeeded()
        {
            if (!autoFindMainCamera || targetCamera != null)
            {
                return;
            }

            targetCamera = Camera.main;
        }

        private void ResolveFogFocusTargetIfNeeded()
        {
            if (!autoFindFogFocusTarget || fogFocusTarget != null || _searchedFogFocusTarget)
            {
                return;
            }

            _searchedFogFocusTarget = true;

            if (targetCamera != null)
            {
                CameraFollow2D cameraFollow = targetCamera.GetComponent<CameraFollow2D>();
                if (cameraFollow != null && cameraFollow.target != null)
                {
                    fogFocusTarget = cameraFollow.target;
                    return;
                }
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                fogFocusTarget = player.transform;
                return;
            }

            DemoPlayer demoPlayer = Object.FindFirstObjectByType<DemoPlayer>();
            if (demoPlayer != null)
            {
                fogFocusTarget = demoPlayer.transform;
            }
        }

        private void FindDayNightControllerIfNeeded()
        {
            if (!autoFindDayNightController || dayNightController != null)
            {
                return;
            }

            DayNightController[] controllers = Object.FindObjectsByType<DayNightController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (controllers != null && controllers.Length > 0)
            {
                dayNightController = controllers[0];
            }
        }

        [ContextMenu("Apply Rendering Order")]
        public void ApplySorting()
        {
            ApplySorting(rainParticles);
            ApplySorting(rainSplashParticles);
            ApplySorting(snowParticles);

            if (fogOverlay != null)
            {
                fogOverlay.sortingLayerName = weatherSortingLayer;
                fogOverlay.sortingOrder = weatherSortingOrder;
                EnsureFogOverlayMaterial();
            }
        }

        private void EnsureFogOverlayMaterial()
        {
            if (fogOverlay == null)
            {
                return;
            }

            if (fogOverlay.sharedMaterial != null && fogOverlay.sharedMaterial.shader != null &&
                fogOverlay.sharedMaterial.shader.name == "LayerLab/Weather/FogOverlay2D")
            {
                return;
            }

            Material material = GetFogOverlayMaterial();
            if (material != null)
            {
                fogOverlay.sharedMaterial = material;
            }
        }

        private Material GetFogOverlayMaterial()
        {
            if (_fogOverlayMaterial != null)
            {
                return _fogOverlayMaterial;
            }

            _fogOverlayMaterial = Resources.Load<Material>(FogOverlayMaterialResourcePath);
            if (_fogOverlayMaterial != null)
            {
                return _fogOverlayMaterial;
            }

            Shader shader = Shader.Find("LayerLab/Weather/FogOverlay2D");
            if (shader == null)
            {
                return null;
            }

            _fogOverlayMaterial = new Material(shader)
            {
                name = "WeatherFogOverlay_Runtime",
                hideFlags = HideFlags.DontSave
            };
            return _fogOverlayMaterial;
        }

        private void ApplyFogDistanceProperties()
        {
            if (fogOverlay == null)
            {
                return;
            }

            EnsureFogOverlayMaterial();

            if (_fogPropertyBlock == null)
            {
                _fogPropertyBlock = new MaterialPropertyBlock();
            }

            Vector3 focusPosition = fogFocusTarget != null
                ? fogFocusTarget.position
                : targetCamera != null
                    ? targetCamera.transform.position
                    : fogOverlay.transform.position;

            fogOverlay.GetPropertyBlock(_fogPropertyBlock);
            _fogPropertyBlock.SetVector(FogFocusPositionId, new Vector4(focusPosition.x, focusPosition.y, 0f, 0f));
            _fogPropertyBlock.SetFloat(FogClearRadiusId, fogClearRadius);
            _fogPropertyBlock.SetFloat(FogFullRadiusId, Mathf.Max(fogClearRadius + 0.01f, fogFullRadius));
            _fogPropertyBlock.SetFloat(FogGradientPowerId, fogGradientPower);
            fogOverlay.SetPropertyBlock(_fogPropertyBlock);
        }

        private void ApplySorting(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer == null)
            {
                return;
            }

            particleRenderer.sortingLayerName = weatherSortingLayer;
            particleRenderer.sortingOrder = weatherSortingOrder;

            if (particleSystem == rainParticles)
            {
                Material rainMaterial = GetRainStreakParticleMaterial();
                if (rainMaterial != null)
                {
                    particleRenderer.sharedMaterial = rainMaterial;
                }
            }
        }

        private Material GetRainStreakParticleMaterial()
        {
            if (_rainStreakMaterial != null)
            {
                return _rainStreakMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            if (shader == null)
            {
                return null;
            }

            _rainStreakMaterial = new Material(shader)
            {
                name = "WeatherRainStreak_Runtime",
                hideFlags = HideFlags.DontSave
            };

            Texture2D texture = GetRainStreakTexture();
            _rainStreakMaterial.mainTexture = texture;
            if (_rainStreakMaterial.HasProperty("_MainTex"))
            {
                _rainStreakMaterial.SetTexture("_MainTex", texture);
            }

            if (_rainStreakMaterial.HasProperty("_BaseMap"))
            {
                _rainStreakMaterial.SetTexture("_BaseMap", texture);
            }

            if (_rainStreakMaterial.HasProperty("_Color"))
            {
                _rainStreakMaterial.SetColor("_Color", Color.white);
            }

            if (_rainStreakMaterial.HasProperty("_BaseColor"))
            {
                _rainStreakMaterial.SetColor("_BaseColor", Color.white);
            }

            return _rainStreakMaterial;
        }

        private Texture2D GetRainStreakTexture()
        {
            if (_rainStreakTexture != null)
            {
                return _rainStreakTexture;
            }

            const int width = 64;
            const int height = 16;
            _rainStreakTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "WeatherRainStreak_Runtime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float widthDistance = Mathf.Abs(v - 0.5f) * 2f;
                float widthAlpha = Smooth01(1f - widthDistance);

                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float fadeT = Mathf.InverseLerp(0.08f, 1f, u);
                    float lengthAlpha = Mathf.Pow(1f - fadeT, 1.75f);
                    float alpha = Mathf.Clamp01(lengthAlpha * widthAlpha);
                    _rainStreakTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            _rainStreakTexture.Apply(false, true);
            return _rainStreakTexture;
        }

#if UNITY_EDITOR
        [ContextMenu("Create/Link Default Particle Rig")]
        public void CreateDefaultParticleRig()
        {
            Transform rig = EnsureWeatherRig();
            rainParticles = EnsureParticleSystem(rig, "RainParticles", ConfigureRainParticles);
            rainSplashParticles = EnsureParticleSystem(rig, "RainSplashParticles", ConfigureRainSplashParticles);
            snowParticles = EnsureParticleSystem(rig, "SnowParticles", ConfigureSnowParticles);
            fogOverlay = EnsureFogOverlay(rig);
            weatherVolume = EnsureWeatherVolume(rig);

            ApplySorting();
            FollowAndResizeRig();
            ApplyCurrentVisualState();
            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }

        [ContextMenu("Auto-Link Default Profiles")]
        public void AutoAssignProfilesFromProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:WeatherProfile", new[] { "Assets/_Project/Weather/Profiles" });
            if (guids == null || guids.Length == 0)
            {
                return;
            }

            List<WeatherProfile> foundProfiles = new List<WeatherProfile>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                WeatherProfile profile = AssetDatabase.LoadAssetAtPath<WeatherProfile>(path);
                if (profile != null)
                {
                    foundProfiles.Add(profile);
                }
            }

            foundProfiles.Sort((left, right) => left.WeatherType.CompareTo(right.WeatherType));
            profiles = foundProfiles.ToArray();

            if (initialProfile == null)
            {
                initialProfile = FindProfile(WeatherType.Clear);
            }
        }

        private Transform EnsureWeatherRig()
        {
            if (weatherRig != null)
            {
                return weatherRig;
            }

            Transform existing = transform.Find("WeatherRig");
            if (existing != null)
            {
                weatherRig = existing;
                return weatherRig;
            }

            GameObject rigObject = new GameObject("WeatherRig");
            Undo.RegisterCreatedObjectUndo(rigObject, "Create Default WeatherRig");
            rigObject.transform.SetParent(transform, false);
            rigObject.transform.localPosition = Vector3.zero;
            weatherRig = rigObject.transform;
            return weatherRig;
        }

        private ParticleSystem EnsureParticleSystem(Transform parent, string childName, System.Action<ParticleSystem> configure)
        {
            Transform existing = parent.Find(childName);
            ParticleSystem particleSystem;

            if (existing != null)
            {
                particleSystem = existing.GetComponent<ParticleSystem>();
                if (particleSystem == null)
                {
                    particleSystem = Undo.AddComponent<ParticleSystem>(existing.gameObject);
                }
            }
            else
            {
                GameObject child = new GameObject(childName);
                Undo.RegisterCreatedObjectUndo(child, "Create Default Weather Particle");
                child.transform.SetParent(parent, false);
                particleSystem = child.AddComponent<ParticleSystem>();
            }

            configure(particleSystem);
            EditorUtility.SetDirty(particleSystem);
            return particleSystem;
        }

        private SpriteRenderer EnsureFogOverlay(Transform parent)
        {
            Transform existing = parent.Find("FogOverlay");
            SpriteRenderer spriteRenderer;

            if (existing != null)
            {
                spriteRenderer = existing.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = Undo.AddComponent<SpriteRenderer>(existing.gameObject);
                }
            }
            else
            {
                GameObject child = new GameObject("FogOverlay");
                Undo.RegisterCreatedObjectUndo(child, "Create Default Fog Overlay");
                child.transform.SetParent(parent, false);
                child.transform.localPosition = new Vector3(0f, 0f, 0.5f);
                spriteRenderer = child.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = CreateRuntimeFogSprite();
            spriteRenderer.color = new Color(0.65f, 0.68f, 0.7f, 0f);
            spriteRenderer.sortingLayerName = weatherSortingLayer;
            spriteRenderer.sortingOrder = weatherSortingOrder;
            Material material = GetFogOverlayMaterial();
            if (material != null)
            {
                spriteRenderer.sharedMaterial = material;
            }
            spriteRenderer.enabled = false;
            EditorUtility.SetDirty(spriteRenderer);
            return spriteRenderer;
        }

        private Volume EnsureWeatherVolume(Transform parent)
        {
            Transform existing = parent.Find("WeatherVolume");
            Volume volume;

            if (existing != null)
            {
                volume = existing.GetComponent<Volume>();
                if (volume == null)
                {
                    volume = Undo.AddComponent<Volume>(existing.gameObject);
                }
            }
            else
            {
                GameObject child = new GameObject("WeatherVolume");
                Undo.RegisterCreatedObjectUndo(child, "Create Default Weather Volume");
                child.transform.SetParent(parent, false);
                volume = child.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.weight = 0f;
            EditorUtility.SetDirty(volume);
            return volume;
        }

        private void ConfigureRainParticles(ParticleSystem particleSystem)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.62f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.12f);
            main.startRotation = -4f * Mathf.Deg2Rad;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.88f, 1f, 0.62f));
            main.maxParticles = 2200;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, 4f, 0f);
            shape.scale = new Vector3(18f, 3f, 0.1f);

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.lengthScale = 2.2f;
            particleRenderer.velocityScale = 0.02f;
            particleRenderer.sortingLayerName = weatherSortingLayer;
            particleRenderer.sortingOrder = weatherSortingOrder;
            particleRenderer.sharedMaterial = GetRainStreakParticleMaterial();
        }

        private void ConfigureRainSplashParticles(ParticleSystem particleSystem)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.9f, 1f, 0.46f));
            main.maxParticles = 1800;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.zero;
            shape.scale = new Vector3(18f, 12f, 0.1f);

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingLayerName = weatherSortingLayer;
            particleRenderer.sortingOrder = weatherSortingOrder;
            particleRenderer.sharedMaterial = GetDefaultParticleMaterial();
        }

        private void ConfigureSnowParticles(ParticleSystem particleSystem)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 0.97f, 1f, 0.75f));
            main.maxParticles = 700;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, 4.2f, 0f);
            shape.scale = new Vector3(18f, 2f, 0.1f);

            ParticleSystem.NoiseModule noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.42f;
            noise.frequency = 0.28f;
            noise.scrollSpeed = 0.22f;

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingLayerName = weatherSortingLayer;
            particleRenderer.sortingOrder = weatherSortingOrder;
            particleRenderer.sharedMaterial = GetDefaultParticleMaterial();
        }

        private Material GetDefaultParticleMaterial()
        {
            Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (material != null)
            {
                return material;
            }

            return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        }

        private Sprite CreateRuntimeFogSprite()
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            texture.name = "WeatherFogOverlay_Runtime";
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
        }
#endif

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static Color MultiplyColor(Color left, Color right)
        {
            return new Color(left.r * right.r, left.g * right.g, left.b * right.b, left.a * right.a);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WeatherController))]
    public class WeatherControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WeatherController controller = (WeatherController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear"))
            {
                controller.ApplyClear();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Rain"))
            {
                controller.ApplyRain();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Snow"))
            {
                controller.ApplySnow();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Fog"))
            {
                controller.ApplyFog();
                EditorUtility.SetDirty(controller);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Rendering Order"))
            {
                controller.ApplySorting();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Auto-Link Default Profiles"))
            {
                controller.AutoAssignProfilesFromProject();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Create/Link Default Particle Rig"))
            {
                controller.CreateDefaultParticleRig();
                EditorUtility.SetDirty(controller);
            }
        }
    }
#endif
}
