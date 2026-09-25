using System;
using UnityEngine;

public class FixHeightLast : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public RectTransform what;
    public RectTransform holderLast;

    public static float GetHeightToCoverChild(
        RectTransform parent,
        RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);

        float minLocalY = float.MaxValue;

        foreach (Vector3 corner in corners)
        {
            Vector3 local = parent.InverseTransformPoint(corner);
            minLocalY = Mathf.Min(minLocalY, local.y);
        }

        // parent pivot.y == 1,
        // поэтому верх находится на localY = 0
        return Mathf.Max(0f, -minLocalY);
    }
    private void Update()
    {
        int l = 0;
        for (int i = 0; i < holderLast.childCount; i++)
            if (holderLast.GetChild(i).gameObject.activeSelf)
                l = i;
        
        float height = GetHeightToCoverChild(what, holderLast.GetChild(l).GetComponent<RectTransform>());

        what.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );
    }
}
