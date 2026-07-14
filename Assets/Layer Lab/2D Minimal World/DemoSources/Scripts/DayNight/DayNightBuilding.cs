using FunkyCode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LayerLab
{
    // Toggles a building's window objects and child lights based on the day/night state.
    public class DayNightBuilding : MonoBehaviour
    {
        [Header("Auto Collect")]
        [SerializeField] private bool autoCollectOnEnable = true;
        [SerializeField] private bool includeInactiveChildren = true;
        [SerializeField] private string darkWindowNameContains = "Window_In_Dark";
        [SerializeField] private string lightWindowNameContains = "Window_In_Light";

        [Header("Manual Add")]
        [SerializeField] private GameObject[] manualDarkWindows;
        [SerializeField] private GameObject[] manualLightWindows;
        [SerializeField] private Light2D[] manualLights;

        [Header("Apply")]
        [SerializeField] private bool controlWindowObjects = true;
        [SerializeField] private bool controlChildLights = true;

        private GameObject[] _autoDarkWindows = System.Array.Empty<GameObject>();
        private GameObject[] _autoLightWindows = System.Array.Empty<GameObject>();
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

        // Re-scans child windows and lights to refresh the cached references.
        [ContextMenu("Recollect Child Windows/Lights")]
        public void RefreshAutoReferences()
        {
            CollectWindows();
            CollectLights();
        }

        public void ApplyDayNightState(float timeOfDay, bool localLightingActive)
        {
            if (controlWindowObjects)
            {
                SetObjectsActive(_autoDarkWindows, !localLightingActive);
                SetObjectsActive(manualDarkWindows, !localLightingActive);
                SetObjectsActive(_autoLightWindows, localLightingActive);
                SetObjectsActive(manualLightWindows, localLightingActive);
            }

            if (controlChildLights)
            {
                SetLightsEnabled(_autoLights, localLightingActive);
                SetLightsEnabled(manualLights, localLightingActive);
            }
        }

        private void CollectWindows()
        {
            Transform[] children = GetComponentsInChildren<Transform>(includeInactiveChildren);
            GameObject[] darkBuffer = new GameObject[children.Length];
            GameObject[] lightBuffer = new GameObject[children.Length];
            int darkCount = 0;
            int lightCount = 0;

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == transform)
                {
                    continue;
                }

                string childName = child.name;
                if (!string.IsNullOrEmpty(darkWindowNameContains) && childName.Contains(darkWindowNameContains))
                {
                    darkBuffer[darkCount] = child.gameObject;
                    darkCount++;
                }

                if (!string.IsNullOrEmpty(lightWindowNameContains) && childName.Contains(lightWindowNameContains))
                {
                    lightBuffer[lightCount] = child.gameObject;
                    lightCount++;
                }
            }

            _autoDarkWindows = CopyObjects(darkBuffer, darkCount);
            _autoLightWindows = CopyObjects(lightBuffer, lightCount);
        }

        private void CollectLights()
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

        private static GameObject[] CopyObjects(GameObject[] source, int count)
        {
            if (count <= 0)
            {
                return System.Array.Empty<GameObject>();
            }

            GameObject[] result = new GameObject[count];
            System.Array.Copy(source, result, count);
            return result;
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

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject target = objects[i];
                if (target != null && target.activeSelf != active)
                {
                    target.SetActive(active);
                }
            }
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
