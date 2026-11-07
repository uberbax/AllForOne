using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class BRATGUIDice : MonoBehaviour
{
    [Header("Внешние вызовы: Open(), Close(), Roll()")]
    [Tooltip("Прозрачный вывод отдельной 3D-камеры поверх доски.")]
    [SerializeField] private RawImage diceView;
    [SerializeField] private Mesh bodyMesh;
    [SerializeField] private Mesh pipsMesh;
    [SerializeField] private Material boneMaterial;
    [SerializeField] private Material pipsMaterial;
    [SerializeField] private Material shadowMaterial;
    [SerializeField] private Mesh shadowMesh;
    [SerializeField] private Button rollButton;
    [Tooltip("Вся область Roll вместе с кубиками. Скрывается при начале броска.")]
    [SerializeField] private CanvasGroup rollGroup;
    [Tooltip("Постоянная кнопка Close. Видна и доступна во время броска.")]
    [SerializeField] private CanvasGroup backGroup;
    [SerializeField] private CanvasGroup resultGroup;
    [SerializeField] private TMP_Text resultValue;
    [SerializeField] private TMP_Text resultFaces;
    [Tooltip("Минимальное время броска перед показом результата. Ждём физической остановки обоих кубиков; работает при Time.timeScale = 0.")]
    [SerializeField, Min(0.5f)] private float rollDuration = 2.1f;

    private static readonly Vector3[] FaceNormals =
    { Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down };
    private readonly Transform[] _dice = new Transform[2];
    private readonly Transform[] _shadows = new Transform[2];
    private readonly MeshRenderer[] _shadowRenderers = new MeshRenderer[2];
    private MaterialPropertyBlock _shadowProperties;
    private readonly Rigidbody[] _bodies = new Rigidbody[2];
    private Scene _simulationScene;
    private PhysicsScene _physicsScene;
    private PhysicsMaterial _contactMaterial;
    private GameObject _stage;
    private Camera _camera;
    private RenderTexture _texture;
    private Coroutine _roll;
    private bool _rolling;
    private int _lastSum;
    public bool IsRolling => _rolling;
    public int LastSum => _lastSum;

    private void OnEnable()
    {
        if (!EnsureStage()) return;
        ResetDice();
        RenderDice();
    }

    private void OnDisable()
    {
        if (_roll != null) StopCoroutine(_roll);
        _roll = null;
        _rolling = false;
        if (rollButton != null) rollButton.interactable = true;
        ReleaseStage();
    }

    private void OnDestroy() => ReleaseStage();

    private void LateUpdate()
    {
        if (_camera != null && EnsureTexture()) RenderDice();
    }

    /// <summary>Открыть окно и подготовить два кубика в нижнем правом углу.</summary>
    public void Open()
    {
        if (gameObject.activeSelf) Close();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    /// <summary>Прервать бросок, освободить 3D-рендер и скрыть окно.</summary>
    public void Close() => gameObject.SetActive(false);

    /// <summary>Бросить два D6. Повторный вызов во время анимации игнорируется.</summary>
    public void Roll()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || _rolling || _lastSum != 0 || !EnsureStage()) return;
        _rolling = true;
        if (rollButton != null) rollButton.interactable = false;
        SetGroup(rollGroup, 0, false);
        if (resultGroup != null) resultGroup.alpha = 0;
        _roll = StartCoroutine(AnimateRoll());
    }

    public static Quaternion RotationForFace(int value, float yaw)
    {
        return Quaternion.AngleAxis(yaw, Vector3.up) *
               Quaternion.FromToRotation(FaceNormals[Mathf.Clamp(value, 1, 6) - 1], Vector3.up);
    }

    public static int ReadTopFace(Quaternion rotation)
    {
        int value = 1;
        float best = -2;
        for (int i = 0; i < FaceNormals.Length; i++)
        {
            float dot = Vector3.Dot(rotation * FaceNormals[i], Vector3.up);
            if (dot > best) { best = dot; value = i + 1; }
        }
        return value;
    }

    private IEnumerator AnimateRoll()
    {
        for (int i = 0; i < 2; i++)
        {
            _bodies[i].WakeUp();
            _bodies[i].linearVelocity = new Vector3(Random.Range(-6f, -4.5f),
                Random.Range(2.1f, 2.8f), Random.Range(1.7f, 3.1f));
            _bodies[i].angularVelocity = Random.onUnitSphere * Random.Range(10f, 16f);
        }
        const float step = 1f / 120f;
        float accumulator = 0, elapsed = 0, stillTime = 0, tiltedTime = 0;
        while (stillTime < .3f || elapsed < Mathf.Max(.5f, rollDuration))
        {
            accumulator += Mathf.Min(Time.unscaledDeltaTime, .1f);
            while (accumulator >= step)
            {
                for (int i = 0; i < 2; i++)
                    if (!_bodies[i].IsSleeping()) _bodies[i].AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
                _physicsScene.Simulate(step);
                accumulator -= step;
                elapsed += step;
                bool still = true, flat = true;
                for (int i = 0; i < 2; i++)
                {
                    still &= _bodies[i].IsSleeping() ||
                        (_bodies[i].linearVelocity.sqrMagnitude < .0025f && _bodies[i].angularVelocity.sqrMagnitude < .01f);
                    int face = ReadTopFace(_dice[i].rotation);
                    flat &= Vector3.Dot(_dice[i].rotation * FaceNormals[face - 1], Vector3.up) > .98f;
                }
                stillTime = still && flat ? stillTime + step : 0;
                tiltedTime = still && !flat ? tiltedTime + step : 0;
                // Rare edge/stack balance: a small physical nudge, never force a chosen face.
                if (tiltedTime > .6f)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        _bodies[i].AddForce(new Vector3(i == 0 ? -.5f : .5f, .55f, .2f), ForceMode.Impulse);
                        _bodies[i].AddTorque(Random.onUnitSphere * .25f, ForceMode.Impulse);
                    }
                    tiltedTime = 0;
                }
            }
            for (int i = 0; i < 2; i++) UpdateShadow(i, Mathf.Max(0, _dice[i].localPosition.y - .41f));
            RenderDice();
            yield return null;
        }
        int a = ReadTopFace(_dice[0].localRotation), b = ReadTopFace(_dice[1].localRotation);
        _lastSum = a + b;
        if (resultValue != null) resultValue.text = "Received <size=125%><color=#8E2433>" + _lastSum + "</color></size> blessing points";
        if (resultFaces != null) resultFaces.text = "";
        if (resultGroup != null) resultGroup.alpha = .2f;
        RenderDice();
        float fade = 0;
        const float fadeDuration = 1.8f;
        while (fade < fadeDuration)
        {
            fade += Time.unscaledDeltaTime;
            float alpha = Mathf.SmoothStep(.2f, 1, fade / fadeDuration);
            if (resultGroup != null) resultGroup.alpha = alpha;
            yield return null;
        }
        if (resultGroup != null) resultGroup.alpha = 1;
        _rolling = false;
        _roll = null;
    }

    private bool EnsureStage()
    {
        if (_stage != null) return true;
        if (diceView == null || bodyMesh == null || pipsMesh == null || boneMaterial == null ||
            pipsMaterial == null || shadowMesh == null || shadowMaterial == null) return false;
        if (_shadowProperties == null) _shadowProperties = new MaterialPropertyBlock();
        _stage = new GameObject("Dice preview stage") { hideFlags = HideFlags.HideAndDontSave };
        _stage.transform.position = new Vector3(10000, 10000, 10000);
        var cameraObject = new GameObject("Dice camera") { hideFlags = HideFlags.HideAndDontSave };
        cameraObject.transform.SetParent(_stage.transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 10, -5.7735f);
        cameraObject.transform.localRotation = Quaternion.Euler(60, 0, 0);
        _camera = cameraObject.AddComponent<Camera>();
        _camera.enabled = false;
        _camera.orthographic = true;
        _camera.orthographicSize = 3.375f;
        _camera.nearClipPlane = .1f;
        _camera.farClipPlane = 25;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.clear;
        _camera.allowHDR = false;
        _camera.allowMSAA = true;
        _camera.useOcclusionCulling = false;
        for (int i = 0; i < 2; i++)
        {
            var die = new GameObject("Die " + (i + 1)) { hideFlags = HideFlags.HideAndDontSave };
            die.transform.SetParent(_stage.transform, false);
            die.transform.localScale = Vector3.one * .82f;
            _dice[i] = die.transform;
            MeshObject("Bone", die.transform, bodyMesh, boneMaterial);
            MeshObject("Engraved dots", die.transform, pipsMesh, pipsMaterial);
            _shadows[i] = MeshObject("Contact shadow " + (i + 1), _stage.transform, shadowMesh, shadowMaterial);
            _shadowRenderers[i] = _shadows[i].GetComponent<MeshRenderer>();
        }
        if (Application.isPlaying) CreatePhysicsStage();
        EnsureTexture();
        return true;
    }

    private void CreatePhysicsStage()
    {
        _simulationScene = SceneManager.CreateScene("Dice physics " + System.Guid.NewGuid().ToString("N"),
            new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        SceneManager.MoveGameObjectToScene(_stage, _simulationScene);
        _physicsScene = _simulationScene.GetPhysicsScene();
        _contactMaterial = new PhysicsMaterial("Dice tray contact")
        {
            hideFlags = HideFlags.HideAndDontSave,
            dynamicFriction = .42f, staticFriction = .5f, bounciness = .28f,
            frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Average
        };
        AddTrayCollider("Tray floor", new Vector3(0, -.15f, 0), new Vector3(10.8f, .3f, 5.8f));
        AddTrayCollider("Left rim", new Vector3(-5.35f, .25f, 0), new Vector3(.3f, .8f, 5.8f));
        AddTrayCollider("Right rim", new Vector3(5.35f, .25f, 0), new Vector3(.3f, .8f, 5.8f));
        AddTrayCollider("Near rim", new Vector3(0, .25f, -2.85f), new Vector3(10.8f, .8f, .3f));
        AddTrayCollider("Far rim", new Vector3(0, .25f, 2.85f), new Vector3(10.8f, .8f, .3f));
        for (int i = 0; i < 2; i++)
        {
            var collider = _dice[i].gameObject.AddComponent<BoxCollider>();
            collider.sharedMaterial = _contactMaterial;
            collider.contactOffset = .005f;
            var body = _dice[i].gameObject.AddComponent<Rigidbody>();
            body.mass = 1;
            body.useGravity = false; // Local gravity is applied only during our fixed simulation steps.
            body.linearDamping = .18f;
            body.angularDamping = .3f;
            body.maxAngularVelocity = 30;
            body.solverIterations = 12;
            body.solverVelocityIterations = 8;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _bodies[i] = body;
        }
    }

    private void AddTrayCollider(string name, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        go.transform.SetParent(_stage.transform, false);
        go.transform.localPosition = position;
        var collider = go.AddComponent<BoxCollider>();
        collider.size = size;
        collider.sharedMaterial = _contactMaterial;
        collider.contactOffset = .005f;
    }

    private static Transform MeshObject(string name, Transform parent, Mesh mesh, Material material)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return go.transform;
    }

    private bool EnsureTexture()
    {
        if (diceView == null || _camera == null) return false;
        var canvas = diceView.canvas;
        float scale = canvas != null ? canvas.scaleFactor : 1;
        float ratio = diceView.rectTransform.rect.width / Mathf.Max(1, diceView.rectTransform.rect.height);
        ratio = Mathf.Clamp(ratio, 1.2f, 2.2f);
        int width = Mathf.Clamp(Mathf.RoundToInt(diceView.rectTransform.rect.width * scale / 8) * 8, 768, 2048);
        int height = Mathf.Max(256, Mathf.RoundToInt(width / ratio));
        if (_texture != null && _texture.width == width && _texture.height == height) return false;
        ReleaseTexture();
        _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        { name = "Dice UI render", antiAliasing = 2, hideFlags = HideFlags.HideAndDontSave };
        _texture.Create();
        _camera.targetTexture = _texture;
        _camera.aspect = ratio;
        diceView.texture = _texture;
        return true;
    }

    private void ResetDice()
    {
        _lastSum = 0;
        SetGroup(rollGroup, 1, true);
        SetGroup(backGroup, 1, true);
        if (resultGroup != null) resultGroup.alpha = 0;
        if (resultValue != null) resultValue.text = "";
        if (resultFaces != null) resultFaces.text = "";
        if (rollButton != null) rollButton.interactable = true;
        for (int i = 0; i < 2; i++)
        {
            _dice[i].localPosition = new Vector3(3.05f + i * 1.28f, .41f, -2.25f + i * .13f);
            _dice[i].localRotation = RotationForFace(i == 0 ? 3 : 5, i == 0 ? -18 : 21);
            if (_bodies[i] != null)
            {
                _bodies[i].position = _dice[i].position;
                _bodies[i].rotation = _dice[i].rotation;
                _bodies[i].linearVelocity = Vector3.zero;
                _bodies[i].angularVelocity = Vector3.zero;
                _bodies[i].Sleep();
            }
            UpdateShadow(i, 0);
        }
    }

    private static void SetGroup(CanvasGroup group, float alpha, bool interactive)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactive;
        group.blocksRaycasts = interactive;
    }

    private void UpdateShadow(int i, float height)
    {
        var p = _dice[i].localPosition;
        _shadows[i].localPosition = new Vector3(p.x + .12f + height * .12f, .005f, p.z - .12f - height * .1f);
        _shadows[i].localScale = Vector3.one * (1.1f + height * .4f);
        _shadowProperties.SetFloat("_Opacity", .3f / (1 + height));
        _shadowRenderers[i].SetPropertyBlock(_shadowProperties);
    }

    private void RenderDice() { if (_camera != null && _texture != null) _camera.Render(); }

    private void ReleaseTexture()
    {
        if (_camera != null) _camera.targetTexture = null;
        if (diceView != null) diceView.texture = null;
        if (_texture == null) return;
        _texture.Release();
        Dispose(_texture);
        _texture = null;
    }

    private void ReleaseStage()
    {
        ReleaseTexture();
        if (_stage != null) Dispose(_stage);
        _stage = null;
        _camera = null;
        if (_simulationScene.IsValid() && _simulationScene.isLoaded && Application.isPlaying)
            SceneManager.UnloadSceneAsync(_simulationScene);
        _simulationScene = default;
        _physicsScene = default;
        if (_contactMaterial != null) Dispose(_contactMaterial);
        _contactMaterial = null;
        for (int i = 0; i < 2; i++) _bodies[i] = null;
    }

    private static void Dispose(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
