using UnityEngine;

namespace LayerLab
{
/// <summary>
/// Provides the runtime demo UI for switching maps and controlling the sample character.
/// </summary>
[DisallowMultipleComponent]
public class DemoControl : MonoBehaviour
{
    private const float ReferenceUiWidth = 1280f;
    private const float ReferenceUiHeight = 720f;
    private const int MaxVisibleMapButtons = 19;
    private const KeyCode ToggleUiKey = KeyCode.H;
    private const string ToggleUiHint = "H: Hide UI";
    private const string MapSwitchHint = "Left/Right Arrow or A/D: Change Map";

    [SerializeField] private GameObject[] mapPrefabs;
    [SerializeField] private Transform character;
    [SerializeField] private SampleCharacterMover characterMover;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CameraFollowTargetX cameraFollow;
    [SerializeField] private GameObject sceneInitialMap;
    [SerializeField] private bool loadFirstMapOnStart = true;
    [HideInInspector, SerializeField] private Vector3 mapPosition;
    [HideInInspector, SerializeField] private Vector3 characterStartPosition = new Vector3(0f, -2f, 0f);
    [HideInInspector, SerializeField] private Vector3 cameraStartPosition = new Vector3(0f, 0f, -10f);
    [HideInInspector, SerializeField] private Rect buttonPanel = new Rect(0f, 0f, 0f, 0f);
    [HideInInspector, SerializeField] private Vector2 buttonSize = new Vector2(48f, 28f);
    [HideInInspector, SerializeField] private float buttonGap = 6f;
    [HideInInspector, SerializeField] private float bottomBarHeight = 74f;
    [HideInInspector, SerializeField] private Color panelColor = new Color(0.03f, 0.04f, 0.04f, 0.58f);
    [HideInInspector, SerializeField] private Color buttonColor = new Color(0.15f, 0.18f, 0.19f, 0.78f);
    [HideInInspector, SerializeField] private Color selectedButtonColor = new Color(0.05f, 0.07f, 0.08f, 0.92f);
    [HideInInspector, SerializeField] private Color sliderTrackColor = new Color(1f, 1f, 1f, 0.22f);
    [HideInInspector, SerializeField] private Color sliderFillColor = new Color(0.48f, 0.78f, 1f, 0.92f);
    [HideInInspector, SerializeField] private Color sliderKnobColor = new Color(0.94f, 0.98f, 1f, 1f);
    public bool showUi = true;

    private GameObject currentMap;
    private int currentMapIndex = -1;
    private bool capturedCharacterStartPosition;
    private bool capturedCameraStartPosition;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle hintStyle;
    private GUIStyle hintBoxStyle;
    private GUIStyle valueStyle;
    private float currentUiScale = 1f;
    private Texture2D sliderTrackTexture;
    private Texture2D sliderFillTexture;
    private Texture2D sliderKnobTexture;

    private void Start()
    {
        ResolveReferences();
        CaptureCharacterStartPosition();
        CaptureCameraStartPosition();
        CaptureSceneInitialMap();

        if (currentMap == null && loadFirstMapOnStart && mapPrefabs != null && mapPrefabs.Length > 0)
        {
            LoadMap(0);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleUiKey))
        {
            showUi = !showUi;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            LoadAdjacentMap(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            LoadAdjacentMap(1);
        }
    }

    private void OnGUI()
    {
        if (!showUi)
        {
            return;
        }

        if (mapPrefabs == null || mapPrefabs.Length == 0)
        {
            return;
        }

        EnsureGuiStyles();

        // Scale all IMGUI coordinates from a 1280x720 reference layout.
        Matrix4x4 previousMatrix = GUI.matrix;
        currentUiScale = GetUiScale();
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(currentUiScale, currentUiScale, 1f));

        try
        {
            Rect bottomBarRect = GetBottomBarRect();
            DrawBottomControlStack(bottomBarRect);
            DrawBottomBar(bottomBarRect);
        }
        finally
        {
            GUI.matrix = previousMatrix;
            currentUiScale = 1f;
        }
    }

    /// <summary>
    /// Replaces the active map while preserving the character speed, direction, and camera depth.
    /// </summary>
    public void LoadMap(int index)
    {
        if (mapPrefabs == null || index < 0 || index >= mapPrefabs.Length || mapPrefabs[index] == null)
        {
            return;
        }

        ResolveReferences();
        CaptureCharacterStartPosition();
        CaptureCameraStartPosition();
        CaptureSceneInitialMap();

        if (currentMap != null)
        {
            Destroy(currentMap);
        }

        ResetCharacterPosition();
        ResetCameraPosition();

        GameObject prefab = mapPrefabs[index];
        currentMap = Instantiate(prefab, mapPosition, prefab.transform.rotation);
        currentMap.name = prefab.name;
        currentMapIndex = index;

        MapRuntimeStreamer streamer = currentMap.GetComponent<MapRuntimeStreamer>();
        if (streamer == null)
        {
            streamer = currentMap.AddComponent<MapRuntimeStreamer>();
        }

        streamer.TargetCamera = targetCamera;
        streamer.InitializeOnStart = false;
        streamer.Initialize();
    }

