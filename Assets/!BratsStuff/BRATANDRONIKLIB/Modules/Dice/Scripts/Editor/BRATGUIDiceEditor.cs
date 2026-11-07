using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BRATGUIDice))]
public sealed class BRATGUIDiceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Button.onClick / внешние вызовы:\nOpen() — открыть, кубики справа снизу.\nRoll() — бросить два D6 и показать сумму как благословение.\nClose() — прервать бросок и скрыть окно.\nЭто визуальное меню; результат доступен через LastSum.\n3D-камера и RenderTexture создаются при открытии и освобождаются при закрытии.", MessageType.Info);
        DrawDefaultInspector();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            var window = (BRATGUIDice)target;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open()")) window.Open();
            if (GUILayout.Button("Roll()")) window.Roll();
            if (GUILayout.Button("Close()")) window.Close();
            EditorGUILayout.EndHorizontal();
        }
    }
}
