using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent, RequireComponent(typeof(Button))]
public sealed class BRATGUIButtonHoverLift : BaseMeshEffect, IMaterialModifier,
    IPointerEnterHandler, IPointerExitHandler
{
    [Min(0f)] public float lift = 4f;
    [Min(1f)] public float hoverScale = 1.035f;
    [Range(0f, 0.5f)] public float brightnessBoost = 0.12f;
    [Min(0.01f)] public float duration = 0.09f;

    private static readonly int TintId = Shader.PropertyToID("_Color");
    private Button button;
    private float progress;
    private Coroutine routine;
    private Material sourceMaterial, hoverMaterial;
    private Color sourceTint;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!Application.isPlaying) return;
        button = GetComponent<Button>();
        button.onClick.AddListener(ResetVisual);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.IsInteractable()) FadeTo(1f);
    }

    public void OnPointerExit(PointerEventData eventData) => FadeTo(0f);

    private void FadeTo(float target)
    {
        if (routine != null) StopCoroutine(routine);
        routine = progress == target ? null : StartCoroutine(Fade(target));
    }

    private IEnumerator Fade(float target)
    {
        while (progress != target)
        {
            yield return null;
            SetProgress(Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, duration)));
        }
        routine = null;
    }

    private void SetProgress(float value)
    {
        bool changeMaterial = (progress == 0f) != (value == 0f);
        progress = value;
        if (!graphic) return;
        graphic.SetVerticesDirty();
        if (hoverMaterial) UpdateTint();
        if (changeMaterial) graphic.SetMaterialDirty();
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive() || progress <= 0f) return;
        // Move the drawn icon and its shadow together, keeping the button's hit rect fixed.
        Vector3 center = graphic.rectTransform.rect.center;
        float scale = Mathf.Lerp(1f, hoverScale, progress);
        Vector3 offset = Vector3.up * (lift * progress);
        UIVertex vertex = default;
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            vertex.position = center + (vertex.position - center) * scale + offset;
            vertices.SetUIVertex(vertex, i);
        }
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!IsActive() || progress <= 0f || !baseMaterial || !baseMaterial.HasProperty(TintId)) return baseMaterial;
        if (!hoverMaterial || sourceMaterial != baseMaterial)
        {
            ReleaseMaterial();
            sourceMaterial = baseMaterial;
            sourceTint = baseMaterial.GetColor(TintId);
            hoverMaterial = new Material(baseMaterial) { hideFlags = HideFlags.HideAndDontSave };
        }
        UpdateTint();
        return hoverMaterial;
    }

    private void UpdateTint()
    {
        Color tint = sourceTint * (1f + brightnessBoost * progress);
        tint.a = sourceTint.a;
        hoverMaterial.SetColor(TintId, tint);
    }

    private void ResetVisual()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (progress != 0f) SetProgress(0f);
    }

    protected override void OnDisable()
    {
        if (button) button.onClick.RemoveListener(ResetVisual);
        ResetVisual();
        if (graphic) graphic.SetMaterialDirty();
        ReleaseMaterial();
        base.OnDisable();
    }

    private void ReleaseMaterial()
    {
        if (hoverMaterial) Destroy(hoverMaterial);
        sourceMaterial = null;
        hoverMaterial = null;
    }
}
