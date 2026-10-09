namespace Animpic.Local.POLYFantasyCharacter.Camp
{
using UnityEngine;

[CreateAssetMenu(fileName = "FantasyCampGlobalWindSettings",
    menuName = "Animpic Studio/Shaders/Fantasy Character/Global Wind Settings")]
public sealed class FantasyCampGlobalWindSettings : ScriptableObject
{
    public const string ResourceName = "POLYFantasyCharacter/FantasyCampGlobalWindSettings";

    static readonly int AvailableId = Shader.PropertyToID(
        "_POLYFantasyCharacterFantasyCampGlobalWindAvailable");
    static readonly int EnabledId = Shader.PropertyToID(
        "_POLYFantasyCharacterFantasyCampGlobalWindEnabled");
    static readonly int DirectionId = Shader.PropertyToID(
        "_POLYFantasyCharacterFantasyCampGlobalWindDirection");
    static readonly int MainId = Shader.PropertyToID(
        "_POLYFantasyCharacterFantasyCampGlobalWindMain");
    static readonly int WaveScaleId = Shader.PropertyToID(
        "_POLYFantasyCharacterFantasyCampGlobalWindWaveScale");
    static readonly int BendId = Shader.PropertyToID(
        "_POLYFantasyCharacterFantasyCampGlobalWindBend");
    static readonly int FlutterId = Shader.PropertyToID(
        "_POLYFantasyCharacterFantasyCampGlobalWindFlutter");

    [SerializeField] bool windEnabled = true;
    [SerializeField] Vector2 direction = Vector2.right;
    [SerializeField, Range(0f, 20f)] float strength = 12.52f;
    [SerializeField, Range(0f, 10f)] float speed = 1f;
    [SerializeField] Vector2 worldWaveScale = new Vector2(0.35f, 0.35f);
    [SerializeField, Range(0f, 3f)] float turbulence = 1f;

    [SerializeField] float rootHeight;
    [SerializeField, Range(0.01f, 2f)] float flexibility = 0.65f;

    [SerializeField, Range(0f, 0.5f)] float flutterStrength = 0.05f;
    [SerializeField, Range(0.01f, 20f)] float flutterScale = 5f;
    [SerializeField, Range(0.01f, 5f)] float flutterSpeed = 1f;

    public static FantasyCampGlobalWindSettings LoadDefault()
    {
        return Resources.Load<FantasyCampGlobalWindSettings>(ResourceName);
    }

    public void Apply()
    {
        Vector2 safeDirection = direction.sqrMagnitude > 0.000001f
            ? direction.normalized
            : Vector2.right;
        Vector2 safeWaveScale = new Vector2(
            Mathf.Max(Mathf.Abs(worldWaveScale.x), 0.0001f),
            Mathf.Max(Mathf.Abs(worldWaveScale.y), 0.0001f));

        Shader.SetGlobalFloat(AvailableId, 1f);
        Shader.SetGlobalFloat(EnabledId, windEnabled ? 1f : 0f);
        Shader.SetGlobalVector(DirectionId,
            new Vector4(safeDirection.x, safeDirection.y, 0f, 0f));
        Shader.SetGlobalVector(MainId,
            new Vector4(strength, speed, turbulence, 0f));
        Shader.SetGlobalVector(WaveScaleId,
            new Vector4(safeWaveScale.x, safeWaveScale.y, 0f, 0f));
        Shader.SetGlobalVector(BendId,
            new Vector4(rootHeight, flexibility, 0f, 0f));
        Shader.SetGlobalVector(FlutterId,
            new Vector4(flutterStrength, flutterScale, flutterSpeed, 0f));
    }

    public static void ClearGlobals()
    {
        Shader.SetGlobalFloat(AvailableId, 0f);
        Shader.SetGlobalFloat(EnabledId, 0f);
    }

    void OnValidate()
    {
        strength = Mathf.Max(0f, strength);
        speed = Mathf.Max(0f, speed);
        turbulence = Mathf.Max(0f, turbulence);
        flexibility = Mathf.Max(0.01f, flexibility);
        flutterStrength = Mathf.Max(0f, flutterStrength);
        flutterScale = Mathf.Max(0.01f, flutterScale);
        flutterSpeed = Mathf.Max(0.01f, flutterSpeed);
        Apply();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyAtRuntimeStart()
    {
        FantasyCampGlobalWindSettings settings = LoadDefault();
        if (settings != null)
            settings.Apply();
        else
            ClearGlobals();
    }
}

}
