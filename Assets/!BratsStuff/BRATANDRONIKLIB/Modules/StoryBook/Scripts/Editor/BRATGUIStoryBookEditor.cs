using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BRATGUIStoryBook))]
public sealed class BRATGUIStoryBookEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Внешние вызовы / Button.onClick:\nOpen() — открыть с первой страницы.\nClose() — закрыть с анимацией и скрыть окно.\nNext() — следующая страница. Back() — предыдущая.\nПроверочные кнопки ниже доступны в Play Mode.", MessageType.Info);
        DrawDefaultInspector();
        var book = (BRATGUIStoryBook)target;
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Open()", "Вызывай извне для открытия окна на первом развороте, даже когда STORY неактивен."))) book.Open();
            if (GUILayout.Button(new GUIContent("Close()", "Вызывай извне для складывания книги и скрытия STORY."))) book.Close();
            if (GUILayout.Button(new GUIContent("Back()", "Перейти на предыдущий разворот; на первом ничего не делает."))) book.Back();
            if (GUILayout.Button(new GUIContent("Next()", "Перейти на следующий разворот; на последнем ничего не делает."))) book.Next();
            EditorGUILayout.EndHorizontal();
        }
    }
}
