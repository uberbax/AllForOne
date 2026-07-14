using FunkyCode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LayerLab
{
    // Enables or disables a group of child Light2D components based on the day/night state.
    public class DayNightLightGroup : MonoBehaviour
    {
        [Header("Auto Collect")]
        [SerializeField] private bool autoCollectOnEnable = true;
        [SerializeField] private bool includeInactiveChildren = true;

        [Header("Manual Add")]
        [SerializeField] private Light2D[] manualLights;

        private Light2D[] _autoLights = System.Array.Empty<Light2D>();

        private void Reset()
        {
            RefreshAutoReferences();
        }

        private void Awake()
        {
            if (autoCollectOnEnable)
            {
                RefreshAutoReferences();
            }
        }

        private void OnEnable()
        {
            if (autoCollectOnEnable)
            {
                RefreshAutoReferences();
            }
        }

        private void OnValidate()
        {
            if (autoCollectOnEnable)
            {
                RefreshAutoReferences();
            }
        }

        // Re-scans child Light2D components to refresh the cached references.
        [ContextMenu("Recollect Child Lights")]
        public void RefreshAutoReferences()
        {
            Light2D[] lights = GetComponentsInChildren<Light2D>(includeInactiveChildren);
            Light2D[] buffer = new Light2D[lights.Length];
            int count = 0;

            for (int i = 0; i < lights.Length; i++)
            {
                Light2D light = lights[i];
                if (light == null)
                {
                    continue;
                }

                buffer[count] = light;
                count++;
            }

            _autoLights = CopyLights(buffer, count);
        }

        public void ApplyDayNightState(float timeOfDay, bool localLightingActive)
        {
            SetLightsEnabled(_autoLights, localLightingActive);
            SetLightsEnabled(manualLights, localLightingActive);
        }

        private static Light2D[] CopyLights(Light2D[] source, int count)
        {
            if (count <= 0)
            {
                return System.Array.Empty<Light2D>();
            }

            Light2D[] result = new Light2D[count];
            System.Array.Copy(source, result, count);
            return result;
        }

        private static void SetLightsEnabled(Light2D[] lights, bool enabled)
        {
            if (lights == null)
            {
                return;
            }

            for (int i = 0; i < lights.Length; i++)
            {
                Light2D light = lights[i];
                if (light != null && light.enabled != enabled)
                {
                    light.enabled = enabled;
                }
            }
        }
    }
}
