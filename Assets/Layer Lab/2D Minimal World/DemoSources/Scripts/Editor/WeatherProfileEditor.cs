using UnityEditor;

namespace LayerLab
{
    // Custom inspector for WeatherProfile that only shows the property groups
    // relevant to the currently selected WeatherType.
    [CustomEditor(typeof(WeatherProfile))]
    public sealed class WeatherProfileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty weatherTypeProperty = DrawRequiredProperty("weatherType");
            DrawRequiredProperty("displayName");

            WeatherType weatherType = (WeatherType)weatherTypeProperty.enumValueIndex;

            DrawAutoTransitionSection();

            switch (weatherType)
            {
                case WeatherType.Rain:
                    DrawRainSection();
                    DrawWeatherVolumeSection();
                    DrawGlobalLightSection();
                    DrawWindSection();
                    break;
                case WeatherType.Snow:
                    DrawSnowSection();
                    DrawWeatherVolumeSection();
                    DrawGlobalLightSection();
                    DrawWindSection();
                    break;
                case WeatherType.Fog:
                    DrawFogSection();
                    DrawWeatherVolumeSection();
                    DrawGlobalLightSection();
                    break;
                default:
                    DrawGlobalLightSection();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAutoTransitionSection()
        {
            DrawHeader("Auto Transition");
            DrawRequiredProperty("selectionWeight");
            DrawRequiredProperty("minDurationSeconds");
            DrawRequiredProperty("maxDurationSeconds");
            DrawRequiredProperty("transitionDurationSeconds");
        }

        private void DrawRainSection()
        {
            DrawHeader("Rain");
            DrawRequiredProperty("particleIntensity");
            DrawRequiredProperty("rainEmissionRate");
            DrawRequiredProperty("rainSplashEmissionRate");
            DrawRequiredProperty("rainColor");
            DrawRequiredProperty("rainStreakMinSize");
            DrawRequiredProperty("rainStreakMaxSize");
            DrawRequiredProperty("rainLengthScale");
            DrawRequiredProperty("rainVelocityLengthScale");
            DrawRequiredProperty("rainSplashColor");

            DrawHeader("Rain Day/Night Adjustment");
            DrawRequiredProperty("adaptRainColorToDayNight");
            DrawRequiredProperty("rainDayAlphaMultiplier");
            DrawRequiredProperty("rainNightAlphaMultiplier");
            DrawRequiredProperty("rainFogAlphaMultiplier");
            DrawRequiredProperty("rainMinAlpha");
            DrawRequiredProperty("rainMaxAlpha");
            DrawRequiredProperty("rainNightBrightnessBoost");
            DrawRequiredProperty("rainSplashDayAlphaMultiplier");
            DrawRequiredProperty("rainSplashNightAlphaMultiplier");
        }

        private void DrawSnowSection()
        {
            DrawHeader("Snow");
            DrawRequiredProperty("particleIntensity");
            DrawRequiredProperty("snowEmissionRate");
            DrawRequiredProperty("snowParticleMinSize");
            DrawRequiredProperty("snowParticleMaxSize");
            DrawRequiredProperty("snowColor");

            DrawHeader("Snow Day/Night Adjustment");
            DrawRequiredProperty("snowDayAlphaMultiplier");
            DrawRequiredProperty("snowNightAlphaMultiplier");
            DrawRequiredProperty("snowNightTint");
            DrawRequiredProperty("snowNightTintBlend");
        }

        private void DrawFogSection()
        {
            DrawHeader("Fog");
            DrawRequiredProperty("fogOverlayAlpha");
            DrawRequiredProperty("fogOverlayColor");

            DrawHeader("Fog Day/Night Adjustment");
            DrawRequiredProperty("fogDayAlphaMultiplier");
            DrawRequiredProperty("fogNightAlphaMultiplier");
            DrawRequiredProperty("fogNightColor");
            DrawRequiredProperty("fogNightColorBlend");
        }

        private void DrawWeatherVolumeSection()
        {
            DrawHeader("Post-processing");
            DrawRequiredProperty("weatherVolumeWeight");
            DrawRequiredProperty("weatherVolumeDayMultiplier");
            DrawRequiredProperty("weatherVolumeNightMultiplier");
        }

        private void DrawGlobalLightSection()
        {
            DrawHeader("Global Light Adjustment");
            DrawRequiredProperty("lightColorMultiplier");
            DrawRequiredProperty("lightIntensityMultiplier");
        }

        private void DrawWindSection()
        {
            DrawHeader("Wind");
            DrawRequiredProperty("windDirection");
            DrawRequiredProperty("windStrength");
        }

        private SerializedProperty DrawRequiredProperty(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(property);
            return property;
        }

        private static void DrawHeader(string label)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }
    }
}
