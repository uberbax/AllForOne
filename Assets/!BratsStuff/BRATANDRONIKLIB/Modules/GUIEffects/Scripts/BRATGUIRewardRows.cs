using UnityEngine;
using UnityEngine.UI;

namespace BRATANDRONIKLIB
{
    // Centre each row independently including a partially filled last row.
    [AddComponentMenu("BRAT/UI/Centred Reward Rows")]
    public sealed class BRATGUIRewardRows : LayoutGroup
    {
        [SerializeField] private Vector2 cellSize = new Vector2(72f, 120f);
        [SerializeField] private Vector2 spacing = new Vector2(14f, 8f);
        private const int Columns = 3;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            int columns = Mathf.Min(Columns, rectChildren.Count);
            float width = Span(columns, cellSize.x, spacing.x) + padding.horizontal;
            SetLayoutInputForAxis(width, width, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int rows = (rectChildren.Count + Columns - 1) / Columns;
            float height = Span(rows, cellSize.y, spacing.y) + padding.vertical;
            SetLayoutInputForAxis(height, height, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            for (int first = 0; first < rectChildren.Count; first += Columns)
            {
                int count = Mathf.Min(Columns, rectChildren.Count - first);
                float x = GetStartOffset(0, Span(count, cellSize.x, spacing.x));
                for (int col = 0; col < count; col++)
                    SetChildAlongAxis(rectChildren[first + col], 0, x + col * (cellSize.x + spacing.x), cellSize.x);
            }
        }

        public override void SetLayoutVertical()
        {
            int rows = (rectChildren.Count + Columns - 1) / Columns;
            float top = GetStartOffset(1, Span(rows, cellSize.y, spacing.y));
            for (int i = 0; i < rectChildren.Count; i++)
                SetChildAlongAxis(rectChildren[i], 1, top + i / Columns * (cellSize.y + spacing.y), cellSize.y);
        }

        private static float Span(int count, float size, float gap)
        {
            return count * size + Mathf.Max(0, count - 1) * gap;
        }
    }
}