    private void LoadAdjacentMap(int direction)
    {
        int buttonCount = GetVisibleMapButtonCount();
        if (buttonCount <= 0)
        {
            return;
        }

        int index = currentMapIndex >= 0 ? currentMapIndex : 0;
        index = (index + direction + buttonCount) % buttonCount;
        LoadMap(index);
    }

    private void CaptureCharacterStartPosition()
    {
        if (capturedCharacterStartPosition)
        {
            return;
        }

        // The scene placement is treated as the reset position for map switching.
        if (character != null)
        {
            characterStartPosition = character.position;
            capturedCharacterStartPosition = true;
        }
    }

    private void CaptureCameraStartPosition()
    {
        if (capturedCameraStartPosition || targetCamera == null)
        {
            return;
        }

        // Only X/Y are reset later; the original camera Z value is never overwritten by the demo UI.
        cameraStartPosition = targetCamera.transform.position;
        capturedCameraStartPosition = true;
    }

    private Rect GetBottomBarRect()
    {
        float height = GetBottomBarHeight();
        return new Rect(0f, Mathf.Max(0f, GetScaledScreenHeight() - height), GetScaledScreenWidth(), height);
    }

    private Rect GetMapButtonPanelRect(Rect bottomBarRect)
    {
        float scaledWidth = GetScaledScreenWidth();
        float margin = 12f;
        float width = Mathf.Max(1f, scaledWidth - margin * 2f);
        float x = margin + buttonPanel.x;

        return new Rect(
            Mathf.Clamp(x, margin, Mathf.Max(margin, scaledWidth - width - margin)),
            bottomBarRect.y,
            width,
            bottomBarRect.height);
    }

    private float GetBottomBarHeight()
    {
        int buttonCount = GetVisibleMapButtonCount();
        if (buttonCount <= 0)
        {
            return bottomBarHeight;
        }

        int columns = GetMapButtonColumnCount(GetScaledScreenWidth() - 24f, buttonCount);
        int rows = Mathf.CeilToInt(buttonCount / (float)columns);
        return 12f + rows * buttonSize.y + Mathf.Max(0, rows - 1) * buttonGap + 12f;
    }

    private int GetMapButtonColumnCount(float availableWidth, int buttonCount)
    {
        int columns = Mathf.FloorToInt((availableWidth - buttonGap) / (buttonSize.x + buttonGap));
        return Mathf.Clamp(columns, 1, Mathf.Max(1, buttonCount));
    }

    private static float GetUiScale()
    {
        // Never scale below 1 so the 1280x720 layout remains the minimum readable size.
        float widthScale = Screen.width / ReferenceUiWidth;
        float heightScale = Screen.height / ReferenceUiHeight;
        return Mathf.Max(1f, Mathf.Min(widthScale, heightScale));
    }

    private float GetScaledScreenWidth()
    {
        return Screen.width / currentUiScale;
    }

    private float GetScaledScreenHeight()
    {
        return Screen.height / currentUiScale;
    }

    private int GetVisibleMapButtonCount()
    {
        if (mapPrefabs == null || mapPrefabs.Length == 0)
        {
            return 0;
        }

        return Mathf.Min(mapPrefabs.Length, MaxVisibleMapButtons);
    }

    private void CaptureSceneInitialMap()
    {
        if (currentMap != null)
        {
            return;
        }

        if (sceneInitialMap != null)
        {
            currentMap = sceneInitialMap;
            ConfigureExistingMap(currentMap);
            return;
        }

        currentMap = FindSceneMapByPrefabName();
        if (currentMap != null)
        {
            ConfigureExistingMap(currentMap);
        }
    }

    private void ConfigureExistingMap(GameObject map)
    {
        // The first scene map defines where future prefabs should be spawned.
        mapPosition = map.transform.position;

        MapRuntimeStreamer streamer = map.GetComponent<MapRuntimeStreamer>();
        if (streamer == null)
        {
            streamer = map.AddComponent<MapRuntimeStreamer>();
        }

        streamer.TargetCamera = targetCamera;
        streamer.InitializeOnStart = false;
        streamer.Initialize();
        currentMapIndex = FindMapPrefabIndex(map.name);
    }

