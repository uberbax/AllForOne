using LayerLab;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LayerLab
{
    /// <summary>
    /// Draws the in-game demo UI (weather and time-of-day controls) and exposes UI hit testing.
    /// </summary>
    public class DemoController : MonoBehaviour
    {
        private const float EdgePadding = 2f;
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;
        private const float UiScaleMultiplier = 1.265f;
        private const float MatchWidthOrHeight = 0.5f;
        private const float BarPaddingX = 10f;
        private const float RowHeight = 22f;
        private const float RowVerticalPadding = 5f;
        private const string GuideText = "WASD | Mouse | Shift | Wheel | H UI";
        private readonly WeatherType[] _weatherOptions =
        {
            WeatherType.Clear,
            WeatherType.Rain,
            WeatherType.Snow,
            WeatherType.Fog
        };

        private readonly string[] _weatherLabels =
        {
            "Clear",
            "Rain",
            "Snow",
            "Fog"
        };

        private Rect _activeHitRect;
        private bool _hasActiveHitRect;

        [Header("Target Controllers")]
        [SerializeField] private WeatherController weatherController;
        [SerializeField] private DayNightController dayNightController;
        [SerializeField] private bool autoFindControllers = true;

        [Header("Demo UI")]
        [SerializeField] private bool showDemoUi = true;
        [SerializeField] private Vector2 panelOffset = new Vector2(2f, 2f);
        [SerializeField] private bool instantWeatherChange = false;
        [SerializeField, Min(0f)] private float weatherButtonFadeSeconds = 0.35f;

        private GUIStyle _barStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _mutedStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _selectedButtonStyle;
        private GUIStyle _miniButtonStyle;
        private Texture2D _barTexture;
        private Texture2D _buttonTexture;
        private Texture2D _selectedButtonTexture;
        private Texture2D _sliderTrackTexture;
        private Texture2D _sliderThumbTexture;
        private float _cachedStyleScale = -1f;

        // Returns true when the given screen position lies over the demo UI bar.
        public bool IsScreenPositionOverDemoUi(Vector2 screenPosition)
        {
            if (!_hasActiveHitRect)
            {
                return false;
            }

            Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return _activeHitRect.Contains(guiPosition);
        }

        private void Awake()
        {
            ResolveControllers();
        }

        // Toggles the demo UI when the H key is pressed.
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.hKey.wasPressedThisFrame)
            {
                return;
            }

            showDemoUi = !showDemoUi;
            if (!showDemoUi)
            {
                ClearHitRects();
            }
        }

        private void OnValidate()
        {
            panelOffset.x = Mathf.Max(0f, panelOffset.x);
            panelOffset.y = Mathf.Max(0f, panelOffset.y);
            weatherButtonFadeSeconds = Mathf.Max(0f, weatherButtonFadeSeconds);
        }

        private void OnDisable()
        {
            ClearHitRects();
        }

        private void OnDestroy()
        {
            ClearHitRects();
            DestroyTexture(_barTexture);
            DestroyTexture(_buttonTexture);
            DestroyTexture(_selectedButtonTexture);
            DestroyTexture(_sliderTrackTexture);
            DestroyTexture(_sliderThumbTexture);
        }

        // Lays out and renders the demo UI bar every IMGUI frame.
        private void OnGUI()
        {
            if (!showDemoUi)
            {
                ClearHitRects();
                return;
            }

            EnsureStyles();

            Rect barRect = CalculateBarRect();
            SetHitRect(barRect);

            GUI.Box(barRect, GUIContent.none, _barStyle);
            DrawCompactBar(barRect);
        }

        private Rect CalculateBarRect()
        {
            float uiScale = GetUiScale();
            float edgeX = Mathf.Clamp(panelOffset.x * 0.25f * uiScale, EdgePadding, 6f * uiScale);
            float edgeY = Mathf.Clamp(panelOffset.y * 0.25f * uiScale, EdgePadding, 6f * uiScale);
            bool narrow = Screen.width < 720f * uiScale;
            float barHeight = (narrow ? 56f : 32f) * uiScale;
            float contentWidth = CalculateContentWidth(uiScale, narrow);
            float width = Mathf.Min(contentWidth, Screen.width - edgeX * 2f);
            float x = Mathf.Max(edgeX, (Screen.width - width) * 0.5f);
            float y = Screen.height - edgeY - barHeight;
            return new Rect(x, y, width, barHeight);
        }

        // Draws the weather, time and guide groups inside the bar.
        private void DrawCompactBar(Rect barRect)
        {
            float uiScale = GetUiScale();
            bool narrow = Screen.width < 720f * uiScale;
            float paddingX = BarPaddingX * uiScale;
            float rowHeight = RowHeight * uiScale;
            float verticalPadding = RowVerticalPadding * uiScale;
            float rowY = barRect.y + (narrow ? verticalPadding : (barRect.height - rowHeight) * 0.5f);
            float x = barRect.x + paddingX;
            float maxX = barRect.xMax - paddingX;

            x = DrawWeatherGroup(x, rowY, rowHeight, uiScale);
            x = DrawDivider(x, rowY, rowHeight, uiScale);
            x = DrawTimeGroup(x, rowY, rowHeight, uiScale);

            if (narrow)
            {
                DrawGuideText(barRect.x + paddingX, barRect.y + 33f * uiScale, maxX - barRect.x - paddingX, 18f * uiScale);
                return;
            }

            x = DrawDivider(x, rowY, rowHeight, uiScale);
            DrawGuideText(x, rowY, Mathf.Max(0f, maxX - x), rowHeight);
        }

        // Measures the same layout units used by DrawCompactBar so the background fits the controls.
        private float CalculateContentWidth(float uiScale, bool narrow)
        {
            float horizontalPadding = BarPaddingX * uiScale * 2f;
            float controlsWidth = GetWeatherGroupWidth(uiScale) + GetDividerWidth(uiScale) + GetTimeGroupWidth(uiScale);
            float guideWidth = GetGuideWidth(GuideText, uiScale);

            if (narrow)
            {
                return horizontalPadding + Mathf.Max(controlsWidth, guideWidth);
            }

            return horizontalPadding + controlsWidth + GetDividerWidth(uiScale) + guideWidth;
        }

        private float GetWeatherGroupWidth(float uiScale)
        {
            float width = 48f * uiScale;
            for (int i = 0; i < _weatherLabels.Length; i++)
            {
                width += (_weatherLabels[i].Length * 7f + 18f) * uiScale + 3f * uiScale;
            }
            return width;
        }

        private static float GetTimeGroupWidth(float uiScale)
        {
            return (35f + 43f + 111f + 58f + 3f + 34f + 3f + 58f + 3f + 44f + 3f) * uiScale;
        }

        private static float GetDividerWidth(float uiScale)
        {
            return 18f * uiScale;
        }

        private float GetGuideWidth(string guideText, float uiScale)
        {
            float measuredWidth = _mutedStyle != null
                ? _mutedStyle.CalcSize(new GUIContent(guideText)).x
                : guideText.Length * 7f * uiScale;
            return measuredWidth + 8f * uiScale;
        }

        private float DrawWeatherGroup(float x, float y, float height, float uiScale)
        {
            DrawLabel("Weather", x, y, 44f * uiScale, height, _labelStyle);
            x += 48f * uiScale;

            if (weatherController == null)
            {
                DrawLabel("None", x, y, 34f * uiScale, height, _mutedStyle);
                return x + 38f * uiScale;
            }

            WeatherType currentWeather = weatherController.CurrentWeatherType;
            for (int i = 0; i < _weatherOptions.Length; i++)
            {
                WeatherType weatherType = _weatherOptions[i];
                GUIStyle style = currentWeather == weatherType ? _selectedButtonStyle : _buttonStyle;
                float width = (_weatherLabels[i].Length * 7f + 18f) * uiScale;
                float buttonHeight = 20f * uiScale;
                float buttonY = y + (height - buttonHeight) * 0.5f;
                if (GUI.Button(new Rect(x, buttonY, width, buttonHeight), _weatherLabels[i], style))
                {
                    weatherController.SetWeather(weatherType, instantWeatherChange, weatherButtonFadeSeconds);
                }

                x += width + 3f * uiScale;
            }

            return x;
        }

        // Draws the time-of-day label, slider and preset buttons.
        private float DrawTimeGroup(float x, float y, float height, float uiScale)
        {
            DrawLabel("Time", x, y, 31f * uiScale, height, _labelStyle);
            x += 35f * uiScale;

            if (dayNightController == null)
            {
                DrawLabel("None", x, y, 34f * uiScale, height, _mutedStyle);
                return x + 38f * uiScale;
            }

            float hour = dayNightController.TimeOfDay;
            DrawLabel(FormatTime(hour), x, y, 39f * uiScale, height, _labelStyle);
            x += 43f * uiScale;

            float newHour = DrawTimeSlider(new Rect(x, y, 104f * uiScale, height), hour, uiScale);
            x += 111f * uiScale;

            if (!Mathf.Approximately(hour, newHour))
            {
                dayNightController.AutoAdvance = false;
                dayNightController.SetTime(newHour >= 23.99f ? 0f : newHour);
            }

            x = DrawTimePresetButton("Morning", x, y, 58f * uiScale, 7f, uiScale);
            x = DrawTimePresetButton("Day", x, y, 34f * uiScale, float.NaN, uiScale);
            x = DrawTimePresetButton("Evening", x, y, 58f * uiScale, 18f, uiScale);
            x = DrawTimePresetButton("Night", x, y, 44f * uiScale, float.PositiveInfinity, uiScale);
            return x;
        }

        private float DrawTimePresetButton(string text, float x, float y, float width, float hour, float uiScale)
        {
            float rowHeight = RowHeight * uiScale;
            float buttonHeight = 20f * uiScale;
            float buttonY = y + (rowHeight - buttonHeight) * 0.5f;
            if (GUI.Button(new Rect(x, buttonY, width, buttonHeight), text, _miniButtonStyle))
            {
                if (float.IsNaN(hour))
                {
                    dayNightController.ApplyDayPreset();
                }
                else if (float.IsPositiveInfinity(hour))
                {
                    dayNightController.ApplyNightPreset();
                }
                else
                {
                    SetTimePreset(hour);
                }
            }

            return x + width + 3f * uiScale;
        }

        // Draws a draggable slider that maps the track position to an hour value.
        private float DrawTimeSlider(Rect rect, float hour, float uiScale)
        {
            float trackHeight = Mathf.Max(1f, 2f * uiScale);
            Rect trackRect = new Rect(rect.x, rect.center.y - trackHeight * 0.5f, rect.width, trackHeight);
            float normalized = Mathf.InverseLerp(0f, 24f, hour);
            float thumbWidth = 6f * uiScale;
            float thumbHeight = 12f * uiScale;
            float thumbX = Mathf.Lerp(trackRect.x, trackRect.xMax - thumbWidth, normalized);
            Rect thumbRect = new Rect(thumbX, rect.center.y - thumbHeight * 0.5f, thumbWidth, thumbHeight);

            GUI.DrawTexture(trackRect, _sliderTrackTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(thumbRect, _sliderThumbTexture, ScaleMode.StretchToFill);

            Event currentEvent = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive, rect);
            switch (currentEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (rect.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        GUIUtility.hotControl = controlId;
                        currentEvent.Use();
                        return SliderPositionToHour(currentEvent.mousePosition.x, trackRect);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        currentEvent.Use();
                        return SliderPositionToHour(currentEvent.mousePosition.x, trackRect);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    break;
            }

            return hour;
        }

        private static float SliderPositionToHour(float mouseX, Rect trackRect)
        {
            float normalized = Mathf.InverseLerp(trackRect.x, trackRect.xMax, mouseX);
            return Mathf.Lerp(0f, 24f, Mathf.Clamp01(normalized));
        }

        private float DrawDivider(float x, float y, float height, float uiScale)
        {
            DrawLabel("|", x + 4f * uiScale, y, 8f * uiScale, height, _mutedStyle);
            return x + 18f * uiScale;
        }

        private void DrawGuideText(float x, float y, float width, float height)
        {
            if (width <= 20f)
            {
                return;
            }

            DrawLabel(GuideText, x, y, width, height, _mutedStyle);
        }

        private static void DrawLabel(string text, float x, float y, float width, float height, GUIStyle style)
        {
            GUI.Label(new Rect(x, y, width, height), text, style);
        }

        private void SetTimePreset(float hour)
        {
            if (dayNightController == null)
            {
                return;
            }

            dayNightController.AutoAdvance = false;
            dayNightController.SetTime(hour);
        }

        // Auto-finds the weather and day/night controllers when none are assigned.
        private void ResolveControllers()
        {
            if (!autoFindControllers)
            {
                return;
            }

            if (weatherController == null)
            {
                weatherController = Object.FindFirstObjectByType<WeatherController>();
            }

            if (dayNightController == null)
            {
                dayNightController = Object.FindFirstObjectByType<DayNightController>();
            }
        }

        private void SetHitRect(Rect barRect)
        {
            _activeHitRect = barRect;
            _hasActiveHitRect = true;
        }

        private void ClearHitRects()
        {
            _hasActiveHitRect = false;
        }

        private static float GetUiScale()
        {
            float widthScale = Mathf.Max(1f, Screen.width) / ReferenceWidth;
            float heightScale = Mathf.Max(1f, Screen.height) / ReferenceHeight;
            float logWidth = Mathf.Log(widthScale, 2f);
            float logHeight = Mathf.Log(heightScale, 2f);
            float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, MatchWidthOrHeight);
            return Mathf.Pow(2f, logWeightedAverage) * UiScaleMultiplier;
        }

        private static int ScaledFontSize(float baseSize, float uiScale)
        {
            return Mathf.Max(8, Mathf.RoundToInt(baseSize * uiScale));
        }

        // Rebuilds the cached GUI styles and textures when the UI scale changes.
        private void EnsureStyles()
        {
            float uiScale = GetUiScale();
            if (_barStyle != null && Mathf.Approximately(_cachedStyleScale, uiScale))
            {
                return;
            }

            _cachedStyleScale = uiScale;

            if (_barTexture == null)
            {
                _barTexture = CreateTexture(new Color(0f, 0f, 0f, 0.46f));
                _buttonTexture = CreateTexture(new Color(0f, 0f, 0f, 0.62f));
                _selectedButtonTexture = CreateTexture(new Color(1f, 1f, 1f, 0.24f));
                _sliderTrackTexture = CreateTexture(new Color(1f, 1f, 1f, 0.22f));
                _sliderThumbTexture = CreateTexture(new Color(1f, 1f, 1f, 0.88f));
            }

            _barStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(
                    Mathf.RoundToInt(6f * uiScale),
                    Mathf.RoundToInt(6f * uiScale),
                    Mathf.RoundToInt(4f * uiScale),
                    Mathf.RoundToInt(4f * uiScale)),
                normal = { background = _barTexture }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = ScaledFontSize(9f, uiScale),
                fontStyle = FontStyle.Normal,
                margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = new Color(1f, 1f, 1f, 0.92f) }
            };

            _mutedStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Normal,
                wordWrap = false,
                normal = { textColor = new Color(1f, 1f, 1f, 0.72f) }
            };

            _buttonStyle = CreateButtonStyle(_buttonTexture, new Color(1f, 1f, 1f, 0.86f), uiScale);
            _selectedButtonStyle = CreateButtonStyle(_selectedButtonTexture, new Color(1f, 1f, 1f, 1f), uiScale);
            _miniButtonStyle = CreateButtonStyle(_buttonTexture, new Color(1f, 1f, 1f, 0.82f), uiScale);
        }

        private GUIStyle CreateButtonStyle(Texture2D background, Color textColor, float uiScale)
        {
            int horizontalPadding = Mathf.RoundToInt(2f * uiScale);
            int topPadding = Mathf.RoundToInt(1f * uiScale);
            int bottomPadding = Mathf.RoundToInt(2f * uiScale);
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = ScaledFontSize(9f, uiScale),
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(horizontalPadding, horizontalPadding, topPadding, bottomPadding),
                margin = new RectOffset(0, 0, 0, 0),
                normal =
                {
                    background = background,
                    textColor = textColor
                },
                hover =
                {
                    background = background,
                    textColor = Color.white
                },
                active =
                {
                    background = background,
                    textColor = Color.white
                }
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        private static string FormatTime(float hour)
        {
            int totalMinutes = Mathf.RoundToInt(hour * 60f) % 1440;
            int displayHour = totalMinutes / 60;
            int displayMinute = totalMinutes % 60;
            return $"{displayHour:00}:{displayMinute:00}";
        }
    }
}
