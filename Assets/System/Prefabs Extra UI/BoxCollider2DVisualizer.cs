using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class BoxCollider2DVisualizer : MonoBehaviour
{
    [SerializeField] private Color color = new Color(0f, 1f, 0f, 0.25f);
    [SerializeField] private bool drawWhenNotSelected = true;
    [SerializeField] private bool drawSolid = true;

    private BoxCollider2D boxCollider;

    private void OnEnable()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void OnDrawGizmos()
    {
        if (drawWhenNotSelected)
            DrawCollider();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawWhenNotSelected)
            DrawCollider();
    }

    private void DrawCollider()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider == null || !boxCollider.enabled)
            return;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Color oldColor = Gizmos.color;

        // Uses the object's position, rotation and scale.
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 center = boxCollider.offset;
        Vector3 size = boxCollider.size;

        if (drawSolid)
        {
            Color fillColor = color;
            fillColor.a = Mathf.Clamp01(color.a);

            Gizmos.color = fillColor;
            Gizmos.DrawCube(center, size);
        }

        Color outlineColor = color;
        outlineColor.a = 1f;

        Gizmos.color = outlineColor;
        Gizmos.DrawWireCube(center, size);

        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }
}