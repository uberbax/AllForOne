using UnityEditor;
using UnityEngine;

namespace Animpic.CharacterStudio.Editor
{
    [CustomEditor(typeof(CharacterCustomizer))]
    public sealed class CharacterCustomizerEditor : UnityEditor.Editor
    {
        private int paintedIndex;
        private string error;
        private void OnEnable() { Undo.undoRedoPerformed += OnUndo; }
        private void OnDisable() { Undo.undoRedoPerformed -= OnUndo; }
        private void OnUndo() { Repaint(); }
        public override void OnInspectorGUI()
        {
            using (new CharacterStudioEditorUI.SkinScope())
            {
                var character = (CharacterCustomizer)target;
                CharacterStudioEditorUI.Header("Character Customizer", character.Catalog ? character.Catalog.label + " · Appearance & individual finishes" : "Configure a character catalogue.");
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Catalogue", character.Catalog, typeof(CharacterCatalog), false);
                if (CharacterStudioEditorUI.Button("Open Character Studio", true)) CharacterStudioWindow.Open();
                EditorGUILayout.Space(8);
                if (EditorUtility.IsPersistent(character))
                {
                    EditorGUILayout.HelpBox("Open this prefab in Prefab Mode or place an instance in a scene to edit its appearance.", MessageType.Info);
                    return;
                }
                CharacterStudioEditorUI.Draw(character, ref paintedIndex, ref error);
            }
        }
    }
}
