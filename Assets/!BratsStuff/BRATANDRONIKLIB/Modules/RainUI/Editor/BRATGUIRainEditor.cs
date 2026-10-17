using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BRATANDRONIKLIB.Editor
{
    [CustomEditor(typeof(BRATGUIRain))]
    public sealed class BRATGUIRainEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Contours use the background image's UV coordinates. Select this object in Scene view to move the contour points. Rain runs in Play Mode, including at timeScale = 0.", MessageType.Info);
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();
            var image = serializedObject.FindProperty("background").objectReferenceValue as Image;
            if (image == null || image.sprite == null) return;
            Rect r = image.GetPixelAdjustedRect();
            if (image.preserveAspect)
            {
                float aspect = image.sprite.rect.width / image.sprite.rect.height;
                if (aspect > r.width / r.height)
                { float h = r.width / aspect; r.y += (r.height - h) * image.rectTransform.pivot.y; r.height = h; }
                else { float w = r.height * aspect; r.x += (r.width - w) * image.rectTransform.pivot.x; r.width = w; }
            }
            if (r.width <= 0 || r.height <= 0) return;
            var surfaces = serializedObject.FindProperty("surfaces");
            Handles.color = new Color(.3f, .85f, 1f, .9f);
            for (int s = 0; s < surfaces.arraySize; s++)
            {
                var surface = surfaces.GetArrayElementAtIndex(s);
                var points = surface.FindPropertyRelative("points");
                Vector3 previous = Vector3.zero;
                for (int i = 0; i < points.arraySize; i++)
                {
                    var point = points.GetArrayElementAtIndex(i);
                    Vector2 uv = point.vector2Value;
                    Vector3 world = image.rectTransform.TransformPoint(new Vector3(r.x + uv.x * r.width, r.y + uv.y * r.height));
                    if (i > 0) Handles.DrawLine(previous, world);
                    else Handles.Label(world, surface.FindPropertyRelative("name").stringValue);
                    EditorGUI.BeginChangeCheck();
                    Vector3 moved = Handles.FreeMoveHandle(world, HandleUtility.GetHandleSize(world) * .025f, Vector3.zero, Handles.DotHandleCap);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 local = image.rectTransform.InverseTransformPoint(moved);
                        point.vector2Value = new Vector2(Mathf.Clamp01((local.x - r.x) / r.width), Mathf.Clamp01((local.y - r.y) / r.height));
                    }
                    previous = world;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