    private GameObject FindSceneMapByPrefabName()
    {
        if (mapPrefabs == null)
        {
            return null;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == transform || target == character || target.GetComponentInParent<DemoControl>() != null)
            {
                continue;
            }

            if (target.parent == null && FindMapPrefabIndex(target.name) >= 0)
            {
                return target.gameObject;
            }
        }

        return null;
    }

    private int FindMapPrefabIndex(string mapName)
    {
        if (mapPrefabs == null)
        {
            return -1;
        }

        string normalizedName = mapName.Replace("(Clone)", string.Empty).Trim();
        for (int i = 0; i < mapPrefabs.Length; i++)
        {
            if (mapPrefabs[i] == null)
            {
                continue;
            }

            string prefabName = mapPrefabs[i].name;
            if (prefabName == normalizedName || normalizedName.StartsWith(prefabName + " ("))
            {
                return i;
            }
        }

        return -1;
    }

    private void ResetCharacterPosition()
    {
        if (characterMover != null)
        {
            characterMover.ResetPosition(characterStartPosition);
            return;
        }

        if (character != null)
        {
            character.position = characterStartPosition;
        }
    }

    private void ResetCameraPosition()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 position = targetCamera.transform.position;
        position.x = cameraStartPosition.x;
        position.y = cameraStartPosition.y;
        targetCamera.transform.position = position;

        if (cameraFollow != null)
        {
            cameraFollow.Target = character;
        }
    }

    private void ResolveReferences()
    {
        if (characterMover == null)
        {
            characterMover = FindObjectOfType<SampleCharacterMover>();
        }

        if (character == null && characterMover != null)
        {
            character = characterMover.transform;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (cameraFollow == null && targetCamera != null)
        {
            cameraFollow = targetCamera.GetComponent<CameraFollowTargetX>();
        }
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = CreateTexture(panelColor);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.88f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 12;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = CreateTexture(buttonColor);
        buttonStyle.hover.background = CreateTexture(new Color(0.22f, 0.26f, 0.28f, 0.86f));
        buttonStyle.active.background = CreateTexture(selectedButtonColor);
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 12;

        selectedButtonStyle = new GUIStyle(buttonStyle);
        selectedButtonStyle.normal.background = CreateTexture(selectedButtonColor);

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.88f);
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.fontSize = 11;
        hintStyle.alignment = TextAnchor.MiddleLeft;
        hintStyle.padding = new RectOffset(10, 10, 0, 0);

        hintBoxStyle = new GUIStyle(GUI.skin.textField);
        hintBoxStyle.normal.background = panelStyle.normal.background;
        hintBoxStyle.hover.background = hintBoxStyle.normal.background;
        hintBoxStyle.active.background = hintBoxStyle.normal.background;
        hintBoxStyle.focused.background = hintBoxStyle.normal.background;

        valueStyle = new GUIStyle(titleStyle);
        valueStyle.alignment = TextAnchor.MiddleRight;

        sliderTrackTexture = CreateTexture(sliderTrackColor);
        sliderFillTexture = CreateTexture(sliderFillColor);
        sliderKnobTexture = CreateTexture(sliderKnobColor);
    }

    private void DrawBottomBar(Rect bottomBarRect)
    {
        GUI.Box(bottomBarRect, GUIContent.none, panelStyle);
        DrawMapButtons(bottomBarRect);
    }

    private void DrawBottomControlStack(Rect bottomBarRect)
    {
        Rect mapButtonPanelRect = GetMapButtonPanelRect(bottomBarRect);
        Rect controlsRect = DrawCharacterControlsAbove(bottomBarRect, mapButtonPanelRect.x);
        DrawToggleUiHintAbove(controlsRect);
    }

    private void DrawToggleUiHintAbove(Rect anchorRect)
    {
        // Keep shortcut help with the controls so the bottom bar remains map-button only.
        GUIContent toggleContent = new GUIContent(ToggleUiHint);
        GUIContent mapContent = new GUIContent(MapSwitchHint);
        Vector2 toggleSize = hintStyle.CalcSize(toggleContent);
        Vector2 mapSize = hintStyle.CalcSize(mapContent);
        float width = Mathf.Max(toggleSize.x, mapSize.x) + 22f;
        float height = toggleSize.y + mapSize.y + 10f;
        Rect rect = new Rect(anchorRect.x, Mathf.Max(8f, anchorRect.y - height - 8f), width, height);
        Rect toggleRect = new Rect(rect.x, rect.y + 3f, rect.width, toggleSize.y);
        Rect mapRect = new Rect(rect.x, toggleRect.yMax + 1f, rect.width, mapSize.y);

        GUI.Box(rect, GUIContent.none, hintBoxStyle);
        GUI.Label(toggleRect, toggleContent, hintStyle);
        GUI.Label(mapRect, mapContent, hintStyle);
    }

    private Rect DrawCharacterControlsAbove(Rect bottomBarRect, float x)
    {
        if (characterMover == null)
        {
            ResolveReferences();
        }

        if (characterMover == null)
        {
            return new Rect(x, bottomBarRect.y, 0f, 0f);
        }

        float panelWidth = Mathf.Clamp(GetScaledScreenWidth() - x - 14f, 260f, 420f);
        Rect panelRect = new Rect(x, bottomBarRect.y - 50f, panelWidth, 42f);
        Rect leftRect = new Rect(panelRect.x + 12f, panelRect.y + 7f, 52f, buttonSize.y);
        Rect rightRect = new Rect(leftRect.xMax, leftRect.y, 52f, buttonSize.y);
        Rect valueRect = new Rect(panelRect.xMax - 56f, panelRect.y + 12f, 44f, 18f);
        Rect sliderRect = new Rect(rightRect.xMax + 16f, panelRect.y + 13f, Mathf.Max(72f, valueRect.x - rightRect.xMax - 24f), 16f);

        GUI.Box(panelRect, GUIContent.none, panelStyle);
        GUI.Label(valueRect, characterMover.SpeedMagnitude.ToString("0.0"), valueStyle);

        if (GUI.Button(leftRect, "Left", characterMover.Speed < 0f ? selectedButtonStyle : buttonStyle))
        {
            characterMover.MoveLeft();
        }

        if (GUI.Button(rightRect, "Right", characterMover.Speed > 0f ? selectedButtonStyle : buttonStyle))
        {
            characterMover.MoveRight();
        }

        float speedMagnitude = DrawSpeedSlider(sliderRect, characterMover.SpeedMagnitude, 0f, characterMover.MaxSpeed);
        characterMover.SetSpeedMagnitude(speedMagnitude);
        return panelRect;
    }

    private float DrawSpeedSlider(Rect rect, float value, float min, float max)
    {
        // Custom slider drawing keeps the demo UI consistent across Unity editor skins.
        int controlId = GUIUtility.GetControlID(FocusType.Passive, rect);
        Event current = Event.current;
        Rect hitRect = new Rect(rect.x - 8f, rect.y - 8f, rect.width + 16f, rect.height + 16f);

        if (current.type == EventType.MouseDown && hitRect.Contains(current.mousePosition))
        {
            GUIUtility.hotControl = controlId;
            value = SliderValueFromMouse(rect, current.mousePosition.x, min, max);
            current.Use();
        }
        else if (current.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
        {
            value = SliderValueFromMouse(rect, current.mousePosition.x, min, max);
            current.Use();
        }
        else if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
        {
            GUIUtility.hotControl = 0;
            current.Use();
        }

        float normalized = Mathf.InverseLerp(min, max, value);
        Rect trackRect = new Rect(rect.x, rect.y + rect.height * 0.5f - 2f, rect.width, 4f);
        Rect fillRect = new Rect(trackRect.x, trackRect.y, trackRect.width * normalized, trackRect.height);
        Rect knobRect = new Rect(trackRect.x + trackRect.width * normalized - 7f, rect.y + rect.height * 0.5f - 7f, 14f, 14f);

        GUI.DrawTexture(trackRect, sliderTrackTexture);
        GUI.DrawTexture(fillRect, sliderFillTexture);
        GUI.DrawTexture(knobRect, sliderKnobTexture);
        return value;
    }

    private static float SliderValueFromMouse(Rect rect, float mouseX, float min, float max)
    {
        float normalized = Mathf.InverseLerp(rect.x, rect.xMax, mouseX);
        return Mathf.Lerp(min, max, Mathf.Clamp01(normalized));
    }

    private void DrawMapButtons(Rect bottomBarRect)
    {
        Rect panelRect = GetMapButtonPanelRect(bottomBarRect);

        int buttonCount = GetVisibleMapButtonCount();
        int columns = GetMapButtonColumnCount(panelRect.width, buttonCount);
        float availableWidth = panelRect.width - buttonGap * (columns + 1);
        float width = Mathf.Max(1f, availableWidth / columns);
        float startX = panelRect.x + buttonGap;
        float startY = bottomBarRect.y + 12f;

        for (int i = 0; i < buttonCount; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rect rect = new Rect(
                startX + column * (width + buttonGap),
                startY + row * (buttonSize.y + buttonGap),
                width,
                buttonSize.y);

            GUIStyle style = i == currentMapIndex ? selectedButtonStyle : buttonStyle;
            if (GUI.Button(rect, (i + 1).ToString(), style))
            {
                LoadMap(i);
            }
        }
    }

    private static Texture2D CreateTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
}
