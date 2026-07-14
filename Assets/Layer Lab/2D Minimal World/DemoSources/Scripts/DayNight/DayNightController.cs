using System.Collections.Generic;
using FunkyCode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LayerLab
{
    // Drives a Light2D-based day/night cycle and notifies reactive targets (buildings and light groups).
    [ExecuteAlways]
    public class DayNightController : MonoBehaviour
    {
        [Header("Time")]
        [SerializeField] private bool autoAdvance = true;
        [SerializeField, Range(0f, 24f)] private float timeOfDay = 12f;
        [SerializeField, Min(1f)] private float cycleDurationSeconds = 600f;

        [Header("Inspector Presets")]
        [SerializeField, Range(0f, 24f)] private float dayPresetTime = 12f;
        [SerializeField, Range(0f, 24f)] private float nightPresetTime = 22f;

        [Header("Time Reference")]
        [SerializeField, Range(0f, 24f)] private float dawnStartTime = 5f;
        [SerializeField, Range(0f, 24f)] private float dayStartTime = 7f;
        [SerializeField, Range(0f, 24f)] private float duskStartTime = 17f;
        [SerializeField, Range(0f, 24f)] private float nightStartTime = 19f;
        [SerializeField, Range(0f, 24f)] private float localLightsOnTime = 18.5f;
        [SerializeField, Range(0f, 24f)] private float localLightsOffTime = 6f;

        [Header("Global Light2D")]
        [SerializeField] private Light2D globalLight;
        [SerializeField] private bool autoFindGlobalLight = true;
        [SerializeField] private Color nightColor = new Color(0.12f, 0.16f, 0.32f, 1f);
        [SerializeField, Min(0f)] private float nightIntensity = 0.32f;

        [Header("Dawn Colors")]
        [SerializeField] private Color preDawnColor = new Color(0.28f, 0.27f, 0.45f, 1f);
        [SerializeField, Min(0f)] private float preDawnIntensity = 0.42f;
        [SerializeField] private Color dawnColor = new Color(1f, 0.55f, 0.36f, 1f);
        [SerializeField, Min(0f)] private float dawnIntensity = 0.68f;
        [SerializeField] private Color morningColor = new Color(1f, 0.82f, 0.58f, 1f);
        [SerializeField, Min(0f)] private float morningIntensity = 0.88f;

        [Header("Day Colors")]
        [SerializeField] private Color dayColor = new Color(1f, 0.98f, 0.92f, 1f);
        [SerializeField, Min(0f)] private float dayIntensity = 1.05f;

        [Header("Dusk Colors")]
        [SerializeField] private Color goldenHourColor = new Color(1f, 0.78f, 0.38f, 1f);
        [SerializeField, Min(0f)] private float goldenHourIntensity = 0.92f;
        [SerializeField] private Color duskColor = new Color(1f, 0.42f, 0.26f, 1f);
        [SerializeField, Min(0f)] private float duskIntensity = 0.72f;
        [SerializeField] private Color twilightColor = new Color(0.42f, 0.28f, 0.58f, 1f);
        [SerializeField, Min(0f)] private float twilightIntensity = 0.48f;

        [Header("Weather Adjustment")]
        [SerializeField] private WeatherController weatherController;
        [SerializeField] private bool autoFindWeatherController = true;

        [Header("Reactive Targets")]
        [SerializeField] private bool autoCollectSceneTargets = true;
        [SerializeField] private bool includeInactiveTargets = true;
        [SerializeField] private DayNightBuilding[] manualBuildings;
        [SerializeField] private DayNightLightGroup[] manualLightGroups;

#if UNITY_EDITOR
        [Header("Editor Display")]
        [SerializeField] private bool showEditorTimeOverlay = true;

        private GUIStyle _editorTimeOverlayStyle;
#endif

        private readonly List<DayNightBuilding> _buildings = new List<DayNightBuilding>(32);
        private readonly List<DayNightLightGroup> _lightGroups = new List<DayNightLightGroup>(32);
        private bool _lastLocalLightsActive;
        private bool _hasAppliedLocalState;

#if UNITY_EDITOR
        private bool _hasPendingEditorStateApply;
#endif

        public bool AutoAdvance
        {
            get => autoAdvance;
            set => autoAdvance = value;
        }

        public float TimeOfDay => timeOfDay;
        public bool LocalLightsActive => IsLocalLightingActive(timeOfDay);

        private void Reset()
        {
            FindGlobalLightIfNeeded();
            FindWeatherControllerIfNeeded();
            CollectReactiveTargets();
            ApplyCurrentState(true);
        }

        private void Awake()
        {
            FindGlobalLightIfNeeded();
            FindWeatherControllerIfNeeded();
            CollectReactiveTargets();
            ApplyCurrentState(true);
        }

        private void OnEnable()
        {
            FindGlobalLightIfNeeded();
            FindWeatherControllerIfNeeded();
            CollectReactiveTargets();
            ApplyCurrentState(true);
        }

        private void Update()
        {
            if (Application.isPlaying && autoAdvance)
            {
                AdvanceTime(Time.deltaTime);
            }

            ApplyCurrentState(false);
        }

        private void OnValidate()
        {
            cycleDurationSeconds = Mathf.Max(1f, cycleDurationSeconds);
            timeOfDay = NormalizeHour(timeOfDay);
            dayPresetTime = NormalizeHour(dayPresetTime);
            nightPresetTime = NormalizeHour(nightPresetTime);
            dawnStartTime = NormalizeHour(dawnStartTime);
            dayStartTime = NormalizeHour(dayStartTime);
            duskStartTime = NormalizeHour(duskStartTime);
            nightStartTime = NormalizeHour(nightStartTime);
            localLightsOnTime = NormalizeHour(localLightsOnTime);
            localLightsOffTime = NormalizeHour(localLightsOffTime);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ScheduleEditorStateApply();
            }
#endif
        }

#if UNITY_EDITOR
        private void ScheduleEditorStateApply()
        {
            if (_hasPendingEditorStateApply)
            {
                return;
            }

            _hasPendingEditorStateApply = true;
            EditorApplication.delayCall += ApplyEditorStateDelayed;
        }

        private void ApplyEditorStateDelayed()
        {
            _hasPendingEditorStateApply = false;

            if (this == null || Application.isPlaying)
            {
                return;
            }

            FindGlobalLightIfNeeded();
            FindWeatherControllerIfNeeded();
            CollectReactiveTargets();
            ApplyCurrentState(true);
        }
#endif

        public void SetTime(float hour)
        {
            timeOfDay = NormalizeHour(hour);
            ApplyCurrentState(true);
        }

        [ContextMenu("Apply Day Preset")]
        public void ApplyDayPreset()
        {
            autoAdvance = false;
            SetTime(dayPresetTime);
        }

        [ContextMenu("Apply Night Preset")]
        public void ApplyNightPreset()
        {
            autoAdvance = false;
            SetTime(nightPresetTime);
        }

        // Rebuilds the reactive target lists from manual references and (optionally) a scene scan.
        [ContextMenu("Recollect Reactive Targets")]
        public void CollectReactiveTargets()
        {
            _buildings.Clear();
            _lightGroups.Clear();

            AddManualTargets();

            if (autoCollectSceneTargets)
            {
                CollectSceneTargets();
            }
        }

        public void ApplyCurrentState(bool forceReactiveTargets)
        {
            // Apply global lighting first, then refresh scene targets that depend on the local-light state.
            ApplyGlobalLight();

            bool localLightsActive = IsLocalLightingActive(timeOfDay);
            if (!forceReactiveTargets && _hasAppliedLocalState && localLightsActive == _lastLocalLightsActive)
            {
                return;
            }

            ApplyReactiveTargets(localLightsActive);
            _lastLocalLightsActive = localLightsActive;
            _hasAppliedLocalState = true;
        }

        private void AdvanceTime(float deltaTime)
        {
            float hoursPerSecond = 24f / cycleDurationSeconds;
            timeOfDay = NormalizeHour(timeOfDay + deltaTime * hoursPerSecond);
        }

        private void ApplyGlobalLight()
        {
            if (globalLight == null)
            {
                FindGlobalLightIfNeeded();
            }

            if (globalLight == null)
            {
                return;
            }

            EvaluateGlobalLight(timeOfDay, out Color color, out float intensity);
            ApplyWeatherModifier(ref color, ref intensity);
            globalLight.color = color;
            globalLight.intensity = intensity;
        }

        private void ApplyReactiveTargets(bool localLightsActive)
        {
            for (int i = _buildings.Count - 1; i >= 0; i--)
            {
                DayNightBuilding building = _buildings[i];
                if (building == null)
                {
                    _buildings.RemoveAt(i);
                    continue;
                }

                building.ApplyDayNightState(timeOfDay, localLightsActive);
            }

            for (int i = _lightGroups.Count - 1; i >= 0; i--)
            {
                DayNightLightGroup lightGroup = _lightGroups[i];
                if (lightGroup == null)
                {
                    _lightGroups.RemoveAt(i);
                    continue;
                }

                lightGroup.ApplyDayNightState(timeOfDay, localLightsActive);
            }
        }

        private void AddManualTargets()
        {
            if (manualBuildings != null)
            {
                for (int i = 0; i < manualBuildings.Length; i++)
                {
                    AddUnique(_buildings, manualBuildings[i]);
                }
            }

            if (manualLightGroups != null)
            {
                for (int i = 0; i < manualLightGroups.Length; i++)
                {
                    AddUnique(_lightGroups, manualLightGroups[i]);
                }
            }
        }

        private void CollectSceneTargets()
        {
            DayNightBuilding[] foundBuildings = UnityEngine.Object.FindObjectsByType<DayNightBuilding>(
                includeInactiveTargets ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < foundBuildings.Length; i++)
            {
                AddUnique(_buildings, foundBuildings[i]);
            }

            DayNightLightGroup[] foundLightGroups = UnityEngine.Object.FindObjectsByType<DayNightLightGroup>(
                includeInactiveTargets ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < foundLightGroups.Length; i++)
            {
                AddUnique(_lightGroups, foundLightGroups[i]);
            }
        }

        private void FindGlobalLightIfNeeded()
        {
            if (!autoFindGlobalLight || globalLight != null)
            {
                return;
            }

            Light2D[] lights = UnityEngine.Object.FindObjectsByType<Light2D>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < lights.Length; i++)
            {
                Light2D light = lights[i];
                if (light != null && light.lightType == Light2D.LightType.Global)
                {
                    globalLight = light;
                    return;
                }
            }
        }

        private void FindWeatherControllerIfNeeded()
        {
            if (!autoFindWeatherController || weatherController != null)
            {
                return;
            }

            WeatherController[] controllers = UnityEngine.Object.FindObjectsByType<WeatherController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (controllers.Length > 0)
            {
                weatherController = controllers[0];
            }
        }

        private void ApplyWeatherModifier(ref Color color, ref float intensity)
        {
            if (weatherController == null)
            {
                FindWeatherControllerIfNeeded();
            }

            if (weatherController == null)
            {
                return;
            }

            Color multiplier = weatherController.LightColorMultiplier;
            color = new Color(
                color.r * multiplier.r,
                color.g * multiplier.g,
                color.b * multiplier.b,
                color.a * multiplier.a);
            intensity *= weatherController.LightIntensityMultiplier;
        }

        private void EvaluateGlobalLight(float hour, out Color color, out float intensity)
        {
            // Named time segments let designers tune each lighting phase without changing the evaluation code.
            hour = NormalizeHour(hour);

            if (hour >= dawnStartTime && hour < dayStartTime)
            {
                float t = Smooth01(Mathf.InverseLerp(dawnStartTime, dayStartTime, hour));
                EvaluateFiveStep(
                    t,
                    nightColor,
                    preDawnColor,
                    dawnColor,
                    morningColor,
                    dayColor,
                    nightIntensity,
                    preDawnIntensity,
                    dawnIntensity,
                    morningIntensity,
                    dayIntensity,
                    out color,
                    out intensity);
                return;
            }

            if (hour >= dayStartTime && hour < duskStartTime)
            {
                color = dayColor;
                intensity = dayIntensity;
                return;
            }

            if (hour >= duskStartTime && hour < nightStartTime)
            {
                float t = Smooth01(Mathf.InverseLerp(duskStartTime, nightStartTime, hour));
                EvaluateFiveStep(
                    t,
                    dayColor,
                    goldenHourColor,
                    duskColor,
                    twilightColor,
                    nightColor,
                    dayIntensity,
                    goldenHourIntensity,
                    duskIntensity,
                    twilightIntensity,
                    nightIntensity,
                    out color,
                    out intensity);
                return;
            }

            color = nightColor;
            intensity = nightIntensity;
        }

        private bool IsLocalLightingActive(float hour)
        {
            hour = NormalizeHour(hour);

            if (Mathf.Approximately(localLightsOnTime, localLightsOffTime))
            {
                return true;
            }

            if (localLightsOnTime < localLightsOffTime)
            {
                return hour >= localLightsOnTime && hour < localLightsOffTime;
            }

            return hour >= localLightsOnTime || hour < localLightsOffTime;
        }

        private static void EvaluateFiveStep(
            float t,
            Color color0,
            Color color1,
            Color color2,
            Color color3,
            Color color4,
            float intensity0,
            float intensity1,
            float intensity2,
            float intensity3,
            float intensity4,
            out Color color,
            out float intensity)
        {
            t = Mathf.Clamp01(t);

            if (t < 0.25f)
            {
                EvaluateLightSegment(t / 0.25f, color0, color1, intensity0, intensity1, out color, out intensity);
                return;
            }

            if (t < 0.5f)
            {
                EvaluateLightSegment((t - 0.25f) / 0.25f, color1, color2, intensity1, intensity2, out color, out intensity);
                return;
            }

            if (t < 0.75f)
            {
                EvaluateLightSegment((t - 0.5f) / 0.25f, color2, color3, intensity2, intensity3, out color, out intensity);
                return;
            }

            EvaluateLightSegment((t - 0.75f) / 0.25f, color3, color4, intensity3, intensity4, out color, out intensity);
        }

        private static void EvaluateLightSegment(
            float t,
            Color startColor,
            Color endColor,
            float startIntensity,
            float endIntensity,
            out Color color,
            out float intensity)
        {
            t = Smooth01(t);
            color = Color.LerpUnclamped(startColor, endColor, t);
            intensity = Mathf.LerpUnclamped(startIntensity, endIntensity, t);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
        }

        private GUIStyle GetEditorTimeOverlayStyle()
        {
            if (_editorTimeOverlayStyle == null)
            {
                _editorTimeOverlayStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 11,
                    fontStyle = FontStyle.Normal,
                    clipping = TextClipping.Clip
                };
            }

            _editorTimeOverlayStyle.normal.textColor = new Color(1f, 1f, 1f, 0.72f);
            return _editorTimeOverlayStyle;
        }

        private static string FormatEditorTime(float hour)
        {
            int totalMinutes = Mathf.RoundToInt(NormalizeHour(hour) * 60f) % 1440;
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours:00}:{minutes:00}";
        }
#endif

        private static float NormalizeHour(float hour)
        {
            hour %= 24f;
            if (hour < 0f)
            {
                hour += 24f;
            }

            return hour;
        }

        private static void AddUnique<T>(List<T> list, T value) where T : Object
        {
            if (value == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return;
                }
            }

            list.Add(value);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DayNightController))]
    public class DayNightControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DayNightController controller = (DayNightController)target;

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Apply Current Time"))
            {
                controller.ApplyCurrentState(true);
                EditorUtility.SetDirty(controller);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Day Preset"))
            {
                controller.ApplyDayPreset();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Apply Night Preset"))
            {
                controller.ApplyNightPreset();
                EditorUtility.SetDirty(controller);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Recollect Reactive Targets"))
            {
                controller.CollectReactiveTargets();
                controller.ApplyCurrentState(true);
                EditorUtility.SetDirty(controller);
            }
        }
    }
#endif
}
